using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;

namespace LmdbCacheServer
{
    public class InMemoryCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase
    {
        const UInt32 MaxPageSize = 1024;

        ConcurrentDictionary<string, AddRequest> state = new ConcurrentDictionary<string, AddRequest>();

        //public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        //{
        //    var result = AddResponse.Types.AddResult.Success;
        //    state.AddOrUpdate(request.Key, request, (k, kv) => { result = AddResponse.Types.AddResult.KeyAlreadyExists; return kv; });
        //    Console.WriteLine($"Added: {request}");
        //    return Task.FromResult(new AddResponse { Result = result });
        //}

        //public override Task<AddOrUpdateResponse> AddOrUpdate(AddRequest request, ServerCallContext context)
        //{
        //    var result = AddOrUpdateResponse.Types.AddOrUpdateResult.KeyAdded;
        //    state.AddOrUpdate(request.Key, request, (k, kv) => { result = AddOrUpdateResponse.Types.AddOrUpdateResult.KeyUpdated; return request; });
        //    Console.WriteLine($"{result}: {request}");
        //    return Task.FromResult(new AddOrUpdateResponse { Result = result });
        //}

        //public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        //{
        //    var result = state.TryRemove(request.Key, out var _) 
        //        ? DeleteResponse.Types.DeleteResult.Success
        //        : DeleteResponse.Types.DeleteResult.NotFound;
        //    Console.WriteLine($"Deleted {result}: {request}");
        //    return Task.FromResult(new DeleteResponse { Result = result });
        //}

        //public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        //{
        //    var result = state.TryGetValue(request.Key, out var v)
        //        ? GetResponse.Types.GetResult.Success
        //        : GetResponse.Types.GetResult.NotFound;
        //    Console.WriteLine($"Returned {result}: {request}");
        //    return Task.FromResult(new GetResponse { Result = result, Value = v.Value });
        //}

        public override async Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context)
        {
            var pageSize = request.PageSize > 0 && request.PageSize < MaxPageSize ? request.PageSize : MaxPageSize;
            foreach (var key in state.Keys.Where(k => (k ?? "").StartsWith(request.KeyPrefix)).Take((int) pageSize)) {
                await responseStream.WriteAsync(new KeyListResponse{ Key = key });
            }
        }

        public override async Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context)
        {
            var pageSize = request.PageSize > 0 && request.PageSize < MaxPageSize ? request.PageSize : MaxPageSize;
            foreach (var entry in state.Where(e => e.Key.StartsWith(request.KeyPrefix)).Take((int) pageSize))
            {
                //await responseStream.WriteAsync(new KeyValueListResponse { Key = entry.Key, Value = entry.Value.Value });
            }
        }
    }
}
