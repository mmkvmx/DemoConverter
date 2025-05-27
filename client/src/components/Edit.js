import React, { useState } from "react";
import { useContext } from "react";
import { SvgContext } from "./SvgProvider";
import axios from "axios";
function Edit() {
  const [placeMarginGorizontal, setPlaceMarginGorizontal] = useState(0);
  const [placeMarginVertical, setPlaceMarginVertical] = useState(0);
  const [placeSizeWidth, setPlaceSizeWidth] = useState(0);
  const [placeSizeHeight, setPlaceSizeHeight] = useState(0);
  const [rectFill, setRectFill] = useState(false);
  const [cornerRadius, setCornerRadius] = useState(0);
  const [fontSize, setFontSize] = useState(9);
  const [fontWeight, setFontWeight] = useState(600);
  const cacheKey = localStorage.getItem("cacheKey");
  const { setSvg } = useContext(SvgContext);
  const handleSubmit = async (e) => {
    e.preventDefault(); // <-- предотвращает перезагрузку страницы
    if (!cacheKey) {
      alert("Кэш-ключ отсутсвует!");
      return;
    }
    try {
      console.log("cacheKey:", cacheKey);
      const response = await axios.post(
      "https://localhost:7214/api/main/edit",
      null, // тело запроса отсутствует
      {
        params: {
          cacheKey,
          placeMarginGorizontal,
          placeMarginVertical,
          placeSizeWidth,
          placeSizeHeight,
          rectFill,
          cornerRadius,
          fontSize,
          fontWeight
        },
        headers: { "Content-Type": "application/json" },
        responseType: "text",
      }
    );
      setSvg(response.data);
      alert("Конвертация завершена!");
    } catch (error) {
      alert("Ошибка конвертации");
    }
  };

  return (
    <div className="container mt-4">
      <h3 className="mt-4 text-center">Редактирование</h3>
      <form onSubmit={handleSubmit}>
        <div className="form-check align-self-end mb-2">
            <input
              type="checkbox"
              className="form-check-input"
              id="rectFill"
              checked={rectFill}
              onChange={e => setRectFill(e.target.checked)}
            />
            <label htmlFor="rectFill" className="form-check-label">
              Заливка мест
            </label>
          </div>
        <div className="d-flex gap-3 flex-wrap mb-3">
          <div>
            <label htmlFor="placeMarginGorizontal" className="form-label">Отступ по горизонтали</label>
            <input
              type="number"
              className="form-control"
              id="placeMarginGorizontal"
              value={placeMarginGorizontal}
              onChange={e => setPlaceMarginGorizontal(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="placeMarginVertical" className="form-label">Отступ по вертикали</label>
            <input
              type="number"
              className="form-control"
              id="placeMarginVertical"
              value={placeMarginVertical}
              onChange={e => setPlaceMarginVertical(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="placeSizeWidth" className="form-label">Ширина места</label>
            <input
              type="number"
              className="form-control"
              id="placeSizeWidth"
              value={placeSizeWidth}
              onChange={e => setPlaceSizeWidth(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="placeSizeHeight" className="form-label">Высота места</label>
            <input
              type="number"
              className="form-control"
              id="placeSizeHeight"
              value={placeSizeHeight}
              onChange={e => setPlaceSizeHeight(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="cornerRadius" className="form-label">Радиус скругления</label>
            <input
              type="number"
              className="form-control"
              id="cornerRadius"
              value={cornerRadius}
              onChange={e => setCornerRadius(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="fontSize" className="form-label">Размер шрифта</label>
            <input
              type="number"
              className="form-control"
              id="fontSize"
              value={fontSize}
              onChange={e => setFontSize(Number(e.target.value))}
            />
          </div>
          <div>
            <label htmlFor="fontWeight" className="form-label">Толщина шрифта</label>
            <input
              type="number"
              className="form-control"
              id="fontWeight"
              value={fontWeight}
              onChange={e => setFontWeight(Number(e.target.value))}
            />
          </div>
        </div>
        <button type="submit" className="btn btn-primary w-100">Принять</button>
      </form>
    </div>
  );
}

export default Edit;