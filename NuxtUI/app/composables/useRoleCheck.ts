/**
 * composable for role verification and authorization management
 */
export const useRoleCheck = () => {
  const config = useRuntimeConfig()
  const currentUser = ref<{ id: string; roles: string[] } | null>(null)
  const isLoading = ref(true)
  const error = ref<string | null>(null)

  /**
   * fetch current user data
   */
  const fetchCurrentUser = async () => {
    try {
      isLoading.value = true
      error.value = null
      currentUser.value = await $fetch<{ id: string; roles: string[] }>(
        `${config.public.apiBase}/api/auth/me`,
        {
          credentials: 'include'
        }
      )
    } catch (err) {
      error.value = 'failed to fetch user data'
      currentUser.value = null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * check if user has a specific role
   */
  const hasRole = (role: string): boolean => {
    if (!currentUser.value) return false
    return currentUser.value.roles.includes(role)
  }

  /**
   * check if user has at least one of the given roles
   */
  const hasAnyRole = (roles: string[]): boolean => {
    if (!currentUser.value) return false
    return roles.some(role => currentUser.value!.roles.includes(role))
  }

  /**
   * check if user has all the specified roles
   */
  const hasAllRoles = (roles: string[]): boolean => {
    if (!currentUser.value) return false
    return roles.every(role => currentUser.value!.roles.includes(role))
  }

  /**
   * check if user is admin
   */
  const isAdmin = (): boolean => hasRole('Admin')

  /**
   * check if user is instructor
   */
  const isInstructor = (): boolean => hasRole('Instructor')

  /**
   * check if user is teacher
   */
  const isTeacher = (): boolean => hasRole('Teacher')

  /**
   * check if user is regular user (default role)
   */
  const isUser = (): boolean => hasRole('User')

  // fetch user on composable mount
  onMounted(() => {
    fetchCurrentUser()
  })

  return {
    currentUser: readonly(currentUser),
    isLoading: readonly(isLoading),
    error: readonly(error),
    fetchCurrentUser,
    hasRole,
    hasAnyRole,
    hasAllRoles,
    isAdmin,
    isInstructor,
    isTeacher,
    isUser
  }
}
