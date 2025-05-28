import React, { useState, useRef, useEffect, useContext } from "react";
import { Navigate } from 'react-router-dom';
import { SvgContext } from "./SvgProvider";
import axios from "axios";

function ConversionResult() {
  const { svg, setSvg } = useContext(SvgContext);
  const [scale, setScale] = useState(1.1);
  const svgContainerRef = useRef(null);
  const [tooltip, setTooltip] = useState({
    visible: false,
    x: 0,
    y: 0,
    elementId: null,
    elementType: null,
    elementXml: null,
  });
  const handleDownloadArchive = async () => {
  const cacheKey = localStorage.getItem("cacheKey");
  if (!cacheKey) {
    alert("Кэш-ключ отсутствует!");
    return;
  }
  try {
    const response = await axios.get(
      `https://localhost:7214/api/main/download`,
      {
        params: { cacheKey },
        responseType: "blob"
      }
    );
    // Получаем имя файла из заголовка Content-Disposition, если оно есть
    let fileName = "converted.zip";
    const disposition = response.headers["content-disposition"];
    if (disposition && disposition.includes("filename=")) {
      const match = disposition.match(/filename="?([^"]+)"?/);
      if (match && match[1]) fileName = match[1];
    }
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(url);
  } catch (e) {
    alert("Ошибка скачивания: " + e.message);
  }
};
  const handleZoomIn = () => setScale(prev => Math.min(prev + 0.1, 5));
  const handleZoomOut = () => setScale(prev => Math.max(prev - 0.1, 0.1));

  const handleSvgClick = (e) => {
  const target = e.target;

  if (
    target.localName === "rect" ||
    target.localName === "text" ||
    target.localName === "path" ||
    target.localName === "circle"
  ) {
    const container = svgContainerRef.current;
    const svgElement = container.querySelector("svg");
    if (!svgElement) return;

    // Получаем координаты клика относительно svg с учетом transform
    const pt = svgElement.createSVGPoint();
    pt.x = e.clientX;
    pt.y = e.clientY;
    const svgP = pt.matrixTransform(svgElement.getScreenCTM().inverse());

    setTooltip({
      visible: true,
      // Не умножаем на scale, координаты уже в системе svg
      x: svgP.x + 10,
      y: svgP.y + 10,
      elementId: target.id,
      elementType: target.localName,
      elementXml: target.outerHTML,
    });
  } else {
    setTooltip(prev => ({ ...prev, visible: false }));
  }
};

  const handleMove = async (dx, dy = 0) => {
  if (!tooltip.elementXml) return;

  const cacheKey = localStorage.getItem("cacheKey");
  if (!cacheKey) return alert("Кэш-ключ отсутствует");

  // Получаем id и тип элемента из XML
  const parser = new DOMParser();
  const doc = parser.parseFromString(tooltip.elementXml, "image/svg+xml");
  const elem = doc.documentElement;
  const id = elem.getAttribute("id");
  const type = elem.tagName;
  if (!id) return alert("Элемент не содержит ID.");

  try {
    // Передаём только id, dx, dy, type
    const response = await axios.post("https://localhost:7214/api/main/margin", null, {
      params: {
        cacheKey,
        elementId: id,
        dx,
        dy
      }
    });

    if (response.data && typeof response.data === "string" && response.data.includes("<svg")) {
      setSvg(response.data);

      // Локально обновляем координаты
      if (elem) {
        const updateAttr = (attr, delta) => {
          if (!elem.hasAttribute(attr)) return;
          const value = parseFloat(elem.getAttribute(attr)) || 0;
          elem.setAttribute(attr, (value + delta).toFixed(3));
        };

        switch (type) {
          case "rect":
          case "image":
            updateAttr("x", dx);
            updateAttr("y", dy);
            break;
          case "circle":
            updateAttr("cx", dx);
            updateAttr("cy", dy);
            break;
          case "text":
            updateAttr("x", dx);
            updateAttr("y", dy);
            break;
          case "path":
            let transform = elem.getAttribute("transform") || "";
            const translateMatch = transform.match(/translate\(([^)]+)\)/);
            let tx = 0, ty = 0;
            if (translateMatch) {
              [tx, ty] = translateMatch[1].split(",").map(parseFloat);
            }
            transform = transform.replace(/translate\([^)]+\)/, "").trim();
            transform += ` translate(${(tx + dx).toFixed(3)},${(ty + dy).toFixed(3)})`;
            elem.setAttribute("transform", transform.trim());
            break;
          default:
            console.warn(`Тип элемента '${type}' не поддерживается.`);
            break;
        }

        setTooltip(prev => ({
          ...prev,
          elementXml: elem.outerHTML // Обновляем XML с ID
        }));
      }
    } else {
      alert("Получен некорректный SVG от сервера");
    }
  } catch (error) {
    alert("Ошибка сдвига: " + error.message);
  }
};



  const handleDelete = async () => {
    if (!tooltip.elementXml) return;
    const cacheKey = localStorage.getItem("cacheKey");
    if (!cacheKey) return alert("Кэш-ключ отсутствует");

    try {
      const response = await axios.post("https://localhost:7214/api/main/delete", null, {
        params: { cacheKey, elementName: tooltip.elementXml }
      });
      if (response.data && typeof response.data === "string" && response.data.includes("<svg")) {
        setSvg(response.data);
      } else {
        alert("Получен некорректный SVG от сервера");
      }
      setTooltip(prev => ({ ...prev, visible: false }));
    } catch (error) {
      alert("Ошибка удаления: " + error.message);
    }
  };

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (svgContainerRef.current && !svgContainerRef.current.contains(e.target)) {
        setTooltip(prev => ({ ...prev, visible: false }));
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  useEffect(() => {
  const container = svgContainerRef.current;
  if (!container || !svg) return;

  const svgElement = container.querySelector("svg");
  if (svgElement) {
    // Не трогаем viewBox, если он уже есть
    if (!svgElement.hasAttribute("viewBox")) {
      try {
        const bbox = svgElement.getBBox?.();
        if (bbox && bbox.width > 0 && bbox.height > 0) {
          svgElement.setAttribute("viewBox", `0 0 ${bbox.width} ${bbox.height}`);
        } else {
          svgElement.setAttribute("viewBox", "0 0 800 600");
        }
      } catch {
        svgElement.setAttribute("viewBox", "0 0 800 600");
      }
    }
    // width/height можно не трогать, если используете scale
  }
}, [svg]);

  if (!svg) return <Navigate to="/" replace />;

  return (
    <div className="container mt-4">
      <div className="d-flex justify-content-center gap-2 mb-3">
        <button className="btn btn-outline-primary" onClick={handleDownloadArchive}>Скачать архив</button>      
      </div>
    <h4 className="text-center">Предпросмотр схемы</h4>
    <div className="d-flex justify-content-center mb-3">
      <button className="btn btn-secondary me-2" onClick={handleZoomIn}>+</button>
      <button className="btn btn-secondary" onClick={handleZoomOut}>-</button>
    </div>

    <div
      ref={svgContainerRef}
      style={{
        width: "800px",
        height: "600px",
        border: "1px solid #ccc",
        overflow: "scroll",
        margin: "0 auto",
        background: "#fff",
        position: "relative",
        pointerEvents: "all"
      }}
      onClick={handleSvgClick}
    >
      <div
        style={{
          transform: `scale(${scale})`,
          transformOrigin: "top left",
          display: "inline-block",
          minWidth: "800px",
          minHeight: "600px"
        }}
      >
        {svg.includes("<svg") ? (
          <div dangerouslySetInnerHTML={{ __html: svg }} />
        ) : (
          <svg width="800" height="600" viewBox="0 0 800 600">
            <text x="10" y="50" fill="red">Ошибка: SVG пустой или некорректный</text>
          </svg>
        )}
      </div>

      {tooltip.visible && (
        <div
          style={{
            position: "absolute",
            left: tooltip.x * scale,
            top: tooltip.y * scale,
            background: "#fff",
            border: "1px solid #333",
            borderRadius: "6px",
            padding: "10px",
            zIndex: 9999,
            minWidth: "130px",
            boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
            pointerEvents: "auto"
          }}
          onClick={e => e.stopPropagation()}
        >
          <div className="mb-2"><strong>ID:</strong> {tooltip.elementId}</div>
          <div className="mb-2"><strong>Тип:</strong> {tooltip.elementType}</div>
          <div className="d-flex justify-content-between mb-1">
            <button className="btn btn-sm btn-outline-secondary" onClick={() => handleMove(0, -1)}>↑</button>
            <button className="btn btn-sm btn-outline-danger" onClick={handleDelete}>🗑</button>
            <button className="btn btn-sm btn-outline-secondary" onClick={() => handleMove(0, 1)}>↓</button>
          </div>
          <div className="d-flex justify-content-between">
            <button className="btn btn-sm btn-outline-primary" onClick={() => handleMove(-1, 0)}>←</button>
            <button className="btn btn-sm btn-outline-primary" onClick={() => handleMove(1, 0)}>→</button>
          </div>
        </div>
      )}
    </div>
  </div>
  );
}

export default ConversionResult;
