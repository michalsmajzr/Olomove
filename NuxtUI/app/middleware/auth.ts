/**
 * generic role-based middleware that allows checking multiple roles at once
 * usage in page:
 * definePageMeta({
 *   middleware: 'auth-role',
 *   requiredRoles: ['Admin', 'Teacher']
 * })
 */
export default defineNuxtRouteMiddleware(async (to) => {
  const config = useRuntimeConfig()
  const requiredRoles = to.meta.requiredRoles as string[] | undefined

  // if no roles are defined, just verify that user is authenticated
  if (!requiredRoles || requiredRoles.length === 0) {
    try {
      await $fetch(`${config.public.apiBase}/api/auth/me`, {
        credentials: 'include'
      })
    } catch {
      return navigateTo('/login')
    }
    return
  }

  try {
    const currentUser = await $fetch<{ roles: string[] }>(
      `${config.public.apiBase}/api/auth/me`,
      {
        credentials: 'include'
      }
    )

    // check if user has at least one of the required roles
    const hasRequiredRole = requiredRoles.some(role =>
      currentUser.roles.includes(role)
    )

    if (!hasRequiredRole) {
      return navigateTo('/')
    }
  } catch {
    return navigateTo('/login')
  }
})
