
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

type CourseSession = {
  id: string
  weekday: number
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
  isActive: boolean
  sessions: CourseSession[]
}

const {
  data: courses,
  pending,
  error,
  refresh
} = useFetch<Course[]>('/api/lecturer/courses', {
  credentials: 'include',
  server: false,
  default: () => []
})

const weekdayNames: Record<number, string> = {
  0: 'Pondělí',
  1: 'Úterý',
  2: 'Středa',
  3: 'Čtvrtek',
  4: 'Pátek',
  5: 'Sobota',
  6: 'Neděle'
}

function formatWeekday(weekday: number): string {
  return weekdayNames[weekday] ?? `Den ${weekday}`
}

function formatTime(time: string): string {
  return time.slice(0, 5)
}

function formatDate(date: string): string {
  return new Date(`${date}T12:00:00`).toLocaleDateString('cs-CZ', {
    day: 'numeric',
    month: 'numeric',
    year: 'numeric'
  })
}

const activeCourses = computed(() =>
  (courses.value ?? []).filter(course => course.isActive)
)

const otherCourses = computed(() =>
  (courses.value ?? []).filter(course => !course.isActive)
)
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-7xl px-6 py-10">

        <!-- Nadpis -->
        <div class="mb-8">
          <p class="mb-2 text-sm font-medium text-orange-600">
            OloMove · Lektorská sekce
          </p>

          <h1 class="text-3xl font-semibold text-highlighted">
            Moje kurzy
          </h1>

          <p class="mt-2 text-muted">
            Přehled kurzů, které vedete.
          </p>
        </div>

        <!-- Souhrn -->
        <div class="mb-8 grid gap-5 sm:grid-cols-3">
          <UCard>
            <div class="flex items-center gap-4">
              <div class="rounded-xl bg-orange-100 p-3">
                <UIcon
                  name="i-lucide-book-open"
                  class="size-6 text-orange-600"
                />
              </div>

              <div>
                <p class="text-sm text-muted">Všechny kurzy</p>
                <p class="text-2xl font-semibold text-highlighted">
                  {{ courses?.length ?? 0 }}
                </p>
              </div>
            </div>
          </UCard>

          <UCard>
            <div class="flex items-center gap-4">
              <div class="rounded-xl bg-green-100 p-3">
                <UIcon
                  name="i-lucide-circle-check"
                  class="size-6 text-green-600"
                />
              </div>

              <div>
                <p class="text-sm text-muted">Probíhající</p>
                <p class="text-2xl font-semibold text-highlighted">
                  {{ activeCourses.length }}
                </p>
              </div>
            </div>
          </UCard>

          <UCard>
            <div class="flex items-center gap-4">
              <div class="rounded-xl bg-slate-100 p-3">
                <UIcon
                  name="i-lucide-calendar-clock"
                  class="size-6 text-slate-600"
                />
              </div>

              <div>
                <p class="text-sm text-muted">Ostatní kurzy</p>
                <p class="text-2xl font-semibold text-highlighted">
                  {{ otherCourses.length }}
                </p>
              </div>
            </div>
          </UCard>
        </div>

        <!-- Chyba -->
        <UAlert
          v-if="error"
          color="error"
          title="Nepodařilo se načíst kurzy"
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
          <p class="mt-3 text-muted">Načítání kurzů…</p>
        </div>

        <!-- Prázdný seznam -->
        <UCard v-else-if="!courses?.length">
          <div class="py-12 text-center">
            <UIcon
              name="i-lucide-book-x"
              class="mx-auto mb-4 size-12 text-muted"
            />

            <h2 class="text-lg font-semibold text-highlighted">
              Zatím nemáte přiřazené žádné kurzy
            </h2>

            <p class="mt-2 text-muted">
              Jakmile vám administrátor přiřadí kurz,
              zobrazí se zde.
            </p>
          </div>
        </UCard>

        <!-- Seznam kurzů -->
        <template v-else>
          <div class="mb-5 flex items-center justify-between">
            <h2 class="text-xl font-semibold text-highlighted">
              Seznam kurzů
            </h2>

            <span class="text-sm text-muted">
              {{ courses.length }} kurzů
            </span>
          </div>

          <div class="grid gap-5 lg:grid-cols-2">
            <UCard
              v-for="course in courses"
              :key="course.id"
              class="flex flex-col"
            >
              <div class="flex h-full flex-col">

                <!-- Název a stav -->
                <div class="mb-3 flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <h3 class="text-xl font-semibold text-highlighted">
                      {{ course.name }}
                    </h3>

                    <p class="mt-1 text-sm text-muted">
                      {{ course.courseType }} · Úroveň {{ course.level }}
                    </p>
                  </div>

                  <UBadge
                    :color="course.isActive ? 'success' : 'neutral'"
                    variant="subtle"
                  >
                    {{ course.isActive ? 'Probíhá' : 'Mimo období' }}
                  </UBadge>
                </div>

                <!-- Popis -->
                <p class="mb-5 line-clamp-3 text-sm text-muted">
                  {{ course.description || 'Kurz nemá vyplněný popis.' }}
                </p>

                <!-- Období -->
                <div class="mb-4 flex items-center gap-2 text-sm text-muted">
                  <UIcon
                    name="i-lucide-calendar-range"
                    class="size-4 text-orange-600"
                  />

                  <span>
                    {{ formatDate(course.startDate) }}
                    –
                    {{ formatDate(course.endDate) }}
                  </span>
                </div>

                <!-- Pravidelné termíny -->
                <div class="mb-5 rounded-lg bg-muted/40 p-4">
                  <p class="mb-3 text-sm font-semibold text-highlighted">
                    Pravidelné lekce
                  </p>

                  <div
                    v-if="!course.sessions.length"
                    class="text-sm text-muted"
                  >
                    Žádné termíny nejsou nastavené.
                  </div>

                  <div
                    v-for="session in course.sessions"
                    :key="session.id"
                    class="mb-3 flex flex-wrap items-start gap-3 last:mb-0"
                  >
                    <UIcon
                      name="i-lucide-clock"
                      class="mt-0.5 size-4 shrink-0 text-orange-600"
                    />

                    <div class="text-sm">
                      <p class="font-medium text-highlighted">
                        {{ formatWeekday(session.weekday) }}
                        {{ formatTime(session.startsAt) }}
                        –
                        {{ formatTime(session.endsAt) }}
                      </p>

                      <p class="mt-0.5 text-muted">
                        {{ session.room || 'Sál neuveden' }}
                      </p>
                    </div>
                  </div>
                </div>

                <!-- Spodní část -->
                <div class="mt-auto flex flex-wrap items-center justify-between gap-3 border-t border-default pt-4">
                  <div class="flex items-center gap-4 text-sm text-muted">
                    <span class="flex items-center gap-1">
                      <UIcon
                        name="i-lucide-users"
                        class="size-4"
                      />
                      Kapacita: {{ course.capacity }}
                    </span>
                  </div>

                  <UButton
                    :to="`/lecturer/courses/${course.id}`"
                    color="primary"
                    variant="outline"
                    trailing-icon="i-lucide-arrow-right"
                  >
                    Detail kurzu
                  </UButton>
                </div>
              </div>
            </UCard>
          </div>
        </template>

      </div>
    </main>
  </div>
</template>
