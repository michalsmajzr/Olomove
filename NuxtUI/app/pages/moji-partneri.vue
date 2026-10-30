<script setup lang="ts">
definePageMeta({
  middleware: 'user'
})

type SavedPartner = {
  id: string
  firstName: string
  lastName: string
  email: string | null
  phoneNumber: string | null
}

const toast = useToast()
const route = useRoute()

const {
  data: partners,
  pending,
  error,
  refresh
} = useFetch<SavedPartner[]>('/api/user/saved-partners', {
  credentials: 'include',
  server: false
})

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: ''
})

const isFormVisible = ref(false)
const editingPartnerId = ref<string | null>(null)
const isSaving = ref(false)
const saveError = ref('')

function resetForm() {
  form.firstName = ''
  form.lastName = ''
  form.email = ''
  form.phoneNumber = ''
  editingPartnerId.value = null
  saveError.value = ''
}

function startAdding() {
  resetForm()
  isFormVisible.value = true
}

function startEditing(partner: SavedPartner) {
  editingPartnerId.value = partner.id
  form.firstName = partner.firstName
  form.lastName = partner.lastName
  form.email = partner.email ?? ''
  form.phoneNumber = partner.phoneNumber ?? ''
  saveError.value = ''
  isFormVisible.value = true
}

// Přímý odkaz Upravit z profilu otevře formulář konkrétní osoby.
watch(
  [partners, () => route.query.edit],
  ([list, editId]) => {
    if (!list || typeof editId !== 'string' || isFormVisible.value) return
    const partner = list.find(p => p.id === editId)
    if (partner) startEditing(partner)
  },
  { immediate: true }
)

function cancelForm() {
  resetForm()
  isFormVisible.value = false
}

async function savePartner() {
  if (isSaving.value) return

  saveError.value = ''

  const firstName = form.firstName.trim()
  const lastName = form.lastName.trim()
  const email = form.email.trim()
  const phoneNumber = form.phoneNumber.trim()

  if (!firstName || !lastName) {
    saveError.value = 'Vyplňte jméno a příjmení.'
    return
  }

  if (!email && !phoneNumber) {
    saveError.value = 'Vyplňte e-mail nebo telefon.'
    return
  }

  if (firstName.length > 100 || lastName.length > 100 ||
      email.length > 256 || phoneNumber.length > 32) {
    saveError.value = 'Některý údaj je příliš dlouhý.'
    return
  }

  isSaving.value = true

  try {
    const editingId = editingPartnerId.value
    const url = editingId
      ? `/api/user/saved-partners/${editingId}`
      : '/api/user/saved-partners'

    await $fetch(url, {
      method: editingId ? 'PUT' : 'POST',
      credentials: 'include',
      body: {
        firstName,
        lastName,
        email: email || null,
        phoneNumber: phoneNumber || null
      }
    })

    await refresh()
    cancelForm()

    toast.add({
      title: editingId ? 'Partner upraven' : 'Partner přidán',
      description: 'Údaje byly úspěšně uloženy.',
      color: 'success'
    })
  } catch (err: any) {
    const data = err?.data
    saveError.value =
      (typeof data === 'string'
        ? data
        : data?.detail || data?.title || data?.message) ||
      'Údaje partnera se nepodařilo uložit.'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="min-h-screen bg-muted/30">
      <div class="mx-auto max-w-5xl px-6 py-10">
        <UButton
          to="/profile"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="ghost"
          class="mb-6"
        >
          Zpět na můj profil
        </UButton>

        <div class="mb-8">
          <h1 class="text-3xl font-semibold text-highlighted">Moji partneři</h1>
          <p class="mt-2 text-muted">
            Osoby, které si můžete vybrat při přihlašování na taneční kurzy.
          </p>
        </div>

        <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-xl font-semibold">Uložení partneři</h2>
          <UButton
            v-if="!isFormVisible"
            icon="i-lucide-plus"
            @click="startAdding"
          >
            Přidat partnera
          </UButton>
        </div>

        <p v-if="pending" class="py-10 text-center text-muted">
          Načítání partnerů…
        </p>

        <UAlert
          v-else-if="error"
          color="error"
          title="Partnery se nepodařilo načíst"
          class="mb-6"
        >
          <template #actions>
            <UButton color="error" variant="outline" @click="refresh()">
              Zkusit znovu
            </UButton>
          </template>
        </UAlert>

        <template v-else>
          <UCard v-if="!partners?.length" class="mb-6">
            <p class="py-6 text-center text-muted">
              Zatím nemáte uloženého žádného partnera.
            </p>
          </UCard>

          <div v-else class="mb-8 grid gap-4 md:grid-cols-2">
            <UCard v-for="partner in partners" :key="partner.id">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <h3 class="font-semibold text-highlighted">
                    {{ partner.firstName }} {{ partner.lastName }}
                  </h3>
                  <p v-if="partner.email" class="mt-2 break-all text-sm text-muted">
                    {{ partner.email }}
                  </p>
                  <p v-if="partner.phoneNumber" class="mt-1 text-sm text-muted">
                    {{ partner.phoneNumber }}
                  </p>
                </div>

                <UButton
                  icon="i-lucide-pencil"
                  variant="outline"
                  color="neutral"
                  size="sm"
                  @click="startEditing(partner)"
                >
                  Upravit
                </UButton>
              </div>
            </UCard>
          </div>
        </template>

        <UCard v-if="isFormVisible">
          <template #header>
            <h2 class="text-lg font-semibold">
              {{ editingPartnerId ? 'Upravit partnera' : 'Přidat partnera' }}
            </h2>
          </template>

          <form class="space-y-5" @submit.prevent="savePartner">
            <div class="grid gap-4 md:grid-cols-2">
              <div>
                <label class="mb-2 block text-sm font-medium">Jméno *</label>
                <UInput
                  v-model="form.firstName"
                  required
                  maxlength="100"
                  placeholder="Jméno"
                  class="w-full"
                />
              </div>

              <div>
                <label class="mb-2 block text-sm font-medium">Příjmení *</label>
                <UInput
                  v-model="form.lastName"
                  required
                  maxlength="100"
                  placeholder="Příjmení"
                  class="w-full"
                />
              </div>
            </div>

            <div>
              <label class="mb-2 block text-sm font-medium">E-mail</label>
              <UInput
                v-model="form.email"
                type="email"
                maxlength="256"
                placeholder="jan@example.cz"
                class="w-full"
              />
            </div>

            <div>
              <label class="mb-2 block text-sm font-medium">Telefon</label>
              <UInput
                v-model="form.phoneNumber"
                type="tel"
                maxlength="32"
                placeholder="+420 777 123 456"
                class="w-full"
              />
            </div>

            <p class="text-xs text-muted">
              Je potřeba alespoň jeden kontakt: e-mail nebo telefon.
            </p>

            <UAlert v-if="saveError" color="error" :title="saveError" />

            <div class="flex justify-end gap-3">
              <UButton
                type="button"
                variant="outline"
                color="neutral"
                :disabled="isSaving"
                @click="cancelForm"
              >
                Zrušit
              </UButton>

              <UButton type="submit" :loading="isSaving">
                {{ editingPartnerId ? 'Uložit změny' : 'Uložit partnera' }}
              </UButton>
            </div>
          </form>
        </UCard>
      </div>
    </main>
  </div>
</template>
