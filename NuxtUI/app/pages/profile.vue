<script setup lang="ts">
definePageMeta({ middleware: 'user' })

type CourseSession = {
  weekday: string | number
  startsAt: string
  endsAt: string
  room: string | null
}

type Enrollment = {
  id: string
  status: string
  danceRole: string | null
  rotation: boolean
  paidCredits: number
  partner: {
    savedPartnerId: string
    firstName: string
    lastName: string
    danceRole: string | null
    rotation: boolean
  } | null
  course: {
    id: string
    name: string
    level: number
    startDate: string
    endDate: string
    sessions: CourseSession[]
    instructors: string[]
  }
}

type MyCoursesResponse = {
  credit: number
  enrollments: Enrollment[]
}

type UserProfile = {
  id: string
  firstName: string
  lastName: string
  email: string | null
  phoneNumber: string | null
  createdAt: string
}

type SavedPartner = {
  id: string
  firstName: string
  lastName: string
  email: string | null
  phoneNumber: string | null
}

const toast = useToast()
const selectedTab = ref<'courses' | 'calendar' | 'edit'>('courses')
const selectedCourseTab = ref<'active' | 'history'>('active')
const confirmCancelId = ref<string | null>(null)
const cancelingId = ref<string | null>(null)
const cancelError = ref('')
const showTopUp = ref(false)
const topUpAmount = ref(500)

const now = new Date()

const { data: coursesData, pending: coursesPending, error: coursesError, refresh: refreshCourses } =
  useFetch<MyCoursesResponse>('/api/user/my-courses', {
    credentials: 'include', server: false
  })
const { data: profile, pending: profilePending, error: profileError, refresh: refreshProfile } =
  useFetch<UserProfile>('/api/user/profile', {
    credentials: 'include', server: false
  })
const { data: partners, pending: partnersPending, error: partnersError, refresh: refreshPartners } =
  useFetch<SavedPartner[]>('/api/user/saved-partners', {
    credentials: 'include', server: false
  })

// Sdílený stav kreditu s AppNavbar.vue (pokud používá olomove-credit).
const navbarCredit = useState<number | null>('olomove-credit', () => null)
watch(() => coursesData.value?.credit, credit => {
  if (typeof credit === 'number') navbarCredit.value = credit
}, { immediate: true })

// Po návratu z přihlášky znovu načteme aktuální kreditový zůstatek.
onMounted(() => { void refreshCourses() })

const form = reactive({ firstName: '', lastName: '', phoneNumber: '' })
const isSaving = ref(false)
const saveError = ref('')

watch(profile, value => {
  if (!value) return
  form.firstName = value.firstName
  form.lastName = value.lastName
  form.phoneNumber = value.phoneNumber ?? ''
}, { immediate: true })

function dateKey(date: Date): string {
  return [date.getFullYear(), String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0')].join('-')
}

function dateFromKey(key: string): Date {
  const [year, month, day] = key.slice(0, 10).split('-').map(Number)
  return new Date(year!, month! - 1, day!, 12)
}

const todayKey = dateKey(now)
const enrollments = computed(() => coursesData.value?.enrollments ?? [])
const activeCourses = computed(() => enrollments.value.filter(e =>
  e.status === 'Registered' && e.course.endDate.slice(0, 10) >= todayKey
))
const historyCourses = computed(() => enrollments.value.filter(e =>
  e.status !== 'Registered' || e.course.endDate.slice(0, 10) < todayKey
))
const displayedCourses = computed(() => selectedCourseTab.value === 'active'
  ? activeCourses.value : historyCourses.value)
const calendarCourses = computed(() => enrollments.value
  .filter(enrollment => enrollment.status === 'Registered')
  .map(enrollment => ({
    id: enrollment.course.id,
    name: enrollment.course.name,
    startDate: enrollment.course.startDate,
    endDate: enrollment.course.endDate,
    level: enrollment.course.level,
    instructors: enrollment.course.instructors,
    sessions: enrollment.course.sessions
  })))

function formatDate(value: string): string {
  return dateFromKey(value).toLocaleDateString('cs-CZ')
}
function formatTime(value?: string | null): string {
  return value?.slice(0, 5) || '—'
}
function formatWeekday(value: string | number): string {
  const labels = ['Pondělí', 'Úterý', 'Středa', 'Čtvrtek', 'Pátek', 'Sobota', 'Neděle']
  const index = weekdayAsMondayIndex(value)
  return index === null ? String(value) : labels[index]!
}
function formatCredit(value: number): string {
  return new Intl.NumberFormat('cs-CZ', { maximumFractionDigits: 2 }).format(value)
}
function enrollmentStatus(status: string): string {
  if (status === 'Registered') return 'Přihlášen/a'
  if (status === 'WaitingList') return 'Čekací listina'
  if (status === 'Cancelled') return 'Zrušeno'
  return status
}
function weekdayAsMondayIndex(value: string | number): number | null {
  const days = ['monday', 'tuesday', 'wednesday', 'thursday',
    'friday', 'saturday', 'sunday']
  if (typeof value === 'number' && Number.isInteger(value) && value >= 0 && value <= 6) return value
  const label = String(value).toLowerCase().trim()
  if (/^[0-6]$/.test(label)) return Number(label)
  const index = days.indexOf(label)
  return index < 0 ? null : index
}

function cancellationDeadline(startDate: string): string {
  const date = dateFromKey(startDate)
  date.setDate(date.getDate() - 7)
  return dateKey(date)
}
function canCancel(enrollment: Enrollment): boolean {
  return enrollment.status === 'Registered'
    && todayKey <= cancellationDeadline(enrollment.course.startDate)
}
async function cancelEnrollment(enrollment: Enrollment) {
  if (!canCancel(enrollment) || cancelingId.value) return
  cancelingId.value = enrollment.id
  cancelError.value = ''
  try {
    await $fetch(`/api/courses/${enrollment.course.id}/enrollments/cancel`, {
      method: 'POST', credentials: 'include'
    })
    confirmCancelId.value = null
    await refreshCourses()
    toast.add({ title: 'Přihláška zrušena',
      description: 'Zaplacené kredity byly vráceny na váš účet.', color: 'success' })
  } catch (error: any) {
    const data = error?.data
    cancelError.value = typeof data === 'string' ? data
      : (data?.detail || data?.message || data?.title || 'Odhlášení se nezdařilo.')
    toast.add({ title: 'Chyba při odhlášení', description: cancelError.value, color: 'error' })
  } finally {
    cancelingId.value = null
  }
}

function resetForm() {
  if (!profile.value) return
  form.firstName = profile.value.firstName
  form.lastName = profile.value.lastName
  form.phoneNumber = profile.value.phoneNumber ?? ''
  saveError.value = ''
}
async function saveProfile() {
  if (isSaving.value || !profile.value) return
  saveError.value = ''
  const firstName = form.firstName.trim()
  const lastName = form.lastName.trim()
  const phoneNumber = form.phoneNumber.trim()
  if (!firstName || !lastName) {
    saveError.value = 'Jméno a příjmení jsou povinné.'
    return
  }
  if (firstName.length > 100 || lastName.length > 100 || phoneNumber.length > 30) {
    saveError.value = 'Některý údaj je příliš dlouhý.'
    return
  }
  isSaving.value = true
  try {
    profile.value = await $fetch<UserProfile>('/api/user/profile', {
      method: 'PUT', credentials: 'include', body: {
        firstName, lastName, phoneNumber: phoneNumber || null
      }
    })
    toast.add({ title: 'Profil aktualizován', color: 'success' })
  } catch {
    saveError.value = 'Profil se nepodařilo uložit.'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />
    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-6xl px-5 py-10">
        <div class="mb-6 flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 class="text-3xl font-semibold text-highlighted">Můj profil</h1>
            <p class="mt-2 text-sm text-muted">Moje lekce, přihlášky a osobní údaje.</p>
          </div>
          <div class="min-w-64 rounded-2xl border border-default bg-white px-7 py-5 shadow-sm">
            <div class="flex items-center gap-2 text-sm font-semibold text-orange-600">
              <UIcon name="i-lucide-coins" class="size-5" /> Moje kredity
            </div>
            <p class="mt-2 text-4xl font-bold text-highlighted" aria-live="polite">
              {{ coursesData ? formatCredit(coursesData.credit) : (coursesPending ? 'Načítám…' : '—') }}
              <span v-if="coursesData" class="text-sm font-normal text-muted">kreditů</span>
            </p>
            <div class="mt-3 flex items-center gap-2">
              <UButton :ui="{ base: 'cursor-pointer' }" size="sm" icon="i-lucide-qr-code" @click="selectedTab = 'courses'; showTopUp = !showTopUp">
                Dobít
              </UButton>
            </div>
          </div>
        </div>

        <!-- Tři přehledné záložky podle návrhu. -->
        <div class="mb-7 grid max-w-2xl grid-cols-3 gap-1 rounded-xl bg-muted/50 p-1">
          <UButton type="button" color="neutral" variant="ghost" :ui="{ base: 'cursor-pointer justify-center', padding: { sm: '', md: '' } }" class="px-3 py-3" :class="selectedTab === 'courses' ? 'bg-orange-500 text-white hover:bg-orange-500 hover:text-white' : ''" @click="selectedTab = 'courses'">
            Moje lekce
          </UButton>
          <UButton type="button" color="neutral" variant="ghost" :ui="{ base: 'cursor-pointer justify-center', padding: { sm: '', md: '' } }" class="px-3 py-3" :class="selectedTab === 'calendar' ? 'bg-orange-500 text-white hover:bg-orange-500 hover:text-white' : ''" @click="selectedTab = 'calendar'">
            Můj kalendář
          </UButton>
          <UButton type="button" color="neutral" variant="ghost" :ui="{ base: 'cursor-pointer justify-center', padding: { sm: '', md: '' } }" class="px-3 py-3" :class="selectedTab === 'edit' ? 'bg-orange-500 text-white hover:bg-orange-500 hover:text-white' : ''" @click="selectedTab = 'edit'">
            Mé údaje
          </UButton>
        </div>

        <section v-if="selectedTab !== 'edit'" class="space-y-6">
          <!-- Dobíjení je zatím pouze připravená vizuální část. -->
          <UCard v-if="showTopUp && selectedTab === 'courses'">
            <template #header>
              <div class="flex items-center justify-between gap-3">
                <h2 class="font-semibold">Dobití kreditů</h2>
                <UBadge color="warning" variant="subtle">Připravujeme</UBadge>
              </div>
            </template>
            <p class="mb-4 text-sm text-muted">Zde později zobrazíme platební QR kód. Zatím se nic neplatí a kredity se nepřipisují.</p>
            <div class="grid gap-4 sm:grid-cols-2">
              <div>
                <label for="top-up-amount" class="mb-2 block text-sm font-medium">Částka v Kč</label>
                <input id="top-up-amount" v-model.number="topUpAmount" type="number" min="1" step="1" class="w-full rounded-md border border-default bg-default px-3 py-2">
              </div>
              <div class="flex min-h-24 items-center justify-center rounded-lg border border-dashed border-default text-muted">
                <UIcon name="i-lucide-qr-code" class="mr-2 size-6" /> Místo pro QR platbu
              </div>
            </div>
            <div class="mt-4 text-right"><UButton :ui="{ base: 'cursor-pointer' }" color="neutral" variant="outline" @click="showTopUp = false">Zavřít</UButton></div>
          </UCard>

          <UAlert v-if="coursesError" color="error" title="Kurzy a kredity se nepodařilo načíst">
            <template #actions><UButton color="error" variant="outline" @click="refreshCourses()">Zkusit znovu</UButton></template>
          </UAlert>
          <p v-else-if="coursesPending && !coursesData" class="py-8 text-center text-muted">Načítání…</p>

          <template v-else-if="coursesData">
            <div v-if="selectedTab === 'calendar'">
              <CourseCalendar :courses="calendarCourses" />
            </div>

            <div v-else class="space-y-6">
            <!-- Moje lekce: velké přehledné karty podle inspiračního obrázku. -->
            <div class="flex flex-wrap items-center justify-between gap-3">
              <div class="flex gap-2">
                <UButton :variant="selectedCourseTab === 'active' ? 'solid' : 'outline'" :ui="{ base: 'cursor-pointer' }" @click="selectedCourseTab = 'active'">
                  Aktuální ({{ activeCourses.length }})
                </UButton>
                <UButton :variant="selectedCourseTab === 'history' ? 'solid' : 'outline'" :ui="{ base: 'cursor-pointer' }" @click="selectedCourseTab = 'history'">
                  Historie
                </UButton>
              </div>
              <UButton to="/our-dances" variant="outline" icon="i-lucide-plus">Najít kurz</UButton>
            </div>

            <UCard v-if="displayedCourses.length === 0">
              <p class="py-7 text-center text-sm text-muted">{{ selectedCourseTab === 'active' ? 'Zatím nemáte žádné aktuální kurzy.' : 'Zatím nemáte žádné kurzy v historii.' }}</p>
            </UCard>
            <div v-else class="space-y-3">
              <UCard v-for="enrollment in displayedCourses" :key="enrollment.id">
                <div class="grid gap-6 md:grid-cols-[minmax(0,1fr)_290px]">
                  <div class="space-y-3">
                    <div class="flex flex-wrap items-center gap-3">
                      <NuxtLink :to="`/our-dances/${enrollment.course.id}`" class="text-xl font-semibold text-orange-600 hover:underline">
                        {{ enrollment.course.name }}
                      </NuxtLink>
                      <UBadge :color="enrollment.status === 'Registered' ? 'success' : 'neutral'" variant="subtle">
                        {{ enrollmentStatus(enrollment.status) }}
                      </UBadge>
                    </div>
                    <p class="text-sm text-muted">Úroveň {{ enrollment.course.level }}</p>
                    <div class="space-y-2 text-sm">
                      <p><strong>Období:</strong> {{ formatDate(enrollment.course.startDate) }}–{{ formatDate(enrollment.course.endDate) }}</p>
                      <p><strong>Lektor:</strong> {{ enrollment.course.instructors?.join(', ') || 'Neuveden' }}</p>
                      <p v-for="(session, index) in (enrollment.course.sessions ?? [])" :key="index">
                        <strong>Kdy:</strong> {{ formatWeekday(session.weekday) }}
                        {{ formatTime(session.startsAt) }}–{{ formatTime(session.endsAt) }}
                        <span v-if="session.room"> · {{ session.room }}</span>
                      </p>
                    </div>
                  </div>
                  <div class="space-y-3 rounded-xl bg-muted/40 p-4 text-sm">
                    <div class="flex items-center justify-between gap-3">
                      <span class="text-muted">Taneční role</span>
                      <strong>{{ enrollment.danceRole || 'Nevyžaduje se' }}</strong>
                    </div>
                    <div class="flex items-center justify-between gap-3">
                      <span class="text-muted">Společná přihláška</span>
                      <strong class="text-right">{{ enrollment.partner ? `${enrollment.partner.firstName} ${enrollment.partner.lastName}` : 'Bez partnera' }}</strong>
                    </div>
                    <div v-if="enrollment.danceRole" class="flex items-center justify-between gap-3">
                      <span class="text-muted">Střídání</span>
                      <strong>{{ enrollment.rotation ? 'Ano' : 'Ne' }}</strong>
                    </div>
                    <div class="flex items-center justify-between gap-3 border-t border-default pt-3">
                      <span class="text-muted">{{ enrollment.status === 'Cancelled' ? 'Původně zaplaceno' : 'Zaplaceno' }}</span>
                      <strong>{{ formatCredit(enrollment.paidCredits ?? 0) }} kreditů</strong>
                    </div>
                    <div class="flex flex-wrap gap-2">
                      <UButton :to="`/our-dances/${enrollment.course.id}`" color="neutral" variant="outline" size="sm">
                        Detail kurzu
                      </UButton>
                      <UButton v-if="canCancel(enrollment)" color="error" variant="outline" size="sm" icon="i-lucide-calendar-x" :ui="{ base: 'cursor-pointer' }" @click="confirmCancelId = enrollment.id; cancelError = ''">
                        Odhlásit se
                      </UButton>
                    </div>
                  </div>
                </div>

                <div v-if="enrollment.partner" class="mt-4 rounded-lg border border-default px-4 py-3 text-sm text-muted">
                  <strong class="text-highlighted">Přihlášeni společně:</strong>
                  {{ enrollment.partner.firstName }} {{ enrollment.partner.lastName }}
                  <span v-if="enrollment.partner.danceRole"> ({{ enrollment.partner.danceRole }})</span>.
                  Jde o společnou přihlášku; případné pozdější přiřazení taneční dvojice lektorem tu zatím neevidujeme.
                </div>

                <p v-if="canCancel(enrollment)" class="mt-4 text-xs text-muted">
                  Bezplatné odhlášení do {{ formatDate(cancellationDeadline(enrollment.course.startDate)) }} včetně.
                </p>
                <div v-if="confirmCancelId === enrollment.id" class="mt-4 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
                  <p class="font-semibold">Opravdu zrušit celou přihlášku{{ enrollment.partner ? ' včetně partnera' : '' }}?</p>
                  <p class="mt-1 text-sm">Zaplacené kredity se vrátí na účet plátce.</p>
                  <p v-if="cancelError" class="mt-2 text-sm">{{ cancelError }}</p>
                  <div class="mt-3 flex gap-2">
                    <UButton color="error" size="sm" :loading="cancelingId === enrollment.id" @click="cancelEnrollment(enrollment)">Potvrdit zrušení</UButton>
                    <UButton color="neutral" variant="outline" size="sm" @click="confirmCancelId = null">Zpět</UButton>
                  </div>
                </div>
              </UCard>
            </div>
            </div> <!-- Konec záložky Moje lekce -->
          </template>
        </section>

        <section v-else class="space-y-6">
          <p v-if="profilePending" class="py-8 text-muted">Načítání profilu…</p>
          <UAlert v-else-if="profileError" color="error" title="Profil se nepodařilo načíst">
            <template #actions><UButton variant="outline" @click="refreshProfile()">Zkusit znovu</UButton></template>
          </UAlert>
          <template v-else-if="profile">
            <UCard>
              <template #header><h2 class="text-lg font-semibold">Osobní údaje</h2></template>
              <form class="space-y-5" @submit.prevent="saveProfile">
                <div class="grid gap-4 md:grid-cols-2">
                  <div>
                    <label class="mb-2 block text-sm font-medium">Jméno *</label>
                    <UInput v-model="form.firstName" maxlength="100" required class="w-full" />
                  </div>
                  <div>
                    <label class="mb-2 block text-sm font-medium">Příjmení *</label>
                    <UInput v-model="form.lastName" maxlength="100" required class="w-full" />
                  </div>
                </div>
                <div>
                  <label class="mb-2 block text-sm font-medium">E-mail</label>
                  <UInput :model-value="profile.email ?? ''" disabled class="w-full" />
                  <p class="mt-1 text-xs text-muted">Přihlašovací e-mail zde zatím nelze měnit.</p>
                </div>
                <div>
                  <label class="mb-2 block text-sm font-medium">Telefon</label>
                  <UInput v-model="form.phoneNumber" type="tel" maxlength="30" class="w-full" />
                </div>
                <UAlert v-if="saveError" color="error" :title="saveError" />
                <div class="flex justify-end gap-2">
                  <UButton type="button" color="neutral" variant="outline" :disabled="isSaving" @click="resetForm" :ui="{ base: 'cursor-pointer' }">
                    Zrušit změny
                  </UButton>
                  <UButton type="submit" icon="i-lucide-save" :loading="isSaving" :ui="{ base: 'cursor-pointer'}">
                    Uložit změny
                  </UButton>
                </div>
              </form>
            </UCard>

            <UCard>
              <template #header>
                <div class="flex flex-wrap items-center justify-between gap-3">
                  <h2 class="text-lg font-semibold">Moji partneři</h2>
                  <UButton to="/our-dances/my-partners" size="sm" icon="i-lucide-plus">Přidat / upravit partnera</UButton>
                </div>
              </template>
              <p v-if="partnersPending" class="text-sm text-muted">Načítání partnerů…</p>
              <UAlert v-else-if="partnersError" color="error" title="Partnery se nepodařilo načíst">
                <template #actions><UButton color="error" variant="outline" @click="refreshPartners()">Zkusit znovu</UButton></template>
              </UAlert>
              <p v-else-if="!partners?.length" class="text-sm text-muted">Zatím nemáte uloženého partnera.</p>
              <div v-else class="grid gap-3 md:grid-cols-2">
                <div v-for="partner in partners" :key="partner.id" class="rounded-lg border border-default p-3">
                  <p class="font-semibold">{{ partner.firstName }} {{ partner.lastName }}</p>
                  <p v-if="partner.email" class="mt-1 break-all text-sm text-muted">{{ partner.email }}</p>
                  <p v-if="partner.phoneNumber" class="mt-1 text-sm text-muted">{{ partner.phoneNumber }}</p>
                  <UButton icon="i-lucide-pencil"
                           variant="outline"
                           color="neutral"
                           size="sm"
                           @click="startEditing(partner)"
                           :to="`/our-dances/my-partners?edit=${partner.id}`"
                           :ui="{ base: 'mt-1 cursor-pointer' }">
                    Upravit
                  </UButton>
                </div>
              </div>
            </UCard>
          </template>
        </section>
      </div>
    </main>
  </div>
</template>
