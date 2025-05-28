import React, { useState, useEffect } from "react";
import { useContext } from "react";
import { SvgContext } from "./SvgProvider";
import axios from "axios";

function Edit() {
  const { svg } = useContext(SvgContext);
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
  useEffect(() => {
  if (!svg) return;
  const parser = new DOMParser();
  const doc = parser.parseFromString(svg, "image/svg+xml");
  // Найти все <g id="places">
  const placesGroup = doc.querySelector('g[id="places"]');
  if (placesGroup) {
    // Найти первый <rect> внутри этой группы (или изменить на нужную логику)
    const rect = placesGroup.querySelector("rect");
    if (rect) {
      const width = parseFloat(rect.getAttribute("width")) || 0;
      const height = parseFloat(rect.getAttribute("height")) || 0;
      setPlaceSizeWidth(width);
      setPlaceSizeHeight(height);
    }
  }
}, [svg]);
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!cacheKey) {
      alert("Кэш-ключ отсутствует!");
      return;
    }
    try {
      console.log("cacheKey:", cacheKey);
      const response = await axios.post(
        "https://localhost:7214/api/main/edit",
        null,
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
          <div className="w-100 mb-3">
          <label className="form-label mb-2"><strong>Коррекция положения места</strong></label>
          <div className="d-flex gap-3">
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
          </div>
        </div>
          <div className="w-100 mb-3">
            <label className="form-label mb-2"><strong>Коррекция размера места</strong></label>
            <div className="d-flex gap-3">
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
            </div>
        </div>
          <div>
            <label htmlFor="cornerRadius" className="form-label">Радиус скругления</label>
            <input
              type="number"
              className="form-control"
              id="cornerRadius"
              step="0.1"
              value={cornerRadius}
              onChange={e => setCornerRadius(Number(e.target.value))}
            />
          </div>
          <div className="w-100">
    <label className="form-label mb-2"><strong>Настройка шрифта</strong></label>
    <div className="d-flex gap-3">
      <div>
      <label htmlFor="fontSize" className="form-label">Размер шрифта</label>
      <input
        type="number"
        className="form-control"
        id="fontSize"
        value={fontSize}
        step="0.1"
        onChange={e => setFontSize(Number(e.target.value))}
      />
    </div>
      <div>
        <label htmlFor="fontWeight" className="form-label">Толщина шрифта</label>
        <select
          className="form-select"
          id="fontWeight"
          value={fontWeight}
          onChange={e => setFontWeight(e.target.value)}
        >
          <option value="600">Жирный</option>
          <option value="500">Обычный</option>
        </select>
      </div>
    </div>
  </div>
        </div>
        <button type="submit" className="btn btn-primary w-100">Принять</button>
      </form>
    </div>
  );
}

export default Edit;