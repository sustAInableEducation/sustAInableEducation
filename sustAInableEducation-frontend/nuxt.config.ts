import { definePreset } from '@primevue/themes';
import Aura from '@primevue/themes/aura';

const sustAInableEducationPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#F3FAF3',
      100: '#E3F5E3',
      200: '#C8EAC9',
      300: '#9DD89F',
      400: '#6ABE6E',
      500: '#45A249',
      600: '#388E3C',
      700: '#2C692F',
      800: '#275429',
      900: '#224525',
      950: '#0E2511'
    },
    colorScheme: {
      light: {
        primary: {
          color: '#2C692F',
          inverseColor: '#F3FAF3',
          hoverColor: '#45A249',
          activeColor: '#388E3C',
        },
        highlight: {
          background: '#0E2511',
          focusBackground: '#2C692F',
          color: '#ffffff',
          focusColor: '#ffffff',
        }
      }
    }
  }
})

export default defineNuxtConfig({
  compatibilityDate: '2024-04-03',
  devtools: { enabled: true },
  modules: [
    '@primevue/nuxt-module',
    '@nuxtjs/google-fonts',
    '@nuxtjs/google-fonts',
    '@nuxt/icon',
  ],
  icon: {
    provider: 'server',
  },
  primevue: {
    options: {
      theme: {
        preset: sustAInableEducationPreset,
        options: {
          darkModeSelector: '.my-dark-selector'
        }
      }
    }
  },
  css: ['~/assets/css/main.css'],
  postcss: {
    plugins: {
      tailwindcss: {},
      autoprefixer: {},
    },
  },
  googleFonts: {
    families: {
      Outfit: true,
      'Open+Sans': true,
    },
    display: 'swap'
  },
})