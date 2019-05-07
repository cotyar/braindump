/* eslint-disable max-len */
const GBC = require('grpc-bus-websocket-client');

export const getMonitoring = (port, onMessage, onError) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'WebClient getStatus' }, (_err, res) => {
      console.log('Monitoring Connected.');
      if (res !== null) onMessage(res);
      if (_err !== null) onError(_err);
    });
  });

export const connectMonitoring = (port, onMessage /* , onError */) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    // gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'WebClient streaming monitoring getStatus' }, (_err, res) => {
    //   console.log('Monitoring Streaming Connected.');
    //   if (res !== null) onMessage(res);
    //   if (_err !== null) onError(_err);
    // });
    gbc.services.LmdbCache.MonitoringService.subscribe({ correlationId: 'WebClient streaming monitoring' }).on('data', (data) => {
      console.log('Monitoring Streaming Update.');
      console.log(data);
      onMessage(data);
    });
  });

export default connectMonitoring;
