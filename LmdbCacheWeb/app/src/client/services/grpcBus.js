const GBC = require('grpc-bus-websocket-client');

export const connectMonitoring = (port, onMessage, onError) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'Gabr' }, (_err, res) => {
      console.log('MESSAGE!!!');
      if (res !== null) onMessage(res);
      if (_err !== null) onError(_err);
      // console.log(res);
      // console.error(_err);
    });
  });

export default connectMonitoring;
