using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using LmdbLight;

namespace LmdbCacheServer
{
    public class LmdbCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase
    {
        private readonly LightningPersistence _lmdb;
        private readonly Table _kvTable;

        public LmdbCacheServiceImpl(LightningConfig config) 
        {
            _lmdb = new LightningPersistence(config);
            _kvTable = _lmdb.OpenTable("kv");
        }

        public override async Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            var batch = request.Entries.Select(ks => (new TableKey(ks.Key), new TableValue(ks.Value))).ToArray();

            var ret = await (request.OverrideExisting ? _lmdb.AddOrUpdateBatch(_kvTable, batch, false) : _lmdb.AddBatch(_kvTable, batch));
            var response = new AddResponse();
            var addResults = ret.Select(kv => kv.Item2 
                ? (request.OverrideExisting ? AddResponse.Types.AddResult.KeyUpdated : AddResponse.Types.AddResult.Failure) // TODO: Recheck statuses
                : (request.OverrideExisting ? AddResponse.Types.AddResult.KeyAdded : AddResponse.Types.AddResult.KeyAlreadyExists));
            response.Results.AddRange(addResults);

            return response;
        }

        public override Task<ContainsKeysResponse> ContainsKeys(GetRequest request, ServerCallContext context)
        {
            var ret = _lmdb.ContainsKeys(_kvTable, request.Keys.Select(k => new TableKey(k)).ToArray());

            var response = new ContainsKeysResponse();
            response.Results.AddRange(ret.Select(kv => kv.Item2));

            return Task.FromResult(response);
        }

        public override async Task<CopyResponse> Copy(CopyRequest request, ServerCallContext context)
        {

            var ret = await _lmdb.WriteAsync(txn =>
                request.Entries.Select(fromTo =>
                {
                    var from = new TableKey(fromTo.KeyFrom);
                    var to = new TableKey(fromTo.KeyTo);

                    if (!txn.ContainsKey(_kvTable, from))
                    {
                        return CopyResponse.Types.CopyResult.FromKeyNotFound;
                    }
                    else if (txn.ContainsKey(_kvTable, to))
                    {
                        return CopyResponse.Types.CopyResult.ToKeyExists;
                    }
                    else
                    {
                        var val = txn.Get(_kvTable, from);
                        txn.Add(_kvTable, to, val);
                        return CopyResponse.Types.CopyResult.Success;
                    }
                }).ToArray(), false);

            var response = new CopyResponse();
            response.Results.AddRange(ret);

            return response;
        }

        public override async Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var keys = request.Keys.Select(k => new TableKey(k)).ToArray();
            var kvs = _lmdb.ContainsKeys(_kvTable, keys).ToArray();

            var foundKeys = kvs.Where(kv => kv.Item2).Select(kv => kv.Item1).ToArray();
            await _lmdb.WriteAsync(txn => txn.Delete(_kvTable, foundKeys), false);

            var response = new DeleteResponse();
            var addResults = kvs.Select(kv => kv.Item2 ? DeleteResponse.Types.DeleteResult.Success : DeleteResponse.Types.DeleteResult.NotFound);
            response.Results.AddRange(addResults);

            return response;
        }

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            var ret = _lmdb.Get(_kvTable, request.Keys.Select(k => new TableKey(k)));

            var response = new GetResponse();
            var getResponseEntries = ret.Select(kv => kv.Item2.HasValue 
                ? new GetResponse.Types.GetResponseEntry { Result = GetResponse.Types.GetResponseEntry.Types.GetResult.Success, Value = kv.Item2.Value.Value }
                : new GetResponse.Types.GetResponseEntry { Result = GetResponse.Types.GetResponseEntry.Types.GetResult.NotFound, Value = ByteString.Empty });
            response.Results.AddRange(getResponseEntries);

            return Task.FromResult(response);
        }

        public override Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context)
        {
            var ret = _lmdb.KeysByPrefix(_kvTable, request.KeyPrefix, 0, uint.MaxValue);

            return responseStream.WriteAllAsync(ret.Select(k => new KeyListResponse { Key = k.ToString() }));
        }

        public override Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context)
        {
            var ret = _lmdb.PageByPrefix(_kvTable, request.KeyPrefix, 0, uint.MaxValue);

            return responseStream.WriteAllAsync(ret.Select(k => new KeyValueListResponse { Key = k.Item1.ToString(), Value = k.Item2.Value }));
        }
    }
}
