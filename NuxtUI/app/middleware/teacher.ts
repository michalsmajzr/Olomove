export default defineNuxtRouteMiddleware(async () => {
  const config = useRuntimeConfig()

  try {
    const currentUser = await $fetch<{ roles: string[] }>(
      `${config.public.apiBase}/api/auth/me`,
      {
        credentials: 'include'
      }
    )

    if (!currentUser.roles.includes('Teacher')) {
      return navigateTo('/')
    }
  } catch {
    return navigateTo('/login')
  }
})
