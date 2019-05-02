/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';

import 'bulma/css/bulma.css';
// import fontawesome from '@fortawesome/fontawesome'
import { Box } from 'bloomer';

export default class Header extends Component {
//   constructor(props) {
//     super(props);
//     this.state = { username: null };
//   }

  componentDidMount() {
    // fetch('/api/getUsername')
    //   .then(res => res.json())
    //   .then(user => this.setState({ username: user.username }));
  }

  render() {
    // const { serverState } = this.state;
    return (
      // <div style={divStyle}>
      //   {username ? <h1>{`Hello ${username} (i.e. me)`}</h1> : <h1>Loading.. please wait!</h1>}
      //   <img src={nodejs_icon} alt="react" height="80"/>
      //   <img src={express_icon} alt="react" height="80"/>
      //   <img src={react_icon} alt="react" height="80"/>
      //   <img src={webpack_icon} alt="react" height="80"/>
      // </div>

      <Box>Headree...</Box>
    );
  }
}
