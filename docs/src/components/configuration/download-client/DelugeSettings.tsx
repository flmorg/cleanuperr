import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "DELUGE__URL",
    description: [
      "URL of the Deluge instance."
    ],
    type: "text",
    defaultValue: "http://localhost:8112",
    required: false,
    examples: ["http://localhost:8112", "http://192.168.1.100:8112", "https://mydomain.com:8112"],
  },
  {
    name: "DELUGE__URL_BASE",
    description: [
      "Adds a prefix to the deluge json url, such as `[DELUGE__URL]/[DELUGE__URL_BASE]/json`."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "DELUGE__PASSWORD",
    description: [
      "Password for Deluge authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
];

export default function DelugeSettings() {
  return <EnvVars vars={settings} />;
} 