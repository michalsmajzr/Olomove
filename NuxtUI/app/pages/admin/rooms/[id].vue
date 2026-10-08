<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type Room = {
  id: string
  name: string
  capacity: number
  description: string | null
}

const route = useRoute()
const toast = useToast()
const isLoading = ref(true)
const isSaving = ref(false)
const isDeleting = ref(false)
const isDeleteModalOpen = ref(false)
const errorMessage = ref('')

const roomFormData = reactive({
  name: '',
  capacity: null as number | null,
  description: ''
})

const roomId = computed(() => String(route.params.id))

async function loadRoom() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const room = await $fetch<Room>(`/api/admin/rooms/${roomId.value}`, {
      credentials: 'include'
    })

    roomFormData.name = room.name
    roomFormData.capacity = room.capacity
    roomFormData.description = room.description ?? ''
  } catch {
    errorMessage.value = 'Sál se nepodařilo načíst.'
  } finally {
    isLoading.value = false
  }
}

function cancelDelete() {
  isDeleteModalOpen.value = false
  toast.add({
    title: 'Mazání zrušeno',
    description: 'Sál nebyl smazán.',
    color: 'neutral'
  })
}

async function deleteRoom() {
  isDeleting.value = true

  try {
    await $fetch(`/api/admin/rooms/${roomId.value}`, {
      method: 'DELETE',
      credentials: 'include'
    })

    toast.add({
      title: 'Úspěch',
      description: 'Sál byl úspěšně smazán.',
      color: 'success'
    })

    await navigateTo('/admin/rooms')
  } catch (error: any) {
    isDeleteModalOpen.value = false
    toast.add({
      title: 'Chyba',
      description: error?.data?.message ?? 'Sál se nepodařilo smazat.',
      color: 'error'
    })
  } finally {
    isDeleting.value = false
  }
}

async function saveRoom() {
  if (!roomFormData.name.trim()) {
    toast.add({ title: 'Chyba', description: 'Název sálu je povinný.', color: 'error' })
    return
  }

  if (roomFormData.capacity === null || roomFormData.capacity < 1) {
    toast.add({ title: 'Chyba', description: 'Kapacita musí být alespoň 1.', color: 'error' })
    return
  }

  isSaving.value = true
  try {
    await $fetch<Room>(`/api/admin/rooms/${roomId.value}`, {
      method: 'PUT',
      body: {
        name: roomFormData.name.trim(),
        capacity: roomFormData.capacity,
        description: roomFormData.description.trim() || null
      },
      credentials: 'include'
    })

    toast.add({
      title: 'Úspěch',
      description: `Sál "${roomFormData.name.trim()}" byl úspěšně upraven.`,
      color: 'success'
    })

    await navigateTo('/admin/rooms')
  } catch (error: any) {
    toast.add({
      title: 'Chyba',
      description: error?.data?.message ?? 'Sál se nepodařilo upravit.',
      color: 'error'
    })
  } finally {
    isSaving.value = false
  }
}

onMounted(loadRoom)
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">Upravit sál</h1>
          <p class="mt-2 text-base text-muted">Úprava údajů sálu</p>
        </div>

        <div v-if="isLoading" class="rounded-xl border border-default bg-default p-6 text-sm text-muted shadow-sm">
          Načítání sálu…
        </div>

        <div v-else-if="errorMessage" class="rounded-xl border border-default bg-default p-6 shadow-sm">
          <p class="text-sm text-error">{{ errorMessage }}</p>
          <div class="flex justify-end mt-4">
            <NuxtLink to="/admin/rooms">
              <UButton color="neutral" variant="ghost" :ui="{ base: 'cursor-pointer' }">Zrušit</UButton>
            </NuxtLink>
          </div>
        </div>

        <form
          v-else
          class="rounded-xl border border-default bg-default p-6 shadow-sm"
          @submit.prevent="saveRoom"
        >
          <div class="space-y-6">
            <UFormField label="Název sálu" required>
              <UInput
                v-model="roomFormData.name"
                class="w-full"
                placeholder="např. Sál 1…"
                :disabled="isSaving || isDeleting"
              />
            </UFormField>

            <UFormField label="Kapacita" required>
              <UInput
                v-model.number="roomFormData.capacity"
                type="number"
                min="1"
                placeholder="číslo"
                class="w-full"
                :disabled="isSaving || isDeleting"
              />
            </UFormField>

            <UFormField label="Popis (volitelné)">
              <UTextarea
                v-model="roomFormData.description"
                class="w-full"
                placeholder="Krátký popis sálu…"
                :rows="4"
                :disabled="isSaving || isDeleting"
              />
            </UFormField>

            <div class="flex justify-end gap-3 pt-6">
              <NuxtLink to="/admin/rooms">
                <UButton color="neutral" variant="ghost" type="button" :disabled="isSaving || isDeleting" :ui="{ base: 'cursor-pointer' }">
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
              <UButton type="submit" icon="i-lucide-save" :loading="isSaving" :disabled="isDeleting" :ui="{ base: 'cursor-pointer' }">
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
            <h2 class="text-lg font-semibold text-highlighted">Smazat sál</h2>
          </template>

          <template #body>
            <p class="text-sm text-muted">
              Opravdu chcete smazat sál „{{ roomFormData.name }}“? Tuto akci nelze vrátit zpět.
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
              label="Smazat sál"
              color="error"
              :loading="isDeleting"
              :ui="{ base: 'cursor-pointer' }"
              @click="deleteRoom"
            />
          </template>
        </UModal>
      </div>
    </main>
  </div>
</template>
