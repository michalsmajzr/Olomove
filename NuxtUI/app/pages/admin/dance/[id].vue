<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type DanceStyle = {
  id: string
  name: string
  genre: string | null
  description: string | null
}

const route = useRoute()
const toast = useToast()
const isLoading = ref(true)
const isSaving = ref(false)
const isDeleting = ref(false)
const isDeleteModalOpen = ref(false)
const errorMessage = ref('')

const danceFormData = reactive({
  name: '',
  genre: '',
  description: ''
})

const danceId = computed(() => String(route.params.id))

async function loadDanceStyle() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const danceStyle = await $fetch<DanceStyle>(`/api/admin/dancestyles/${danceId.value}`, {
      credentials: 'include'
    })

    danceFormData.name = danceStyle.name
    danceFormData.genre = danceStyle.genre ?? ''
    danceFormData.description = danceStyle.description ?? ''
  } catch {
    errorMessage.value = 'Tanec se nepodařilo načíst.'
  } finally {
    isLoading.value = false
  }
}

function cancelDelete() {
  isDeleteModalOpen.value = false
  toast.add({
    title: 'Mazání zrušeno',
    description: 'Tanec nebyl smazán.',
    color: 'neutral'
  })
}

async function deleteDanceStyle() {
  isDeleting.value = true

  try {
    await $fetch(`/api/admin/dancestyles/${danceId.value}`, {
      method: 'DELETE',
      credentials: 'include'
    })

    toast.add({
      title: 'Úspěch',
      description: 'Položka byla úspěšně smazána.',
      color: 'success'
    })

    await navigateTo('/admin/dance')
  } catch (error: any) {
    isDeleteModalOpen.value = false
    toast.add({
      title: 'Chyba',
      description: error?.data?.message ?? 'Tanec se nepodařilo smazat.',
      color: 'error'
    })
  } finally {
    isDeleting.value = false
  }
}

async function saveDanceStyle() {
  if (!danceFormData.name.trim()) {
    toast.add({ title: 'Chyba', description: 'Název tance je povinný.', color: 'error' })
    return
  }

  isSaving.value = true
  try {
    await $fetch<DanceStyle>(`/api/admin/dancestyles/${danceId.value}`, {
      method: 'PUT',
      body: {
        name: danceFormData.name.trim(),
        genre: danceFormData.genre.trim() || null,
        description: danceFormData.description.trim() || null
      },
      credentials: 'include'
    })

    toast.add({
      title: 'Úspěch',
      description: `Tanec "${danceFormData.name.trim()}" byl úspěšně upraven.`,
      color: 'success'
    })

    await navigateTo('/admin/dance')
  } catch (error: any) {
    toast.add({
      title: 'Chyba',
      description: error?.data?.message ?? 'Tanec se nepodařilo upravit.',
      color: 'error'
    })
  } finally {
    isSaving.value = false
  }
}

onMounted(loadDanceStyle)
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">Upravit tanec</h1>
          <p class="mt-2 text-base text-muted">Úprava údajů tanečního stylu</p>
        </div>

        <div v-if="isLoading" class="rounded-xl border border-default bg-default p-6 text-sm text-muted shadow-sm">
          Načítání tance…
        </div>

        <div v-else-if="errorMessage" class="rounded-xl border border-default bg-default p-6 shadow-sm">
          <p class="text-sm text-error">{{ errorMessage }}</p>
          <div class="flex justify-end mt-4">
            <NuxtLink to="/admin/dance">
              <UButton color="neutral" variant="ghost" :ui="{ base: 'cursor-pointer' }">Zrušit</UButton>
            </NuxtLink>
          </div>
        </div>

        <form
          v-else
          class="rounded-xl border border-default bg-default p-6 shadow-sm"
          @submit.prevent="saveDanceStyle"
        >
          <div class="space-y-6">
            <UFormField label="Název tance" required>
              <UInput
                v-model="danceFormData.name"
                class="w-full"
                placeholder="např. Waltz, Tango, Samba…"
                :disabled="isSaving"
              />
            </UFormField>

            <UFormField label="Žánr (volitelné)">
              <UInput
                v-model="danceFormData.genre"
                class="w-full"
                placeholder="např. Společenský tanec, Latinskoamerický…"
                :disabled="isSaving"
              />
            </UFormField>

            <UFormField label="Popis (volitelné)">
              <UTextarea
                v-model="danceFormData.description"
                class="w-full"
                placeholder="Krátký popis tance…"
                :rows="4"
                :disabled="isSaving"
              />
            </UFormField>

            <div class="flex justify-end gap-3 pt-6">
              <NuxtLink to="/admin/dance">
                <UButton color="neutral" variant="ghost" type="button" :disabled="isSaving" :ui="{ base: 'cursor-pointer' }">
                  Zrušit
                </UButton>
              </NuxtLink>
              <UButton
                type="button"
                icon="i-lucide-trash-2"
                color="error"
                :disabled="isSaving || isDeleting"
                :ui="{ base: 'cursor-pointer' }"
                @click="isDeleteModalOpen = true"
              >
                Smazat
              </UButton>
              <UButton type="submit" icon="i-lucide-save" :loading="isSaving" :ui="{ base: 'cursor-pointer' }">
                Uložit změny
              </UButton>
            </div>
          </div>
        </form>

        <UModal
          v-model:open="isDeleteModalOpen"
          :dismissible="!isDeleting"
          :ui="{ footer: 'justify-end' }"
        >
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">Smazat tanec</h2>
          </template>

          <template #body>
            <p class="text-sm text-muted">
              Opravdu chcete smazat tanec „{{ danceFormData.name }}“? Tuto akci nelze vrátit zpět.
            </p>
          </template>

          <template #footer>
            <UButton
              label="Zrušit"
              color="neutral"
              variant="outline"
              :disabled="isDeleting"
              :ui="{ base: 'cursor-pointer' }"
              @click="cancelDelete"
            />
            <UButton
              label="Smazat tanec"
              color="error"
              :loading="isDeleting"
              :ui="{ base: 'cursor-pointer' }"
              @click="deleteDanceStyle"
            />
          </template>
        </UModal>
      </div>
    </main>
  </div>
</template>
