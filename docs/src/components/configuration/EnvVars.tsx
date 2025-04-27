import React, { useEffect, useRef } from "react";
import ReactMarkdown from 'react-markdown';
import { useLocation } from "@docusaurus/router";
import Admonition from "../Admonition";

export type DescriptionContent =
  | string
  | {
    type: "text" | "code" | "list";
    title: string;
    content: string | string[];
  };

export interface EnvVarProps {
  name: string;
  description: DescriptionContent[];
  type: string;
  reference?: string;
  required?: boolean | string;
  defaultValue?: string;
  examples?: string[];
  children?: React.ReactNode;
  notes?: string[];
  important?: string[];
  warnings?: string[];
}

interface EnvVarsProps {
  vars: EnvVarProps[];
}

export default function EnvVars({ vars }: EnvVarsProps) {
  return vars.map((env) => <EnvVar key={env.name} env={env} />);
}

function EnvVar({ env }: { env: EnvVarProps }) {
  const ref = useRef<HTMLDivElement>(null);
  const location = useLocation();

  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const queryKeys = Array.from(searchParams.keys());

    const matched = queryKeys.find(
      (key) => key.toLowerCase() === env.name.toLowerCase()
    );

    if (matched && ref.current) {
      // Scroll to the variable
      ref.current.scrollIntoView({ behavior: "smooth", block: "start" });

      // Add highlight effect
      ref.current.classList.add("env-var-highlight");

      setTimeout(() => {
        ref.current.classList.add("highlight-removing");
      }, 2000);

      setTimeout(() => {
        ref.current.classList.remove("env-var-highlight", "highlight-removing");
      }, 3000);
    }
  }, [location.search, env.name]);

  const renderDescriptionContent = (
    content: DescriptionContent,
    index: number
  ) => {
    if (typeof content === "string") {
      return <ReactMarkdown components={{ p: ({ children }) => <>{children}</> }}>{content}</ReactMarkdown>;
    }

    switch (content.type) {
      case "text":
        return <ReactMarkdown components={{ p: ({ children }) => <>{children}</> }}>{content.content}</ReactMarkdown>;
      case "code":
        return (
          <section>
            {content.title && <strong>{content.title}</strong>}
            <br />
            <pre key={index}>
              <code>{content.content}</code>
            </pre>
          </section>
        );
      case "list":
        return (
          <section>
            {content.title && <strong>{content.title}</strong>}
            <br />
            <ul key={index}>
              {(Array.isArray(content.content)
                ? content.content
                : [content.content]
              ).map((item, i) => (
                <li key={i}>{item}</li>
              ))}
            </ul>
          </section>
        );
      default:
        return null;
    }
  };

  const renderAdmonition = (type: "important" | "warning" | "note", items: string[]) => {
    if (!items || items.length === 0) return null;

    return (
      <Admonition type={type}>
        <ReactMarkdown components={{ p: ({ children }) => <>{children}</> }}>
          {items.join("\n\n")}
        </ReactMarkdown>
      </Admonition>
    );
  };

  return (
    <div id={env.name} ref={ref} className="env-var-block">
      <h3>
        <code>{env.name}</code>
      </h3>
      {env.description.map((desc, index) =>
        renderDescriptionContent(desc, index)
      )}
      <ul style={{ listStyle: "none", paddingLeft: 0 }}>
        {env.required !== undefined && (
          <li>
            <strong>Required:</strong>{" "}
            {typeof env.required === "boolean"
              ? env.required
                ? "Yes"
                : "No"
              : env.required}
          </li>
        )}
        {env.defaultValue !== undefined && (
          <li>
            <strong>Default:</strong> <code>{env.defaultValue}</code>
          </li>
        )}
      </ul>
      {env.examples && env.examples.length > 0 && (
        <section>
          <strong>Examples:</strong>
          <ul>
            {env.examples.map((example, index) => (
              <li key={index}>
                <code>{example}</code>
              </li>
            ))}
          </ul>
        </section>
      )}

      {env.notes && renderAdmonition("note", env.notes)}
      {env.important && renderAdmonition("important", env.important)}
      {env.warnings && renderAdmonition("warning", env.warnings)}

      <div style={{ marginTop: "0.5rem" }}>{env.children}</div>
      <hr />
    </div>
  );
}
