<script setup lang="ts">
definePageMeta({ middleware: 'user' })

const route = useRoute()
const courseId = computed(() => String(route.params.id))
const toast = useToast()

type CoursePreview = {
  id: string
  name: string
  startDate: string
  endDate: string
  price: number
  capacity: number
  availablePlaces: number
  canRegister: boolean
  isSolo: boolean
  allowLeaderRegistration: boolean
  allowFollowerRegistration: boolean
  myStatus: string | null
  credit: number
  leaderCount: number
  followerCount: number
  maxPerRole: number
  canRegisterLeader: boolean
  canRegisterFollower: boolean
  canRegisterPair: boolean
}

type SavedPartner = {
  id: string
  firstName: string
  lastName: string
  email: string | null
  phoneNumber: string | null
}

const { data: course, pending, error, refresh } = useFetch<CoursePreview>(
  () => `/api/courses/${courseId.value}/enrollments/preview`,
  { credentials: 'include', server: false }
)

const { data: partners, error: partnersError } = useFetch<SavedPartner[]>(
  '/api/user/saved-partners',
  { credentials: 'include', server: false }
)

// Formulář: jednotlivě, nebo kompletní dvojice. U partnera už není druhý checkbox.
const withPartner = ref(false)
const savedPartnerId = ref('')
const danceRole = ref<'Leader' | 'Follower'>('Follower')
const noRotation = ref(false)
const isSubmitting = ref(false)
const submitError = ref('')

const hasRoleAvailability = computed(() => {
  const c = course.value
  if (!c || c.isSolo) return true
  return Number.isFinite(c.leaderCount)
    && Number.isFinite(c.followerCount)
    && Number.isFinite(c.maxPerRole)
    && typeof c.canRegisterLeader === 'boolean'
    && typeof c.canRegisterFollower === 'boolean'
    && typeof c.canRegisterPair === 'boolean'
})

const partnerOptions = computed(() =>
  (partners.value ?? []).map(p => ({
    label: `${p.firstName} ${p.lastName}`,
    value: p.id
  }))
)

const danceRoleOptions = computed(() => {
  const c = course.value
  const leaderLabel = hasRoleAvailability.value && c
    ? `Leader (${c.leaderCount}/${c.maxPerRole})`
    : 'Leader'
  const followerLabel = hasRoleAvailability.value && c
    ? `Follower (${c.followerCount}/${c.maxPerRole})`
    : 'Follower'

  return [
    {
      label: leaderLabel,
      value: 'Leader',
      disabled: !c || !hasRoleAvailability.value ||
        (withPartner.value ? !c.canRegisterPair : !c.canRegisterLeader)
    },
    {
      label: followerLabel,
      value: 'Follower',
      disabled: !c || !hasRoleAvailability.value ||
        (withPartner.value ? !c.canRegisterPair : !c.canRegisterFollower)
    }
  ]
})

const canRegisterSelectedRole = computed(() => {
  const c = course.value
  if (!c) return false
  if (c.isSolo) return true
  if (!hasRoleAvailability.value) return false
  if (withPartner.value) return c.canRegisterPair
  return danceRole.value === 'Leader'
    ? c.canRegisterLeader
    : c.canRegisterFollower
})

// U nové volby režimu nabídneme dostupnou roli, nikdy však nevymyslíme volnou kapacitu.
watch([course, withPartner], () => {
  if (!course.value || course.value.isSolo || !hasRoleAvailability.value) return
  const selected = danceRoleOptions.value.find(option => option.value === danceRole.value)
  if (selected?.disabled) {
    const available = danceRoleOptions.value.find(option => !option.disabled)
    if (available) danceRole.value = available.value as 'Leader' | 'Follower'
  }
})

watch(withPartner, value => {
  if (!value) savedPartnerId.value = ''
  submitError.value = ''
})

const alreadyRegistered = computed(() =>
  course.value?.myStatus === 'Registered' || course.value?.myStatus === 'WaitingList'
)
const peopleCount = computed(() => withPartner.value && !course.value?.isSolo ? 2 : 1)
const totalPrice = computed(() => (course.value?.price ?? 0) * peopleCount.value)
const creditAfterPayment = computed(() => (course.value?.credit ?? 0) - totalPrice.value)
const insufficientCredit = computed(() => creditAfterPayment.value < 0)
const hasEnoughPlaces = computed(() =>
  (course.value?.availablePlaces ?? 0) >= peopleCount.value
)

const canSubmit = computed(() =>
  Boolean(course.value?.canRegister)
  && !alreadyRegistered.value
  && !isSubmitting.value
  && !insufficientCredit.value
  && hasEnoughPlaces.value
  && canRegisterSelectedRole.value
  && (!withPartner.value || Boolean(savedPartnerId.value))
)

function formatPrice(value: number): string {
  return new Intl.NumberFormat('cs-CZ', { maximumFractionDigits: 2 }).format(value) + ' kreditů'
}

function formatDate(value: string): string {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number)
  return new Date(year!, month! - 1, day!, 12).toLocaleDateString('cs-CZ')
}

function getErrorMessage(error: unknown): string {
  const response = error as { data?: string | { message?: string; detail?: string; title?: string } }
  const data = response?.data
  if (typeof data === 'string') return data
  return data?.message || data?.detail || data?.title || 'Přihlášení se nepodařilo.'
}

async function submitEnrollment() {
  if (!canSubmit.value || !course.value) return
  isSubmitting.value = true
  submitError.value = ''

  try {
    await $fetch(`/api/courses/${courseId.value}/enrollments`, {
      method: 'POST',
      credentials: 'include',
      body: {
        danceRole: course.value.isSolo ? null : danceRole.value,
        // Backend ukládá Rotation = true, pokud se chce střídat.
        rotation: course.value.isSolo ? false : !noRotation.value,
        savedPartnerId: !course.value.isSolo && withPartner.value
          ? savedPartnerId.value
          : null,
        // Společná volba pro oba členy dvojice; druhý dotaz u partnera není.
        partnerRotation: !course.value.isSolo && withPartner.value
          ? !noRotation.value
          : false
      }
    })

    toast.add({
      title: 'Úspěšné přihlášení!',
      description: `Kurz ${course.value.name} · Zaplaceno ${formatPrice(totalPrice.value)}.`,
      color: 'success',
      icon: 'i-lucide-check-circle'
    })
    await navigateTo('/profile')
  } catch (err) {
    submitError.value = getErrorMessage(err)
    toast.add({ title: 'Přihlášení se nezdařilo', description: submitError.value, color: 'error' })
    await refresh()
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />
    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-3xl px-5 py-10">
        <UButton
          :to="`/nase-tance/${courseId}`"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-5"
        >
          Zpět na detail kurzu
        </UButton>

        <h1 class="text-3xl font-semibold text-highlighted">Přihlášení na kurz</h1>
        <p class="mt-2 mb-8 text-muted">Vyberte způsob přihlášení a zkontrolujte platbu.</p>

        <p v-if="pending" class="py-12 text-center text-muted">Načítání kurzu…</p>
        <UAlert v-else-if="error" color="error" title="Kurz se nepodařilo načíst." />

        <div v-else-if="course" class="space-y-6">
          <UCard>
            <template #header>
              <h2 class="text-xl font-semibold">{{ course.name }}</h2>
            </template>
            <div class="flex flex-wrap gap-x-6 gap-y-2 text-sm text-muted">
              <span><strong class="text-highlighted">Začátek:</strong> {{ formatDate(course.startDate) }}</span>
              <span><strong class="text-highlighted">Cena za osobu:</strong> {{ formatPrice(course.price) }}</span>
              <span><strong class="text-highlighted">Volná místa:</strong> {{ course.availablePlaces }}/{{ course.capacity }}</span>
            </div>
            <div class="mt-3">
              <UBadge :color="course.isSolo ? 'neutral' : 'info'" variant="subtle">
                {{ course.isSolo ? 'Kurz pro jednotlivce' : 'Párový taneční kurz' }}
              </UBadge>
            </div>
          </UCard>

          <UAlert
            v-if="alreadyRegistered"
            color="info"
            title="Na tento kurz už máte přihlášku."
            description="Najdete ji v sekci Moje kurzy."
          />
          <UAlert
            v-else-if="!course.canRegister"
            color="warning"
            title="Přihlašování na tento kurz je uzavřené."
            description="Přihlášení je možné nejpozději 7 dní před začátkem kurzu."
          />

          <UCard v-else>
            <template #header>
              <h2 class="text-lg font-semibold">Údaje přihlášky</h2>
            </template>
            <form class="space-y-7" @submit.prevent="submitEnrollment">
              <template v-if="!course.isSolo">
                <section class="space-y-3">
                  <label class="block text-sm font-semibold">Jak se chcete přihlásit?</label>
                  <div class="grid grid-cols-2 rounded-xl bg-muted/60 p-1">
                    <button
                      type="button"
                      class="rounded-lg px-3 py-3 text-sm font-medium transition-colors"
                      :class="!withPartner ? 'bg-white text-orange-600 shadow-sm dark:bg-neutral-800' : 'text-muted hover:text-highlighted'"
                      :aria-pressed="!withPartner"
                      @click="withPartner = false"
                    >
                      Tančím sám/sama
                    </button>
                    <button
                      type="button"
                      class="rounded-lg px-3 py-3 text-sm font-medium transition-colors"
                      :class="withPartner ? 'bg-white text-orange-600 shadow-sm dark:bg-neutral-800' : 'text-muted hover:text-highlighted'"
                      :aria-pressed="withPartner"
                      @click="withPartner = true"
                    >
                      Tančíme jako pár
                    </button>
                  </div>
                </section>

                <section class="space-y-3">
                  <label class="block text-sm font-semibold">Moje taneční role</label>
                  <USelect v-model="danceRole" :items="danceRoleOptions" class="w-full" />
                  <div v-if="hasRoleAvailability" class="flex flex-wrap gap-4 text-sm text-muted">
                    <span>Leaders: <strong>{{ course.leaderCount }}/{{ course.maxPerRole }}</strong></span>
                    <span>Followers: <strong>{{ course.followerCount }}/{{ course.maxPerRole }}</strong></span>
                  </div>
                  <UAlert
                    v-else
                    color="error"
                    title="Nedostupné informace o obsazenosti rolí."
                    description="Zkuste znovu načíst stránku nebo zkontrolovat spuštěný backend."
                  />
                </section>

                <section v-if="withPartner" class="space-y-3 rounded-xl border border-default p-4">
                  <div class="flex flex-wrap items-center justify-between gap-2">
                    <label class="font-semibold">Můj taneční partner</label>
                    <UButton to="/moji-partneri" color="neutral" variant="outline" size="xs" icon="i-lucide-plus">
                      Spravovat partnery
                    </UButton>
                  </div>
                  <USelect
                    v-model="savedPartnerId"
                    :items="partnerOptions"
                    placeholder="Vyberte uloženého partnera"
                    class="w-full"
                  />
                  <p class="text-sm text-muted">Partner získá opačnou roli než vy. Přihláška a platba budou společné.</p>
                  <UAlert v-if="partnersError" color="error" title="Seznam partnerů se nepodařilo načíst." />
                  <p v-else-if="!partnerOptions.length" class="text-sm text-muted">
                    Ještě nemáte uloženého partnera. Přidejte ho přes tlačítko výše.
                  </p>
                </section>

                <UCheckbox
                  v-model="noRotation"
                  :label="withPartner ? 'Nechceme během kurzu střídat taneční partnery' : 'Nechci během kurzu střídat taneční partnery'"
                />

                <UAlert
                  v-if="!canRegisterSelectedRole && hasRoleAvailability"
                  color="warning"
                  title="Vybraná možnost přihlášení není dostupná."
                  description="Zvolte jinou dostupnou roli nebo způsob přihlášení. Pokud je některá role plná, není možné přihlásit ani celý pár."
                />
              </template>

              <UAlert
                v-else
                color="info"
                title="Přihlašujete se jako jednotlivec."
                description="Tento kurz nevyžaduje taneční roli ani partnera."
              />

              <section class="space-y-3 rounded-xl bg-muted/50 p-5">
                <h3 class="text-lg font-semibold">Shrnutí přihlášky</h3>
                <div class="flex justify-between gap-4 text-sm">
                  <span>Počet osob</span><strong>{{ peopleCount }}</strong>
                </div>
                <div class="flex justify-between gap-4 text-sm">
                  <span>Cena za osobu</span><strong>{{ formatPrice(course.price) }}</strong>
                </div>
                <div class="flex justify-between gap-4 border-t border-default pt-3">
                  <span class="font-semibold">Celkem k zaplacení</span>
                  <strong class="text-lg">{{ formatPrice(totalPrice) }}</strong>
                </div>
                <div class="flex justify-between gap-4 text-sm text-muted">
                  <span>Moje kredity</span><strong>{{ formatPrice(course.credit) }}</strong>
                </div>
                <div class="flex justify-between gap-4 text-sm">
                  <span>Zůstatek po zaplacení</span>
                  <strong :class="insufficientCredit ? 'text-red-600' : ''">
                    {{ formatPrice(creditAfterPayment) }}
                  </strong>
                </div>
              </section>

              <UAlert v-if="insufficientCredit" color="warning" title="Nemáte dostatek kreditů." />
              <UAlert v-if="!hasEnoughPlaces" color="warning" title="Na kurzu není dost volných míst." />
              <UAlert v-if="submitError" color="error" :title="submitError" />

              <div class="flex flex-wrap justify-end gap-3">
                <UButton to="/profile" color="neutral" variant="outline">Zpět na profil</UButton>
                <UButton type="submit" icon="i-lucide-check" :loading="isSubmitting" :disabled="!canSubmit">
                  Potvrdit přihlášku
                </UButton>
              </div>
            </form>
          </UCard>
        </div>
      </div>
    </main>
  </div>
</template>
