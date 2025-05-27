import React, { createContext, useState } from "react";

export const SvgContext = createContext();

export function SvgProvider({ children }) {
  const [svg, setSvg] = useState("");
  return (
    <SvgContext.Provider value={{ svg, setSvg }}>
      {children}
    </SvgContext.Provider>
  );
}