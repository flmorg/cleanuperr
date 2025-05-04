import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "LIDARR__ENABLED",
    description: [
      "Enables or disables Lidarr cleanup."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "LIDARR__BLOCK__TYPE",
    description: [
      "Determines how file blocking works for Lidarr."
    ],
    type: "text",
    defaultValue: "blacklist",
    required: false,
    acceptedValues: ["blacklist", "whitelist"],
  },
  {
    name: "LIDARR__BLOCK__PATH",
    description: [
      "Path to the blocklist file (local file or URL).",
      "The value must be JSON compatible.",
      {
        type: "code",
        title: "The blocklists support the following patterns:",
        content: `*example            // file name ends with \"example\"
example*            // file name starts with \"example\"
*example*           // file name has \"example\" in the name
example             // file name is exactly the word \"example\"
regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with \"regex:\"`,
      }
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
    examples: ["/blocklist.json", "https://example.com/blocklist.json"]
  },
  {
    name: "LIDARR__INSTANCES__0__URL",
    description: [
      "URL of the Lidarr instance."
    ],
    type: "text",
    defaultValue: "http://localhost:8686",
    required: false,
    examples: ["http://localhost:8686", "http://lidarr:8686"],
  },
  {
    name: "LIDARR__INSTANCES__0__APIKEY",
    description: [
      "API key for the Lidarr instance."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  }
];

export default function LidarrSettings() {
  return <EnvVars vars={settings} />;
} 