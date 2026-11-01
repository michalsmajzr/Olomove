
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

type CourseParticipant = {
  enrollmentId: string | null
  participantId: string
  personType: 'Student' | 'User' | 'Partner'
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  danceRole: string | null
  rotation: boolean | null
  registeredAt: string | null
  dateOfBirth: string | null
  teacherName: string | null
  registeredWith: string | null
}

type CourseParticipantsResponse = {
  courseId: string
  courseName: string
  capacity: number
  registeredCount: number
  partnerCount: number
  studentCount: number
  balancedRoles: boolean
  leaderCount: number
  followerCount: number
  unassignedRoleCount: number
  totalCount: number
  occupiedPlaces: number
  availablePlaces: number
  isFull: boolean
  overCapacity: number
  participants: CourseParticipant[]
}

const {
  data: courseParticipants,
  pending: participantsPending,
  error: participantsError,
  refresh: refreshParticipants
} = useFetch<CourseParticipantsResponse>(
  () => `/api/lecturer/courses/${courseId.value}/participants`,
  {
    credentials: 'include',
    server: false
  }
)

const participantSearch = ref('')

const filteredParticipants = computed(() => {
  const search = participantSearch.value.trim().toLocaleLowerCase('cs-CZ')
  const participants = courseParticipants.value?.participants ?? []

  if (!search) return participants

  return participants.filter(participant =>
    [
      participant.fullName,
      participant.email ?? '',
      participant.phoneNumber ?? '',
      participant.teacherName ?? '',
      participant.registeredWith ?? '',
      participant.personType === 'Student'
        ? 'student dítě škola'
        : participant.personType === 'Partner'
          ? 'partner bez účtu'
          : 'uživatel',
      participant.danceRole ?? ''
    ].some(value => value.toLocaleLowerCase('cs-CZ').includes(search))
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
  <NuxtPage v-if="route.params.participantId" />

  <div v-else>
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

              
                <!-- Účastníci kurzu: účty, partneři bez účtu i školní studenti -->
              <UCard>
                <template #header>
                  <div class="flex flex-wrap items-center justify-between gap-3">
                    <div class="flex items-center gap-3">
                      <UIcon name="i-lucide-users" class="size-5 text-orange-600" />
                      <h2 class="text-lg font-semibold text-highlighted">Přihlášení účastníci</h2>
                      <UBadge v-if="courseParticipants" color="neutral" variant="soft">
                        {{ courseParticipants.totalCount }} / {{ courseParticipants.capacity }}
                      </UBadge>
                    </div>
                    <UButton
                      icon="i-lucide-refresh-cw"
                      color="neutral"
                      variant="outline"
                      size="sm"
                      :loading="participantsPending"
                      @click="refreshParticipants()"
                    >
                      Aktualizovat
                    </UButton>
                  </div>
                </template>

                <div v-if="participantsPending && !courseParticipants" class="py-10 text-center text-muted">
                  Načítání přihlášených…
                </div>

                <UAlert
                  v-else-if="participantsError"
                  color="error"
                  title="Nepodařilo se načíst účastníky."
                  description="Zkontrolujte spuštěný backend a přihlášení lektora."
                />

                <template v-else-if="courseParticipants">
                  <div class="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
                    <div class="rounded-lg border border-default p-3">
                      <p class="text-xs text-muted">Uživatelé</p>
                      <p class="mt-1 text-xl font-semibold">{{ courseParticipants.registeredCount }}</p>
                    </div>
                    <div class="rounded-lg border border-default p-3">
                      <p class="text-xs text-muted">Partneři bez účtu</p>
                      <p class="mt-1 text-xl font-semibold">{{ courseParticipants.partnerCount ?? 0 }}</p>
                    </div>
                    <div class="rounded-lg border border-default p-3">
                      <p class="text-xs text-muted">Děti ze škol</p>
                      <p class="mt-1 text-xl font-semibold">{{ courseParticipants.studentCount }}</p>
                    </div>
                    <div class="rounded-lg border border-default p-3">
                      <p class="text-xs text-muted">Volná místa</p>
                      <p class="mt-1 text-xl font-semibold">{{ courseParticipants.availablePlaces }}</p>
                    </div>
                  </div>

                  <div
                    v-if="courseParticipants.balancedRoles"
                    class="mb-5 flex flex-wrap gap-3 rounded-lg bg-muted/40 px-4 py-3 text-sm"
                  >
                    <span><strong>Leaders:</strong> {{ courseParticipants.leaderCount ?? 0 }}</span>
                    <span><strong>Followers:</strong> {{ courseParticipants.followerCount ?? 0 }}</span>
                    <span v-if="courseParticipants.unassignedRoleCount">
                      <strong>Bez role:</strong> {{ courseParticipants.unassignedRoleCount }}
                    </span>
                  </div>

                  <p v-if="courseParticipants.overCapacity > 0" class="mb-4 text-sm text-red-600">
                    Pozor: kapacita je překročena o {{ courseParticipants.overCapacity }} osob.
                  </p>

                  <UInput
                    v-model="participantSearch"
                    icon="i-lucide-search"
                    placeholder="Hledat podle jména, partnera nebo taneční role…"
                    class="mb-5 w-full sm:max-w-md"
                  />

                  <p v-if="!courseParticipants.participants.length" class="py-8 text-center text-muted">
                    Na tento kurz zatím není nikdo přihlášený.
                  </p>
                  <p v-else-if="!filteredParticipants.length" class="py-8 text-center text-muted">
                    Nikdo neodpovídá zadanému hledání.
                  </p>

                  <div v-else class="overflow-x-auto">
                    <table class="w-full min-w-[680px] text-left text-sm">
                      <thead>
                        <tr class="border-b border-default text-muted">
                          <th class="px-3 py-3 font-semibold">Jméno</th>
                          <th class="px-3 py-3 font-semibold">Typ</th>
                          <th class="px-3 py-3 font-semibold">Taneční role</th>
                          <th class="px-3 py-3 font-semibold">Střídání / učitel</th>
                          <th class="px-3 py-3 font-semibold">Kontakt</th>
                          <th class="px-3 py-3 font-semibold">Detail</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr
                          v-for="participant in filteredParticipants"
                          :key="`${participant.personType}-${participant.participantId}`"
                          class="border-b border-default last:border-b-0"
                        >
                          <td class="px-3 py-3">
                            <p class="font-medium text-highlighted">{{ participant.fullName }}</p>
                            <p v-if="participant.registeredWith" class="mt-1 text-xs text-muted">
                              Přihlášen/a s: {{ participant.registeredWith }}
                            </p>
                          </td>
                          <td class="px-3 py-3">
                            <UBadge
                              :color="participant.personType === 'Partner' ? 'info' : 'neutral'"
                              variant="subtle"
                            >
                              {{ participant.personType === 'Partner'
                                ? 'Partner bez účtu'
                                : participant.personType === 'Student'
                                  ? 'Dítě ze školy'
                                  : 'Uživatel' }}
                            </UBadge>
                          </td>
                          <td class="px-3 py-3">
                            {{ participant.personType === 'Student'
                              ? '—'
                              : formatDanceRole(participant.danceRole) }}
                          </td>
                          <td class="px-3 py-3">
                            <template v-if="participant.personType === 'Student'">
                              {{ participant.teacherName || 'Učitel neuveden' }}
                            </template>
                            <template v-else>
                              {{ participant.rotation == null
                                ? 'Neuvedeno'
                                : participant.rotation
                                  ? 'Střídá'
                                  : 'Nestřídá' }}
                            </template>
                          </td>
                          <td class="px-3 py-3">
                            <a
                              v-if="participant.email"
                              :href="`mailto:${participant.email}`"
                              class="text-orange-600 hover:underline"
                            >{{ participant.email }}</a>
                            <a
                              v-else-if="participant.phoneNumber"
                              :href="`tel:${participant.phoneNumber}`"
                              class="text-orange-600 hover:underline"
                            >{{ participant.phoneNumber }}</a>
                            <span v-else class="text-muted">Neuvedeno</span>
                          </td>
                          <td class="px-3 py-3">
                            <UButton
                              :to="`/lecturer/courses/${courseId}/participants/${participant.participantId}?type=${participant.personType}`"
                              icon="i-lucide-arrow-up-right"
                              color="neutral"
                              variant="outline"
                              size="xs"
                            >Otevřít</UButton>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>

                  <p v-if="courseParticipants.participants.length" class="mt-4 text-right text-xs text-muted">
                    Zobrazeno {{ filteredParticipants.length }} z {{ courseParticipants.totalCount }} účastníků.
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
            </div>

          </div>
        </template>

      </div>
    </main>
  </div>
</template>
