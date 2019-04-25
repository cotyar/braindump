import React from 'react'; // we need this to make JSX compile
import { LightningConfig } from "./generated/lmdb_cache_remoting_pb";
import proto from 'google-protobuf'
import {grpc} from "@improbable-eng/grpc-web";

const msg = new LightningConfig();
msg.setName("John Doe");

type CardProps = {
  title: string,
  paragraph: string
}

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