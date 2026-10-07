<script setup lang="ts">
type UserRow = {
  id: string
  name: string
  email: string | null
  role: string
  credit: number
}

type CurrentUser = { roles: string[] }

const config = useRuntimeConfig()
const users = ref<UserRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const columns = [
  { accessorKey: 'name', header: 'Jméno' },
  { accessorKey: 'email', header: 'E-mail' },
  { accessorKey: 'role', header: 'Role' },
  { accessorKey: 'credit', header: 'Kredit' }
]

const roleLabels: Record<string, string> = {
  Admin: 'Administrátor',
  Lecturer: 'Lektor',
  Teacher: 'Učitel',
  User: 'Klient'
}

const tableRows = computed(() => users.value.map(user => ({
  ...user,
  role: roleLabels[user.role] ?? user.role,
  credit: `${user.credit.toLocaleString('cs-CZ')} Kč`
})))

onMounted(async () => {
  try {
    const currentUser = await $fetch<CurrentUser>(`${config.public.apiBase}/api/auth/me`, {
      credentials: 'include'
    })

    if (!currentUser.roles.includes('Admin')) {
      await navigateTo('/')
      return
    }
  } catch {
    await navigateTo('/')
    return
  }

  try {
    users.value = await $fetch<UserRow[]>(`${config.public.apiBase}/api/admin/users`, {
      credentials: 'include'
    })
  } catch {
    errorMessage.value = 'Seznam uživatelů se nepodařilo načíst.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
            Uživatelé
          </h1>
          <p class="mt-2 text-base text-muted">
            Správa klientů
          </p>
        </div>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">
              Seznam uživatelů
            </h2>
          </template>

          <UTable
            v-if="!isLoading && !errorMessage"
            :data="tableRows"
            :columns="columns"
            class="w-full"
          />

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání uživatelů…' }}
          </div>
        </UCard>
      </div>
    </main>
  </div>
</template>
