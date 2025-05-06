import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Cleanuperr',
  tagline: 'Cleaning arrs since \'24.',
  favicon: 'img/16.png',

  url: 'https://flmorg.github.io',
  baseUrl: '/cleanuperr/',

  organizationName: 'flmorg',
  projectName: 'cleanuperr',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'Cleanuperr',
      logo: {
        alt: 'Cleanuperr Logo',
        src: 'img/cleanuperr.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'configurationSidebar',
          position: 'left',
          label: 'Docs',
          activeBasePath: '/docs',
        },
        {
          href: 'https://github.com/flmorg/cleanuperr',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://discord.gg/SCtMCgtsc4',
          label: 'Discord',
          position: 'right',
        }
      ],
    },
    footer: {
      style: 'dark',
      links: [],
      copyright: `Copyright © ${new Date().getFullYear()} Cleanuperr. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
