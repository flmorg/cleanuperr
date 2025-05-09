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
];

export default function DownloadClientSettings() {
  return <EnvVars vars={settings} />;
} 