import React from 'react'; // we need this to make JSX compile
import proto from 'google-protobuf'
import {grpc} from "@improbable-eng/grpc-web";

import { LightningConfig, MonitoringUpdateRequest } from "./generated/lmdb_cache_remoting_pb";

import {
  MonitoringServiceClient,
  MonitoringService,
  MonitoringServiceGetStatus
} from "./generated/lmdb_cache_remoting_pb_service";

const msg = new LightningConfig();
msg.setName("John Doe");

type CardProps = {
  title: string,
  paragraph: string
}

const client = new MonitoringServiceClient("http://localhost:41051");
const req = new MonitoringUpdateRequest();
client.getStatus(req, (err, cacheStatus) => {
  /* ... */
  console.log(cacheStatus);
  console.error(err);
});

export const Card = ({ title, paragraph }: CardProps) => <aside>
  <h2>{ title }</h2>
  <p>
    { paragraph }
  </p>
  <p>
      {msg.getName()}
  </p>
</aside>

export default Card