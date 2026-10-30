<script setup lang="ts">
// Veřejný kalendář. Používá pouze kurzy a rozvrhy vrácené GET /api/courses.
// Nezapisuje nic do databáze a neupravuje přihlášky.
type CourseSession = {
  weekday: number | string
  startsAt: string
  endsAt: string
  room?: string | null
}

type CalendarCourse = {
  id: string
  name: string
  startDate: string
  endDate: string
  level: number
  capacity: number
  occupiedPlaces?: number
  instructors: string[]
  sessions?: CourseSession[]
}

type CalendarEvent = {
  key: string
  date: string
  course: CalendarCourse
  startsAt: string | null
  endsAt: string | null
  room: string | null
  isCourseStart: boolean
}

const props = defineProps<{ courses: CalendarCourse[] }>()

function keyOf(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
function fromKey(key: string): Date {
  const [y, m, d] = key.slice(0, 10).split('-').map(Number)
  return new Date(y!, m! - 1, d!, 12)
}
function mondayWeekday(value: number | string): number | null {
  if (typeof value === 'number') return value >= 0 && value <= 6 ? value : null
  const names = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
  const text = String(value).toLowerCase().trim()
  const found = names.indexOf(text)
  if (found >= 0) return found
  return /^[0-6]$/.test(text) ? Number(text) : null
}
function formatTime(value: string | null): string {
  return value?.slice(0, 5) || ''
}

const now = new Date()
const todayKey = keyOf(now)
const year = ref(now.getFullYear())
const month = ref(now.getMonth())
const selectedDay = ref(todayKey)
const monthTitle = computed(() => new Intl.DateTimeFormat('cs-CZ', {
  month: 'long', year: 'numeric'
}).format(new Date(year.value, month.value, 1)))
const selectedDayTitle = computed(() => new Intl.DateTimeFormat('cs-CZ', {
  weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
}).format(fromKey(selectedDay.value)))

const monthDays = computed<(string | null)[]>(() => {
  const first = new Date(year.value, month.value, 1)
  const endDay = new Date(year.value, month.value + 1, 0).getDate()
  const offset = (first.getDay() + 6) % 7
  const cells: (string | null)[] = Array(offset).fill(null)
  for (let d = 1; d <= endDay; d++) cells.push(keyOf(new Date(year.value, month.value, d)))
  while (cells.length % 7 !== 0) cells.push(null)
  return cells
})

const monthEvents = computed<CalendarEvent[]>(() => {
  const first = new Date(year.value, month.value, 1, 12)
  const last = new Date(year.value, month.value + 1, 0, 12)
  const events: CalendarEvent[] = []

  for (const course of props.courses) {
    const start = fromKey(course.startDate)
    const end = fromKey(course.endDate)
    if (end < first || start > last) continue

    if (!course.sessions?.length) {
      if (start >= first && start <= last) {
        events.push({
          key: `${course.id}-${keyOf(start)}-start`,
          date: keyOf(start), course,
          startsAt: null, endsAt: null, room: null,
          isCourseStart: true
        })
      }
      continue
    }

    const rangeStart = start > first ? start : first
    const rangeEnd = end < last ? end : last
    const firstWeekday = (rangeStart.getDay() + 6) % 7

    for (const [i, session] of course.sessions.entries()) {
      const weekday = mondayWeekday(session.weekday)
      if (weekday === null) continue
      const day = new Date(rangeStart)
      day.setDate(day.getDate() + (weekday - firstWeekday + 7) % 7)
      while (day <= rangeEnd) {
        const date = keyOf(day)
        events.push({
          key: `${course.id}-${i}-${date}`,
          date, course,
          startsAt: session.startsAt,
          endsAt: session.endsAt,
          room: session.room ?? null,
          isCourseStart: false
        })
        day.setDate(day.getDate() + 7)
      }
    }
  }

  return events.sort((a, b) =>
    a.date.localeCompare(b.date) ||
    (a.startsAt ?? '').localeCompare(b.startsAt ?? '') ||
    a.course.name.localeCompare(b.course.name)
  )
})

const daysWithLessons = computed(() => {
  const dayMap = new Map<string, CalendarEvent[]>()
  for (const event of monthEvents.value) {
    const day = dayMap.get(event.date) ?? []
    day.push(event)
    dayMap.set(event.date, day)
  }
  return dayMap
})
const selectedEvents = computed(() => daysWithLessons.value.get(selectedDay.value) ?? [])

// Po načtení kurzů přejdeme na nejbližší nadcházející kurz, pokud aktuální měsíc nic nenabízí.
const initialized = ref(false)
watch(() => props.courses, async (courses) => {
  if (initialized.value || !courses.length) return
  initialized.value = true
  if (monthEvents.value.length) {
    selectedDay.value = monthEvents.value.find(e => e.date >= todayKey)?.date
      ?? monthEvents.value[0]!.date
    return
  }
  const nextCourse = [...courses]
    .filter(c => c.endDate.slice(0, 10) >= todayKey)
    .sort((a, b) => a.startDate.localeCompare(b.startDate))[0]
  if (!nextCourse) return
  const date = fromKey(nextCourse.startDate.slice(0, 10) > todayKey
    ? nextCourse.startDate : todayKey)
  year.value = date.getFullYear()
  month.value = date.getMonth()
  await nextTick()
  selectedDay.value = monthEvents.value.find(e => e.date >= todayKey)?.date
    ?? keyOf(date)
}, { immediate: true })

function changeMonth(amount: number) {
  const date = new Date(year.value, month.value + amount, 1)
  year.value = date.getFullYear()
  month.value = date.getMonth()
  selectedDay.value = keyOf(date)
}
function goToToday() {
  const date = new Date()
  year.value = date.getFullYear()
  month.value = date.getMonth()
  selectedDay.value = keyOf(date)
}
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="text-xl font-semibold">Kalendář tanečních lekcí</h2>
          <p class="mt-1 text-sm text-muted">Vyberte den a zobrazte si kurzy, které se konají.</p>
        </div>
        <div class="flex items-center gap-1">
          <UButton icon="i-lucide-chevron-left" color="neutral" variant="ghost" aria-label="Předchozí měsíc" @click="changeMonth(-1)" />
          <span class="min-w-36 text-center text-sm font-semibold capitalize">{{ monthTitle }}</span>
          <UButton icon="i-lucide-chevron-right" color="neutral" variant="ghost" aria-label="Následující měsíc" @click="changeMonth(1)" />
          <UButton size="xs" color="neutral" variant="outline" class="ml-1" @click="goToToday">Dnes</UButton>
        </div>
      </div>
    </template>

    <div class="grid gap-7 lg:grid-cols-[1fr_0.95fr]">
      <div>
        <div class="grid grid-cols-7 gap-1 text-center">
          <div v-for="day in ['Po', 'Út', 'St', 'Čt', 'Pá', 'So', 'Ne']" :key="day" class="py-2 text-xs font-semibold text-muted">
            {{ day }}
          </div>
          <div v-for="(day, index) in monthDays" :key="index">
            <button
              v-if="day"
              type="button"
              class="flex min-h-12 w-full flex-col items-center justify-center rounded-lg border text-sm transition-colors"
              :class="day === selectedDay
                ? 'border-orange-500 bg-orange-50 text-orange-700 font-semibold'
                : day === todayKey
                  ? 'border-orange-200 bg-default'
                  : 'border-transparent hover:bg-muted/60'"
              :aria-label="`Zobrazit lekce ${day}`"
              @click="selectedDay = day"
            >
              {{ Number(day.slice(-2)) }}
              <span v-if="daysWithLessons.has(day)" class="mt-1 size-1.5 rounded-full bg-orange-500" />
            </button>
          </div>
        </div>
        <p class="mt-3 text-xs text-muted">Oranžová tečka znamená, že je na tento den naplánovaná lekce.</p>
      </div>

      <div class="space-y-3">
        <h3 class="font-semibold capitalize">{{ selectedDayTitle }}</h3>
        <p v-if="!selectedEvents.length" class="rounded-lg bg-muted/40 p-5 text-sm text-muted">
          Na tento den nejsou ve vybraných kurzech naplánované lekce.
        </p>
        <NuxtLink
          v-for="event in selectedEvents"
          :key="event.key"
          :to="`/nase-tance/${event.course.id}`"
          class="block rounded-xl border border-default bg-default p-4 transition-colors hover:border-orange-400"
        >
          <div class="flex items-start justify-between gap-4">
            <div>
              <p class="font-semibold text-highlighted">{{ event.course.name }}</p>
              <p class="mt-1 text-sm text-muted">
                <template v-if="event.isCourseStart">Začátek kurzu (rozvrh není uveden)</template>
                <template v-else>
                  {{ formatTime(event.startsAt) }}–{{ formatTime(event.endsAt) }}
                  <span v-if="event.room"> · {{ event.room }}</span>
                </template>
              </p>
              <p class="mt-1 text-xs text-muted">
                Úroveň {{ event.course.level }} · {{ event.course.instructors.join(', ') || 'Lektor neuveden' }}
              </p>
            </div>
            <span class="shrink-0 text-xs text-muted">
              {{ event.course.occupiedPlaces ?? '—' }}/{{ event.course.capacity }}
            </span>
          </div>
        </NuxtLink>
      </div>
    </div>
  </UCard>
</template>
