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
      serverPorts: props.ports,
      serverStates: props.ports.map(() => null),
      activeIndex: -1
    };
  }

  componentDidMount() {
    const { serverPorts, serverStates, activeIndex } = this.state;
    serverPorts.forEach((port, idx) => connectMonitoring(port, (statusMsg) => {
      const i = idx;
      console.info(statusMsg);
      serverStates[i] = statusMsg;
      const newState = { serverStates, activeIndex };
      if (activeIndex === -1) {
        newState.activeIndex = i;
      }
      this.setState(newState);
    }, console.error));
  }

  render() {
    const { serverPorts, serverStates, activeIndex } = this.state;
    return !serverStates[activeIndex]
      ? <div/>
      : (
        <div>
          <Tabs>
            <TabList>
              { serverStates.map((s, idx) => {
                const i = idx;
                return (
                  <Tab isActive={i === activeIndex}>
                    <TabLink onClick={() => this.setState({ activeIndex: i })}>
                      {
                        serverStates[i]
                          ? <span><strong>{serverStates[i].status.replicaId}:&nbsp;</strong>{serverPorts[i]}<strong>,&nbsp;Started:&nbsp;</strong>{formatTicksOffsetUtc(serverStates[i].status.started.ticksOffsetUtc)}</span>
                          : <span>{serverPorts[i]}</span>
                      }
                    </TabLink>
                  </Tab>
                );
              })}
            </TabList>
          </Tabs>
          <ServerInfo serverState={serverStates[activeIndex]}/>)
        </div>
      );
  }
}
