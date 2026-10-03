<script setup lang="ts">
const config = useRuntimeConfig()
type CurrentUser = { roles: string[] }
const currentUser = ref<CurrentUser | null>(null)
const authLoaded = ref(false)

onMounted(async () => {
  try {
    currentUser.value = await $fetch<CurrentUser>(`${config.public.apiBase}/api/auth/me`, { credentials: 'include' })
  } catch {
    currentUser.value = null
  } finally {
    authLoaded.value = true
  }
})

const isLoggedIn = computed(() => currentUser.value !== null)
const isAdmin = computed(() => currentUser.value?.roles.includes('Admin') ?? false)

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
  { label: 'Uživatelé', to: '/admin' },
  { label: 'Plánování kurzů', to: '/admin/courses' },
  { label: 'Kontakty', to: '/kontakty' }
]

const computedRole = computed(() => {
  if (isAdmin.value) return adminMenuItems
  if (isLoggedIn.value) return signedInMenuItems
  return guestMenuItems
})

async function logout() {
  await $fetch(`${config.public.apiBase}/api/auth/logout`, {
    method: 'POST',
    credentials: 'include'
  })
  currentUser.value = null
  await navigateTo('/')
}
</script>

<template>
  <header class="relative border-b border-slate-200 bg-white">
    <div class="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
      <!-- Logo -->
     <NuxtLink to="/">
      <AppLogo class="h-14 pt-2 pb-4 w-auto" />
    </NuxtLink>

      <!-- Navigation menu -->
      <UNavigationMenu
        :items="computedRole"
        orientation="horizontal"
        class="hidden md:flex"
        :ui="{
          list: 'gap-2',
          link: 'rounded-lg px-3 py-1.5 text-sm font-medium leading-6 text-muted hover:bg-elevated hover:text-primary data-[active]:bg-elevated data-[active]:text-primary'
        }"
      />

      <!-- Right icons -->
      <div v-if="authLoaded && isLoggedIn" class="hidden items-center gap-4 md:flex">
        <UButton
          label="Počet kreditů"
          color="primary"
          size="sm"
          class="cursor-pointer"
        />

        <UButton
          to="/profile"
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
      </div>

      <div v-else-if="authLoaded" class="hidden items-center gap-4 md:flex">
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
      </div>

      <HamburgerMenu :items="computedRole" :authenticated="isLoggedIn" @logout="logout" />
    </div>
  </header>
</template>
