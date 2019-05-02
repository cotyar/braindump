/* eslint-disable import/order */
/* eslint-disable indent */
import React, { Component } from 'react';

import 'bulma/css/bulma.css';
// import fontawesome, { faUser } from '@fortawesome/fontawesome-free';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCoffee, faTimes } from '@fortawesome/free-solid-svg-icons';

import { Box, Columns, Column, Notification, Container,
  Card, CardContent, CardFooter, CardFooterItem,
  CardHeader, CardHeaderIcon, CardHeaderTitle, CardImage,
  Icon
} from 'bloomer';

export default class ServerInfo extends Component {
  constructor(props) {
    super(props);
    this.state = { serverState: 'null  ss' };
  }

  componentDidMount() {
    // fetch('/api/getUsername')
    //   .then(res => res.json())
    //   .then(user => this.setState({ username: user.username }));
  }

  render() {
    const { serverState } = this.state;
    const columnSize = { tablet: '1/3', desktop: '1/5' };

    return (
      // <div style={divStyle}>
      //   {username ? <h1>{`Hello ${username} (i.e. me)`}</h1> : <h1>Loading.. please wait!</h1>}
      //   <img src={nodejs_icon} alt="react" height="80"/>
      //   <img src={express_icon} alt="react" height="80"/>
      //   <img src={react_icon} alt="react" height="80"/>
      //   <img src={webpack_icon} alt="react" height="80"/>
      // </div>
      <Box>
        <h3>Server state:</h3>
        <Container isFluid>
          <Columns isCentered isMultiline>
            <Column isSize={columnSize}>
              <CardHeader>
                <CardHeaderTitle>Component</CardHeaderTitle>
                <CardHeaderIcon>
                  <FontAwesomeIcon icon={faTimes}/>
                </CardHeaderIcon>
              </CardHeader>
              <Notification isColor="success" hasTextAlign="centered"> isOneThird </Notification>
            </Column>
            <Column isSize={columnSize}>
              <Notification isColor="warning" hasTextAlign="centered"> 2 </Notification>
            </Column>
            <Column isSize={columnSize}>
              <Notification isColor="danger" hasTextAlign="centered"> Third column </Notification>
            </Column>
            <Column isSize={columnSize}>
              <Notification isColor="primary" hasTextAlign="centered"> Fourth column </Notification>
            </Column>
          </Columns>
        </Container>
      </Box>
    );
  }
}
