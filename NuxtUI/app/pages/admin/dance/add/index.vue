<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

const toast = useToast()
const isAddingDance = ref(false)

const danceFormData = reactive({
  name: '',
  genre: '',
  description: ''
})

async function addNewDance() {
  if (!danceFormData.name.trim()) {
    toast.add({ title: 'Chyba', description: 'Název tance je povinný.', color: 'error' })
    return
  }

  isAddingDance.value = true
  try {
    const created = await $fetch('/api/admin/dancestyles', {
      method: 'POST',
      body: {
        name: danceFormData.name.trim(),
        genre: danceFormData.genre.trim() || null,
        description: danceFormData.description.trim() || null
      },
      credentials: 'include'
    })

    toast.add({ title: 'Úspěch', description: `Tanec "${created?.name ?? danceFormData.name}" byl úspěšně přidán.`, color: 'success' })

    // reset form
    danceFormData.name = ''
    danceFormData.genre = ''
    danceFormData.description = ''

    // navigate back to listing
    await navigateTo('/admin/dance')
  } catch (error: any) {
    toast.add({ title: 'Chyba', description: error?.data?.message ?? 'Nepodařilo se přidat tanec.', color: 'error' })
  } finally {
    isAddingDance.value = false
  }
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">Přidat nový tanec</h1>
          <p class="mt-2 text-base text-muted">Formulář pro přidání nového stylu tance</p>
        </div>

        <form class="rounded-xl border border-default bg-default p-6 shadow-sm" @submit.prevent="addNewDance">
          <div class="space-y-6">
            <UFormField label="Název tance" required>
              <UInput v-model="danceFormData.name" class="w-full" placeholder="např. Waltz, Tango, Samba…" :disabled="isAddingDance" />
            </UFormField>

            <UFormField label="Žánr (volitelné)">
              <UInput v-model="danceFormData.genre" class="w-full" placeholder="např. Společenský tanec, Latinskoamerický…" :disabled="isAddingDance" />
            </UFormField>

            <UFormField label="Popis (volitelné)">
              <UTextarea v-model="danceFormData.description" class="w-full" placeholder="Krátký popis tance…" :rows="4" :disabled="isAddingDance" />
            </UFormField>

            <div class="flex justify-end gap-3 pt-6">
              <NuxtLink to="/admin/dance">
                <UButton color="neutral" variant="ghost" type="button" :ui="{ base: 'cursor-pointer' }">Zrušit</UButton>
              </NuxtLink>

              <UButton type="submit" icon="i-lucide-plus" :loading="isAddingDance" :ui="{ base: 'cursor-pointer' }">Přidat tanec</UButton>
            </div>
          </div>
        </form>
      </div>
    </main>
  </div>
</template>
