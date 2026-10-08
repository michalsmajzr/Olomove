<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type DanceRow = {
  id: string
  name: string
  genre?: string | null
  description: string | null
}

const dances = ref<DanceRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const columns = [
  { accessorKey: 'name', header: 'Název' },
  { accessorKey: 'genre', header: 'Žánr' },
  { accessorKey: 'description', header: 'Krátký popis' }
]

const tableRows = computed(() => dances.value.map(d => ({
  ...d,
  genre: d.genre,
  description: d.description
})))

function shortDescription(text: string | null, maxWords = 5) {
  if (!text) return ''
  const words = text.split(/\s+/).filter(Boolean)
  if (words.length <= maxWords) return text
  return words.slice(0, maxWords).join(' ') + '…'
}

onMounted(async () => {
  try {
    dances.value = await $fetch<DanceRow[]>('/api/admin/dancestyles', {
      credentials: 'include'
    })
  } catch {
    errorMessage.value = 'Seznam tanců se nepodařilo načíst.'
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
              Správa tanců
            </h1>
            <p class="mt-2 text-base text-muted">
              Vytváření a editace tanců
            </p>
          </div>
          <div>
            <NuxtLink to="/admin/dance/add">
              <UButton label="Přidat nový tanec" color="primary" :ui="{ base: 'cursor-pointer' }" />
            </NuxtLink>
          </div>
        </div>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">
              Seznam tanců
            </h2>
          </template>

          <UTable
            v-if="!isLoading && !errorMessage"
            :data="tableRows"
            :columns="columns"
            class="w-full min-w-0"
            :ui="{ base: 'table-fixed', td: 'max-w-0' }"
          >
            <template #description-cell="{ row }">
              <div class="truncate text-sm text-muted">
                {{ row.original.description }}
              </div>
            </template>
          </UTable>

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání stylů…' }}
          </div>
        </UCard>
      </div>
    </main>
  </div>
</template>
