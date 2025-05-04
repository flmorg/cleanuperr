import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "DOWNLOAD_CLIENT",
    description: [
      "Specifies which download client is used by *arrs."
    ],
    type: "text",
    defaultValue: "none",
    required: false,
    acceptedValues: ["none", "qbittorrent", "deluge", "transmission", "disabled"],
    notes: [
      "Only one download client can be enabled at a time. If you have more than one download client, you should deploy multiple instances of Cleanuperr."
    ],
    warnings: [
      "When the download client is set to `disabled`, the Queue Cleaner will be able to remove items that are failed to be imported even if there is no download client configured. This means that all downloads, including private ones, will be completely removed.",
      "Setting `DOWNLOAD_CLIENT=disabled` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account."
    ]
  },
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
    name: "QBITTORRENT__PASSWORD",
    description: [
      "Password for qBittorrent authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
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
  {
    name: "TRANSMISSION__URL",
    description: [
      "URL of the Transmission instance."
    ],
    type: "text",
    defaultValue: "http://localhost:9091",
    required: false,
    examples: ["http://localhost:9091", "http://192.168.1.100:9091", "https://mydomain.com:9091"],
  },
  {
    name: "TRANSMISSION__URL_BASE",
    description: [
      "Adds a prefix to the Transmission rpc url, such as `[TRANSMISSION__URL]/[TRANSMISSION__URL_BASE]/rpc`."
    ],
    type: "text",
    defaultValue: "transmission",
    required: false,
  },
  {
    name: "TRANSMISSION__USERNAME",
    description: [
      "Username for Transmission authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "TRANSMISSION__PASSWORD",
    description: [
      "Password for Transmission authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  }
];

export default function DownloadClientSettings() {
  return <EnvVars vars={settings} />;
} 