
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

type Lesson = {
  courseId: string
  courseName: string
  date: string
  startsAt: string
  endsAt: string
  room: string | null
}

type Dashboard = {
  activeCourses: number
  upcomingLessons: Lesson[]
}

const { data, pending, error, refresh } = useFetch<Dashboard>(
  '/api/lecturer/dashboard',
  {
    credentials: 'include',
    server: false
  }
)

const formatDate = (date: string) => {
  return new Date(`${date}T12:00:00`).toLocaleDateString('cs-CZ', {
    weekday: 'long',
    day: 'numeric',
    month: 'long'
  })
}

const formatTime = (time: string) => time.slice(0, 5)
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-7xl px-6 py-10">

        <div class="mb-8">
          <h1 class="text-3xl font-semibold text-highlighted">
            Přehled lektora
          </h1>

          <p class="mt-2 text-muted">
            Přehled vašich kurzů a nejbližších lekcí.
          </p>
        </div>

        <div v-if="pending" class="py-12 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="size-8 animate-spin text-orange-500"
          />
          <p class="mt-3 text-muted">Načítání přehledu…</p>
        </div>

        <UAlert
          v-else-if="error"
          color="error"
          title="Nepodařilo se načíst přehled"
          description="Zkontrolujte připojení k serveru."
        >
          <template #actions>
            <UButton color="error" variant="outline" @click="refresh()">
              Zkusit znovu
            </UButton>
          </template>
        </UAlert>

        <template v-else-if="data">

          <div class="mb-10 grid gap-5 md:grid-cols-2">

            <UCard>
              <div class="flex items-center gap-4">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-book-open"
                    class="size-7 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">Aktivní kurzy</p>
                  <p class="text-3xl font-semibold text-highlighted">
                    {{ data.activeCourses }}
                  </p>
                </div>
              </div>
            </UCard>

            <UCard>
              <div class="flex items-center gap-4">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-calendar-days"
                    class="size-7 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">Nejbližší lekce</p>
                  <p class="text-3xl font-semibold text-highlighted">
                    {{ data.upcomingLessons.length }}
                  </p>
                </div>
              </div>
            </UCard>

          </div>

          <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
            <h2 class="text-xl font-semibold text-highlighted">
              Nejbližší lekce
            </h2>

            <UButton
              to="/lecturer/lessons"
              color="primary"
              variant="outline"
              trailing-icon="i-lucide-arrow-right"
            >
              Celý rozvrh
            </UButton>
          </div>

          <UCard :ui="{ body: 'p-0 sm:p-0' }">

            <div
              v-if="!data.upcomingLessons.length"
              class="px-6 py-12 text-center"
            >
              <UIcon
                name="i-lucide-calendar-x"
                class="mx-auto mb-3 size-10 text-muted"
              />
              <p class="text-muted">
                V nejbližších 30 dnech nemáte naplánované žádné lekce.
              </p>
            </div>

            <div
              v-for="(lesson, index) in data.upcomingLessons"
              :key="`${lesson.courseId}-${lesson.date}-${lesson.startsAt}-${index}`"
              class="flex flex-wrap items-center gap-5 border-b border-default px-6 py-5 last:border-b-0"
            >
              <div class="min-w-36">
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
      </div>
    </main>
  </div>
</template>
