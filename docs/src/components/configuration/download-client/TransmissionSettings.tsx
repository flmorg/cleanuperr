import React from "react";
import EnvVars, { EnvVarProps } from "../EnvVars";

const settings: EnvVarProps[] = [
  {
    name: "TRANSMISSION__URL",
    description: [
      "URL of the Transmission instance."
    ],
    type: "text",
    defaultValue: "http://localhost:9091",
    required: false,
    examples: ["http://localhost:9091", "http://192.168.1.100:9091", "https://mydomain.com:9091"],
  },
  {
    name: "TRANSMISSION__URL_BASE",
    description: [
      "Adds a prefix to the Transmission rpc url, such as `[TRANSMISSION__URL]/[TRANSMISSION__URL_BASE]/rpc`."
    ],
    type: "text",
    defaultValue: "transmission",
    required: false,
  },
  {
    name: "TRANSMISSION__USERNAME",
    description: [
      "Username for Transmission authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  },
  {
    name: "TRANSMISSION__PASSWORD",
    description: [
      "Password for Transmission authentication."
    ],
    type: "text",
    defaultValue: "Empty",
    required: false,
  }
];

export default function TransmissionSettings() {
  return <EnvVars vars={settings} />;
} 