using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;

namespace LmdbCacheServer.Monitoring
{
    public class Monitor : MonitoringService.MonitoringServiceBase
    {
        private readonly Func<Task<ReplicaStatus>> _collectStatus;

        public Monitor(Func<Task<ReplicaStatus>> collectStatus)
        {
            _collectStatus = collectStatus;
        }

        public override Task<MonitoringUpdateResponse> GetStatus(MonitoringUpdateRequest request, ServerCallContext context) => 
            Task.Run(async () => new MonitoringUpdateResponse { Status = await _collectStatus() });

        public override Task Subscribe(MonitoringUpdateRequest request, IServerStreamWriter<MonitoringUpdateResponse> responseStream, ServerCallContext context)
        {
            return base.Subscribe(request, responseStream, context);
        }
    }
}
