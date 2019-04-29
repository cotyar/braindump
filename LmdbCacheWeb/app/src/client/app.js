import React, { Component } from 'react';
import ReactDOM from 'react-dom';
import style from './assets/css/style.scss';
import { express_icon, nodejs_icon, react_icon, webpack_icon } from './assets/img';

const divStyle = {
  margin: '0 auto',
  paddingTop: '15%',
  width: '500px'
};

const GBC = require('grpc-bus-websocket-client');

new GBC('ws://localhost:8081/', 'lmdb_cache_remoting.proto', { LmdbCache: { MonitoringService: 'localhost:43051' } })
  .connect()
  .then((gbc) => {
    gbc.services.LmdbCache.MonitoringService.getStatus({ correlationId: 'Gabr' }, (_err, res) => {
      console.log('MESSAGE!!!');
      console.log(res);
      console.error(_err);
    });
  });

export default class App extends Component {
  constructor(props) {
    super(props);
    this.state = { username: null };
  }

  componentDidMount() {
    fetch('/api/getUsername')
      .then(res => res.json())
      .then(user => this.setState({ username: user.username }));
  }

  render() {
    const { username } = this.state;
    return (
      <div style={divStyle}>
        {username ? <h1>{`Hello ${username} (i.e. me)`}</h1> : <h1>Loading.. please wait!</h1>}
        <img src={nodejs_icon} alt="react" height="80"/>
        <img src={express_icon} alt="react" height="80"/>
        <img src={react_icon} alt="react" height="80"/>
        <img src={webpack_icon} alt="react" height="80"/>
      </div>
    );
  }
}
