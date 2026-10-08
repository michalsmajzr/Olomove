<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type RoomsRow = {
  id: string
  name: string
  capacity: number
  description: string | null
}

const rooms = ref<RoomsRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const columns = [
  { accessorKey: 'name', header: 'Název' },
  { accessorKey: 'capacity', header: 'Kapacita' },
  { accessorKey: 'description', header: 'Popis' }
]

const tableRows = computed(() => rooms.value)

function handleRowSelect(
  rowOrEvent: { original?: RoomsRow; id?: string } | Event,
  selectedRow?: { original?: RoomsRow; id?: string }
) {
  const row = selectedRow ?? (rowOrEvent instanceof Event ? undefined : rowOrEvent)
  const id = row?.original?.id ?? row?.id

  if (id) {
    navigateTo(`/admin/rooms/${id}`)
  }
}

onMounted(async () => {
  try {
    rooms.value = await $fetch<RoomsRow[]>('/api/admin/rooms', {
      credentials: 'include'
    })
  } catch {
    errorMessage.value = 'Seznam sálů se nepodařilo načíst.'
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
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
              Správa sálů
            </h1>
            <p class="mt-2 text-base text-muted">
              Vytváření a editace sálů
            </p>
          </div>
          <div>
            <NuxtLink to="/admin/rooms/add">
              <UButton label="Přidat nový sál" color="primary" :ui="{ base: 'cursor-pointer' }" />
            </NuxtLink>
          </div>
        </div>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">
              Seznam sálů
            </h2>
          </template>

          <UTable
            v-if="!isLoading && !errorMessage"
            :data="tableRows"
            :columns="columns"
            @select="handleRowSelect"
            class="w-full min-w-0"
            :ui="{
              base: 'table-fixed',
              th: 'cursor-default hover:bg-transparent',
              tr: 'cursor-pointer',
              tbody: '[&>tr]:hover:bg-elevated/50',
              td: 'max-w-0'
            }"
          >
            <template #description-cell="{ row }">
              <div class="truncate text-sm text-muted">
                {{ row.original.description }}
              </div>
            </template>
          </UTable>

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání sálů…' }}
          </div>
        </UCard>
      </div>
    </main>
  </div>
</template>
