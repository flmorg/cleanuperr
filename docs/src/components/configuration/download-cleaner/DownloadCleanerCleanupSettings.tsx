import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "DOWNLOADCLEANER__CATEGORIES__0__NAME",
    description: ["Name of the category to clean."],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["tv-sonarr", "movies-radarr", "music-lidarr"],
    notes: [
      "The category name must match the category that was set in the *arr.",
      "For qBittorrent, the category name is the name of the download category.",
      "For Deluge, the category name is the name of the label.",
      "For Transmission, the category name is the last directory from the save location.",
    ],
  },
  {
    name: "DOWNLOADCLEANER__CATEGORIES__0__MAX_RATIO",
    description: ["Maximum ratio to reach before removing a download."],
    type: "decimal number",
    defaultValue: "-1",
    required: false,
    examples: ["-1", "1.0", "2.0", "3.0"],
    notes: ["`-1` means no limit/disabled."],
  },
  {
    name: "DOWNLOADCLEANER__CATEGORIES__0__MIN_SEED_TIME",
    description: [
      "Minimum number of hours to seed before removing a download, if the ratio has been met.",
      "Used with `MAX_RATIO` to ensure a minimum seed time.",
    ],
    type: "positive decimal number",
    defaultValue: "0",
    required: false,
    examples: ["0", "24", "48", "72"],
  },
  {
    name: "DOWNLOADCLEANER__CATEGORIES__0__MAX_SEED_TIME",
    description: [
      "Maximum number of hours to seed before removing a download.",
    ],
    type: "decimal number",
    defaultValue: "-1",
    required: false,
    examples: ["-1", "24", "48", "72"],
    notes: ["`-1` means no limit/disabled."],
  },
];

export default function DownloadCleanerCleanupSettings() {
  return <EnvVars vars={settings} />;
}
