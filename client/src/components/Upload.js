import React from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import axios from "axios";
import { useState } from "react";
function Upload({ setStatus, setSubmitted }) {
  const [file, setFile] = useState(null);
  const handleFileChange = async (e) => {
    const selectedFile = e.target.files[0];
    if (!selectedFile) return;
    setFile(selectedFile);
  
    const formData = new FormData();
    formData.append("file", selectedFile);
  
    try {
      const response = await axios.post("https://localhost:7214/api/main/upload", formData);
      const cache = response.data.cacheKey;
      alert("Файл успешно загружен. Кэш: " + cache);
      //setStatus("Файл загружен");
      setSubmitted(true); // если нужно показать UI после загрузки
    } catch (error) {
      console.error(error);
      //setStatus("Ошибка загрузки файла");
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
            defaultChecked
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
            defaultChecked
          />
          <label htmlFor="clearCssCheckbox" className="form-check-label">
            Очистить CSS-стили и все лишние атрибуты
          </label>
        </div>

        <div className="mb-3">
          <label htmlFor="customCssTextarea">Введите альтернативный CSS:</label>
          <textarea
            id="customCssTextarea"
            rows="7"
            className="form-control"
            defaultValue={`g {
  fill: #fff;
  stroke: #000;
}
#places .place text {
  font-size: 9px;
}`}
          />
        </div>

        <b>Коррекция нумерации мест:</b>
        <div className="mb-3 d-flex align-items-center gap-3">
          <span>По горизонтали</span>
          <input
            type="number"
            className="form-control"
            style={{ width: "120px" }}
          />
          <span>По вертикали</span>
          <input
            type="number"
            className="form-control"
            style={{ width: "120px" }}
          />
        </div>

        <b>Коррекция размера мест:</b>
        <div className="mb-3 d-flex align-items-center gap-3">
          <span>По ширине</span>
          <input
            type="number"
            className="form-control"
            style={{ width: "120px" }}
          />
          <span>По высоте</span>
          <input
            type="number"
            className="form-control"
            style={{ width: "120px" }}
          />
        </div>

        <button
          type="button"
          className="btn btn-primary"
          
        >
          Конвертировать
        </button>
      </form>
    </div>
  );
}

export default Upload;
