/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';
import ReactDOM from 'react-dom';
import ReactQueryParams from 'react-query-params';

import style from './assets/css/style.scss';

import 'bulma/css/bulma.css';
// import fontawesome from '@fortawesome/fontawesome'
import { Box, Section } from 'bloomer';

import Header from './components/header';
import ServerInfoTabs from './components/serverInfoTabs';

// const divStyle = {
//   margin: '0 auto',
//   paddingTop: '15%',
//   width: '500px'
// };


export default class App extends ReactQueryParams {
  constructor(props) {
    super(props);
    this.state = { username: null };
  }

  componentDidMount() {
    // fetch('/api/getUsername')
    //   .then(res => res.json())
    //   .then(user => this.setState({ username: user.username }));
  }

  render() {
    // const { username } = this.state;
    let serverPorts = this.queryParams.ports;
    if (!serverPorts) {
      serverPorts = [43051, 43551];
      this.setQueryParams({
        ports: serverPorts
      });
    }
    return (
      <div>
        <Box>A white box to contain other elements</Box>
        <Section>
          <Header />
        </Section>
        <Section>
          <ServerInfoTabs ports={serverPorts}/>
        </Section>
      </div>
    );
  }
}
