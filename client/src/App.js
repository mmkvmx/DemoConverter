import 'bootstrap/dist/css/bootstrap.min.css';
import Upload from './components/Upload';
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import React from 'react';
import './index.scss'
import axios from 'axios';
import ConversionResult from './components/ConversionResult'; 
import ResultPage from './components/ResultPage';
import { SvgProvider } from "./components/SvgProvider";


function App() {
  
  return (
    <BrowserRouter>
    <SvgProvider>
    <div className="App">
      <div className="text-center">
        <h1 className="display-4">Конвертер Залов</h1>
      </div>
      <Routes>
        <Route path="/" element={<Upload />} />
        <Route path="/result" element={<ResultPage />} />
      </Routes>
    </div>
    </SvgProvider>
    </BrowserRouter>
  );
}

export default App;
