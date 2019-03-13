using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;

namespace LmdbCacheServer
{
    public class LmdbCacheServiceImpl : LmdbCacheService.LmdbCacheServiceBase
    {
        public override Task<AddResponse> Add(AddRequest request, ServerCallContext context)
        {
            return base.Add(request, context);
        }

        public override Task<ContainsKeysResponse> ContainsKeys(GetRequest request, ServerCallContext context)
        {
            return base.ContainsKeys(request, context);
        }

        public override Task<CopyResponse> Copy(CopyRequest request, ServerCallContext context)
        {
            return base.Copy(request, context);
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            return base.Delete(request, context);
        }

        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            return base.Get(request, context);
        }

        public override Task ListKeys(KeyListRequest request, IServerStreamWriter<KeyListResponse> responseStream, ServerCallContext context)
        {
            return base.ListKeys(request, responseStream, context);
        }

        public override Task ListKeyValues(KeyListRequest request, IServerStreamWriter<KeyValueListResponse> responseStream, ServerCallContext context)
        {
            return base.ListKeyValues(request, responseStream, context);
        }
    }
}
