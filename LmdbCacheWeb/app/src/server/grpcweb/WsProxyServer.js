/* eslint-disable no-console */
// import * as util from 'util';

import * as grpc from 'grpc';
import * as grpcBus from 'grpc-bus';
import * as protobuf from 'protobufjs';
import { Server as WebSocketServer } from 'ws';

function WsProxyServer(port) {
  const wss = new WebSocketServer({ port });
  // console.log(`protobuf  : ${protobuf.loadProtoFile}`);
  const gbBuilder = protobuf.loadProtoFile('C:\\Work2\\braindump\\LmdbCacheWeb\\app\\src\\server\\grpcweb\\grpc-bus.proto');
  // console.log(`gbBuilder  : ${gbBuilder}`);
  const gbTree = gbBuilder.build().grpcbus;

  wss.on('connection', (ws) => {
    console.log('connected');

    // eslint-disable-next-line no-unused-vars
    ws.once('message', (dataFirstMessage) => {
      const message = JSON.parse(dataFirstMessage);
      console.log('connected with');
      console.dir(message, { depth: null });
      const protoFileExt = message.filename.substr(message.filename.lastIndexOf('.') + 1);
      const protoDefs = (protoFileExt === 'json')
        ? protobuf.loadJson(message.contents, null, message.filename)
        : protobuf.loadProto(message.contents, null, message.filename);

      const gbServer = new grpcBus.Server(protoDefs, (serverMessage) => {
        console.log('sending (pre-stringify): %s');
        console.dir(serverMessage, { depth: null });
        console.log('sending (post-stringify): %s');
        console.dir(JSON.stringify(serverMessage));
        // ws.send(JSON.stringify(message));
        const pbMessage = new gbTree.GBServerMessage(serverMessage);
        console.log('sending (pbMessage message):', pbMessage);
        console.log('sending (raw message):', pbMessage.toBuffer());
        console.log('re-decoded message:', gbTree.GBServerMessage.decode(pbMessage.toBuffer()));
        if (ws.readyState === ws.OPEN) {
          ws.send(pbMessage.toBuffer());
        } else {
          console.log('WebSocket closed before message could be sent:', pbMessage);
        }
      }, grpc);

      ws.on('message', (data, flags) => {
        console.log('received (raw):');
        console.log(data);
        console.log('with flags:');
        console.dir(flags);
        // var message = JSON.parse(data);
        const clientMessage = gbTree.GBClientMessage.decode(data);
        console.log('received (parsed):');
        // We specify a constant depth here because the incoming
        // message may contain the Metadata object, which has
        // circular references and crashes console.dir if its
        // allowed to recurse to print. Depth of 3 was chosen
        // because it supplied enough detail when printing
        console.dir(clientMessage, { depth: 3 });
        gbServer.handleMessage(clientMessage);
      });
    });
  });

  return () => {
    wss.close(e => console.error(`Grpc websocket errored when closing: ${e}`));
    console.log('Grpc websocket has been closed');
  };
}

export default WsProxyServer;
