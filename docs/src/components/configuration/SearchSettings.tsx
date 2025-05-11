import React from "react";
import EnvVars, { EnvVarProps } from "./EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "SEARCH_ENABLED",
    description: [
      "Enabled searching for replacements after a download has been removed from an arr."
    ],
    type: "boolean",
    defaultValue: "true",
    required: false,
    acceptedValues: ["true", "false"],
    notes: [
        "If you are using [Huntarr](https://github.com/plexguide/Huntarr.io), this setting should be set to false to let Huntarr do the searching.",
    ]
  },
  {
    name: "SEARCH_DELAY",
    description: [
      "If searching for replacements is enabled, this setting will delay the searches by the specified number of seconds.",
      "This is useful to avoid overwhelming the indexer with too many requests at once.",
    ],
    type: "positive integer number",
    defaultValue: "30",
    required: false,
    important: [
        "A lower value or `0` will result in faster searches, but may cause issues such as being rate limited or banned by the indexer.",
    ]
  },
];

export default function SearchSettings() {
  return <EnvVars vars={settings} />;
}
