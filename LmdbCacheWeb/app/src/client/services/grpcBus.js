const GBC = require('grpc-bus-websocket-client');

export const getMonitoring = (port, onMessage, onError) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'Gabr' }, (_err, res) => {
      console.log('MESSAGE!!!');
      if (res !== null) onMessage(res);
      if (_err !== null) onError(_err);
    });
  });

export const connectMonitoring = (port, onMessage, onError) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'Gabr' }, (_err, res) => {
      console.log('Streamed MESSAGE!!!');
      if (res !== null) onMessage(res);
      if (_err !== null) onError(_err);
    });
    gbc.services.LmdbCache.MonitoringService.subscribe({ correlationId: 'Gabr' }).on('data', (data) => {
      console.log(data);
      onMessage(data);
    });
  });

export default connectMonitoring;
