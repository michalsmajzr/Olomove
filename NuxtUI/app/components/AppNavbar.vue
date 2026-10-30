<script setup lang="ts">
const config = useRuntimeConfig()
const route = useRoute()
type CurrentUser = { roles: string[] }

const currentUser = useState<CurrentUser | null>('auth-user', () => null)
const isAuthLoaded = useState<boolean>('auth-loaded', () => false)


async function fetchUser() {
  try {
    currentUser.value = await $fetch<CurrentUser>(
      `${config.public.apiBase}/api/auth/me`,
      { credentials: 'include' }
    )

    await fetchCredits()
  } catch {
    currentUser.value = null
    navbarCredit.value = null
  } finally {
    isAuthLoaded.value = true
  }
}


onMounted(() => {
  fetchUser()
})

watch(() => route.path, () => {
  fetchUser()
})

const isLoggedIn = computed(() => currentUser.value !== null)
const isAdmin = computed(() => currentUser.value?.roles.includes('Admin') ?? false)
const isInstructor = computed(() =>
  currentUser.value?.roles.includes('Instructor') ?? false
)

const navbarCredit = useState<number | null>(
  'olomove-credit',
  () => null
)

const creditLabel = computed(() => {
  if (navbarCredit.value === null) return 'Kredity: —'

  return `${new Intl.NumberFormat('cs-CZ').format(navbarCredit.value)} kreditů`
})

async function fetchCredits() {
  if (!currentUser.value || isAdmin.value || isInstructor.value) {
    navbarCredit.value = null
    return
  }

  try {
    const response = await $fetch<{ credit: number }>(
      `${config.public.apiBase}/api/user/my-courses`,
      { credentials: 'include' }
    )

    navbarCredit.value = response.credit
  } catch {
    navbarCredit.value = null
  }
}


const guestMenuItems = [
  { label: 'Úvod', to: '/' },
  { label: 'O nás', to: '/o-nas' },
  { label: 'Lektoři', to: '/lektori' },
  { label: 'Naše tance', to: '/nase-tance' },
  { label: 'Kontakty', to: '/kontakty' }
]

const signedInMenuItems = [
  { label: 'Lektoři', to: '/lektori' },
  { label: 'Naše tance', to: '/nase-tance' },
  { label: 'Kontakty', to: '/kontakty' }
]

const adminMenuItems = [
  { label: 'O nás', to: '/o-nas' },
  { label: 'Uživatelé', to: '/admin/users' },
  { label: 'Plánování kurzů', to: '/admin/courses' },
  { label: 'Kontakty', to: '/kontakty' }
]

const instructorMenuItems = [
  { label: 'Přehled', to: '/lecturer' },
  { label: 'Moje lekce', to: '/lecturer/lessons' },
  { label: 'Moje kurzy', to: '/lecturer/courses' },
]

const computedRole = computed(() => {
  if (isAdmin.value) return adminMenuItems
  if (isInstructor.value) return instructorMenuItems
  if (isLoggedIn.value) return signedInMenuItems
  return guestMenuItems
})

async function logout() {
  await $fetch(`${config.public.apiBase}/api/auth/logout`, {
    method: 'POST',
    credentials: 'include'
  })
  currentUser.value = null
  navbarCredit.value = null
  isAuthLoaded.value = false
  await navigateTo('/')
}
</script>

<template>
  <header class="relative border-b border-slate-200 bg-white">
    <div class="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
      <NuxtLink to="/">
        <AppLogo class="h-14 pt-2 pb-4 w-auto" />
      </NuxtLink>

      <UNavigationMenu
        v-if="isAuthLoaded"
        :items="computedRole"
        orientation="horizontal"
        class="hidden md:flex"
        :ui="{
          list: 'gap-2',
          link: 'rounded-lg px-3 py-1.5 text-sm font-medium leading-6 text-muted hover:bg-elevated hover:text-primary data-[active]:bg-elevated data-[active]:text-primary'
        }"
      />

      <div v-if="isAuthLoaded" class="hidden items-center gap-4 md:flex">
        <template v-if="isLoggedIn">
          <UButton
            v-if="!isAdmin && !isInstructor"
            :label="creditLabel"
            to="/profile"
            icon="i-lucide-coins"
            color="primary"
            variant="solid"
            size="sm"
            class="cursor-pointer"
          />
          <UButton
            :to="isInstructor ? '/lecturer/profile' : '/profile'"
            icon="i-lucide-circle-user-round"
            aria-label="Můj profil"
            variant="ghost"
            color="neutral"
            class="text-highlighted hover:bg-transparent hover:opacity-80"
            :ui="{ base: 'size-6 p-0', leadingIcon: 'size-6' }"
          />
          <UButton
            icon="i-lucide-log-out"
            aria-label="Odhlášení"
            variant="ghost"
            color="neutral"
            class="cursor-pointer text-highlighted hover:bg-transparent hover:opacity-80"
            :ui="{ base: 'size-6 p-0', leadingIcon: 'size-6' }"
            @click="logout"
          />
        </template>

        <template v-else>
          <UButton
            to="https://www.instagram.com/"
            target="_blank"
            icon="i-lucide-instagram"
            aria-label="Instagram"
            variant="ghost"
            color="neutral"
            class="text-highlighted hover:bg-transparent hover:opacity-80"
            :ui="{ base: 'size-6 p-0', leadingIcon: 'size-6' }"
          />
          <UButton
            to="/login"
            icon="i-lucide-log-in"
            aria-label="Přihlášení"
            variant="ghost"
            color="neutral"
            class="text-highlighted hover:bg-transparent hover:opacity-80"
            :ui="{ base: 'size-6 p-0', leadingIcon: 'size-6' }"
          />
        </template>
      </div>

      <HamburgerMenu 
        v-if="isAuthLoaded" 
        :items="computedRole" 
        :authenticated="isLoggedIn" 
        @logout="logout" 
      />
    </div>
  </header>
</template>
