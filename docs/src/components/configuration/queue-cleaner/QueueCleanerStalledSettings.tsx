import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "QUEUECLEANER__STALLED_MAX_STRIKES",
    description: [
      "Number of strikes before removing a stalled download. Set to `0` to never remove stalled downloads.",
      "A strike is given when an item is stalled (not downloading) or stuck while downloading metadata (qBitTorrent only)."
    ],
    type: "positive integer number",
    defaultValue: "0",
    required: false,
    notes: [
      "`0` means to never remove stalled downloads.",
      "If not set to 0, the minimum value is `3`."
    ]
  },
  {
    name: "QUEUECLEANER__STALLED_RESET_STRIKES_ON_PROGRESS",
    description: [
      "Controls whether to remove the given strikes if any download progress was made since last checked."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "QUEUECLEANER__STALLED_IGNORE_PRIVATE",
    description: [
      "Controls whether to ignore stalled downloads from private trackers from being processed by Cleanuperr."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "QUEUECLEANER__STALLED_DELETE_PRIVATE",
    description: [
      "Controls whether stalled downloads from private trackers should be removed from the download client.",
      "Has no effect if QUEUECLEANER__STALLED_IGNORE_PRIVATE is true."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
    important: [
      "Setting this to true means you don't care about seeding, ratio, H&R and potentially losing your private tracker account."
    ]
  },
  {
    name: "QUEUECLEANER__DOWNLOADING_METADATA_MAX_STRIKES",
    description: [
      "Number of strikes before removing a download stuck while downloading metadata.",
    ],
    type: "positive integer number",
    defaultValue: "0",
    required: false,
    notes: [
      "`0` means to never remove downloads stuck while downloading metadata.",
      "If not set to `0`, the minimum value is `3`."
    ]
  }
];

export default function QueueCleanerStalledSettings() {
  return <EnvVars vars={settings} />;
} 