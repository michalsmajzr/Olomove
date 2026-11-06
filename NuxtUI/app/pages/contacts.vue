<script setup lang="ts">
type CurrentUser = { roles: string[] }
const currentUser = useState<CurrentUser | null>('auth-user', () => null)
const isAdmin = computed(() => currentUser.value?.roles.includes('Admin') ?? false)
const toast = useToast()

type ContactPage = {
  id: number
  name: string
  email: string
  phone: string
  city: string
  address: string
  postCode: string
  mapUrl: string
  updatedAt: string
}

const { data: contactData, refresh } = await useAsyncData<ContactPage>('contact-page', () =>
  $fetch<ContactPage>('/api/contact')
)

const form = reactive({
  name: '',
  email: '',
  phone: '',
  city: '',
  address: '',
  postCode: '',
  mapUrl: ''
})

watch(contactData, (newData) => {
  if (newData) {
    form.name = newData.name ?? ''
    form.email = newData.email ?? ''
    form.phone = newData.phone ?? ''
    form.city = newData.city ?? ''
    form.address = newData.address ?? ''
    form.postCode = newData.postCode ?? ''
    form.mapUrl = newData.mapUrl ?? ''
  }
}, { immediate: true })

const isSaving = ref(false)

async function saveContent() {
  isSaving.value = true
  try {
    await $fetch('/api/admin/contact', {
      method: 'PUT',
      body: form
    })
    await refresh()
    toast.add({
        title: 'Úspěch',
        description: 'Obsah byl úspěšně aktualizován.',
    color: 'success'
 })
  } catch (error) {
    const errors = error?.data?.errors
    const validationMessage = errors
        ? Object.values(errors).flat().join(' ')
        : null

    toast.add({
        title: 'Chyba',
        description: validationMessage
            ?? error?.data?.detail
            ?? error?.data?.message
            ?? 'Obsah se nepodařilo aktualizovat.',
    color: 'error'
  })
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-white">
    <AppNavbar />

    <div class="max-w-6xl mx-auto py-12 px-4 sm:px-6">
      <!-- nadpis -->
      <h1 class="text-3xl sm:text-4xl font-bold text-orange-500 mb-10">
        Kontakty
      </h1>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">

        <!-- leva cast -->
        <div class="space-y-6">

          <!-- osoba -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm">
            <div class="flex items-start space-x-4">
              <div class="p-3 bg-orange-50 text-orange-500 rounded-xl">
                <UIcon name="i-lucide-user" class="w-6 h-6" />
              </div>

              <div class="flex-1 space-y-3">
                <!-- admin uprava -->
                <div v-if="isAdmin">
                  <label class="block text-xs font-semibold text-slate-400 mb-1">Jméno a příjmení</label>
                  <UInput v-model="form.name" size="sm" />

                  <label class="block text-xs font-semibold text-slate-400 mt-2 mb-1">E-mail</label>
                  <UInput v-model="form.email" size="sm" />

                  <label class="block text-xs font-semibold text-slate-400 mt-2 mb-1">Telefon</label>
                  <UInput v-model="form.phone" size="sm" />
                </div>

                <!-- ostatni zobrazeni -->
                <div v-else>
                  <h3 class="font-bold text-slate-800 text-lg">{{ contactData?.name }}</h3>
                  <p class="text-slate-600 text-sm mt-1">{{ contactData?.email }}</p>
                  <p class="text-slate-600 text-sm">{{ contactData?.phone }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- adresa -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-sm">
            <div class="flex items-start space-x-4">
              <div class="p-3 bg-orange-50 text-orange-500 rounded-xl">
                <UIcon name="i-lucide-map-pin" class="w-6 h-6" />
              </div>

              <div class="flex-1 space-y-3">
                <h3 class="font-bold text-slate-800 text-lg">Adresa</h3>

                <!-- admin uprava -->
                <div v-if="isAdmin" class="space-y-2">
                  <div>
                    <label class="block text-xs font-semibold text-slate-400 mb-1">Město</label>
                    <UInput v-model="form.city" size="sm" />
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-slate-400 mb-1">Ulice a č.p.</label>
                    <UInput v-model="form.address" size="sm" />
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-slate-400 mb-1">PSČ</label>
                    <UInput v-model="form.postCode" size="sm" />
                  </div>
                </div>

                <!-- ostatni zobrazeni -->
                <div v-else class="text-slate-600 text-sm space-y-1">
                  <p>Město: {{ contactData?.city }}</p>
                  <p>Adresa: {{ contactData?.address }}</p>
                  <p>Poštovní směrovací číslo: {{ contactData?.postCode }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Usave -->
          <div v-if="isAdmin" class="pt-2">
            <UButton label="Uložit změny kontaktů"
                     color="primary"
                     size="lg"
                     icon="i-lucide-check"
                     :loading="isSaving"
                     class="w-full justify-center cursor-pointer"
                     @click="saveContent" />
          </div>

        </div>

        <!-- prava cast -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-4 shadow-sm overflow-hidden h-full min-h-[400px] flex flex-col">
          <div v-if="isAdmin" class="mb-4">
            <label class="block text-xs font-semibold text-slate-400 mb-1">URL mapy</label>
            <UInput v-model="form.mapUrl" size="sm" placeholder="https://frame.mapy.cz/" />
          </div>

          <!-- mapa -->
          <div class="flex-1 w-full h-[400px] rounded-xl overflow-hidden bg-slate-100">
            <iframe :src="(isAdmin ? form.mapUrl : contactData?.mapUrl) || ''"
                    class="w-full h-full border-0"
                    allowfullscreen=""
                    loading="lazy">
            </iframe>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
