
<script setup lang="ts">
const route = useRoute()

const courseId = computed(() => String(route.params.id))

type Instructor = {
  firstName?: string
  lastName?: string
  name?: string
}

type CourseSession = {
  weekday: string | number
  startsAt: string
  endsAt: string
  room?: string | { name: string } | null
}

type CourseDetail = {
  id: string
  name: string
  courseType?: string
  level: number
  description?: string | null
  price: number
  capacity: number
  occupied?: number
  availablePlaces?: number
  startDate: string
  endDate: string
  balancedRoles: boolean
  danceStyles?: (string | { name: string })[]
  instructors?: (string | Instructor)[]
  sessions?: CourseSession[]
}

const {
  data: course,
  pending,
  error,
  refresh
} = useFetch<CourseDetail>(
  () => `/api/courses/${courseId.value}`,
  {
    server: false
  }
)

const formatPrice = (price: number) =>
  new Intl.NumberFormat('cs-CZ', {
    maximumFractionDigits: 2
  }).format(price)

function formatDate(value: string) {
  const date = new Date(value.slice(0, 10) + 'T12:00:00')

  if (Number.isNaN(date.getTime())) return value

  return date.toLocaleDateString('cs-CZ')
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function formatInstructor(
  instructor: string | Instructor
) {
  if (typeof instructor === 'string') {
    return instructor
  }

  return instructor.name ||
    `${instructor.firstName ?? ''} ${instructor.lastName ?? ''}`.trim()
}

function formatDanceStyle(
  style: string | { name: string }
) {
  return typeof style === 'string' ? style : style.name
}

const availablePlaces = computed(() => {
  if (!course.value) return 0

  if (course.value.availablePlaces !== undefined) {
    return course.value.availablePlaces
  }

  return Math.max(
    0,
    course.value.capacity - (course.value.occupied ?? 0)
  )
})

const registrationDeadline = computed(() => {
  if (!course.value) return ''

  const date = new Date(
    course.value.startDate.slice(0, 10) + 'T12:00:00'
  )

  date.setDate(date.getDate() - 7)

  return date.toLocaleDateString('cs-CZ')
})

// Informační upozornění na datum.
// Rozhodující kontrolu provede backend při přihlášení.
const registrationClosed = computed(() => {
  if (!course.value) return true

  const start = new Date(
    course.value.startDate.slice(0, 10) + 'T12:00:00'
  )

  start.setDate(start.getDate() - 7)
  start.setHours(23, 59, 59, 999)

  return new Date() > start
})
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-5xl px-6 py-10">

        <!-- Návrat na seznam kurzů -->
        <UButton
          to="/nase-tance"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-6"
        >
          Zpět na naše tance
        </UButton>

        <!-- Načítání -->
        <div
          v-if="pending"
          class="py-16 text-center text-muted"
        >
          Načítání kurzu…
        </div>

        <!-- Chyba -->
        <UAlert
          v-else-if="error"
          color="error"
          title="Kurz se nepodařilo načíst."
          description="Zkontrolujte, zda kurz existuje."
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

        <template v-else-if="course">

          <!-- Hlavička -->
          <div class="mb-8">
            <div class="mb-3 flex flex-wrap gap-2">
              <UBadge color="primary" variant="subtle">
                Úroveň {{ course.level }}
              </UBadge>

              <UBadge
                v-for="(style, index) in course.danceStyles ?? []"
                :key="index"
                color="neutral"
                variant="subtle"
              >
                {{ formatDanceStyle(style) }}
              </UBadge>
            </div>

            <h1 class="text-3xl font-bold text-highlighted">
              {{ course.name }}
            </h1>

            <p class="mt-3 text-muted">
              {{
                course.description ||
                'Přidejte se k našemu tanečnímu kurzu.'
              }}
            </p>
          </div>

          <div class="grid gap-6 lg:grid-cols-3">

            <!-- Informace o kurzu -->
            <div class="space-y-6 lg:col-span-2">

              <UCard>
                <template #header>
                  <h2 class="text-xl font-semibold">
                    Informace o kurzu
                  </h2>
                </template>

                <div class="space-y-4">

                  <div class="flex items-start gap-3">
                    <UIcon
                      name="i-lucide-calendar-days"
                      class="mt-1 size-5 text-primary"
                    />

                    <div>
                      <p class="font-medium">
                        Termín kurzu
                      </p>

                      <p class="text-sm text-muted">
                        {{ formatDate(course.startDate) }}
                        –
                        {{ formatDate(course.endDate) }}
                      </p>
                    </div>
                  </div>

                  <div class="flex items-start gap-3">
                    <UIcon
                      name="i-lucide-users"
                      class="mt-1 size-5 text-primary"
                    />

                    <div>
                      <p class="font-medium">
                        Kapacita
                      </p>

                      <p class="text-sm text-muted">
                        {{ availablePlaces }} volných míst
                        z {{ course.capacity }}
                      </p>
                    </div>
                  </div>

                  <div
                    v-if="course.instructors?.length"
                    class="flex items-start gap-3"
                  >
                    <UIcon
                      name="i-lucide-user-round"
                      class="mt-1 size-5 text-primary"
                    />

                    <div>
                      <p class="font-medium">
                        Lektoři
                      </p>

                      <p class="text-sm text-muted">
                        {{
                          course.instructors
                            .map(formatInstructor)
                            .join(', ')
                        }}
                      </p>
                    </div>
                  </div>

                </div>
              </UCard>

              <!-- Rozvrh -->
              <UCard>
                <template #header>
                  <h2 class="text-xl font-semibold">
                    Rozvrh lekcí
                  </h2>
                </template>

                <div
                  v-if="course.sessions?.length"
                  class="space-y-3"
                >
                  <div
                    v-for="(session, index) in course.sessions"
                    :key="index"
                    class="flex items-center justify-between gap-4 border-b border-default py-3 last:border-0"
                  >
                    <div>
                      <p class="font-medium">
                        {{ session.weekday }}
                      </p>

                      <p class="text-sm text-muted">
                        {{
                          typeof session.room === 'string'
                            ? session.room
                            : session.room?.name || 'Sál neuveden'
                        }}
                      </p>
                    </div>

                    <p class="font-semibold">
                      {{ formatTime(session.startsAt) }}
                      –
                      {{ formatTime(session.endsAt) }}
                    </p>
                  </div>
                </div>

                <p v-else class="text-sm text-muted">
                  Rozvrh zatím není uveden.
                </p>
              </UCard>

            </div>

            <!-- Přihlášení -->
            <div>
              <UCard>
                <div class="space-y-5">

                  <div>
                    <p class="text-sm text-muted">
                      Cena kurzu za osobu
                    </p>

                    <p class="mt-1 text-3xl font-bold text-primary">
                      {{ formatPrice(course.price) }} Kč
                    </p>

                    <p class="mt-1 text-xs text-muted">
                      Platba pomocí kreditů.
                      1 kredit = 1 Kč.
                    </p>
                  </div>

                  <div class="border-t border-default pt-4">
                    <p class="text-sm font-medium">
                      Přihlašování do:
                    </p>

                    <p class="text-sm text-muted">
                      {{ registrationDeadline }}
                    </p>
                  </div>

                  <UAlert
                    v-if="registrationClosed"
                    color="warning"
                    title="Přihlašování skončilo."
                  />

                  <UAlert
                    v-else-if="availablePlaces === 0"
                    color="warning"
                    title="Kurz je obsazený."
                  />

                  <UButton
                    v-if="!registrationClosed && availablePlaces > 0"
                    :to="`/prihlaseni/${course.id}`"
                    icon="i-lucide-calendar-check"
                    size="lg"
                    block
                  >
                    Přihlásit se na kurz
                  </UButton>

                  <UButton
                    v-else
                    disabled
                    size="lg"
                    block
                  >
                    Přihlášení není dostupné
                  </UButton>

                  <p class="text-xs text-muted">
                    U kurzů s tanečními rolemi se můžete
                    přihlásit sami nebo společně s partnerem.
                    Počet míst a kredity se ověří při potvrzení.
                  </p>

                </div>
              </UCard>
            </div>

          </div>

        </template>
      </div>
    </main>
  </div>
</template>
