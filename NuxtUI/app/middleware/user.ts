export default defineNuxtRouteMiddleware(async () => {
  const config = useRuntimeConfig()

  try {
    await $fetch(`${config.public.apiBase}/api/auth/me`, {
      credentials: 'include'
    })
  } catch {
    return navigateTo('/login')
  }
})
