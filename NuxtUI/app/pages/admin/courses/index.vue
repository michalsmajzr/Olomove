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

const config = useRuntimeConfig()
const users = ref<UserRow[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

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

const danceStyles = [
  { label: 'Tanec nevybrán', value: 'unassigned' },
]

const levels = Array.from({ length: 8 }, (_, index) => ({
  label: `Úroveň ${index + 1}`,
  value: String(index + 1)
}))

const instructors = [
  { label: 'Zatím nevybrán', value: 'unassigned' },
]

const rooms = [
  { label: 'Sál nevybrán', value: 'unassigned' },
]

const weekdays = [
  { label: 'Pondělí', value: 'monday' },
  { label: 'Úterý', value: 'tuesday' },
  { label: 'Středa', value: 'wednesday' },
  { label: 'Čtvrtek', value: 'thursday' },
  { label: 'Pátek', value: 'friday' },
  { label: 'Sobota', value: 'saturday' },
  { label: 'Neděle', value: 'sunday' }
]

</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
            Plánování kurzů
          </h1>
          <p class="mt-2 text-base text-muted">
            Správa kurzů, termínů a sálů
          </p>
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
            <USelect
              v-model="form.danceStyle"
              :items="danceStyles"
              :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
              class="w-full"
              placeholder="Vyberte tanec"
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
  </div>
</template>
