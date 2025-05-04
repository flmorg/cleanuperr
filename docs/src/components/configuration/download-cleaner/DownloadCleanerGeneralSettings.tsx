import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "DOWNLOADCLEANER__ENABLED",
    description: [
      "Enables or disables the Download Cleaner functionality.",
      "When enabled, automatically cleans up downloads that have been seeding for a certain amount of time."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "TRIGGERS__DOWNLOADCLEANER",
    description: [
      "Cron schedule for the Download Cleaner job."
    ],
    type: "text",
    reference: "https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html",
    defaultValue: "0 0 * * * ?",
    defaultValueComment: "every hour",
    required: false,
    notes: [
      "Maximum interval is 6 hours."
    ]
  },
  {
    name: "DOWNLOADCLEANER__IGNORED_DOWNLOADS_PATH",
    description: [
      "Local path to the file containing ignored downloads.",
      "If the contents of the file are changed, they will be reloaded on the next job run.",
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
    name: "DOWNLOADCLEANER__DELETE_PRIVATE",
    description: [
      "Controls whether to delete private downloads."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
    important: [
      "Setting `DOWNLOADCLEANER__DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account."
    ]
  }
];

export default function DownloadCleanerGeneralSettings() {
  return <EnvVars vars={settings} />;
} 