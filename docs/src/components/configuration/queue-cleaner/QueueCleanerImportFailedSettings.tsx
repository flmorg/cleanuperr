import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES",
    description: [
      "Number of strikes before removing a failed import. Set to `0` to never remove failed imports.",
      "A strike is given when an item fails to be imported."
    ],
    type: "positive integer number",
    defaultValue: "0",
    required: false,
    notes: [
      "`0` means to never remove failed imports.",
      "If not set to `0`, the minimum value is `3`."
    ]
  },
  {
    name: "QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE",
    description: [
      "Controls whether to ignore failed imports from private trackers from being processed by Cleanuperr."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE",
    description: [
      "Controls whether to delete failed imports from private trackers from the download client.",
      "Has no effect if QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE is true."
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
    name: "QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__0",
    description: [
      "Patterns to look for in failed import messages that should be ignored.",
      "Multiple patterns can be specified using incrementing numbers starting from 0.",
      {
        type: "code",
        title: "Configuration example:",
        content: `QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__0=title mismatch
QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__1=manual import required`
      }
    ],
    type: "text array",
    defaultValue: "Empty",
    required: false,
    examples: ["title mismatch", "manual import required"],
  }
];

export default function QueueCleanerImportFailedSettings() {
  return <EnvVars vars={settings} />;
} 