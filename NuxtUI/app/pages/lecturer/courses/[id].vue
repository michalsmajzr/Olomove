
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

const route = useRoute()

const courseId = computed(() => String(route.params.id))

type CourseSession = {
  id: string
  weekday: number
  startsAt: string
  endsAt: string
  room: string | null
}

type CourseDetail = {
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


type CourseStudent = {
  enrollmentId: string
  studentId: string
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  danceRole: string | null
  rotation: boolean
  registeredAt: string
}

type CourseStudentsResponse = {
  courseId: string
  courseName: string
  capacity: number
  registeredCount: number
  availablePlaces: number
  students: CourseStudent[]
}

const {
  data: courseStudents,
  pending: studentsPending,
  error: studentsError,
  refresh: refreshStudents
} = useFetch<CourseStudentsResponse>(
  () => `/api/lecturer/courses/${courseId.value}/students`,
  {
    credentials: 'include',
    server: false
  }
)

const studentSearch = ref('')

const filteredStudents = computed(() => {
  const search = studentSearch.value
    .trim()
    .toLocaleLowerCase('cs-CZ')

  const students = courseStudents.value?.students ?? []

  if (!search) return students

  return students.filter(student =>
    [
      student.fullName,
      student.email ?? '',
      student.danceRole ?? ''
    ].some(value =>
      value.toLocaleLowerCase('cs-CZ').includes(search)
    )
  )
})

function formatDanceRole(role: string | null): string {
  switch (role) {
    case 'Leader':
      return 'Leader'
    case 'Follower':
      return 'Follower'
    default:
      return 'Neuvedeno'
  }
}



const {
  data: course,
  pending,
  error,
  refresh
} = useFetch<CourseDetail>(
  () => `/api/lecturer/courses/${courseId.value}`,
  {
    credentials: 'include',
    server: false
  }
)

// V backendu je pondělí = 0, neděle = 6
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
  return time?.slice(0, 5) ?? '—'
}

function formatDate(date: string): string {
  if (!date) return '—'

  return new Date(`${date}T12:00:00`).toLocaleDateString(
    'cs-CZ',
    {
      day: 'numeric',
      month: 'numeric',
      year: 'numeric'
    }
  )
}

function formatPrice(price: number): string {
  return new Intl.NumberFormat('cs-CZ', {
    style: 'currency',
    currency: 'CZK',
    maximumFractionDigits: 0
  }).format(price)
}

const sortedSessions = computed(() =>
  [...(course.value?.sessions ?? [])].sort((a, b) =>
    a.weekday - b.weekday ||
    a.startsAt.localeCompare(b.startsAt)
  )
)

const courseStatus = computed(() => {
  if (!course.value) {
    return {
      label: 'Neznámý',
      color: 'neutral' as const
    }
  }

  const today = new Date()
  const todayString = [
    today.getFullYear(),
    String(today.getMonth() + 1).padStart(2, '0'),
    String(today.getDate()).padStart(2, '0')
  ].join('-')

  if (course.value.startDate > todayString) {
    return {
      label: 'Připravuje se',
      color: 'info' as const
    }
  }

  if (course.value.endDate < todayString) {
    return {
      label: 'Ukončen',
      color: 'neutral' as const
    }
  }

  return {
    label: 'Probíhá',
    color: 'success' as const
  }
})
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-7xl px-6 py-10">

        <!-- Návrat -->
        <UButton
          to="/lecturer/courses"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-6"
        >
          Zpět na moje kurzy
        </UButton>

        <!-- Načítání -->
        <div v-if="pending" class="py-20 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="size-9 animate-spin text-orange-500"
          />

          <p class="mt-3 text-muted">
            Načítání detailu kurzu…
          </p>
        </div>

        <!-- Chyba -->
        <UAlert
          v-else-if="error"
          color="error"
          title="Kurz se nepodařilo načíst"
          description="Kurz neexistuje, nemáte k němu přístup nebo došlo k chybě serveru."
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

        <!-- Detail kurzu -->
        <template v-else-if="course">

          <!-- Hlavička -->
          <div class="mb-8">
            <p class="mb-2 text-sm font-medium text-orange-600">
              OloMove · Lektorská sekce
            </p>

            <div class="flex flex-wrap items-center gap-4">
              <h1 class="text-3xl font-semibold text-highlighted">
                {{ course.name }}
              </h1>

              <UBadge
                :color="courseStatus.color"
                variant="subtle"
                size="lg"
              >
                {{ courseStatus.label }}
              </UBadge>
            </div>

            <p class="mt-3 text-muted">
              {{ course.courseType }}
              ·
              Úroveň {{ course.level }}
            </p>
          </div>

          <!-- Souhrnné informace -->
          <div class="mb-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">

            <UCard>
              <div class="flex items-center gap-3">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-calendar-range"
                    class="size-6 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">
                    Zahájení
                  </p>

                  <p class="font-semibold text-highlighted">
                    {{ formatDate(course.startDate) }}
                  </p>
                </div>
              </div>
            </UCard>

            <UCard>
              <div class="flex items-center gap-3">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-calendar-check"
                    class="size-6 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">
                    Ukončení
                  </p>

                  <p class="font-semibold text-highlighted">
                    {{ formatDate(course.endDate) }}
                  </p>
                </div>
              </div>
            </UCard>

            <UCard>
              <div class="flex items-center gap-3">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-users"
                    class="size-6 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">
                    Kapacita
                  </p>

                  <p class="font-semibold text-highlighted">
                    {{ course.capacity }} osob
                  </p>
                </div>
              </div>
            </UCard>

            <UCard>
              <div class="flex items-center gap-3">
                <div class="rounded-xl bg-orange-100 p-3">
                  <UIcon
                    name="i-lucide-wallet"
                    class="size-6 text-orange-600"
                  />
                </div>

                <div>
                  <p class="text-sm text-muted">
                    Cena kurzu
                  </p>

                  <p class="font-semibold text-highlighted">
                    {{ formatPrice(course.price) }}
                  </p>
                </div>
              </div>
            </UCard>

          </div>

          <!-- Obsah -->
          <div class="grid gap-6 lg:grid-cols-3">

            <!-- Levá část -->
            <div class="space-y-6 lg:col-span-2">

              <!-- Popis kurzu -->
              <UCard>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon
                      name="i-lucide-book-open"
                      class="size-5 text-orange-600"
                    />

                    <h2 class="text-lg font-semibold text-highlighted">
                      O kurzu
                    </h2>
                  </div>
                </template>

                <p class="whitespace-pre-line text-sm leading-7 text-muted">
                  {{ course.description || 'Tento kurz zatím nemá vyplněný popis.' }}
                </p>
              </UCard>

              <!-- Rozvrh -->
              <UCard>
                <template #header>
                  <div class="flex items-center justify-between gap-3">
                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-calendar-days"
                        class="size-5 text-orange-600"
                      />

                      <h2 class="text-lg font-semibold text-highlighted">
                        Pravidelný rozvrh
                      </h2>
                    </div>

                    <UBadge color="neutral" variant="subtle">
                      {{ sortedSessions.length }} termínů
                    </UBadge>
                  </div>
                </template>

                <div
                  v-if="!sortedSessions.length"
                  class="py-8 text-center text-muted"
                >
                  Kurz zatím nemá nastavené žádné pravidelné termíny.
                </div>

                <div v-else class="divide-y divide-default">
                  <div
                    v-for="session in sortedSessions"
                    :key="session.id"
                    class="flex flex-wrap items-center justify-between gap-4 py-4 first:pt-0 last:pb-0"
                  >
                    <div class="flex items-center gap-4">
                      <div class="rounded-xl bg-orange-50 p-3">
                        <UIcon
                          name="i-lucide-clock"
                          class="size-5 text-orange-600"
                        />
                      </div>

                      <div>
                        <h3 class="font-semibold text-highlighted">
                          {{ formatWeekday(session.weekday) }}
                        </h3>

                        <p class="mt-1 text-sm text-muted">
                          {{ formatTime(session.startsAt) }}
                          –
                          {{ formatTime(session.endsAt) }}
                        </p>
                      </div>
                    </div>

                    <div class="flex items-center gap-2 text-sm text-muted">
                      <UIcon
                        name="i-lucide-map-pin"
                        class="size-4"
                      />

                      {{ session.room || 'Sál neuveden' }}
                    </div>
                  </div>
                </div>
              </UCard>

              
                <!-- Studenti kurzu -->
                <UCard>
                <template #header>
                    <div class="flex flex-wrap items-center justify-between gap-3">
                    <div class="flex items-center gap-3">
                        <UIcon
                        name="i-lucide-users"
                        class="size-5 text-orange-600"
                        />

                        <h2 class="text-lg font-semibold text-highlighted">
                        Studenti kurzu
                        </h2>
                    </div>

                    <UBadge
                        v-if="courseStudents"
                        color="primary"
                        variant="subtle"
                    >
                        {{ courseStudents.registeredCount }}
                        /
                        {{ courseStudents.capacity }}
                    </UBadge>
                    </div>
                </template>

                <!-- Načítání -->
                <div
                    v-if="studentsPending"
                    class="py-12 text-center"
                >
                    <UIcon
                    name="i-lucide-loader-circle"
                    class="size-8 animate-spin text-orange-500"
                    />

                    <p class="mt-3 text-sm text-muted">
                    Načítání studentů…
                    </p>
                </div>

                <!-- Chyba -->
                <div
                    v-else-if="studentsError"
                    class="py-8 text-center"
                >
                    <UIcon
                    name="i-lucide-circle-alert"
                    class="mx-auto mb-3 size-9 text-red-500"
                    />

                    <p class="mb-4 text-sm text-muted">
                    Nepodařilo se načíst seznam studentů.
                    </p>

                    <UButton
                    color="error"
                    variant="outline"
                    @click="refreshStudents()"
                    >
                    Zkusit znovu
                    </UButton>
                </div>

                <template v-else-if="courseStudents">

                    <!-- Statistiky -->
                    <div class="mb-6 grid gap-4 sm:grid-cols-3">
                    <div class="rounded-xl bg-muted/40 p-4">
                        <p class="text-sm text-muted">
                        Přihlášení studenti
                        </p>

                        <p class="mt-2 text-2xl font-semibold text-highlighted">
                        {{ courseStudents.registeredCount }}
                        </p>
                    </div>

                    <div class="rounded-xl bg-muted/40 p-4">
                        <p class="text-sm text-muted">
                        Volná místa
                        </p>

                        <p class="mt-2 text-2xl font-semibold text-highlighted">
                        {{ courseStudents.availablePlaces }}
                        </p>
                    </div>

                    <div class="rounded-xl bg-muted/40 p-4">
                        <p class="text-sm text-muted">
                        Kapacita kurzu
                        </p>

                        <p class="mt-2 text-2xl font-semibold text-highlighted">
                        {{ courseStudents.capacity }}
                        </p>
                    </div>
                    </div>

                    <!-- Vyhledávání -->
                    <div class="mb-5">
                    <UInput
                        v-model="studentSearch"
                        icon="i-lucide-search"
                        placeholder="Hledat studenta…"
                        class="w-full sm:max-w-sm"
                    />
                    </div>

                    <!-- Žádní studenti -->
                    <div
                    v-if="!courseStudents.students.length"
                    class="py-10 text-center"
                    >
                    <UIcon
                        name="i-lucide-user-round-x"
                        class="mx-auto mb-3 size-10 text-muted"
                    />

                    <p class="font-medium text-highlighted">
                        Zatím žádní přihlášení studenti
                    </p>

                    <p class="mt-2 text-sm text-muted">
                        Jakmile se někdo přihlásí na kurz,
                        zobrazí se zde.
                    </p>
                    </div>

                    <!-- Vyhledávání bez výsledků -->
                    <div
                    v-else-if="!filteredStudents.length"
                    class="py-10 text-center text-sm text-muted"
                    >
                    Žádný student neodpovídá hledání.
                    </div>

                    <!-- Tabulka studentů -->
                    <div v-else class="overflow-x-auto">
                    <table class="w-full min-w-[650px] text-left text-sm">
                        <thead>
                        <tr class="border-b border-default text-muted">
                            <th class="px-3 py-3 font-semibold">
                            Student
                            </th>

                            <th class="px-3 py-3 font-semibold">
                            Taneční role
                            </th>

                            <th class="px-3 py-3 font-semibold">
                            Rotace
                            </th>

                            <th class="px-3 py-3 font-semibold">
                            Kontakt
                            </th>

                            <th class="px-3 py-3 font-semibold">
                            Akce
                            </th>
                        </tr>
                        </thead>

                        <tbody>
                        <tr
                            v-for="student in filteredStudents"
                            :key="student.enrollmentId"
                            class="border-b border-default last:border-b-0"
                        >
                            <!-- Student -->
                            <td class="px-3 py-4">
                            <div class="flex items-center gap-3">
                                <div
                                class="flex size-9 shrink-0 items-center justify-center rounded-full bg-orange-100"
                                >
                                <UIcon
                                    name="i-lucide-user-round"
                                    class="size-5 text-orange-600"
                                />
                                </div>

                                <span class="font-medium text-highlighted">
                                {{ student.fullName }}
                                </span>
                            </div>
                            </td>

                            <!-- Role -->
                            <td class="px-3 py-4">
                            <UBadge
                                color="neutral"
                                variant="subtle"
                            >
                                {{ formatDanceRole(student.danceRole) }}
                            </UBadge>
                            </td>

                            <!-- Rotace -->
                            <td class="px-3 py-4">
                            <div class="flex items-center gap-2">
                                <UIcon
                                :name="student.rotation
                                    ? 'i-lucide-circle-check'
                                    : 'i-lucide-circle-minus'"
                                :class="student.rotation
                                    ? 'text-green-600'
                                    : 'text-muted'"
                                class="size-4"
                                />

                                <span>
                                {{ student.rotation ? 'Ano' : 'Ne' }}
                                </span>
                            </div>
                            </td>

                            <!-- Kontakt -->
                            <td class="px-3 py-4">
                            <a
                                v-if="student.email"
                                :href="`mailto:${student.email}`"
                                class="text-orange-600 hover:underline"
                            >
                                {{ student.email }}
                            </a>

                            <span v-else class="text-muted">
                                Neuvedeno
                            </span>
                            </td>

                            <!-- Detail studenta -->
                            <td class="px-3 py-4">
                                <UButton
                                    :to="`/lecturer/courses/${courseId}/students/${student.studentId}`"
                                    icon="i-lucide-arrow-up-right"
                                    color="primary"
                                    variant="outline"
                                    size="sm"
                                >
                                    Detail
                                </UButton>
                            </td>
                        </tr>
                        </tbody>
                    </table>
                    </div>

                    <!-- Počet výsledků -->
                    <p
                    v-if="courseStudents.students.length"
                    class="mt-4 text-right text-xs text-muted"
                    >
                    Zobrazeno {{ filteredStudents.length }}
                    z {{ courseStudents.registeredCount }} studentů
                    </p>

                </template>
                </UCard>


            </div>

            <!-- Pravá část -->
            <div class="space-y-6">

              <!-- Základní informace -->
              <UCard>
                <template #header>
                  <h2 class="text-lg font-semibold text-highlighted">
                    Informace o kurzu
                  </h2>
                </template>

                <div class="space-y-5">
                  <div>
                    <p class="text-sm text-muted">
                      Název kurzu
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ course.name }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Typ kurzu
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ course.courseType }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Úroveň
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ course.level }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Období
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ formatDate(course.startDate) }}
                      –
                      {{ formatDate(course.endDate) }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Počet pravidelných termínů
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ sortedSessions.length }}
                    </p>
                  </div>
                </div>
              </UCard>

              <!-- Rychlá navigace -->
              <UCard>
                <template #header>
                  <h2 class="text-lg font-semibold text-highlighted">
                    Rychlé odkazy
                  </h2>
                </template>

                <div class="space-y-3">
                  <UButton
                    to="/lecturer/lessons"
                    icon="i-lucide-calendar-days"
                    color="primary"
                    variant="outline"
                    block
                  >
                    Kalendář lekcí
                  </UButton>

                  <UButton
                    to="/lecturer/courses"
                    icon="i-lucide-book-open"
                    color="neutral"
                    variant="outline"
                    block
                  >
                    Všechny moje kurzy
                  </UButton>

                  <UButton
                    to="/lecturer"
                    icon="i-lucide-layout-dashboard"
                    color="neutral"
                    variant="ghost"
                    block
                  >
                    Zpět na přehled
                  </UButton>
                </div>
              </UCard>

            </div>

          </div>
        </template>

      </div>
    </main>
  </div>
</template>
