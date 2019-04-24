using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Utils;
using LmdbCache;
using LmdbCacheServer.Tables;
using LmdbLight;
using static LmdbCache.Helper;
using static LmdbCache.AddRequest.Types;
using static LmdbCache.AddResponse.Types;
using static LmdbCache.DeleteResponse.Types;
using static LmdbCache.GetResponse.Types;
using static LmdbCache.GetResponse.Types.GetResponseEntry.Types;
using static LmdbCache.KvMetadata.Types;
using static LmdbCache.KvMetadata.Types.Status;
using static LmdbCache.KvMetadata.Types.UpdateAction;
using static LmdbCache.ValueMetadata.Types.HashedWith;

namespace LmdbCacheServer
{
    public class LmdbCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase
    {
        private readonly KvTable _kvTable;

        public LmdbCacheServiceImpl(KvTable kvTable) 
        {
            _kvTable = kvTable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<AddResponse> Add(IEnumerable<AddRequestEntry> entries, Header header) =>
            GrpcSafeHandler(async () =>
            {
                var batch = entries.Select(ks => (new KvKey(ks.Key), new KvMetadata
                {
                    Status = Active,
                    Expiry = ks.Expiry,
                    Action = Added,
                    CorrelationId = header.CorrelationId,
                    ValueMetadata = ks.ValueMetadata
                }, new KvValue(ks.Value))).ToArray();

                var ret = await (header.OverrideExisting ? _kvTable.AddOrUpdate(batch) : _kvTable.Add(batch));
                var response = new AddResponse();
                var addResults = ret.Select(kv => kv.Item2
                    ? (header.OverrideExisting ? AddResult.KeyUpdated : AddResult.KeyAdded) // TODO: Recheck statuses
                    : (header.OverrideExisting ? AddResult.Failure : AddResult.KeyAlreadyExists));
                response.Results.AddRange(addResults);

                return response;
            });

        public override Task<AddResponse> Add(AddRequest request, ServerCallContext context) =>
            GrpcSafeHandler(() => Add(request.Entries, request.Header));

        public override Task<AddResponse> AddStream(IAsyncStreamReader<AddStreamRequest> requestStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {

                if (!await requestStream.MoveNext() ||
                    requestStream.Current.MsgCase != AddStreamRequest.MsgOneofCase.Header)
                {
                    Console.Error.WriteLine(
                        $"AddStream unexpected message type: {requestStream.Current.MsgCase}. 'Header' message expected");
                    throw new MessageFormatException("Header expected");
                }

                var header = requestStream.Current.Header;

                var entries = new List<AddRequestEntry>((int) header.ChunksCount);
                for (uint index = 0; await requestStream.MoveNext(); index++)
                {
                    if (requestStream.Current.MsgCase != AddStreamRequest.MsgOneofCase.Chunk)
                    {
                        var message =
                            $"AddStream unexpected message type: {requestStream.Current.MsgCase}. 'DataChunk' message expected";
                        Console.Error.WriteLine(message);
                        throw new MessageFormatException(message);
                    }

                    if (requestStream.Current.Chunk.Index != index)
                    {
                        var message =
                            $"AddStream incorrect Chunk index. Expected: {index}, received {requestStream.Current.Chunk.Index}.";
                        Console.Error.WriteLine(message);
                        throw new MessageFormatException(message);
                    }

                    entries.Add(requestStream.Current.Chunk.Entry);
                }

//            Console.WriteLine($"Stream was fully written for key: {key}");

                return await Add(entries, header);
            });

        public override Task<ContainsKeysResponse> ContainsKeys(GetRequest request, ServerCallContext context) =>
            GrpcSafeHandler(() =>
            {
                var ret = _kvTable.ContainsKeys(request.Keys.Select(k => new KvKey(k)).ToArray());

                var response = new ContainsKeysResponse();
                response.Results.AddRange(ret.Select(kv => kv.Item2));

                return response;
            });

        public override Task<CopyResponse> Copy(CopyRequest request, ServerCallContext context) =>
            GrpcSafeHandler(() =>_kvTable.Copy(request));

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var keys = request.Keys.Select(k => 
                    (new KvKey(k), new KvMetadata
                        {
                            Status = Deleted,
                            Action = Added,
                            CorrelationId = request.CorrelationId
                        })).ToArray();
                var kvs = await _kvTable.Delete(keys);

                var response = new DeleteResponse();
                var addResults = kvs.Select(kv => kv.Item2 ? DeleteResult.Success : DeleteResult.NotFound);
                response.Results.AddRange(addResults);

                return response;
            });

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context) =>
            GrpcSafeHandler(() =>
            {
                var ret = _kvTable.Get(request.Keys.Select(k => new KvKey(k)));

                var response = new GetResponse();
                var getResponseEntries = ret.Select((kv, i) =>
                {
                    if (!kv.Item3.HasValue) return new GetResponseEntry {Result = GetResult.NotFound, Index = (uint) i};

                    var gre = new GetResponseEntry
                    {
                        Result = GetResult.Success,
                        Index = (uint) i,
                        ValueMetadata = kv.Item2.ValueMetadata,
                        Value = kv.Item3.Value.Value
                    };
                    return gre;
                });
                response.Results.AddRange(getResponseEntries);
                return response;
            });

        public override Task GetStream(GetRequest request, IServerStreamWriter<GetStreamResponse> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var kvs = _kvTable.Get(request.Keys.Select(k => new KvKey(k)));

                for (var i = 0; i < kvs.Length; i++) 
                {
                    var (_, kvMetadata, kvValue) = kvs[i];
                    var gre = kvValue.HasValue
                        ? new GetResponseEntry
                            {
                                Result = GetResult.Success,
                                Index = (uint) i,
                                ValueMetadata = kvMetadata.ValueMetadata,
                                Value = kvValue.Value.Value
                            }
                        : new GetResponseEntry {Result = GetResult.NotFound, Index = (uint) i};
                    await responseStream.WriteAsync(new GetStreamResponse {Result = gre});
                }
            });

        public override Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var ret = _kvTable.KeysByPrefix(new KvKey(request.KeyPrefix), 0, uint.MaxValue);
                await responseStream.WriteAllAsync(ret.Select(k => new KeyListResponse {Key = k.Key}));
            });

        public override Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                var ret = _kvTable.PageByPrefix(new KvKey(request.KeyPrefix), 0, uint.MaxValue);
                await responseStream.WriteAllAsync(ret.Select(k => new KeyValueListResponse {Key = k.Item1.ToString(), Value = k.Item2.Value})); // TODO: Add value streaming
            });
    }
}
