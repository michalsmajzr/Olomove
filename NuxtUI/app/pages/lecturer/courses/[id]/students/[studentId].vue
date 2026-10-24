
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

const route = useRoute()

const courseId = computed(() => String(route.params.id))
const studentId = computed(() => String(route.params.studentId))

type Student = {
  userId: string
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  danceRole: string | null
  rotation: boolean
  registeredAt: string
}

type StudentCourse = {
  courseId: string
  courseName: string
  startDate: string
  endDate: string
  danceRole: string | null
  rotation: boolean
}

type StudentDetailResponse = {
  student: Student
  courses: StudentCourse[]
}

const {
  data,
  pending,
  error,
  refresh
} = useFetch<StudentDetailResponse>(
  () => `/api/lecturer/courses/${courseId.value}/students/${studentId.value}`,
  {
    credentials: 'include',
    server: false
  }
)

const student = computed(() => data.value?.student ?? null)
const studentCourses = computed(() => data.value?.courses ?? [])

function formatDate(value: string | null | undefined): string {
  if (!value) return '—'

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return '—'

  return date.toLocaleDateString('cs-CZ', {
    day: 'numeric',
    month: 'numeric',
    year: 'numeric'
  })
}

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

const initials = computed(() => {
  if (!student.value) return ''

  return (
    (student.value.firstName?.charAt(0) ?? '') +
    (student.value.lastName?.charAt(0) ?? '')
  ).toUpperCase()
})
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-6xl px-6 py-10">

        <!-- Návrat -->
        <UButton
          :to="`/lecturer/courses/${courseId}`"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-6"
        >
          Zpět na detail kurzu
        </UButton>

        <!-- Načítání -->
        <div
          v-if="pending"
          class="py-20 text-center"
        >
          <UIcon
            name="i-lucide-loader-circle"
            class="size-9 animate-spin text-orange-500"
          />

          <p class="mt-3 text-muted">
            Načítání detailu studenta…
          </p>
        </div>

        <!-- Chyba -->
        <UAlert
          v-else-if="error"
          color="error"
          title="Studenta se nepodařilo načíst"
          description="Student neexistuje, není přihlášený na tento kurz nebo k němu nemáte přístup."
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

        <!-- Detail studenta -->
        <template v-else-if="student">

          <!-- Hlavička -->
          <div class="mb-8">
            <p class="mb-2 text-sm font-medium text-orange-600">
              OloMove · Lektorská sekce
            </p>

            <h1 class="text-3xl font-semibold text-highlighted">
              Detail studenta
            </h1>

            <p class="mt-2 text-muted">
              Základní informace a kurzy studenta
            </p>
          </div>

          <!-- Profilová karta -->
          <UCard class="mb-6">
            <div class="flex flex-wrap items-center gap-5">

              <div
                class="flex size-20 shrink-0 items-center justify-center rounded-full bg-orange-100 text-2xl font-semibold text-orange-600"
              >
                {{ initials }}
              </div>

              <div class="min-w-0 flex-1">
                <h2 class="text-2xl font-semibold text-highlighted">
                  {{ student.fullName }}
                </h2>

                <p class="mt-1 text-sm text-muted">
                  Student tanečního kurzu
                </p>

                <div class="mt-3 flex flex-wrap gap-2">
                  <UBadge color="primary" variant="subtle">
                    {{ formatDanceRole(student.danceRole) }}
                  </UBadge>

                  <UBadge
                    :color="student.rotation ? 'success' : 'neutral'"
                    variant="subtle"
                  >
                    Rotace: {{ student.rotation ? 'Ano' : 'Ne' }}
                  </UBadge>
                </div>
              </div>
            </div>
          </UCard>

          <!-- Hlavní obsah -->
          <div class="grid gap-6 lg:grid-cols-3">

            <!-- Levá část -->
            <div class="space-y-6 lg:col-span-2">

              <!-- Kontaktní údaje -->
              <UCard>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon
                      name="i-lucide-contact"
                      class="size-5 text-orange-600"
                    />

                    <h2 class="text-lg font-semibold text-highlighted">
                      Kontaktní údaje
                    </h2>
                  </div>
                </template>

                <div class="grid gap-6 sm:grid-cols-2">

                  <div>
                    <p class="mb-2 text-sm text-muted">
                      E-mail
                    </p>

                    <a
                      v-if="student.email"
                      :href="`mailto:${student.email}`"
                      class="flex items-center gap-2 font-medium text-orange-600 hover:underline"
                    >
                      <UIcon
                        name="i-lucide-mail"
                        class="size-4"
                      />

                      {{ student.email }}
                    </a>

                    <p v-else class="text-highlighted">
                      Neuvedeno
                    </p>
                  </div>

                  <div>
                    <p class="mb-2 text-sm text-muted">
                      Telefon
                    </p>

                    <a
                      v-if="student.phoneNumber"
                      :href="`tel:${student.phoneNumber}`"
                      class="flex items-center gap-2 font-medium text-orange-600 hover:underline"
                    >
                      <UIcon
                        name="i-lucide-phone"
                        class="size-4"
                      />

                      {{ student.phoneNumber }}
                    </a>

                    <p v-else class="text-highlighted">
                      Neuvedeno
                    </p>
                  </div>

                </div>
              </UCard>

              <!-- Kurzy studenta -->
              <UCard>
                <template #header>
                  <div class="flex items-center justify-between gap-3">
                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-book-open"
                        class="size-5 text-orange-600"
                      />

                      <h2 class="text-lg font-semibold text-highlighted">
                        Kurzy studenta
                      </h2>
                    </div>

                    <UBadge color="neutral" variant="subtle">
                      {{ studentCourses.length }}
                    </UBadge>
                  </div>
                </template>

                <p class="mb-5 text-sm text-muted">
                  Zobrazeny jsou pouze kurzy, které vyučujete.
                </p>

                <div
                  v-if="!studentCourses.length"
                  class="py-8 text-center text-muted"
                >
                  Student nemá žádné dostupné kurzy.
                </div>

                <div v-else class="divide-y divide-default">
                  <div
                    v-for="course in studentCourses"
                    :key="course.courseId"
                    class="flex flex-wrap items-center justify-between gap-4 py-5 first:pt-0 last:pb-0"
                  >
                    <div class="flex items-start gap-3">
                      <div class="rounded-xl bg-orange-50 p-3">
                        <UIcon
                          name="i-lucide-calendar-days"
                          class="size-5 text-orange-600"
                        />
                      </div>

                      <div>
                        <h3 class="font-semibold text-highlighted">
                          {{ course.courseName }}
                        </h3>

                        <p class="mt-1 text-sm text-muted">
                          {{ formatDate(course.startDate) }}
                          –
                          {{ formatDate(course.endDate) }}
                        </p>

                        <div class="mt-2 flex flex-wrap gap-2">
                          <UBadge color="neutral" variant="subtle">
                            {{ formatDanceRole(course.danceRole) }}
                          </UBadge>

                          <UBadge
                            :color="course.rotation ? 'success' : 'neutral'"
                            variant="subtle"
                          >
                            Rotace: {{ course.rotation ? 'Ano' : 'Ne' }}
                          </UBadge>
                        </div>
                      </div>
                    </div>

                    <UButton
                      :to="`/lecturer/courses/${course.courseId}`"
                      icon="i-lucide-arrow-up-right"
                      color="primary"
                      variant="outline"
                      size="sm"
                    >
                      Detail kurzu
                    </UButton>
                  </div>
                </div>
              </UCard>
            </div>

            <!-- Pravá část -->
            <div class="space-y-6">

              <!-- Informace o přihlášce -->
              <UCard>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon
                      name="i-lucide-clipboard-check"
                      class="size-5 text-orange-600"
                    />

                    <h2 class="text-lg font-semibold text-highlighted">
                      Přihláška
                    </h2>
                  </div>
                </template>

                <div class="space-y-5">

                  <div>
                    <p class="text-sm text-muted">
                      Stav přihlášky
                    </p>

                    <div class="mt-2">
                      <UBadge color="success" variant="subtle">
                        Přihlášen
                      </UBadge>
                    </div>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Datum přihlášení
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ formatDate(student.registeredAt) }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Taneční role
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ formatDanceRole(student.danceRole) }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Střídání partnerů
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ student.rotation ? 'Ano' : 'Ne' }}
                    </p>
                  </div>

                </div>
              </UCard>

              <!-- Navigace -->
              <UCard>
                <template #header>
                  <h2 class="text-lg font-semibold text-highlighted">
                    Rychlé odkazy
                  </h2>
                </template>

                <div class="space-y-3">
                  <UButton
                    :to="`/lecturer/courses/${courseId}`"
                    icon="i-lucide-arrow-left"
                    color="primary"
                    variant="outline"
                    block
                  >
                    Zpět na kurz
                  </UButton>

                  <UButton
                    to="/lecturer/courses"
                    icon="i-lucide-book-open"
                    color="neutral"
                    variant="outline"
                    block
                  >
                    Moje kurzy
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
