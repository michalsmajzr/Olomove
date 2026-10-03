<script setup lang="ts">
const colorMode = useColorMode()
colorMode.preference = 'light'

useHead({
  htmlAttrs: { lang: 'cs' },
  meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1' }],
  link: [{ rel: 'icon', href: '/favicon.ico' }]
})

useSeoMeta({ title: 'OloMove', description: 'Přihlášení do OloMove' })
</script>

<template>
  <UApp>
    <NuxtPage />
  </UApp>
</template>
