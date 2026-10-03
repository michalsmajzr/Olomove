const apiProxyTarget = process.env.NUXT_API_PROXY_TARGET

// https://nuxt.com/docs/api/configuration/nuxt-config
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
      // In production override with NUXT_PUBLIC_API_BASE.
      apiBase: 'https://localhost:7168'
    }
  },

  nitro: apiProxyTarget
    ? {
        devProxy: {
          '/api': {
            target: `${apiProxyTarget}/api`,
            changeOrigin: true
          }
        }
      }
    : undefined,

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
        'lucide:x'
      ]
    }
  },

  routeRules: {
    '/': { prerender: true }
  },

  compatibilityDate: '2026-06-30',

  eslint: {
    config: {
      stylistic: {
        commaDangle: 'never',
        braceStyle: '1tbs'
      }
    }
  }
})
