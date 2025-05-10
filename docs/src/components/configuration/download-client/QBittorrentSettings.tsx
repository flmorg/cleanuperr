import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "QBITTORRENT__URL",
    description: [
      "URL of the qBittorrent instance."
    ],
    type: "text",
    defaultValue: "http://localhost:8080",
    required: false,
    examples: ["http://localhost:8080", "http://192.168.1.100:8080", "https://mydomain.com:8080"],
  },
  {
    name: "QBITTORRENT__URL_BASE",
    description: [
      "Adds a prefix to the qBittorrent url, such as `[QBITTORRENT__URL]/[QBITTORRENT__URL_BASE]/api`."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "QBITTORRENT__USERNAME",
    description: [
      "Username for qBittorrent authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "QBITTORRENT__PASSWORD",
    description: [
      "Password for qBittorrent authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
];

export default function QBittorrentSettings() {
  return <EnvVars vars={settings} />;
} 