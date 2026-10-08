<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type RoomsRow = {
  id: string
  name: string
  capacity: number
  description: string | null
}

const toast = useToast()
const isAddingRoom = ref(false)

const form = reactive({
  name: '',
  capacity: null as number | null,
  description: ''
})

async function addNewRoom() {
  if (!form.name.trim()) {
    toast.add({ title: 'Chyba', description: 'Název sálu je povinný.', color: 'error' })
    return
  }

  if (form.capacity === null || form.capacity < 1) {
    toast.add({ title: 'Chyba', description: 'Kapacita musí být alespoň 1.', color: 'error' })
    return
  }

  isAddingRoom.value = true
  try {
    const created = await $fetch<RoomsRow>('/api/admin/rooms', {
      method: 'POST',
      body: {
        name: form.name.trim(),
        capacity: form.capacity,
        description: form.description.trim() || null
      },
      credentials: 'include'
    })

    toast.add({ title: 'Úspěch', description: `Sál "${created.name}" byl úspěšně přidán.`, color: 'success' })

    form.name = ''
    form.capacity = null
    form.description = ''

    await navigateTo('/admin/rooms')
  } catch (error: any) {
    toast.add({ title: 'Chyba', description: error?.data?.message ?? 'Nepodařilo se přidat sál.', color: 'error' })
  } finally {
    isAddingRoom.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">Přidat nový sál</h1>
          <p class="mt-2 text-base text-muted">Formulář pro přidání nového sálu</p>
        </div>

        <form class="rounded-xl border border-default bg-default p-6 shadow-sm" @submit.prevent="addNewRoom">
          <div class="space-y-6">
            <UFormField label="Název sálu" required>
              <UInput v-model="form.name" class="w-full" placeholder="např. Sál 1…" :disabled="isAddingRoom" />
            </UFormField>

            <UFormField label="Kapacita" required>
              <UInput
                v-model.number="form.capacity"
                type="number"
                min="1"
                placeholder="číslo"
                class="w-full"
                :disabled="isAddingRoom"
              />
            </UFormField>

            <UFormField label="Popis (volitelné)">
              <UTextarea v-model="form.description" class="w-full" placeholder="Krátký popis sálu…" :rows="4" :disabled="isAddingRoom" />
            </UFormField>

            <div class="flex justify-end gap-3 pt-6">
              <NuxtLink to="/admin/rooms">
                <UButton color="neutral" variant="ghost" type="button" :disabled="isAddingRoom" :ui="{ base: 'cursor-pointer' }">Zrušit</UButton>
              </NuxtLink>

              <UButton type="submit" icon="i-lucide-plus" :loading="isAddingRoom" :ui="{ base: 'cursor-pointer' }">Přidat sál</UButton>
            </div>
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
