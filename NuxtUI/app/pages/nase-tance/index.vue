
<script setup lang="ts">
type CourseSession = {
  weekday: number | string
  startsAt: string
  endsAt: string
  room: string | null
}

type Course = {
  id: string
  name: string
  description: string | null
  courseType: string
  level: number
  startDate: string
  endDate: string
  capacity: number
  price: number
  danceStyles: string[]
  instructors: string[]
  occupiedPlaces?: number
  availablePlaces?: number
  sessions?: CourseSession[]
}

// Seznam kurzů načtených z backendu
const courses = ref<Course[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

// Hodnoty jednotlivých filtrů
const searchQuery = ref('')
const selectedLevel = ref('all')
const selectedInstructor = ref('all')
const viewMode = ref<'calendar' | 'list'>('calendar')

// Úrovně dostupné v načtených kurzech
const availableLevels = computed(() => {
  const levels = courses.value.map(course => course.level)

  return [...new Set(levels)].sort((a, b) => a - b)
})

// Lektoři dostupní v načtených kurzech
const availableInstructors = computed(() => {
  const instructors = courses.value.flatMap(
    course => course.instructors
  )

  return [...new Set(instructors)].sort()
})

// Zjištění, zda je aktivní nějaký filtr
const hasActiveFilters = computed(() => {
  return searchQuery.value.trim() !== ''
    || selectedLevel.value !== 'all'
    || selectedInstructor.value !== 'all'
})

// Filtrování kurzů
const filteredCourses = computed(() => {
  const search = searchQuery.value.toLowerCase().trim()

  return courses.value.filter((course) => {
    const matchesSearch =
      course.name.toLowerCase().includes(search)
      || course.danceStyles.some(style =>
        style.toLowerCase().includes(search)
      )

    const matchesLevel =
      selectedLevel.value === 'all'
      || String(course.level) === selectedLevel.value

    const matchesInstructor =
      selectedInstructor.value === 'all'
      || course.instructors.includes(selectedInstructor.value)

    return matchesSearch && matchesLevel && matchesInstructor
  })
})

// Vymazání všech filtrů
function resetFilters() {
  searchQuery.value = ''
  selectedLevel.value = 'all'
  selectedInstructor.value = 'all'
}

// Formátování data
function formatDate(date: string) {
  const [year, month, day] = date.split('-').map(Number)

  if (!year || !month || !day) return date

  return new Intl.DateTimeFormat('cs-CZ').format(
    new Date(year, month - 1, day)
  )
}


// Barevná obsazenost: 0–49 % zelená, 50–74 % světle oranžová,
// 75–99 % tmavě oranžová, 100 % červená.
function capacityBarClass(course: Course): string {
  if (course.capacity <= 0) return 'bg-red-500'
  const used = (course.occupiedPlaces ?? 0) / course.capacity
  if (used >= 1) return 'bg-red-500'
  if (used >= 0.75) return 'bg-orange-600'
  if (used >= 0.5) return 'bg-amber-400'
  return 'bg-green-500'
}

function capacityPercentage(course: Course): number {
  return course.capacity > 0
    ? Math.max(0, Math.min(100, (course.occupiedPlaces ?? 0) / course.capacity * 100))
    : 0
}

// Formátování ceny
function formatPrice(price: number) {
  return new Intl.NumberFormat('cs-CZ').format(price)
}

// Načtení kurzů z API
onMounted(async () => {
  try {
    courses.value = await $fetch<Course[]>('/api/courses')
  } catch {
    errorMessage.value = 'Kurzy se nepodařilo načíst.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-7xl px-6 py-10">

        <!-- Nadpis -->
        <div class="mb-10">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
            Naše tance
          </h1>

          <p class="mt-2 text-base text-muted">
            Vyberte si z naší nabídky tanečních kurzů.
          </p>
        </div>

        <!-- Přepínání kalendáře a současných kartiček kurzů. -->
        <div class="mb-8 flex flex-wrap items-center justify-between gap-3">
          <div class="inline-flex rounded-xl border border-default bg-white p-1">
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold"
              :class="viewMode === 'calendar' ? 'bg-orange-500 text-white' : 'text-muted hover:text-highlighted'"
              :aria-pressed="viewMode === 'calendar'"
              @click="viewMode = 'calendar'"
            >
              <UIcon name="i-lucide-calendar-days" class="size-4" /> Kalendář
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold"
              :class="viewMode === 'list' ? 'bg-orange-500 text-white' : 'text-muted hover:text-highlighted'"
              :aria-pressed="viewMode === 'list'"
              @click="viewMode = 'list'"
            >
              <UIcon name="i-lucide-layout-grid" class="size-4" /> Seznam kurzů
            </button>
          </div>
          <span class="text-sm text-muted">Kurzy: {{ filteredCourses.length }}</span>
        </div>

        <!-- Filtry -->
        <div
          v-if="!isLoading && !errorMessage && courses.length > 0"
          class="mb-8"
        >
          <div class="grid gap-5 md:grid-cols-3">

            <!-- Vyhledávání -->
            <div>
              <label class="mb-2 block text-sm font-medium">
                Vyhledat tanec
              </label>

              <UInput
                v-model="searchQuery"
                placeholder="Např. Salsa, Valčík..."
                icon="i-lucide-search"
                size="lg"
                class="w-full"
              />
            </div>

            <!-- Taneční úroveň -->
            <div>
              <label class="mb-2 block text-sm font-medium">
                Taneční úroveň
              </label>

              <select
                v-model="selectedLevel"
                class="w-full rounded-md border border-default bg-white px-3 py-2.5 text-sm"
              >
                <option value="all">
                  Všechny úrovně
                </option>

                <option
                  v-for="level in availableLevels"
                  :key="level"
                  :value="String(level)"
                >
                  Úroveň {{ level }}
                </option>
              </select>
            </div>

            <!-- Lektor -->
            <div>
              <label class="mb-2 block text-sm font-medium">
                Lektor
              </label>

              <select
                v-model="selectedInstructor"
                class="w-full rounded-md border border-default bg-white px-3 py-2.5 text-sm"
              >
                <option value="all">
                  Všichni lektoři
                </option>

                <option
                  v-for="instructor in availableInstructors"
                  :key="instructor"
                  :value="instructor"
                >
                  {{ instructor }}
                </option>
              </select>
            </div>
          </div>

          <!-- Počet výsledků a reset filtrů -->
          <div class="mt-5 flex items-center justify-between gap-4">
            <p class="text-sm text-muted">
              Nalezeno kurzů: {{ filteredCourses.length }}
            </p>

            <UButton
              v-if="hasActiveFilters"
              variant="outline"
              color="neutral"
              size="sm"
              @click="resetFilters"
            >
              Zrušit filtry
            </UButton>
          </div>
        </div>

        <!-- Načítání -->
        <div
          v-if="isLoading"
          class="py-12 text-center text-muted"
        >
          Načítání kurzů…
        </div>

        <!-- Chyba při načítání -->
        <UAlert
          v-else-if="errorMessage"
          color="error"
          :title="errorMessage"
        />

        <!-- V databázi nejsou žádné kurzy -->
        <div
          v-else-if="courses.length === 0"
          class="rounded-lg border border-default bg-white px-8 py-12 text-center"
        >
          <p class="text-muted">
            Momentálně nejsou dostupné žádné kurzy.
          </p>
        </div>

        <!-- Žádný kurz neodpovídá filtrům -->
        <div
          v-else-if="filteredCourses.length === 0"
          class="rounded-lg border border-default bg-white px-8 py-12 text-center"
        >
          <p class="text-muted">
            Žádné kurzy neodpovídají zvoleným filtrům.
          </p>

          <UButton
            class="mt-6"
            variant="outline"
            @click="resetFilters"
          >
            Zrušit filtry
          </UButton>
        </div>

        <!-- Kalendář používá stejné filtry jako seznam kurzů. -->
        <div v-else-if="viewMode === 'calendar'">
          <CourseCalendar :courses="filteredCourses" />
        </div>

        <!-- Výpis vyfiltrovaných kurzů -->
        <div
          v-else
          class="grid gap-6 md:grid-cols-2 lg:grid-cols-3"
        >
          <UCard
            v-for="course in filteredCourses"
            :key="course.id"
            class="h-full"
          >
            <div class="flex h-full flex-col gap-5">

              <!-- Název a taneční styl -->
              <div>
                <h2 class="text-xl font-semibold text-highlighted">
                  {{ course.name }}
                </h2>

                <p class="mt-1 text-sm text-muted">
                  {{ course.danceStyles.join(', ') }}
                </p>
              </div>

              <!-- Popis -->
              <p
                v-if="course.description"
                class="text-sm text-muted"
              >
                {{ course.description }}
              </p>

              <!-- Informace o kurzu -->
              <div class="space-y-2 text-sm">
                <p>
                  <strong>Úroveň:</strong>
                  {{ course.level }}
                </p>

                <p>
                  <strong>Období:</strong>
                  {{ formatDate(course.startDate) }}
                  –
                  {{ formatDate(course.endDate) }}
                </p>

                <p>
                  <strong>Lektor:</strong>
                  {{ course.instructors.join(', ') || 'Neuveden' }}
                </p>
              </div>

              <!-- Obsazenost -->
              <div class="space-y-2 border-t border-default pt-4">
                <template v-if="course.occupiedPlaces !== undefined">
                  <div class="flex items-center justify-between text-sm">
                    <span>Obsazenost</span>
                    <span class="font-medium">
                      {{ course.occupiedPlaces }}/{{ course.capacity }}
                    </span>
                  </div>

                  <div class="h-2.5 w-full overflow-hidden rounded-full bg-gray-200">
                    <div
                      class="h-full rounded-full transition-all duration-300"
                      :class="capacityBarClass(course)"
                      :style="{ width: `${capacityPercentage(course)}%` }"
                    />
                  </div>

                  <p class="text-sm text-muted">
                    {{
                      course.availablePlaces !== undefined
                        ? course.availablePlaces > 0
                          ? `Volná místa: ${course.availablePlaces}`
                          : 'Kurz je obsazený'
                        : 'Kapacita: ' + course.capacity
                    }}
                  </p>
                </template>

                <p v-else class="text-sm text-muted">
                  Kapacita: {{ course.capacity }} osob
                </p>
              </div>

              <!-- Cena -->
              <div class="border-t border-default pt-4">
                <p class="text-lg font-semibold text-primary">
                  {{ formatPrice(course.price) }} kreditů
                </p>

                <p class="mt-1 text-xs text-muted">
                  Cena za jednu osobu
                </p>
              </div>

              <!-- NOVÉ: Tlačítko pro detail a přihlášení -->
              <div class="mt-auto pt-2">
                <UButton
                  :to="`/nase-tance/${course.id}`"
                  icon="i-lucide-arrow-right"
                  color="primary"
                  size="lg"
                  block
                >
                  Zobrazit detail a přihlásit se
                </UButton>
              </div>

            </div>
          </UCard>
        </div>

      </div>
    </main>
  </div>
</template>
