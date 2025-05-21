import 'bootstrap/dist/css/bootstrap.min.css';
import Upload from './components/Upload';
import React from 'react';
import './index.scss'
import axios from 'axios';
import ConversionResult from './components/ConversionResult'; 
function App() {
  
  return (
    <div className="App">
      <div className="text-center">
        <h1 className="display-4">Конвертер Залов</h1>
      </div>
      <>
        <Upload />
      </>
    </div>
  );
}

export default App;
