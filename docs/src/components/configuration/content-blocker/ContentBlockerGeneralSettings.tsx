import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "CONTENTBLOCKER__ENABLED",
    description: [
      "Enables or disables the Content Blocker functionality.",
      "When enabled, processes all items in the *arr queue and marks unwanted files."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    examples: ["true", "false"],
  },
  {
    name: "TRIGGERS__CONTENTBLOCKER",
    description: [
      "Cron schedule for the Content Blocker job."
    ],
    type: "text",
    reference: "https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html",
    defaultValue: "0 0/5 * * * ?",
    defaultValueComment: "every 5 minutes",
    required: "Only required if CONTENTBLOCKER__ENABLED is true",
    examples: ["0 0/5 * * * ?", "0 0 * * * ?", "0 0 0/1 * * ?"],
    notes: [
      "Maximum interval is 6 hours."
    ]
  },
  {
    name: "CONTENTBLOCKER__IGNORED_DOWNLOADS_PATH",
    description: [
      "Local path to the file containing downloads to be ignored from being processed by Cleanuperr.",
      "If the contents of the file are changed, they will be reloaded on the next job run.",
      "This file is not automatically created, so you need to create it manually.",
      {
        type: "list",
        title:
          "Accepted values inside the file (each value needs to be on a new line):",
        content: [
          "torrent hash",
          "qBitTorrent tag or category",
          "Deluge label",
          "Transmission category (last directory from the save location)",
          "torrent tracker domain",
        ],
      },
      {
        type: "code",
        title: "Example of file contents:",
        content: `fa800a7d7c443a2c3561d1f8f393c089036dade1
tv-sonarr
qbit-tag
mytracker.com
...`,
      },
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["/ignored.txt", "/config/ignored.txt"],
    warnings: [
      "Some people have experienced problems using Docker where the mounted file would not update inside the container if it was modified on the host. This is a Docker configuration problem and can not be solved by cleanuperr.",
    ],
  },
  {
    name: "CONTENTBLOCKER__IGNORE_PRIVATE",
    description: [
      "Controls whether to ignore downloads from private trackers from being processed by Cleanuperr."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "CONTENTBLOCKER__DELETE_PRIVATE",
    description: [
      "Controls whether to delete private downloads that have all files blocked from the download client.",
      "Has no effect if CONTENTBLOCKER__IGNORE_PRIVATE is true."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
    important: [
      "Setting CONTENTBLOCKER__DELETE_PRIVATE=true means you don't care about seeding, ratio, H&R and potentially losing your private tracker account."
    ]
  }
];

export default function ContentBlockerGeneralSettings() {
  return <EnvVars vars={settings} />;
} 