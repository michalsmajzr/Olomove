export default defineNuxtConfig({
  modules: [
    '@nuxt/eslint',
    '@nuxt/ui'
  ],

  devtools: {
    enabled: true
  },

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    public: {
      apiBase: ''
    }
  },

  routeRules: {
    '/': { prerender: true },
    '/api/**': { proxy: 'http://localhost:5147/api/**' }
  },

  compatibilityDate: '2026-06-30',

  eslint: {
    config: {
      stylistic: {
        commaDangle: 'never',
        braceStyle: '1tbs'
      }
    }
  },

  icon: {
    clientBundle: {
      scan: true,
      icons: [
        'lucide:arrow-left',
        'lucide:circle-user-round',
        'lucide:eye',
        'lucide:eye-off',
        'lucide:instagram',
        'lucide:log-in',
        'lucide:log-out',
        'lucide:menu',
        'lucide:user-round-plus',
        'lucide:save',
        'lucide:x'
      ]
    }
  }
})
