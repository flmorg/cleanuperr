import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "NOTIFIARR__API_KEY",
    description: [
      "Notifiarr API key for sending notifications.",
      "Requires Notifiarr's [Passthrough](https://notifiarr.wiki/pages/integrations/passthrough/) integration to work."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "NOTIFIARR__CHANNEL_ID",
    description: [
      "Discord channel ID where notifications will be sent."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "NOTIFIARR__ON_IMPORT_FAILED_STRIKE",
    description: [
      "Controls whether to notify when an item receives a failed import strike."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "NOTIFIARR__ON_STALLED_STRIKE",
    description: [
      "Controls whether to notify when an item receives a stalled download strike. This includes strikes for being stuck while downloading metadata."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "NOTIFIARR__ON_SLOW_STRIKE",
    description: [
      "Controls whether to notify when an item receives a slow download strike. This includes strikes for having a low download speed or slow estimated finish time."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "NOTIFIARR__ON_QUEUE_ITEM_DELETED",
    description: [
      "Controls whether to notify when a queue item is deleted."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "NOTIFIARR__ON_DOWNLOAD_CLEANED",
    description: [
      "Controls whether to notify when a download is cleaned."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "NOTIFIARR__ON_CATEGORY_CHANGED",
    description: [
      "Controls whether to notify when a download's category is changed."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  }
];

export default function NotifiarrSettings() {
  return <EnvVars vars={settings} />;
} 