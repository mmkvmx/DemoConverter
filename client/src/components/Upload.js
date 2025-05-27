import React, { useState, useContext } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import axios from "axios";
import { useNavigate } from 'react-router-dom';
import { SvgContext } from "./SvgProvider";

function Upload() {
  const [file, setFile] = useState(null);
  const [cacheKey, setCacheKey] = useState("");
  const [updateCircleToRect, setUpdateCircleToRect] = useState(true);
  const [clearCss, setClearCss] = useState(true);
  const navigate = useNavigate();
  const { setSvg } = useContext(SvgContext);

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    setFile(selectedFile);
    if (selectedFile) {
      handleUpload(selectedFile);
    }
  };

  const handleUpload = async (uploadFile) => {
    const fileToUpload = uploadFile || file;
    if (!fileToUpload) return;

    const formData = new FormData();
    formData.append("file", fileToUpload);

    try {
      const response = await axios.post("https://localhost:7214/api/main/upload", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });
      setCacheKey(response.data.cacheKey);
      alert("Файл загружен! cacheKey: " + response.data.cacheKey);
      localStorage.setItem("cacheKey", response.data.cacheKey);
    } catch (error) {
      alert("Ошибка загрузки файла");
    }
  };

  const handleConvert = async () => {
    if (!cacheKey) {
      alert("Сначала загрузите архив!");
      return;
    }
    try {
      const response = await axios.post(
        "https://localhost:7214/api/main/convert",
        null,
        {
          params: {
            cacheKey,
            updateCircleToRect,
            clearCss,
          },
          headers: { "Content-Type": "application/json" },
          responseType: "text",
        }
      );
      setSvg(response.data); // сохраняем svg в контекст
      alert("Конвертация завершена!");
      navigate('/result'); // просто переход, svg уже в контексте
    } catch (error) {
      alert("Ошибка конвертации");
    }
  };

  return (
    <div className="container mt-4">
      <h3 className="mt-4">Загрузите исходные материалы</h3>
      <form className="mt-3 needs-validation" noValidate>
        <div className="mb-3">
          <label htmlFor="uploadedFile" className="form-label">
            Загрузите zip-архив:
          </label>
          <input
            type="file"
            className="form-control"
            id="uploadedFile"
            accept=".zip"
            onChange={handleFileChange}
          />
        </div>

        <div className="form-check mb-3">
          <input
            type="checkbox"
            className="form-check-input"
            id="updateCircleToRectCheckbox"
            checked={updateCircleToRect}
            onChange={e => setUpdateCircleToRect(e.target.checked)}
          />
          <label htmlFor="updateCircleToRectCheckbox" className="form-check-label">
            Заменять круглые места на прямоугольные
          </label>
        </div>

        <div className="form-check mb-3">
          <input
            type="checkbox"
            className="form-check-input"
            id="clearCssCheckbox"
            checked={clearCss}
            onChange={e => setClearCss(e.target.checked)}
          />
          <label htmlFor="clearCssCheckbox" className="form-check-label">
            Очистить CSS-стили и все лишние атрибуты
          </label>
        </div>

        <button
          type="button"
          className="btn btn-primary"
          onClick={handleConvert}
        >
          Конвертировать
        </button>
      </form>
    </div>
  );
}

export default Upload;