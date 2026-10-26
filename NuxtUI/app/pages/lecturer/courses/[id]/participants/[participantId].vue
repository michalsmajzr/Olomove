<script setup lang="ts">
definePageMeta({ middleware: 'instructor' })

const route = useRoute()
const courseId = computed(() => String(route.params.id))
const participantId = computed(() => String(route.params.participantId))
const requestedType = computed(() => route.query.type === 'Student' || route.query.type === 'User' ? route.query.type : null)

type Participant = {
  id?: string
  userId?: string
  firstName: string
  lastName: string
  fullName: string
  email?: string | null
  phoneNumber?: string | null
  danceRole?: string | null
  rotation?: boolean | null
  registeredAt?: string | null
  dateOfBirth?: string | null
  guardianName?: string | null
  guardianEmail?: string | null
  guardianPhone?: string | null
  teacherId?: string | null
  teacherName?: string | null
  courseId?: string
  courseName?: string
  note?: string | null
  createdAt?: string
  updatedAt?: string | null
}

type ParticipantCourse = {
  courseId: string
  courseName: string
  startDate?: string | null
  endDate?: string | null
  danceRole?: string | null
  rotation?: boolean | null
}

type ParticipantDetailResponse = {
  personType: 'Student' | 'User'
  participant: Participant
  courses: ParticipantCourse[]
}

const { data, pending, error, refresh } = useFetch<ParticipantDetailResponse>(
  () => `/api/lecturer/courses/${courseId.value}/participants/${participantId.value}`,
  { credentials: 'include', server: false }
)

const participant = computed(() => data.value?.participant ?? null)
const isStudent = computed(() => data.value?.personType === 'Student')
const participantCourses = computed(() => data.value?.courses ?? [])
const initials = computed(() => {
  const p = participant.value
  return p ? `${p.firstName?.charAt(0) ?? ''}${p.lastName?.charAt(0) ?? ''}`.toUpperCase() : ''
})

function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value.length === 10 ? `${value}T12:00:00` : value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('cs-CZ', { day: 'numeric', month: 'numeric', year: 'numeric' })
}

function calculateAge(value: string | null | undefined): string {
  if (!value) return '—'
  const parts = value.slice(0, 10).split('-').map(Number)
  if (parts.length !== 3 || parts.some(n => !Number.isFinite(n))) return '—'
  const [year, month, day] = parts as [number, number, number]
  const birth = new Date(year, month - 1, day)
  if (birth.getFullYear() !== year || birth.getMonth() !== month - 1 || birth.getDate() !== day) return '—'
  const today = new Date()
  let age = today.getFullYear() - year
  if (today.getMonth() + 1 < month || (today.getMonth() + 1 === month && today.getDate() < day)) age--
  return age >= 0 ? `${age} let` : '—'
}

function formatDanceRole(role: string | null | undefined): string {
  return role === 'Leader' ? 'Leader' : role === 'Follower' ? 'Follower' : 'Neuvedeno'
}
</script>

<template>
  <div>
    <AppNavbar />
    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-6xl px-6 py-10">
        <UButton :to="`/lecturer/courses/${courseId}`" icon="i-lucide-arrow-left" color="neutral" variant="ghost" class="mb-6">
          Zpět na detail kurzu
        </UButton>

        <div v-if="pending" class="py-20 text-center">
          <UIcon name="i-lucide-loader-circle" class="size-9 animate-spin text-orange-500" />
          <p class="mt-3 text-muted">Načítání detailu účastníka…</p>
        </div>

        <UAlert v-else-if="error" color="error" title="Účastníka se nepodařilo načíst"
          description="Účastník neexistuje, není v tomto kurzu nebo k němu nemáte přístup.">
          <template #actions>
            <UButton color="error" variant="outline" @click="refresh()">Zkusit znovu</UButton>
          </template>
        </UAlert>

        <template v-else-if="participant && data">
          <div class="mb-8">
            <h1 class="text-3xl font-semibold text-highlighted">Detail účastníka</h1>
            <p class="mt-2 text-muted">Informace o účastníkovi kurzu · pouze pro čtení</p>
          </div>

          <UAlert v-if="requestedType && requestedType !== data.personType" color="warning"
            title="Typ účastníka se liší od odkazu"
            description="Zobrazen je typ osoby vrácený serverem." class="mb-6" />

          <UCard class="mb-6">
            <div class="flex flex-wrap items-center gap-5">
              <div class="flex size-20 shrink-0 items-center justify-center rounded-full bg-orange-100 text-2xl font-semibold text-orange-600">
                {{ initials }}
              </div>
              <div class="min-w-0 flex-1">
                <h2 class="text-2xl font-semibold text-highlighted">{{ participant.fullName }}</h2>
                <div class="mt-3 flex flex-wrap gap-2">
                  <UBadge :color="isStudent ? 'primary' : 'neutral'" variant="subtle">
                    {{ isStudent ? 'Student' : 'Uživatel' }}
                  </UBadge>
                  <template v-if="!isStudent">
                    <UBadge color="primary" variant="subtle">{{ formatDanceRole(participant.danceRole) }}</UBadge>
                    <UBadge :color="participant.rotation ? 'success' : 'neutral'" variant="subtle">
                      Rotace: {{ participant.rotation ? 'Ano' : 'Ne' }}
                    </UBadge>
                  </template>
                </div>
              </div>
            </div>
          </UCard>

          <div class="grid gap-6 lg:grid-cols-3">
            <div class="space-y-6 lg:col-span-2">
              <UCard v-if="isStudent">
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon name="i-lucide-user-round" class="size-5 text-orange-600" />
                    <h2 class="text-lg font-semibold text-highlighted">Osobní údaje studenta</h2>
                  </div>
                </template>
                <div class="grid gap-6 sm:grid-cols-2">
                  <div><p class="text-sm text-muted">Jméno</p><p class="mt-1 font-medium text-highlighted">{{ participant.firstName }} {{ participant.lastName }}</p></div>
                  <div><p class="text-sm text-muted">Datum narození</p><p class="mt-1 font-medium text-highlighted">{{ formatDate(participant.dateOfBirth) }}</p></div>
                  <div><p class="text-sm text-muted">Věk</p><p class="mt-1 font-medium text-highlighted">{{ calculateAge(participant.dateOfBirth) }}</p></div>
                  <div>
                    <p class="text-sm text-muted">Telefon studenta</p>
                    <a v-if="participant.phoneNumber" :href="`tel:${participant.phoneNumber}`" class="mt-1 inline-block font-medium text-orange-600 hover:underline">{{ participant.phoneNumber }}</a>
                    <p v-else class="mt-1 font-medium text-highlighted">Neuvedeno</p>
                  </div>
                </div>
              </UCard>

              <UCard v-if="isStudent">
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon name="i-lucide-contact" class="size-5 text-orange-600" />
                    <h2 class="text-lg font-semibold text-highlighted">Zákonný zástupce</h2>
                  </div>
                </template>
                <div class="grid gap-6 sm:grid-cols-2">
                  <div><p class="text-sm text-muted">Jméno</p><p class="mt-1 font-medium text-highlighted">{{ participant.guardianName || 'Neuvedeno' }}</p></div>
                  <div>
                    <p class="text-sm text-muted">Telefon</p>
                    <a v-if="participant.guardianPhone" :href="`tel:${participant.guardianPhone}`" class="mt-1 inline-block font-medium text-orange-600 hover:underline">{{ participant.guardianPhone }}</a>
                    <p v-else class="mt-1 font-medium text-highlighted">Neuvedeno</p>
                  </div>
                  <div>
                    <p class="text-sm text-muted">E-mail</p>
                    <a v-if="participant.guardianEmail" :href="`mailto:${participant.guardianEmail}`" class="mt-1 inline-block break-all font-medium text-orange-600 hover:underline">{{ participant.guardianEmail }}</a>
                    <p v-else class="mt-1 font-medium text-highlighted">Neuvedeno</p>
                  </div>
                </div>
              </UCard>

              <UCard v-if="!isStudent">
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon name="i-lucide-contact" class="size-5 text-orange-600" />
                    <h2 class="text-lg font-semibold text-highlighted">Kontaktní údaje</h2>
                  </div>
                </template>
                <div class="grid gap-6 sm:grid-cols-2">
                  <div>
                    <p class="text-sm text-muted">E-mail</p>
                    <a v-if="participant.email" :href="`mailto:${participant.email}`" class="mt-1 inline-block break-all font-medium text-orange-600 hover:underline">{{ participant.email }}</a>
                    <p v-else class="mt-1 font-medium text-highlighted">Neuvedeno</p>
                  </div>
                  <div>
                    <p class="text-sm text-muted">Telefon</p>
                    <a v-if="participant.phoneNumber" :href="`tel:${participant.phoneNumber}`" class="mt-1 inline-block font-medium text-orange-600 hover:underline">{{ participant.phoneNumber }}</a>
                    <p v-else class="mt-1 font-medium text-highlighted">Neuvedeno</p>
                  </div>
                </div>
              </UCard>

              <UCard>
                <template #header>
                  <div class="flex items-center justify-between gap-3">
                    <div class="flex items-center gap-3">
                      <UIcon name="i-lucide-book-open" class="size-5 text-orange-600" />
                      <h2 class="text-lg font-semibold text-highlighted">{{ isStudent ? 'Kurz studenta' : 'Kurzy uživatele' }}</h2>
                    </div>
                    <UBadge color="neutral" variant="subtle">{{ participantCourses.length }}</UBadge>
                  </div>
                </template>
                <p v-if="!isStudent" class="mb-5 text-sm text-muted">Zobrazeny jsou pouze kurzy, které vyučujete.</p>
                <p v-if="!participantCourses.length" class="py-8 text-center text-muted">Žádné dostupné kurzy.</p>
                <div v-else class="divide-y divide-default">
                  <div v-for="course in participantCourses" :key="course.courseId"
                    class="flex flex-wrap items-center justify-between gap-4 py-5 first:pt-0 last:pb-0">
                    <div class="flex items-start gap-3">
                      <div class="rounded-xl bg-orange-50 p-3"><UIcon name="i-lucide-calendar-days" class="size-5 text-orange-600" /></div>
                      <div>
                        <h3 class="font-semibold text-highlighted">{{ course.courseName }}</h3>
                        <p v-if="course.startDate || course.endDate" class="mt-1 text-sm text-muted">
                          {{ formatDate(course.startDate) }} – {{ formatDate(course.endDate) }}
                        </p>
                        <div v-if="!isStudent" class="mt-2 flex flex-wrap gap-2">
                          <UBadge color="neutral" variant="subtle">{{ formatDanceRole(course.danceRole) }}</UBadge>
                          <UBadge :color="course.rotation ? 'success' : 'neutral'" variant="subtle">Rotace: {{ course.rotation ? 'Ano' : 'Ne' }}</UBadge>
                        </div>
                      </div>
                    </div>
                    <UButton :to="`/lecturer/courses/${course.courseId}`" icon="i-lucide-arrow-up-right" color="primary" variant="outline" size="sm">Detail kurzu</UButton>
                  </div>
                </div>
              </UCard>

              <UCard v-if="isStudent">
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon name="i-lucide-sticky-note" class="size-5 text-orange-600" />
                    <h2 class="text-lg font-semibold text-highlighted">Poznámka</h2>
                  </div>
                </template>
                <p class="whitespace-pre-line text-sm leading-7 text-muted">{{ participant.note || 'Bez poznámky.' }}</p>
              </UCard>
            </div>

            <div class="space-y-6">
              <UCard v-if="isStudent">
                <template #header><h2 class="text-lg font-semibold text-highlighted">Přiřazený učitel</h2></template>
                <div class="flex items-center gap-3">
                  <UIcon name="i-lucide-graduation-cap" class="size-5 text-orange-600" />
                  <p class="font-medium text-highlighted">{{ participant.teacherName || 'Učitel neuveden' }}</p>
                </div>
              </UCard>

              <UCard v-else>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon name="i-lucide-clipboard-check" class="size-5 text-orange-600" />
                    <h2 class="text-lg font-semibold text-highlighted">Přihláška</h2>
                  </div>
                </template>
                <div class="space-y-5">
                  <div><p class="text-sm text-muted">Stav přihlášky</p><UBadge class="mt-2" color="success" variant="subtle">Přihlášen</UBadge></div>
                  <div><p class="text-sm text-muted">Datum přihlášení</p><p class="mt-1 font-medium text-highlighted">{{ formatDate(participant.registeredAt) }}</p></div>
                  <div><p class="text-sm text-muted">Taneční role</p><p class="mt-1 font-medium text-highlighted">{{ formatDanceRole(participant.danceRole) }}</p></div>
                  <div><p class="text-sm text-muted">Střídání partnerů</p><p class="mt-1 font-medium text-highlighted">{{ participant.rotation ? 'Ano' : 'Ne' }}</p></div>
                </div>
              </UCard>

              <UCard>
                <template #header><h2 class="text-lg font-semibold text-highlighted">Rychlé odkazy</h2></template>
                <div class="space-y-3">
                  <UButton :to="`/lecturer/courses/${courseId}`" icon="i-lucide-arrow-left" color="primary" variant="outline" block>Zpět na kurz</UButton>
                  <UButton to="/lecturer/courses" icon="i-lucide-book-open" color="neutral" variant="outline" block>Moje kurzy</UButton>
                </div>
              </UCard>
            </div>
          </div>
        </template>
      </div>
    </main>
  </div>
</template>
