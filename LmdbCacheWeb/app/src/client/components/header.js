/* eslint-disable max-len */
/* eslint-disable react/prop-types */
/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';

import 'bulma/css/bulma.css';
import { Box, Panel, PanelHeading, PanelBlock, Input, Control, Button, Table } from 'bloomer';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSearch } from '@fortawesome/free-solid-svg-icons';

import { getLmdbCacheService } from '../services/grpcCacheService';

export default class Header extends Component {
  constructor(props) {
    super(props);
    this.state = {
      serverPorts: props.ports,
      prefix: null,
      cacheServer: null,
      list: [],
    };
  }

  componentDidMount() {
    const { serverPorts, prefix } = this.state;
    getLmdbCacheService(serverPorts[0], (service) => {
      service.echo(console.log, console.error);
      this.setState({ cacheServer: service });
      // service.listKeys('', response => this.setState({ list: response.keyResponse }), console.error);
    });
  }

  render() {
    const { prefix, cacheServer, list } = this.state;
    const self = this;
    return (
      // <Box>Headree...</Box>
      <Panel>
        <PanelHeading>Keys</PanelHeading>
        <PanelBlock>
          <Control hasIcons="left">
            <Input
              isSize="small"
              placeholder="Search"
              className="is-rounded"
              onChange={(event) => {
                const pfx = event.target.value;
                // this.setState({ prefix: pfx, list: [] });
                console.log(pfx);

                if (cacheServer) {
                  cacheServer.listKeys(pfx, (response) => {
                    this.setState({ prefix: pfx, list: response.keyResponse });
                    // console.log(lst);
                  });
                }
              }}
            />
            <span className="icon is-left" aria-hidden="true"><FontAwesomeIcon icon={faSearch} /></span>
          </Control>
        </PanelBlock>
        <PanelBlock>
          {/* <Button isOutlined isFullWidth isColor="primary"> Reset all filters</Button> */}
          <Table isBordered isStriped isFullWidth /* isNarrow */>
            <thead>
              <tr>
                <th>Key</th>
                <th>Status</th>
                <th>Expiry</th>
                <th>Action</th>
                <th>Size</th>
                <th>Compressed Size</th>
                <th>Compression</th>
              </tr>
            </thead>
            <tbody>
              {list.map((km) => {
                const md = km.metadata.toRaw();
                return (
                  <tr key={km.key}>
                    <td>{km.key}</td>
                    <td>{md.status}</td>
                    <td>{md.expiry.ticksOffsetUtc.toNumber()}</td>
                    <td>{md.action}</td>
                    <td>{md.valueMetadata.sizeFull}</td>
                    <td>{md.valueMetadata.sizeCompressed}</td>
                    <td>{md.valueMetadata.compression}</td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </PanelBlock>
      </Panel>
    );
  }
}
