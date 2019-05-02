import WsProxyServer from './grpcweb/WsProxyServer';

const os = require('os');
const path = require('path');
const express = require('express');

const app = express();
const port = process.env.PORT || 8080;

// app.get('/apia', (req, res) => res.send({ aaa: 'Hello World!' }));
// app.get('/vapia', (req, res) => res.send({ vaaa: 'Hello World!' }));
app.get('/assets/css/fontawesome.min.css', (req, res) => res.sendFile(path.resolve(__dirname, '../client/assets/css/fontawesome.min.css')));
app.get('/grpc-bus.proto', (req, res) => res.sendFile(path.resolve(__dirname, 'grpcweb/grpc-bus.proto')));
app.get('/lmdb_cache_remoting.proto', (req, res) => res.sendFile(path.resolve(__dirname, 'grpcweb/lmdb_cache_remoting.proto')));


/**
 * TODOs:
 * - configure middleware (body parser, logger)
 */

/**
 * API
 */
app.get('/api/getUsername', (req, res) => res.send({ username: os.userInfo().username }));

/**
 * serve react app
 */
app.use(express.static('dist'));
// app.use(express.static('assets'));
app.get('*', (req, res) => res.sendFile(path.resolve(__dirname, 'index.html')));


/**
 * production mode
 */
if (process.env.NODE_ENV === 'production') {
  // Serve any static files
  app.use(express.static(path.resolve(__dirname, 'dist')));
  // Handle React routing, return all requests to React app
  app.get('*', (req, res) => res.sendFile(path.resolve(__dirname, 'dist/index.html')));
}

/**
 * start the server
 */
app.listen(port, () => console.log('Listening on port 8080!'));

const grpcwps = new WsProxyServer(8081);
