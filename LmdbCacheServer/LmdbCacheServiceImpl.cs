using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.AddResponse.Types;
using static LmdbCache.DeleteResponse.Types;
using static LmdbCache.GetResponse.Types;
using static LmdbCache.GetResponse.Types.GetResponseEntry.Types;

namespace LmdbCacheServer
{
    public class LmdbCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase
    {
        private readonly Func<VectorClock> _clock;
        private readonly LightningPersistence _lmdb;
        private readonly KvTable _kvTable;
        private readonly ExpiryTable _kvExpiryTable;

        public LmdbCacheServiceImpl(LightningConfig config, Func<VectorClock> clock, Action<WriteTransaction, WriteLogEvent> kvUpdateHandler) 
        {
            _lmdb = new LightningPersistence(config);
            _kvExpiryTable = new ExpiryTable(_lmdb, "kvexpiry");
            _kvTable = new KvTable(_lmdb, "kv", _kvExpiryTable, 
                () => DateTimeOffset.UtcNow.ToTimestamp(), (transaction, table, key, expiry) => {}, kvUpdateHandler);
            _clock = clock;
        }

        public override async Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            var batch = request.Entries.Select(ks => (new KvKey(ks.Key), new KvMetadata(ks.Expiry, _clock()), new KvValue(new [] { ks.Value }))).ToArray();

            var ret = await (request.OverrideExisting ? _kvTable.AddOrUpdate(batch) : _kvTable.Add(batch));
            var response = new AddResponse();
            var addResults = ret.Select(kv => kv.Item2 
                ? (request.OverrideExisting ? AddResult.KeyUpdated : AddResult.KeyAdded) // TODO: Recheck statuses
                : (request.OverrideExisting ? AddResult.Failure : AddResult.KeyAlreadyExists));
            response.Results.AddRange(addResults);

            return response;
        }

        public override Task<ContainsKeysResponse> ContainsKeys(GetRequest request, ServerCallContext context)
        {
            var ret = _kvTable.ContainsKeys(request.Keys.Select(k => new KvKey(k)).ToArray());

            var response = new ContainsKeysResponse();
            response.Results.AddRange(ret.Select(kv => kv.Item2));

            return Task.FromResult(response);
        }

        public override async Task<CopyResponse> Copy(CopyRequest request, ServerCallContext context) => await _kvTable.Copy(request);

        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var keys = request.Keys.Select(k => new KvKey(k)).ToArray();
            var kvs = await _kvTable.Delete(keys);

            var response = new DeleteResponse();
            var addResults = kvs.Select(kv => kv.Item2 ? DeleteResult.Success : DeleteResult.NotFound);
            response.Results.AddRange(addResults);

            return response;
        }

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            var ret = _kvTable.Get(request.Keys.Select(k => new KvKey(k)));

            var response = new GetResponse();
            var getResponseEntries = ret.Select(kv =>
                {
                    if (!kv.Item2.HasValue) return new GetResponseEntry {Result = GetResult.NotFound};

                    var gre = new GetResponseEntry {Result = GetResult.Success}; // TODO: Add value streaming
                    gre.Value.AddRange(kv.Item2.Value.Value);
                    return gre;
                });
            response.Results.AddRange(getResponseEntries);

            return Task.FromResult(response);
        }

        public override Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context)
        {
            var ret = _kvTable.KeysByPrefix(new KvKey(request.KeyPrefix), 0, uint.MaxValue);
            return responseStream.WriteAllAsync(ret.Select(k => new KeyListResponse { Key = k.Key }));
        }

        public override Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context)
        {
            var ret = _kvTable.PageByPrefix(new KvKey(request.KeyPrefix), 0, uint.MaxValue);
            return responseStream.WriteAllAsync(ret.Select(k => new KeyValueListResponse { Key = k.Item1.ToString(), Value = k.Item2.Value[0] })); // TODO: Add value streaming
        }
    }
}
