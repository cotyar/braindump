// import { prefix } from '@fortawesome/free-solid-svg-icons';

const GBC = require('grpc-bus-websocket-client');

export const getLmdbCacheService = (port, onConnect) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { LmdbCacheService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    console.log('LmdbCacheService connected.');
    const listKeys = (prefix, onMessage) => {
      const ret = gbc.services.LmdbCache.LmdbCacheService.listKeyValues({
        keyPrefix: prefix,
        pageSize: 10,
        page: 0,
        correlationId: 'Web Client'
      }).on('data', (data) => {
        console.log(data);
        onMessage(data);
      });

      return ret;
    };
    onConnect({ listKeys });
  })
  .catch(err => console.error(err));

export const echo = (port, onMessage, onError) => new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { LmdbCacheService: `localhost:${port}` } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.LmdbCacheService.echo({ echo: 'Hello world!' }, (_err, res) => {
      console.log('Echo received.');
      if (res !== null) onMessage(res);
      if (_err !== null) onError(_err);
    });
  });

export default getLmdbCacheService;
