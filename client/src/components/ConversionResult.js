import React, { useState, useRef, useEffect, useContext, useCallback } from "react";
import { Navigate } from 'react-router-dom';
import { SvgContext } from "./SvgProvider";
import axios from "axios";
import debounce from "lodash/debounce";

function ConversionResult() {
  const { svg, setSvg } = useContext(SvgContext);
  const [scale, setScale] = useState(1);
  const svgContainerRef = useRef(null);
  const [tooltip, setTooltip] = useState({
    visible: false,
    x: 0,
    y: 0,
    elementId: null,
    elementType: null,
    elementXml: null,
  });

  // Дебонсим обработчик wheel для оптимизации производительности
  const handleWheel = useCallback(
    debounce((e) => {
      e.preventDefault();
      const delta = e.deltaY > 0 ? -0.1 : 0.1;
      setScale((prev) => Math.max(0.1, Math.min(5, prev + delta)));
    }, 50),
    []
  );

  // Мемоизация обработчиков
  const handleZoomIn = useCallback(() => {
    setScale((prev) => Math.min(prev * 1.2, 5));
  }, []);

  const handleZoomOut = useCallback(() => {
    setScale((prev) => Math.max(prev / 1.2, 0.1));
  }, []);

  const handleSvgClick = useCallback((e) => {
    const target = e.target;
    if (
      (target.localName === "rect" || target.localName === "text" || target.localName === "path") &&
      target.id
    ) {
      const container = svgContainerRef.current;
      const containerRect = container.getBoundingClientRect();
      const bbox = target.getBoundingClientRect();
      // Учитываем масштаб и прокрутку контейнера
      const adjustedX = (bbox.left + bbox.width / 2 - containerRect.left + container.scrollLeft) / scale;
      const adjustedY = (bbox.top + bbox.height / 2 - containerRect.top + container.scrollTop) / scale - 40 / scale;
      // Ограничиваем позицию tooltip'а, чтобы он не выходил за пределы контейнера
      const tooltipWidth = 120; // Примерная ширина tooltip'а
      const tooltipHeight = 100; // Примерная высота tooltip'а
      const maxX = containerRect.width / scale - tooltipWidth;
      const maxY = containerRect.height / scale - tooltipHeight;
      const finalX = Math.max(0, Math.min(adjustedX, maxX));
      const finalY = Math.max(-40 / scale, Math.min(adjustedY, maxY));

      setTooltip({
        visible: true,
        x: finalX,
        y: finalY,
        elementId: target.id,
        elementType: target.localName,
        elementXml: target.outerHTML,
      });
    } else {
      setTooltip((prev) => ({ ...prev, visible: false }));
    }
  }, [scale]);

  const handleMove = useCallback(
    async (dx, dy) => {
      if (!tooltip.elementId) return;
      const cacheKey = localStorage.getItem("cacheKey");
      if (!cacheKey) {
        alert("Кэш-ключ отсутствует");
        return;
      }
      try {
        const response = await axios.post("https://localhost:7214/api/main/margin", null, {
          params: { cacheKey, elementId: tooltip.elementId, dx, dy },
        });
        if (response.data) setSvg(response.data);
        alert(`Место ${tooltip.elementId} сдвинуто`);
      } catch {
        alert("Ошибка сдвига");
      }
    },
    [tooltip.elementId, setSvg]
  );

  const handleDelete = useCallback(async () => {
    if (!tooltip.elementXml) return;
    const cacheKey = localStorage.getItem("cacheKey");
    if (!cacheKey) {
      alert("Кэш-ключ отсутствует");
      return;
    }
    try {
      const response = await axios.post("https://localhost:7214/api/main/delete", null, {
        params: { cacheKey, elementName: tooltip.elementXml },
      });
      alert("Элемент удалён");
      if (response.data) setSvg(response.data);
      setTooltip((prev) => ({ ...prev, visible: false }));
    } catch {
      alert("Ошибка удаления");
    }
  }, [tooltip.elementXml, setSvg]);

  // Обработка событий
  useEffect(() => {
    const container = svgContainerRef.current;
    if (!container) return;

    container.addEventListener("wheel", handleWheel, { passive: false });
    const handleClickOutside = (e) => {
      if (container && !container.contains(e.target)) {
        setTooltip((prev) => ({ ...prev, visible: false }));
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      container.removeEventListener("wheel", handleWheel);
      document.removeEventListener("mousedown", handleClickOutside);
      handleWheel.cancel(); // Очистка дебонсера
    };
  }, [handleWheel]);

  // Удаление width/height у SVG и установка начальных размеров
  useEffect(() => {
    const container = svgContainerRef.current;
    if (!container) return;
    const svgElement = container.querySelector("svg");
    if (svgElement) {
      svgElement.removeAttribute("width");
      svgElement.removeAttribute("height");
      svgElement.style.minWidth = "100%";
      svgElement.style.minHeight = "100%";
    }
  }, [svg]);

  // Динамическое обновление размеров контейнера для прокрутки
  useEffect(() => {
    const container = svgContainerRef.current;
    if (!container) return;
    const innerDiv = container.firstChild;
    if (innerDiv) {
      const svgElement = innerDiv.querySelector("svg");
      if (svgElement) {
        const bbox = svgElement.getBBox();
        innerDiv.style.width = `${bbox.width * scale}px`;
        innerDiv.style.height = `${bbox.height * scale}px`;
      }
    }
  }, [scale, svg]);

  if (!svg) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="container mt-4">
      <h4 className="text-center">Предпросмотр схемы</h4>
      <div className="d-flex justify-content-center mb-3">
        <button className="btn btn-secondary me-2" onClick={handleZoomIn}>
          +
        </button>
        <button className="btn btn-secondary" onClick={handleZoomOut}>
          -
        </button>
      </div>

      <div
        ref={svgContainerRef}
        style={{
          width: "600px",
          height: "600px",
          border: "1px solid #ccc",
          overflow: "auto",
          margin: "0 auto",
          background: "#fff",
          position: "relative",
          willChange: "transform, scroll-position",
          touchAction: "pan-x pan-y pinch-zoom",
        }}
        onClick={handleSvgClick}
      >
        <div
          style={{
            transform: `scale(${scale})`,
            transformOrigin: "center center",
            transition: "transform 0.1s ease-out",
            display: "inline-block",
            minWidth: "100%",
            minHeight: "100%",
          }}
        >
          <div
            dangerouslySetInnerHTML={{ __html: svg }}
            style={{
              display: "inline-block",
              pointerEvents: "all",
              width: "100%",
              height: "100%",
            }}
          />
        </div>

        {tooltip.visible && (
          <div
            style={{
              position: "absolute",
              left: tooltip.x,
              top: tooltip.y,
              background: "#fff",
              border: "1px solid #333",
              borderRadius: "6px",
              padding: "10px",
              zIndex: 1000,
              minWidth: "120px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
              pointerEvents: "all",
              transform: `scale(${1 / scale})`,
              transformOrigin: "top left",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-2">
              <strong>ID:</strong> {tooltip.elementId}
            </div>
            <div className="mb-2">
              <strong>Тип:</strong> {tooltip.elementType}
            </div>
            <div className="d-flex justify-content-between mb-2">
              <button
                className="btn btn-sm btn-outline-primary"
                onClick={() => handleMove(-10, 0)}
              >
                ←
              </button>
              <button
                className="btn btn-sm btn-outline-primary"
                onClick={() => handleMove(10, 0)}
              >
                →
              </button>
            </div>
            <div className="d-flex justify-content-between">
              <button
                className="btn btn-sm btn-outline-primary"
                onClick={() => handleMove(0, -10)}
              >
                ↑
              </button>
              <button
                className="btn btn-sm btn-outline-danger"
                onClick={handleDelete}
              >
                🗑
              </button>
              <button
                className="btn btn-sm btn-outline-primary"
                onClick={() => handleMove(0, 10)}
              >
                ↓
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default ConversionResult;