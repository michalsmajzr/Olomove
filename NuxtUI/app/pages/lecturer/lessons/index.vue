
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

type Lesson = {
  id: string
  sessionId: string
  courseId: string
  courseName: string
  date: string
  startsAt: string
  endsAt: string
  room: string | null
}

type ViewMode = 'calendar' | 'list'

const viewMode = ref<ViewMode>('calendar')

// Aktuálně zobrazený měsíc
const today = new Date()
const selectedMonth = ref(
  new Date(today.getFullYear(), today.getMonth(), 1)
)

// Datumy pro API
function toDateString(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

const dateFrom = computed(() =>
  toDateString(selectedMonth.value)
)

const dateTo = computed(() =>
  toDateString(
    new Date(
      selectedMonth.value.getFullYear(),
      selectedMonth.value.getMonth() + 1,
      0
    )
  )
)

// Načtení lekcí z backendu
const {
  data: lessons,
  pending,
  error,
  refresh
} = useFetch<Lesson[]>('/api/lecturer/lessons', {
  query: {
    from: dateFrom,
    to: dateTo
  },
  credentials: 'include',
  server: false,
  default: () => []
})

// Navigace mezi měsíci
function changeMonth(offset: number) {
  selectedMonth.value = new Date(
    selectedMonth.value.getFullYear(),
    selectedMonth.value.getMonth() + offset,
    1
  )
}

function goToToday() {
  selectedMonth.value = new Date(
    new Date().getFullYear(),
    new Date().getMonth(),
    1
  )
}

const monthTitle = computed(() =>
  selectedMonth.value.toLocaleDateString('cs-CZ', {
    month: 'long',
    year: 'numeric'
  })
)

// Pondělí = první den týdne
const weekDays = [
  'Pondělí',
  'Úterý',
  'Středa',
  'Čtvrtek',
  'Pátek',
  'Sobota',
  'Neděle'
]

// Dny pro kalendář včetně prázdných polí
const calendarDays = computed(() => {
  const year = selectedMonth.value.getFullYear()
  const month = selectedMonth.value.getMonth()

  const firstDay = new Date(year, month, 1)
  const daysInMonth = new Date(year, month + 1, 0).getDate()

  const offset = (firstDay.getDay() + 6) % 7

  const days: (string | null)[] = []

  for (let i = 0; i < offset; i++) {
    days.push(null)
  }

  for (let day = 1; day <= daysInMonth; day++) {
    days.push(toDateString(new Date(year, month, day)))
  }

  while (days.length % 7 !== 0) {
    days.push(null)
  }

  return days
})

// Lekce seskupené podle data
const lessonsByDate = computed<Record<string, Lesson[]>>(() => {
  const grouped: Record<string, Lesson[]> = {}

  for (const lesson of lessons.value ?? []) {
    if (!grouped[lesson.date]) {
      grouped[lesson.date] = []
    }

    grouped[lesson.date]!.push(lesson)
  }

  return grouped
})

function getLessonsForDate(date: string | null): Lesson[] {
  if (!date) return []
  return lessonsByDate.value[date] ?? []
}

// Seznam seřazený podle data a času
const sortedLessons = computed(() =>
  [...(lessons.value ?? [])].sort((a, b) => {
    const dateCompare = a.date.localeCompare(b.date)

    return dateCompare !== 0
      ? dateCompare
      : a.startsAt.localeCompare(b.startsAt)
  })
)

function formatDate(date: string): string {
  return new Date(`${date}T12:00:00`).toLocaleDateString(
    'cs-CZ',
    {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    }
  )
}

function formatTime(time: string): string {
  return time.slice(0, 5)
}

function isToday(date: string | null): boolean {
  return date === toDateString(new Date())
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-7xl px-6 py-10">

        <!-- Nadpis -->
        <div class="mb-8">
          <h1 class="text-3xl font-semibold text-highlighted">
            Moje lekce
          </h1>

          <p class="mt-2 text-muted">
            Rozvrh vašich lekcí v kalendáři nebo seznamu.
          </p>
        </div>

        <!-- Ovládání -->
        <div
          class="mb-6 flex flex-wrap items-center justify-between gap-4"
        >
          <div class="flex items-center gap-3">
            <UButton
              icon="i-lucide-chevron-left"
              color="neutral"
              variant="outline"
              aria-label="Předchozí měsíc"
              @click="changeMonth(-1)"
              :ui="{ base: 'cursor-pointer' }"
            />

            <h2
              class="min-w-44 text-center text-lg font-semibold capitalize"
            >
              {{ monthTitle }}
            </h2>

            <UButton
              icon="i-lucide-chevron-right"
              color="neutral"
              variant="outline"
              aria-label="Následující měsíc"
              @click="changeMonth(1)"
              :ui="{ base: 'cursor-pointer' }"
            />

            <UButton
              color="neutral"
              variant="ghost"
              @click="goToToday"
            >
              Dnes
            </UButton>
          </div>

          <div class="flex items-center gap-2">
            <UButton
              icon="i-lucide-calendar-days"
              :color="viewMode === 'calendar' ? 'primary' : 'neutral'"
              :variant="viewMode === 'calendar' ? 'solid' : 'outline'"
              @click="viewMode = 'calendar'"
              :ui="{ base: 'cursor-pointer' }"
            >
              Kalendář
            </UButton>

            <UButton
              icon="i-lucide-list"
              :color="viewMode === 'list' ? 'primary' : 'neutral'"
              :variant="viewMode === 'list' ? 'solid' : 'outline'"
              @click="viewMode = 'list'"
              :ui="{ base: 'cursor-pointer' }"
            >
              Seznam
            </UButton>
          </div>
        </div>

        <!-- Chyba -->
        <UAlert
          v-if="error"
          color="error"
          title="Nepodařilo se načíst lekce"
          description="Zkontrolujte připojení k serveru."
          class="mb-6"
        >
          <template #actions>
            <UButton
              color="error"
              variant="outline"
              @click="refresh()"
            >
              Zkusit znovu
            </UButton>
          </template>
        </UAlert>

        <!-- Načítání -->
        <div v-else-if="pending" class="py-16 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="size-8 animate-spin text-orange-500"
          />
          <p class="mt-3 text-muted">
            Načítání lekcí…
          </p>
        </div>

        <!-- Kalendář -->
        <template v-else-if="viewMode === 'calendar'">
          <UCard :ui="{ body: 'p-0 sm:p-0' }">
            <div class="overflow-x-auto">
              <div class="min-w-[840px]">

                <!-- Dny týdne -->
                <div class="grid grid-cols-7 border-b border-default">
                  <div
                    v-for="day in weekDays"
                    :key="day"
                    class="border-r border-default px-3 py-4 text-center text-sm font-semibold text-muted last:border-r-0"
                  >
                    {{ day }}
                  </div>
                </div>

                <!-- Dny měsíce -->
                <div class="grid grid-cols-7">
                  <div
                    v-for="(day, index) in calendarDays"
                    :key="index"
                    class="min-h-36 border-b border-r border-default p-2"
                    :class="{
                      'bg-muted/20': !day,
                      'bg-orange-50/50': isToday(day)
                    }"
                  >
                    <template v-if="day">
                      <div class="mb-2 flex justify-end">
                        <span
                          class="flex size-7 items-center justify-center rounded-full text-sm"
                          :class="isToday(day)
                            ? 'bg-orange-500 font-semibold text-white'
                            : 'text-muted'"
                        >
                          {{ Number(day.slice(-2)) }}
                        </span>
                      </div>

                      <div class="space-y-1.5">
                        <NuxtLink
                          v-for="lesson in getLessonsForDate(day)"
                          :key="lesson.id"
                          :to="`/lecturer/courses/${lesson.courseId}`"
                          class="block rounded-lg border border-orange-200 bg-orange-50 p-2 transition hover:bg-orange-100"
                        >
                          <p class="text-xs font-semibold text-orange-700">
                            {{ formatTime(lesson.startsAt) }}
                            –
                            {{ formatTime(lesson.endsAt) }}
                          </p>

                          <p class="mt-1 truncate text-xs font-medium text-slate-800">
                            {{ lesson.courseName }}
                          </p>

                          <p class="mt-1 truncate text-xs text-slate-500">
                            {{ lesson.room || 'Sál neuveden' }}
                          </p>
                        </NuxtLink>
                      </div>
                    </template>
                  </div>
                </div>

              </div>
            </div>
          </UCard>
        </template>

        <!-- Seznam -->
        <template v-else>
          <UCard :ui="{ body: 'p-0 sm:p-0' }">
            <div
              v-if="!sortedLessons.length"
              class="px-6 py-16 text-center"
            >
              <UIcon
                name="i-lucide-calendar-x"
                class="mx-auto mb-3 size-10 text-muted"
              />

              <p class="text-muted">
                V tomto měsíci nemáte žádné naplánované lekce.
              </p>
            </div>

            <div
              v-for="lesson in sortedLessons"
              :key="lesson.id"
              class="flex flex-wrap items-center gap-5 border-b border-default px-6 py-5 last:border-b-0"
            >
              <div class="min-w-40">
                <p class="text-sm font-semibold capitalize text-orange-600">
                  {{ formatDate(lesson.date) }}
                </p>
              </div>

              <div class="min-w-0 flex-1">
                <h3 class="font-semibold text-highlighted">
                  {{ lesson.courseName }}
                </h3>

                <p class="mt-1 text-sm text-muted">
                  {{ formatTime(lesson.startsAt) }}
                  –
                  {{ formatTime(lesson.endsAt) }}
                  ·
                  {{ lesson.room || 'Sál neuveden' }}
                </p>
              </div>

              <UButton
                :to="`/lecturer/courses/${lesson.courseId}`"
                color="neutral"
                variant="ghost"
                trailing-icon="i-lucide-chevron-right"
              >
                Detail kurzu
              </UButton>
            </div>
          </UCard>
        </template>

        <!-- Počet lekcí -->
        <p
          v-if="!pending && !error"
          class="mt-4 text-right text-sm text-muted"
        >
          Počet lekcí v měsíci: {{ sortedLessons.length }}
        </p>

      </div>
    </main>
  </div>
</template>
