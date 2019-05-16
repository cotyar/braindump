using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using LmdbCache;
using NLog;
using static LmdbCache.Helper;

namespace LmdbCacheServer.Monitoring
{
    public class Monitor : MonitoringService.MonitoringServiceBase, IDisposable
    {
        private Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly Func<Task<ReplicaStatus>> _collectStatus;
        private readonly ConcurrentDictionary<string, (IServerStreamWriter<MonitoringUpdateResponse>, TaskCompletionSource<int>)> _subscriptions;
        private readonly Task _loopTask;
        private readonly CancellationTokenSource _cts;

        public Monitor(Func<Task<ReplicaStatus>> collectStatus, uint monitoringInterval)
        {
            _subscriptions = new ConcurrentDictionary<string, (IServerStreamWriter<MonitoringUpdateResponse>, TaskCompletionSource<int>)>();
            _collectStatus = collectStatus;
            _cts = new CancellationTokenSource();

            _loopTask = GrpcSafeHandler(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        await MonitoringTimerTick();
                        await Task.Delay((int) monitoringInterval, _cts.Token);
                    }
                });
        }

        public override Task<MonitoringUpdateResponse> GetStatus(MonitoringUpdateRequest request, ServerCallContext context) =>
            GrpcSafeHandler(async () => new MonitoringUpdateResponse { Status = await _collectStatus() });

        public override Task Subscribe(MonitoringUpdateRequest request, IServerStreamWriter<MonitoringUpdateResponse> responseStream, ServerCallContext context) =>
            GrpcSafeHandler(async () =>
            {
                _log.Info($"Peer: {context.Peer}, host: {context.Host}");
                await responseStream.WriteAsync(new MonitoringUpdateResponse {Status = await _collectStatus()});
                var tcs = new TaskCompletionSource<int>();
                if (!_subscriptions.TryAdd(request.CorrelationId, (responseStream, tcs)))
                {
                    var msg = $"Monitoring client failed for subscribe. '{request.CorrelationId}' already used";
                    _log.Info(msg);
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
                    _log.Info($"Monitoring update failed for subscriber '{corrId}' with exception '{e}'");
                    _subscriptions.TryRemove(corrId, out _);
                }
            }

            _log.Info("Monitoring timer job finished.");
        }

        public void Dispose()
        {
            _cts.Cancel();

            foreach (var (_, shutdownTcs) in _subscriptions.Values)
            {
                shutdownTcs.SetResult(0);
            }
        }
    }
}
