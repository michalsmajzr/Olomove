
<script setup lang="ts">
definePageMeta({
  middleware: 'instructor'
})

type LecturerProfile = {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email: string | null
  phoneNumber: string | null
  createdAt: string
}

const toast = useToast()

const {
  data: profile,
  pending,
  error,
  refresh
} = useFetch<LecturerProfile>('/api/lecturer/profile', {
  credentials: 'include',
  server: false
})

const form = reactive({
  firstName: '',
  lastName: '',
  phoneNumber: ''
})

const isEditing = ref(false)
const isSaving = ref(false)
const saveError = ref('')

watch(
  profile,
  (value) => {
    if (!value || isEditing.value) return

    form.firstName = value.firstName ?? ''
    form.lastName = value.lastName ?? ''
    form.phoneNumber = value.phoneNumber ?? ''
  },
  { immediate: true }
)

const initials = computed(() => {
  if (!profile.value) return ''

  return (
    (profile.value.firstName?.charAt(0) ?? '') +
    (profile.value.lastName?.charAt(0) ?? '')
  ).toUpperCase()
})

const displayName = computed(() => {
  if (!profile.value) return 'Lektor'

  return (
    `${profile.value.firstName} ${profile.value.lastName}`
  ).trim()
})

function formatDate(value: string | null | undefined): string {
  if (!value) return '—'

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) return '—'

  return date.toLocaleDateString('cs-CZ', {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  })
}

function startEditing() {
  if (!profile.value) return

  form.firstName = profile.value.firstName ?? ''
  form.lastName = profile.value.lastName ?? ''
  form.phoneNumber = profile.value.phoneNumber ?? ''

  saveError.value = ''
  isEditing.value = true
}

function cancelEditing() {
  if (!profile.value) return

  form.firstName = profile.value.firstName ?? ''
  form.lastName = profile.value.lastName ?? ''
  form.phoneNumber = profile.value.phoneNumber ?? ''

  saveError.value = ''
  isEditing.value = false
}

async function saveProfile() {
  if (isSaving.value) return

  saveError.value = ''

  const firstName = form.firstName.trim()
  const lastName = form.lastName.trim()
  const phoneNumber = form.phoneNumber.trim()

  if (!firstName) {
    saveError.value = 'Vyplňte jméno.'
    return
  }

  if (!lastName) {
    saveError.value = 'Vyplňte příjmení.'
    return
  }

  if (firstName.length > 100 || lastName.length > 100) {
    saveError.value = 'Jméno a příjmení mohou mít maximálně 100 znaků.'
    return
  }

  if (phoneNumber.length > 30) {
    saveError.value = 'Telefon může mít maximálně 30 znaků.'
    return
  }

  isSaving.value = true

  try {
    const updated = await $fetch<LecturerProfile>(
      '/api/lecturer/profile',
      {
        method: 'PUT',
        credentials: 'include',
        body: {
          firstName,
          lastName,
          phoneNumber: phoneNumber || null
        }
      }
    )

    // Backend vrací aktualizované údaje.
    // Aktualizujeme lokální profil bez dalšího GET.
    profile.value = updated

    form.firstName = updated.firstName
    form.lastName = updated.lastName
    form.phoneNumber = updated.phoneNumber ?? ''

    isEditing.value = false

    toast.add({
      title: 'Profil aktualizován',
      description: 'Vaše změny byly úspěšně uloženy.',
      color: 'success',
      icon: 'i-lucide-circle-check'
    })
  } catch (err: any) {
    const responseData = err?.data

    saveError.value =
      (typeof responseData === 'string'
        ? responseData
        : responseData?.message ||
          responseData?.title) ||
      'Profil se nepodařilo uložit. Zkuste to znovu.'

    toast.add({
      title: 'Chyba při ukládání',
      description: saveError.value,
      color: 'error',
      icon: 'i-lucide-circle-alert'
    })
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-6xl px-6 py-10">

        <!-- Navigace zpět -->
        <UButton
          to="/lecturer"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-6"
        >
          Zpět na přehled
        </UButton>

        <!-- Hlavička stránky -->
        <div class="mb-8">
          <p class="mb-2 text-sm font-medium text-orange-600">
            OloMove · Lektorská sekce
          </p>

          <h1 class="text-3xl font-semibold text-highlighted">
            Můj profil
          </h1>

          <p class="mt-2 text-muted">
            Osobní a kontaktní údaje vašeho účtu
          </p>
        </div>

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
            Načítání profilu…
          </p>
        </div>

        <!-- Chyba načítání -->
        <UAlert
          v-else-if="error"
          color="error"
          title="Profil se nepodařilo načíst"
          description="Zkontrolujte připojení nebo se zkuste znovu přihlásit."
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

        <!-- Obsah profilu -->
        <template v-else-if="profile">

          <!-- Profilová karta -->
          <UCard class="mb-6">
            <div
              class="flex flex-wrap items-center justify-between gap-5"
            >
              <div class="flex items-center gap-5">

                <!-- Avatar -->
                <div
                  class="flex size-20 shrink-0 items-center justify-center rounded-full bg-orange-100 text-2xl font-semibold text-orange-600"
                >
                  {{ initials }}
                </div>

                <!-- Jméno -->
                <div>
                  <h2 class="text-2xl font-semibold text-highlighted">
                    {{ displayName }}
                  </h2>

                  <p class="mt-1 text-sm text-muted">
                    Lektor · Olomove
                  </p>

                  <div class="mt-3">
                    <UBadge
                      color="primary"
                      variant="subtle"
                    >
                      <UIcon
                        name="i-lucide-graduation-cap"
                        class="mr-1 size-4"
                      />

                      Lektorský účet
                    </UBadge>
                  </div>
                </div>
              </div>

              <!-- Úprava profilu -->
              <UButton
                v-if="!isEditing"
                icon="i-lucide-pencil"
                color="primary"
                variant="outline"
                @click="startEditing"
              >
                Upravit profil
              </UButton>

            </div>
          </UCard>

          <div class="grid gap-6 lg:grid-cols-3">

            <!-- Levý sloupec -->
            <div class="space-y-6 lg:col-span-2">

              <!-- Osobní údaje -->
              <UCard>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon
                      name="i-lucide-user-round"
                      class="size-5 text-orange-600"
                    />

                    <h2 class="text-lg font-semibold text-highlighted">
                      Osobní údaje
                    </h2>
                  </div>
                </template>

                <div class="grid gap-5 sm:grid-cols-2">

                  <!-- Jméno -->
                  <div>
                    <label
                      for="lecturer-first-name"
                      class="mb-2 block text-sm font-medium text-muted"
                    >
                      Jméno
                    </label>

                    <UInput
                      v-if="isEditing"
                      id="lecturer-first-name"
                      v-model="form.firstName"
                      placeholder="Jméno"
                      :maxlength="100"
                      class="w-full"
                    />

                    <p
                      v-else
                      class="font-medium text-highlighted"
                    >
                      {{ profile.firstName }}
                    </p>
                  </div>

                  <!-- Příjmení -->
                  <div>
                    <label
                      for="lecturer-last-name"
                      class="mb-2 block text-sm font-medium text-muted"
                    >
                      Příjmení
                    </label>

                    <UInput
                      v-if="isEditing"
                      id="lecturer-last-name"
                      v-model="form.lastName"
                      placeholder="Příjmení"
                      :maxlength="100"
                      class="w-full"
                    />

                    <p
                      v-else
                      class="font-medium text-highlighted"
                    >
                      {{ profile.lastName }}
                    </p>
                  </div>

                </div>
              </UCard>

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

                <div class="space-y-6">

                  <!-- E-mail -->
                  <div>
                    <p class="mb-2 text-sm font-medium text-muted">
                      E-mail
                    </p>

                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-mail"
                        class="size-5 text-orange-600"
                      />

                      <span class="font-medium text-highlighted">
                        {{ profile.email || 'Neuvedeno' }}
                      </span>
                    </div>

                    <p
                      v-if="isEditing"
                      class="mt-2 text-xs text-muted"
                    >
                      E-mail nelze změnit na této stránce.
                    </p>
                  </div>

                  <USeparator />

                  <!-- Telefon -->
                  <div>
                    <label
                      for="lecturer-phone"
                      class="mb-2 block text-sm font-medium text-muted"
                    >
                      Telefon
                    </label>

                    <UInput
                      v-if="isEditing"
                      id="lecturer-phone"
                      v-model="form.phoneNumber"
                      placeholder="+420 777 123 456"
                      type="tel"
                      :maxlength="30"
                      icon="i-lucide-phone"
                      class="w-full"
                    />

                    <div
                      v-else
                      class="flex items-center gap-3"
                    >
                      <UIcon
                        name="i-lucide-phone"
                        class="size-5 text-orange-600"
                      />

                      <span class="font-medium text-highlighted">
                        {{ profile.phoneNumber || 'Neuvedeno' }}
                      </span>
                    </div>
                  </div>

                </div>
              </UCard>

              <!-- Chyba při ukládání -->
              <UAlert
                v-if="saveError && isEditing"
                color="error"
                icon="i-lucide-circle-alert"
                title="Profil se nepodařilo uložit"
                :description="saveError"
              />

              <!-- Akce -->
              <div
                v-if="isEditing"
                class="flex flex-wrap justify-end gap-3"
              >
                <UButton
                  color="neutral"
                  variant="outline"
                  :disabled="isSaving"
                  @click="cancelEditing"
                >
                  Zrušit
                </UButton>

                <UButton
                  icon="i-lucide-save"
                  color="primary"
                  :loading="isSaving"
                  :disabled="isSaving"
                  @click="saveProfile"
                >
                  Uložit změny
                </UButton>
              </div>

            </div>

            <!-- Pravý sloupec -->
            <div class="space-y-6">

              <!-- Informace o účtu -->
              <UCard>
                <template #header>
                  <div class="flex items-center gap-3">
                    <UIcon
                      name="i-lucide-shield-check"
                      class="size-5 text-orange-600"
                    />

                    <h2 class="text-lg font-semibold text-highlighted">
                      Informace o účtu
                    </h2>
                  </div>
                </template>

                <div class="space-y-5">

                  <div>
                    <p class="text-sm text-muted">
                      Typ účtu
                    </p>

                    <div class="mt-2">
                      <UBadge color="primary" variant="subtle">
                        Lektor
                      </UBadge>
                    </div>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Členem od
                    </p>

                    <p class="mt-1 font-medium text-highlighted">
                      {{ formatDate(profile.createdAt) }}
                    </p>
                  </div>

                  <div>
                    <p class="text-sm text-muted">
                      Stav účtu
                    </p>

                    <div class="mt-2">
                      <UBadge color="success" variant="subtle">
                        Aktivní
                      </UBadge>
                    </div>
                  </div>

                </div>
              </UCard>

              <!-- Rychlé odkazy -->
              <UCard>
                <template #header>
                  <h2 class="text-lg font-semibold text-highlighted">
                    Rychlé odkazy
                  </h2>
                </template>

                <div class="space-y-3">

                  <UButton
                    to="/lecturer"
                    icon="i-lucide-layout-dashboard"
                    color="primary"
                    variant="outline"
                    block
                  >
                    Můj přehled
                  </UButton>

                  <UButton
                    to="/lecturer/lessons"
                    icon="i-lucide-calendar-days"
                    color="neutral"
                    variant="outline"
                    block
                  >
                    Moje lekce
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
