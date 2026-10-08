<script setup lang="ts">
definePageMeta({
  middleware: 'admin'
})

type UserRow = {
  id: string
  name: string
  email: string | null
  role: string
  credit: number
}

type CurrentUser = { roles: string[] }

interface DanceStyle {
  id: string
  name: string
  genre: string | null
  description: string | null
}

const config = useRuntimeConfig()
const toast = useToast()
const users = ref<UserRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const isAddDanceOpen = ref(false)
const isAddingDance = ref(false)

const danceFormData = reactive({
  name: '',
  genre: '',
  description: ''
})

const form = reactive({
  name: '',
  courseType: 'regular',
  danceStyle: '',
  level: '',
  instructor: '',
  room: '',
  dateFrom: '',
  dateTo: '',
  weekday: '',
  timeFrom: '',
  timeTo: '',
  capacity: 20,
  balancedRoles: false,
  price: null as number | null,
  note: ''
})

const courseTypes = [
  { label: 'Běžný kurz', value: 'regular' },
  { label: 'Školní kurz', value: 'school' }
]

const danceStyles = ref<{ label: string; value: string }[]>([])

onMounted(async () => {
  try {
    const styles = await $fetch<DanceStyle[]>('/api/admin/dancestyles', { credentials: 'include' })
    if (Array.isArray(styles) && styles.length) {
      danceStyles.value = styles.map((s) => ({ label: s.name, value: s.id }))
    } else {
      danceStyles.value = []
    }
  } catch (err) {
    danceStyles.value = []
    console.error('Failed to load dance styles', err)
  }
})

const levels = Array.from({ length: 8 }, (_, index) => ({
  label: `Úroveň ${index + 1}`,
  value: String(index + 1)
}))

const weekdays = [
  { label: 'Pondělí', value: 'monday' },
  { label: 'Úterý', value: 'tuesday' },
  { label: 'Středa', value: 'wednesday' },
  { label: 'Čtvrtek', value: 'thursday' },
  { label: 'Pátek', value: 'friday' },
  { label: 'Sobota', value: 'saturday' },
  { label: 'Neděle', value: 'sunday' }
]

async function addNewDance() {
  if (!danceFormData.name.trim()) {
    toast.add({
      title: 'Chyba',
      description: 'Název tance je povinný.',
      color: 'error'
    })
    return
  }

  isAddingDance.value = true
  try {
    const created = await $fetch<DanceStyle>('/api/admin/dancestyles', {
      method: 'POST',
      body: {
        name: danceFormData.name.trim(),
        genre: danceFormData.genre.trim() || null,
        description: danceFormData.description.trim() || null
      },
      credentials: 'include'
    })

    // Add created style to local select options and preselect it
    if (created && created.id) {
      danceStyles.value.unshift({ label: created.name, value: created.id })
      form.danceStyle = created.id
    }

    toast.add({
      title: 'Úspěch',
      description: `Tanec "${created?.name ?? danceFormData.name}" byl úspěšně přidán do databáze.`,
      color: 'success'
    })

    closeDanceModal()
  } catch (error: any) {
    toast.add({
      title: 'Chyba',
      description:
        error?.data?.message ||
        'Nepodařilo se přidat tanec. Zkuste to prosím později.',
      color: 'error'
    })
  } finally {
    isAddingDance.value = false
  }
}

function closeDanceModal() {
  isAddDanceOpen.value = false
  danceFormData.name = ''
  danceFormData.genre = ''
  danceFormData.description = ''
}
</script>

<template>
  <AppNavbar />

  <main class="bg-muted/30">
    <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
            Plánování kurzů
          </h1>
          <p class="mt-2 text-base text-muted">
            Správa kurzů, termínů a sálů
          </p>
        </div>
        <div>
          <NuxtLink to="/admin/dance">
            <UButton label="Správa tanců" color="primary" :ui="{ base: 'cursor-pointer' }" />
          </NuxtLink>
        </div>
      </div>

      <form
    class="rounded-xl border border-default bg-default p-6 shadow-sm"
    @submit.prevent="submitCourse"
  >
    <div class="space-y-6">
      <UFormField label="Název kurzu" required>
        <UInput
          v-model="form.name"
          class="w-full"
          placeholder="Např. Salsa"
        />
      </UFormField>

      <UFormField label="Typ kurzu" required>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <UButton
          v-for="type in courseTypes"
          :key="type.value"
          type="button"
          variant="outline"
          :class="form.courseType === type.value
            ? 'border-primary bg-primary/10 text-primary'
            : 'border-default hover:bg-elevated'"
          :ui="{ base: 'w-full cursor-pointer justify-start px-4 py-3' }"
          @click="form.courseType = type.value"
        >
          {{ type.label }}
        </UButton>
        </div>
      </UFormField>

      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <UFormField label="Styl tance" required>
          <div class="space-y-2">
            <USelect
              v-model="form.danceStyle"
              :items="danceStyles"
              :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
              class="w-full"
              placeholder="Vyberte tanec"
            />
            <UModal v-model:open="isAddDanceOpen" @close="closeDanceModal" :ui="{ base: 'max-w-4xl w-full', footer: 'justify-end' }">
              <UButton
                icon="i-lucide-plus"
                color="primary"
                :ui="{ base: 'cursor-pointer' }"
                label="Přidat nový tanec"
                variant="outline"
                @click="isAddDanceOpen = true"
              />

              <template #body>
                <div class="p-6">
                  <div class="mb-6">
                    <h2 class="text-lg font-semibold text-highlighted">
                      Přidat nový tanec
                    </h2>
                  </div>

                  <div class="space-y-6">
                    <UFormField label="Název tance" required>
                      <UInput
                        v-model="danceFormData.name"
                        class="w-full"
                        placeholder="např. Waltz, Tango, Samba…"
                        :disabled="isAddingDance"
                        @keydown.enter="addNewDance"
                      />
                    </UFormField>

                    <UFormField label="Žánr (volitelné)">
                      <UInput
                        v-model="danceFormData.genre"
                        class="w-full"
                        placeholder="např. Společenský tanec, Latinskoamerický…"
                        :disabled="isAddingDance"
                        @keydown.enter="addNewDance"
                      />
                    </UFormField>

                    <UFormField label="Popis (volitelné)">
                      <UTextarea
                        v-model="danceFormData.description"
                        class="w-full"
                        placeholder="Krátký popis tance…"
                        :rows="4"
                        :disabled="isAddingDance"
                      />
                    </UFormField>
                  </div>
                </div>
              </template>

              <template #footer="{ close }">
                <UButton label="Zrušit" color="primary" variant="outline" @click="close" :ui="{ base: 'cursor-pointer' }" />
                <UButton
                  label="Přidat tanec"
                  color="primary"
                  @click="addNewDance"
                  :loading="isAddingDance"
                  :disabled="!danceFormData.name.trim() || isAddingDance"
                  :ui="{ base: 'cursor-pointer' }"
                />
              </template>
            </UModal>
          </div>
        </UFormField>


        <UFormField label="Sál" required>
          <USelect
            v-model="form.room"
            :items="rooms"
            :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
            class="w-full"
            placeholder="Vyberte sál"
          />
        </UFormField>
      </div>

      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <UFormField label="Lektor">
          <USelect
            v-model="form.instructor"
            :items="instructors"
            :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
            class="w-full"
            placeholder="Lektora lze doplnit později"
          />
        </UFormField>

        <UFormField label="Úroveň" required>
        <USelect
          v-model="form.level"
          :items="levels"
          :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
          class="w-full"
          placeholder="Vyberte úroveň"
        />
      </UFormField>
      </div>

      <div class="border-t border-default pt-6">
        <h2 class="mb-4 text-lg font-semibold text-highlighted">
          Období a termín kurzu
        </h2>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <UFormField label="Datum od" required>
            <UInput v-model="form.dateFrom" type="date" class="w-full" :ui="{ base: 'cursor-text' }" />
          </UFormField>

          <UFormField label="Datum do" required>
            <UInput v-model="form.dateTo" type="date" class="w-full" :ui="{ base: 'cursor-text' }" />
          </UFormField>

          <UFormField label="Den" required>
            <USelect
              v-model="form.weekday"
              :items="weekdays"
              :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
              class="w-full"
              placeholder="Vyberte den"
            />
          </UFormField>

          <div class="grid grid-cols-2 gap-4">
            <UFormField label="Čas od" required>
              <UInput v-model="form.timeFrom" type="time" class="w-full" :ui="{ base: 'cursor-text' }" />
            </UFormField>

            <UFormField label="Čas do" required>
              <UInput v-model="form.timeTo" type="time" class="w-full" :ui="{ base: 'cursor-text' }" />
            </UFormField>
          </div>
        </div>

        <UButton
          class="mt-5"
          color="neutral"
          variant="soft"
          icon="i-lucide-plus"
          type="button"
          :ui="{ base: 'cursor-pointer' }"
        >
          Přidat další den
        </UButton>
      </div>

      <div class="border-t border-default pt-6">
        <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <UFormField label="Kapacita" required>
            <UInput
              v-model.number="form.capacity"
              type="number"
              min="1"
              class="w-full"
            />
          </UFormField>

          <UFormField label="Cena kurzu (Kč)" required>
            <UInput
              v-model.number="form.price"
              type="number"
              min="0"
              class="w-full"
              placeholder="Např. 2490"
            />
          </UFormField>
        </div>

        <UCheckbox
          v-model="form.balancedRoles"
          :ui="{
            base: 'cursor-pointer',
            label: 'cursor-pointer'
          }"
          class="mt-5"
          label="Vyžadovat vybrání role leader / follower"
        />
      </div>

      <UFormField label="Popis kurzu">
        <UTextarea
          v-model="form.note"
          class="w-full"
          :rows="4"
          placeholder="Pro koho je kurz určen, co se bude učit apod."
        />
      </UFormField>

      <div class="flex justify-end gap-3 pt-6">
        <UButton color="neutral" variant="ghost" type="button" :ui="{ base: 'cursor-pointer' }">
          Zrušit
        </UButton>

        <UButton type="submit" icon="i-lucide-plus" :ui="{ base: 'cursor-pointer' }">
          Vytvořit kurz
        </UButton>
      </div>
    </div>
  </form>
    </div>
  </main>
</template>
