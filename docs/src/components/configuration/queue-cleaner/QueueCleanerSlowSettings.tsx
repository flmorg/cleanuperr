import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "QUEUECLEANER__SLOW_MAX_STRIKES",
    description: [
      "Number of strikes before removing a slow download. Set to `0` to never remove slow downloads.",
      "A strike is given when an item is slow."
    ],
    type: "positive integer number",
    defaultValue: "0",
    required: false,
    examples: ["0", "3", "10"],
    notes: [
      "If not set to 0, the minimum value is `3`."
    ]
  },
  {
    name: "QUEUECLEANER__SLOW_RESET_STRIKES_ON_PROGRESS",
    description: [
      "Controls whether to remove the given strikes if the download speed or estimated time are not slow anymore."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "QUEUECLEANER__SLOW_IGNORE_PRIVATE",
    description: [
      "Controls whether to ignore slow downloads from private trackers from being processed by Cleanuperr."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "QUEUECLEANER__SLOW_DELETE_PRIVATE",
    description: [
      "Controls whether slow downloads from private trackers should be removed from the download client.",
      "Has no effect if QUEUECLEANER__SLOW_IGNORE_PRIVATE is true."
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
    name: "QUEUECLEANER__SLOW_MIN_SPEED",
    description: [
      "The minimum speed a download should have.",
      "Downloads receive strikes if their speed falls below this value.",
      "If not specified, downloads will not receive strikes for slow download speed."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["1.5KB", "400KB", "2MB"],
  },
  {
    name: "QUEUECLEANER__SLOW_MAX_TIME",
    description: [
      "The maximum estimated hours a download should take to finish. Downloads receive strikes if their estimated finish time is above this value. If not specified (or 0), downloads will not receive strikes for slow estimated finish time."
    ],
    type: "positive number",
    defaultValue: "0",
    required: false,
    examples: ["0", "1.5", "24", "48"],
  },
  {
    name: "QUEUECLEANER__SLOW_IGNORE_ABOVE_SIZE",
    description: [
      "Downloads above this size will not be removed for being slow."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["10KB", "200MB", "3GB"],
  }
];

export default function QueueCleanerSlowSettings() {
  return <EnvVars vars={settings} />;
} 