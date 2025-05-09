import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
    {
        name: "DOWNLOADCLEANER__UNLINKED_TARGET_CATEGORY",
        description: [
            "The category to set on downloads that do not have hardlinks."
        ],
        type: "text",
        defaultValue: "cleanuperr-unlinked",
        required: false,
    },
    {
      name: "DOWNLOADCLEANER__UNLINKED_USE_TAG",
      description: [
        "If set to true, a tag will be set instead of changing the category.",
      ],
      type: "boolean",
      defaultValue: "false",
      required: false,
      acceptedValues: ["true", "false"],
      notes: [
        "Works only for qBittorrent.",
      ],

    },
    {
      name: "DOWNLOADCLEANER__UNLINKED_IGNORED_ROOT_DIR",
      description: [
        "This is useful if you are using [cross-seed](https://www.cross-seed.org/).",
        "The downloads root directory where the original and cross-seed hardlinks reside. All other hardlinks from this directory will be treated as if they do not exist (e.g. if you have a download with the original file and a cross-seed hardlink, it will be deleted).",
      ],
      type: "text",
      defaultValue: "Empty",
      required: false,
    },
    {
      name: "DOWNLOADCLEANER__UNLINKED_CATEGORIES__0",
      description: [
        "The categories of downloads to check for available hardlinks.",
        {
          type: "code",
          title: "Multiple patterns can be specified using incrementing numbers starting from 0.",
          content: `DOWNLOADCLEANER__UNLINKED_CATEGORIES__0=tv-sonarr
DOWNLOADCLEANER__UNLINKED_CATEGORIES__1=radarr`
        },
      ],
      type: "text",
      defaultValue: "Empty",
      required: false,
      notes: [
        "The category name must match the category that was set in the *arr.",
        "For qBittorrent, the category name is the name of the download category.",
        "For Deluge, the category name is the name of the label.",
        "For Transmission, the category name is the last directory from the save location.",
      ],
    }
];

export default function DownloadCleanerHardlinksSettings() {
  return <EnvVars vars={settings} />;
}
