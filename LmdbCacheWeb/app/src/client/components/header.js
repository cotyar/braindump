/* eslint-disable max-len */
/* eslint-disable react/prop-types */
/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';

import 'bulma/css/bulma.css';
import { Box, Panel, PanelHeading, PanelBlock, Input, Control, Icon } from 'bloomer';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSearch } from '@fortawesome/free-solid-svg-icons';

import { getLmdbCacheService, echo } from '../services/grpcCacheService';

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
    // fetch('/api/getUsername')
    //   .then(res => res.json())
    //   .then(user => this.setState({ username: user.username }));
    const { serverPorts, prefix } = this.state;
    // getLmdbCacheService(serverPorts[0], cs => this.setState({ cacheServer: cs }));
    echo(serverPorts[0], console.log, console.error);
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
                this.setState({ prefix: pfx, list: [] });
                console.log(pfx);

                if (cacheServer) {
                  // cacheServer.listKeys(pfx, (msg) => {
                  //   console.log(msg);
                  //   const lst = self.state.list;
                  //   lst.push(msg);
                  //   this.setState({ list: lst });
                  //   console.log(lst);
                  // });
                }
              }}
            />
            <span className="icon is-left" aria-hidden="true"><FontAwesomeIcon icon={faSearch} /></span>
          </Control>
        </PanelBlock>
      </Panel>
    );
  }
}
