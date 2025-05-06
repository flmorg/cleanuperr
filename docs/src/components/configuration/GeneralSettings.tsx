import React from "react";
import EnvVars, { EnvVarProps } from "./EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "TZ",
    description: [
      "The time zone to use."
    ],
    type: "text",
    defaultValue: "UTC",
    required: false,
    examples: ["America/New_York", "Europe/London", "Asia/Tokyo"],
  },
  {
    name: "DRY_RUN",
    description: [
      "When enabled, simulates irreversible operations (like deletions and notifications) without making actual changes."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "LOGGING__LOGLEVEL",
    description: [
      "Controls the detail level of application logs."
    ],
    type: "text",
    defaultValue: "Information",
    required: false,
    acceptedValues: ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"],
  },
  {
    name: "LOGGING__FILE__ENABLED",
    description: [
      "Enables logging to a file."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "LOGGING__FILE__PATH",
    description: [
      "Directory where log files will be saved."
    ],
    type: "text",
    defaultValue: "Empty (file is saved where the app is)",
    required: false,
  },
  {
    name: "LOGGING__ENHANCED",
    description: [
      "Provides more detailed descriptions in logs whenever possible.",
      "Will be deprecated in a future version."
    ],
    type: "boolean",
    defaultValue: "true",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "HTTP_MAX_RETRIES",
    description: [
      "The number of times to retry a failed HTTP call.",
      "Applies when communicating with *arrs, download clients and other services through HTTP calls."
    ],
    type: "positive integer number",
    defaultValue: "0",
    required: false,
  },
  {
    name: "HTTP_TIMEOUT",
    description: [
      "The number of seconds to wait before failing an HTTP call.",
      "Applies to calls to *arrs, download clients, and other services."
    ],
    type: "positive integer number",
    defaultValue: "100",
    required: false,
  },
  {
    name: "HTTP_VALIDATE_CERT",
    description: [
      "Controls whether to validate SSL certificates for HTTPS connections.",
      "Set to `Disabled` to ignore SSL certificate errors."
    ],
    type: "text",
    defaultValue: "Enabled",
    required: false,
    acceptedValues: ["Enabled", "DisabledForLocalAddresses", "Disabled"],
  }
];

export default function GeneralSettings() {
  return <EnvVars vars={settings} />;
}
