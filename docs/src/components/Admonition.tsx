import React from "react";

export type AdmonitionType = "important" | "warning" | "note";

interface AdmonitionProps {
  type: AdmonitionType;
  children: React.ReactNode;
}

export default function Admonition({ type, children }: AdmonitionProps) {
  return (
    <div className={`admonition admonition-${type} alert alert--${type}`}>
      <div className="admonition-heading">
        <h5>{type.charAt(0).toUpperCase() + type.slice(1)}</h5>
      </div>
      <div className="admonition-content">
        {children}
      </div>
    </div>
  );
}

export function Important({ children }: { children: React.ReactNode }) {
  return <Admonition type="important">{children}</Admonition>;
}

export function Warning({ children }: { children: React.ReactNode }) {
  return <Admonition type="warning">{children}</Admonition>;
}

export function Note({ children }: { children: React.ReactNode }) {
  return <Admonition type="note">{children}</Admonition>;
} 