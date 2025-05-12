import React from "react";

function ConversionResult({
  resultZipFilePathWeb,
  resultSchemeFilePathWeb,
  resultPlacesFilePathWeb,
  resultSectorsFilePathWeb,
}) {
  return (
    <div className="container mt-4">
      <h3 className="mt-4 text-center">Сохранить исходные материалы</h3>
      <div className="d-flex justify-content-center gap-3 flex-wrap mt-3">
        <a href={resultZipFilePathWeb} download className="btn btn-outline-primary">
            Скачать архив целиком
        </a>
        <a href={resultSchemeFilePathWeb} download className="btn btn-outline-primary">
            Скачать схему
        </a>
        <a href={resultPlacesFilePathWeb} download className="btn btn-outline-primary">
            Скачать файл мест
        </a>
        <a href={resultSectorsFilePathWeb} download className="btn btn-outline-primary">
            Скачать файл секторов
        </a>
    </div>
      <div className="container mt-4 text-center">
        <h4 className="mt-4">Предпросмотр схемы</h4>
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
          <div>
            <button className="btn btn-secondary me-2">+</button>
            <button className="btn btn-secondary">-</button>
          </div>
          
        </div>
        
      </div>
    </div>
  );
}

export default ConversionResult;
