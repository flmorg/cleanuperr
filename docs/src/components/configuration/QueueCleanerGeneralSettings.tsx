import React from "react";
import EnvVars, { EnvVarProps } from "./EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "QUEUECLEANER__ENABLED",
    description: [
      "Enables or disables the queue cleaning functionality. When enabled, processes all items in the *arr queue."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    examples: ["true", "false"],
  },
  {
    name: "TRIGGERS__QUEUECLEANER",
    description: [
      "Cron schedule for the queue cleaner job."
    ],
    type: "text",
    reference: "https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html",
    defaultValue: "0 0/5 * * * ?",
    required: "Only required if QUEUECLEANER__ENABLED is true",
    examples: ["0 0/5 * * * ?", "0 0 * * * ?", "0 0 0/1 * * ?"],
    notes: [
      "Maximum interval is 6 hours.",
      "Is ignored if `QUEUECLEANER__RUNSEQUENTIALLY=true` and `CONTENTBLOCKER__ENABLED=true`."
    ]
  },
  {
    name: "QUEUECLEANER__IGNORED_DOWNLOADS_PATH",
    description: [
      "Local path to the file containing downloads to be ignored from being processed by Cleanuperr.",
      "If the contents of the file are changed, they will be reloaded on the next job run.",
      "This file is not automatically created, so you need to create it manually.",
      {
        type: "list",
        title: "Accepted values inside the file (each value needs to be on a new line):",
        content: [
          "torrent hash",
          "qBitTorrent tag or category",
          "Deluge label",
          "Transmission category (last directory from the save location)",
          "torrent tracker domain"
        ]
      },
      {
        type: "code",
        title: "Example of file contents:",
        content: `fa800a7d7c443a2c3561d1f8f393c089036dade1
tv-sonarr
qbit-tag
mytracker.com
...`
      }
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["/ignored.txt", "/config/ignored.txt"],
    warnings: [
      "Some people have experienced problems using Docker where the mounted file would not update inside the container if it was modified on the host. This is a Docker configuration problem and can not be solved by cleanuperr."
    ]
  },
  {
    name: "QUEUECLEANER__RUNSEQUENTIALLY",
    description: [
      "Controls whether queue cleaner runs after content blocker instead of in parallel. When true, streamlines the cleaning process by running immediately after content blocker."
    ],
    type: "boolean",
    defaultValue: "true",
    required: false,
    examples: ["true", "false"]
  }
];

export default function QueueCleanerGeneralSettings() {
  return <EnvVars vars={settings} />;
} 