/* eslint-disable no-plusplus */
/* eslint-disable no-proto */
/* eslint-disable react/prop-types */
/* eslint-disable max-len */
/* eslint-disable react/jsx-one-expression-per-line */
/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';
import TreeView from 'react-treeview';

import 'bulma/css/bulma.css';
import 'react-treeview/react-treeview.css';
// import fontawesome, { faUser } from '@fortawesome/fontawesome-free';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCoffee, faTimes } from '@fortawesome/free-solid-svg-icons';

import { Box, Columns, Column, Notification, Container,
  Card, CardContent, CardFooter, CardFooterItem,
  CardHeader, CardHeaderIcon, CardHeaderTitle, CardImage,
  Icon, Content, Subtitle, Table,
  Tabs, TabList, Tab, TabLink
} from 'bloomer';

import { connectMonitoring } from '../services/grpcBus';

export const DataItem = ({ color, label, value }) => (
  <Notification isColor={color} hasTextAlign="centered" >
    <Subtitle tag="p" isSize={5}><strong>{label}:</strong> {value}</Subtitle>
  </Notification>
);

const defaultColumnSize = { tablet: '1/4', desktop: '1/4' };
export const DataColumn = ({ color, label, value, columnSize }) => (
  <Column isSize={columnSize || defaultColumnSize}>
    <DataItem color={color} label={label} value={value}/>
  </Column>
);

export const DataColumnGroup = ({ children }) => (
  <Columns isCentered isMultiline >
    { children }
  </Columns>
);

export const formatTicksOffsetUtc = ticksOffsetUtc => `${ticksOffsetUtc.high}|${ticksOffsetUtc.low}`;

export const TicksOffsetUtc = ({ ticksOffsetUtc }) => (<DataColumn label="Ticks Offset Utc" value={formatTicksOffsetUtc(ticksOffsetUtc)}/>);

export const CurrentClock = ({ clock }) => (
  <Card>
    <CardHeader>
      <CardHeaderTitle className="has-background-light">Current Clock</CardHeaderTitle>
    </CardHeader>
    <CardContent>
      <Container>
        <DataColumnGroup>
          <TicksOffsetUtc ticksOffsetUtc={clock.ticksOffsetUtc}/>
          <Column isSize={defaultColumnSize}>
            <Table isBordered isStriped isNarrow>
              <thead>
                <tr>
                  <th>Replica</th>
                  <th>Pos</th>
                </tr>
              </thead>
              <tbody>
                {Object.keys(clock.replicas.map).map(r => (
                  <tr key={r}>
                    <td>{r}</td>
                    <td>{clock.replicas.map[r].value.low}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </Column>
        </DataColumnGroup>
      </Container>
    </CardContent>
  </Card>
);

export const ReplicaConfig = ({ config }) => (
  <Card>
    <CardHeader>
      <CardHeaderTitle className="has-background-light">Replica Config</CardHeaderTitle>
    </CardHeader>
    <CardContent>
      <Container>
        <DataColumnGroup>
          <DataColumn label="Host" value={config.hostName}/>
          <DataColumn label="Master Node" value={config.masterNode}/>
          <DataColumn label="Monitoring Interval" value={config.monitoringInterval}/>
          <DataColumn label="Monitoring Port" value={config.monitoringPort}/>
          <DataColumn label="Port" value={config.port}/>
          <DataColumn label="Replica Id" value={config.replicaId}/>
          <DataColumn label="WebUI Port" value={config.webUIPort}/>
        </DataColumnGroup>
      </Container>
    </CardContent>
  </Card>
);

export const ReplicationConfig = ({ replication }) => (
  <Card>
    <CardHeader>
      <CardHeaderTitle className="has-background-light">Replication Config</CardHeaderTitle>
    </CardHeader>
    <CardContent>
      <Container>
        <DataColumnGroup>
          <DataColumn label="Port" value={replication.port}/>
          <DataColumn label="Page Size" value={replication.pageSize}/>
          <DataColumn label="Await SyncFrom" value={replication.awaitSyncFrom ? 'true' : 'false'}/>
          <DataColumn label="Use Batching" value={replication.useBatching ? 'true' : 'false'}/>
        </DataColumnGroup>
      </Container>
    </CardContent>
  </Card>
);


export const PersistenceConfig = ({ persistence }) => (
  <Card>
    <CardHeader>
      <CardHeaderTitle className="has-background-light">Storage Config</CardHeaderTitle>
    </CardHeader>
    <CardContent>
      <Container>
        <DataColumnGroup>
          <DataColumn label="Path" value={persistence.name} columnSize={{ tablet: '3/4', desktop: '3/4' }}/>
          <DataColumn label="Max Tables" value={persistence.maxTables}/>
          <DataColumn label="Limit Gb" value={persistence.storageLimit.low}/>
          <DataColumn label="Disk Sync Mode" value={persistence.syncMode}/>
          <DataColumn label="WB Max Queue" value={persistence.writeBatchMaxDelegates}/>
          <DataColumn label="WB Timeout ms" value={persistence.writeBatchTimeoutMilliseconds}/>
        </DataColumnGroup>
      </Container>
    </CardContent>
  </Card>
);

export const Counters = ({ counters }) => (
  <Card>
    <CardHeader>
      <CardHeaderTitle className="has-background-light">Counters</CardHeaderTitle>
    </CardHeader>
    <CardContent>
      <Container>
        <DataColumnGroup>
          <DataColumn label="Add" value={counters.addsCounter.low} color="warning" />
          <DataColumn label="Contains" value={counters.containsCounter.low}/>
          <DataColumn label="Copy" value={counters.copysCounter.low}/>
          <DataColumn label="Delete" value={counters.deletesCounter.low}/>
          <DataColumn label="Get" value={counters.getCounter.low} color="warning" />
          <DataColumn label="Key Search" value={counters.keySearchCounter.low}/>
          <DataColumn label="Metadata Search" value={counters.metadataSearchCounter.low}/>
          <DataColumn label="Page Search" value={counters.pageSearchCounter.low}/>
          <DataColumn label="Replicated Add" value={counters.replicatedAdds.low}/>
          <DataColumn label="Replicated Delete" value={counters.replicatedDeletes.low}/>
          <DataColumn label="Largest Key" value={counters.largestKeySize}/>
          <DataColumn label="Largest Value" value={counters.largestValueSize}/>
        </DataColumnGroup>
      </Container>
    </CardContent>
  </Card>
);

export const CollectedStats = ({ stats }) => {
  console.log(stats);
  return (
    <Card>
      <CardHeader>
        <CardHeaderTitle className="has-background-light">Collected Stats</CardHeaderTitle>
        <CardHeaderIcon>
          {/* <Icon className="fa fa-angle-down" /> */}
          <span className="icon" aria-hidden="true" ><FontAwesomeIcon icon={faCoffee} /></span>
        </CardHeaderIcon>
      </CardHeader>
      <CardContent>
        <Container>
          <DataColumnGroup>
            <DataColumn label="Active Keys" value={stats.activeKeys.low} color="warning" />
            <DataColumn label="Deleted Keys" value={stats.deletedKeys.low} />
            <DataColumn label="Expired Keys" value={stats.expiredKeys.low} />
            <DataColumn label="Non-Exp Keys" value={stats.nonExpiredKeys.low} />
            <DataColumn label="All Keys" value={stats.allKeys.low} />
          </DataColumnGroup>
        </Container>
      </CardContent>
    </Card>
  );
};

let key1 = 0;
function TreeColumn({ label, parenLabel: parentLabel, value }) {
  console.info(`${label}|${value}|${parentLabel}`);
  key1 += 1;
  return (
    <Column isSize={defaultColumnSize}>
      {/* <DataItem color={color} label={label} value={value}/> */}
      <TreeView nodeLabel={label} key={key1} defaultCollapsed={false}>
        { Object.keys(value)
            // eslint-disable-next-line no-prototype-builtins
            .filter(p => value.hasOwnProperty(p) && p !== 'builder')
            // eslint-disable-next-line no-nested-ternary
            .map(p => (value && typeof value === 'object'
              ? (value.__proto__.$type && value[p]
                ? TreeColumn({ label: p, parentLabel: (`${parentLabel}^${label}`), value: value[p] })
                : <div className="info">{p}: <span>[object]</span></div>)
              : <div className="info">{p}: <span>{value || 'null'}</span></div>)) }
      </TreeView>
    </Column>
  );
}

function printTree({ label, parentLabel, value }) {
  // console.info(`${value.__proto__.$type}|${label}|${parentLabel}|${value}`);
  console.info(`${label}|${value}|${parentLabel}`);
  return Object.keys(value)
          // eslint-disable-next-line no-prototype-builtins
          .filter(p => value.hasOwnProperty(p) && p !== 'builder')
          .map(p => (value && typeof value === 'object' && value.__proto__.$type && value[p]
            ? printTree({ label: p, parentLabel: (`${parentLabel}^${label}`), value: value[p] })
            : value));
}

export default class ServerInfo extends Component {
  constructor(props) {
    super(props);
    this.state = {
      serverState: props.serverState
    };
  }

  // componentDidMount() {
  //   const { serverPort } = this.state;
  //   connectMonitoring(serverPort, (statusMsg) => {
  //       console.info(statusMsg);
  //       this.setState({ serverState: statusMsg });
  //     }, console.error);
  // }

  render() {
    const { serverState } = this.state;
    return !serverState
      ? <div/>
      : (
        <Card>
          {/* <CardHeader>
            <CardHeaderTitle className="has-background-light">
              {serverState.status.replicaId}:&nbsp;<span className="notbold">{serverPort}</span>,&nbsp;Started:&nbsp;<span className="notbold">{formatTicksOffsetUtc(serverState.status.started.ticksOffsetUtc)}</span>
            </CardHeaderTitle>
          </CardHeader> */}
          <CardContent>
            <CurrentClock clock={serverState.status.currentClock} />
            <br/>
            <ReplicaConfig config={serverState.status.replicaConfig} />
            <ReplicationConfig replication={serverState.status.replicaConfig.replication} />
            <PersistenceConfig persistence={serverState.status.replicaConfig.persistence} />
            <br/>
            <Counters counters={serverState.status.counters} />
            <br/>
            <CollectedStats stats={serverState.status.collectedStats} />
          </CardContent>
        </Card>
    );
  }
}
