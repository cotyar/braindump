using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;

namespace LmdbCacheServer.Monitoring
{
    public class Monitor : MonitoringService.MonitoringServiceBase, IDisposable
    {
        private readonly Func<Task<ReplicaStatus>> _collectStatus;
        private readonly uint _monitoringInterval;
        private readonly ConcurrentDictionary<string, (IServerStreamWriter<MonitoringUpdateResponse>, TaskCompletionSource<int>)> _subscriptions;
        private readonly Timer _monitoringTimer;

        public Monitor(Func<Task<ReplicaStatus>> collectStatus, uint monitoringInterval)
        {
            _subscriptions = new ConcurrentDictionary<string, (IServerStreamWriter<MonitoringUpdateResponse>, TaskCompletionSource<int>)>();
            _collectStatus = collectStatus;
            _monitoringInterval = monitoringInterval;
            _monitoringTimer = new Timer(_ => MonitoringTimerTick().Wait(), null, TimeSpan.FromMilliseconds(_monitoringInterval), TimeSpan.FromMilliseconds(_monitoringInterval));
        }

        public override Task<MonitoringUpdateResponse> GetStatus(MonitoringUpdateRequest request, ServerCallContext context) => 
            Task.Run(async () => new MonitoringUpdateResponse { Status = await _collectStatus() });

        public override Task Subscribe(MonitoringUpdateRequest request, IServerStreamWriter<MonitoringUpdateResponse> responseStream, ServerCallContext context) =>
            Task.Run(async () =>
            {
                Console.WriteLine($"Peer: {context.Peer}, host: {context.Host}");
                await responseStream.WriteAsync(new MonitoringUpdateResponse {Status = await _collectStatus()});
                var tcs = new TaskCompletionSource<int>();
                if (!_subscriptions.TryAdd(request.CorrelationId, (responseStream, tcs)))
                {
                    var msg = $"Monitoring client failed for subscribe. '{request.CorrelationId}' already used";
                    Console.WriteLine(msg);
                    throw new Exception(msg);
                }

                await tcs.Task;
            });

        private async Task MonitoringTimerTick()
        {
            var response = new MonitoringUpdateResponse {Status = await _collectStatus()};
            foreach (var (corrId, (stream, _)) in _subscriptions)
            {
                try
                {
                    await stream.WriteAsync(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Monitoring update failed for subscriber '{corrId}' with exception '{e}'");
                    _subscriptions.TryRemove(corrId, out _);
                }
            }

            Console.WriteLine("Monitoring timer job finished.");
        }

        public void Dispose()
        {
            _monitoringTimer.Dispose();

            foreach (var (_, shutdownTcs) in _subscriptions.Values)
            {
                shutdownTcs.SetResult(0);
            }
        }
    }
}
