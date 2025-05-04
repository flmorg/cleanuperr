import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "RADARR__ENABLED",
    description: [
      "Enables or disables Radarr cleanup."
    ],
    type: "boolean",
    defaultValue: "false",
    required: false,
    acceptedValues: ["true", "false"],
  },
  {
    name: "RADARR__BLOCK__TYPE",
    description: [
      "Determines how file blocking works for Radarr."
    ],
    type: "text",
    defaultValue: "blacklist",
    required: false,
    acceptedValues: ["blacklist", "whitelist"],
  },
  {
    name: "RADARR__BLOCK__PATH",
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
    examples: ["/blocklist.json", "https://example.com/blocklist.json"],
    notes: [
      "[This blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist), [this permissive blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist_permissive) and [this whitelist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist) can be used for Sonarr and Radarr."
    ]
  },
  {
    name: "RADARR__INSTANCES__0__URL",
    description: [
      "URL of the Radarr instance."
    ],
    type: "text",
    defaultValue: "http://localhost:7878",
    required: false,
    examples: ["http://localhost:7878", "http://radarr:7878"],
  },
  {
    name: "RADARR__INSTANCES__0__APIKEY",
    description: [
      "API key for the Radarr instance."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false
  }
];

export default function RadarrSettings() {
  return <EnvVars vars={settings} />;
} 