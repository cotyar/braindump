import React from 'react';
import logo from './logo.svg';
//import './App.css';
import '@coreui/coreui/dist/css/coreui.min.css';

import {grpc} from "@improbable-eng/grpc-web";
import Card from './Card'

function App() {
  return ( 
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.js</code> and save to reload.
        </p>
        <Card title="Welcome!" paragraph="To this example" />
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          Learn React
        </a>
      </header>
    </div>
  );
}

export default App;
