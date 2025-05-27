import React from "react";
import Edit from "./Edit";
import ConversionResult from "./ConversionResult";

function ResultPage() {
  return (
    <div style={{ display: "flex", height: "100vh" }}>
      <div style={{ flex: "0 0 40%", overflow: "auto" }}>
        <Edit />
      </div>
      <div style={{ flex: "0 0 60%"}}>
        <ConversionResult />
      </div>
    </div>
  );
}

export default ResultPage;