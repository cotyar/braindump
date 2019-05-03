/* eslint-disable react/prop-types */
/* eslint-disable max-len */
/* eslint-disable react/jsx-one-expression-per-line */
import React, { Component } from 'react';
import { Box, Columns, Column, Notification, Container,
  Card, CardContent, CardFooter, CardFooterItem,
  CardHeader, CardHeaderIcon, CardHeaderTitle, CardImage,
  Icon, Content, Subtitle, Table,
  Tabs, TabList, Tab, TabLink
} from 'bloomer';

import { connectMonitoring } from '../services/grpcBus';

import ServerInfo, { formatTicksOffsetUtc } from './serverInfo';

export default class ServerInfoTabs extends Component {
  constructor(props) {
    super(props);
    this.state = {
      serverPort: props.port,
      serverState: null
    };
  }

  componentDidMount() {
    const { serverPort } = this.state;
    connectMonitoring(serverPort, (statusMsg) => {
      console.info(statusMsg);
      this.setState({ serverState: statusMsg });
    }, console.error);
  }

  render() {
    const { serverPort, serverState } = this.state;
    return !serverState
      ? <div/>
      : (
        <div>
          <Tabs>
            <TabList>
              <Tab isActive>
                <TabLink onClick={() => console.log('click!!!')}>
                  <span><strong>{serverState.status.replicaId}:&nbsp;</strong>{serverPort}<strong>,&nbsp;Started:&nbsp;</strong>{formatTicksOffsetUtc(serverState.status.started.ticksOffsetUtc)}</span>
                </TabLink>
              </Tab>
            </TabList>
          </Tabs>
          {!serverState ? (<div/>) : (<ServerInfo serverState={serverState}/>)}
        </div>
      );
  }
}
