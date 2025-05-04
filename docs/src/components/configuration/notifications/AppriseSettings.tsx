import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "APPRISE__URL",
    description: [
      "[Apprise url](https://github.com/caronc/apprise-api) where to send notifications.",
      "The `/notify` endpoint is automatically appended."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: [
        "http://localhost:8000"
    ]
  },
  {
    name: "APPRISE__KEY",
    description: ["[Apprise configuration key](https://github.com/caronc/apprise-api?tab=readme-ov-file#screenshots) containing all 3rd party notification providers which Cleanuperr would notify."],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "APPRISE__ON_IMPORT_FAILED_STRIKE",
    description: [
      "Controls whether to notify when an item receives a failed import strike.",
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "APPRISE__ON_STALLED_STRIKE",
    description: [
      "Controls whether to notify when an item receives a stalled download strike. This includes strikes for being stuck while downloading metadata.",
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "APPRISE__ON_SLOW_STRIKE",
    description: [
      "Controls whether to notify when an item receives a slow download strike. This includes strikes for having a low download speed or slow estimated finish time.",
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "APPRISE__ON_QUEUE_ITEM_DELETED",
    description: ["Controls whether to notify when a queue item is deleted."],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "APPRISE__ON_DOWNLOAD_CLEANED",
    description: ["Controls whether to notify when a download is cleaned."],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "APPRISE__ON_CATEGORY_CHANGED",
    description: [
      "Controls whether to notify when a download's category is changed."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  }
];

export default function AppriseSettings() {
  return <EnvVars vars={settings} />;
}
