<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type CourseRow = {
  id: string
  name: string
  capacity: number
  level: number
  startDate: string
  endDate: string
}

const courses = ref<CourseRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const route = useRoute()
const toast = useToast()

function formatDate(date: string) {
  const [year, month, day] = date.split('-').map(Number)
  return new Intl.DateTimeFormat('cs-CZ').format(new Date(year, month - 1, day))
}

const columns = [
  { accessorKey: 'name', header: 'Název kurzu' },
  { accessorKey: 'period', header: 'Období kurzu' },
  { accessorKey: 'capacity', header: 'Kapacita' },
  { accessorKey: 'level', header: 'Úroveň' }
]

function handleRowSelect(rowOrEvent: { original?: CourseRow; id?: string } | Event, selectedRow?: { original?: CourseRow; id?: string }) {
  const row = selectedRow ?? (rowOrEvent instanceof Event ? undefined : rowOrEvent)
  const id = row?.original?.id ?? row?.id

  if (id) {
    navigateTo(`/admin/courses/${id}`)
  }
}

onMounted(async () => {
  if (route.query.created === '1') {
    toast.add({
      title: 'Úspěch',
      description: 'Kurz byl úspěšně vytvořen.',
      color: 'success'
    })

    await navigateTo('/admin/courses', { replace: true })
  }

  try {
    courses.value = await $fetch<CourseRow[]>('/api/admin/courses', {
      credentials: 'include'
    })
  } catch {
    errorMessage.value = 'Seznam kurzů se nepodařilo načíst.'
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
        <div class="mb-8 flex items-center justify-between">
          <div>
            <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
              Plánování kurzů
            </h1>
            <p class="mt-2 text-base text-muted">
              Správa kurzů, termínů a sálů
            </p>
          </div>
          <NuxtLink to="/admin/courses/add">
            <UButton label="Přidat nový kurz" color="primary" :ui="{ base: 'cursor-pointer' }" />
          </NuxtLink>
        </div>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">
              Seznam kurzů
            </h2>
          </template>

          <UTable
            v-if="!isLoading && !errorMessage"
            :data="courses"
            :columns="columns"
            @select="handleRowSelect"
            class="w-full"
            :ui="{ th: 'cursor-default hover:bg-transparent', tr: 'cursor-pointer', tbody: '[&>tr]:hover:bg-elevated/50' }"
          >
            <template #capacity-cell="{ row }">
              {{ row.original.capacity }} míst
            </template>
            <template #period-cell="{ row }">
              {{ formatDate(row.original.startDate) }} – {{ formatDate(row.original.endDate) }}
            </template>
            <template #level-cell="{ row }">
              Úroveň {{ row.original.level }}
            </template>
          </UTable>

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání kurzů…' }}
          </div>
        </UCard>
      </div>
    </main>
  </div>
</template>
