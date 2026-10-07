<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type UserRow = {
  id: string
  name: string
  email: string | null
  role: string
  credit: number
}

const users = ref<UserRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const isUpdating = ref<string | null>(null)

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

const roleOptions = [
  { label: 'Uživatel', value: 'User' },
  { label: 'Lektor', value: 'Lecturer' },
  { label: 'Učitel', value: 'Teacher' }
]

const tableRows = computed(() => users.value.map(user => ({
  ...user,
  originalRole: user.role,
  roleLabel: roleLabels[user.role] ?? user.role,
  credit: `${user.credit.toLocaleString('cs-CZ')} Kč`
})))

onMounted(async () => {
  try {
    users.value = await $fetch<UserRow[]>('/api/admin/users', {
      credentials: 'include'
    })
  } catch {
    errorMessage.value = 'Seznam uživatelů se nepodařilo načíst.'
  } finally {
    isLoading.value = false
  }
})

async function updateRole(userId: string, selectedRole: string) {
  if (!selectedRole) return

  isUpdating.value = userId
  try {
    await $fetch(`/api/admin/users/${userId}/role`, {
      method: 'PUT',
      body: { role: selectedRole },
      credentials: 'include'
    })
    const user = users.value.find(u => u.id === userId)
    if (user) {
      user.role = selectedRole
    }
  } catch {
    alert('Nepodařilo se změnit roli.')
  } finally {
    isUpdating.value = null
  }
}
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
            Správa klientů a rolí
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
          >
            <template #role-cell="{ row }">
              <span v-if="row.original.originalRole === 'Admin'" class="font-medium text-slate-500">
                {{ row.original.roleLabel }}
              </span>

              <USelect
                v-else
                :model-value="row.original.originalRole"
                :items="roleOptions"
                label-key="label"
                value-key="value"
                :loading="isUpdating === row.original.id"
                :disabled="isUpdating === row.original.id"
                class="w-40"
                @update:model-value="updateRole(row.original.id, $event)"
              />
            </template>
          </UTable>

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání uživatelů…' }}
          </div>
        </UCard>
      </div>
    </main>
  </div>
</template>
