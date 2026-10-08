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

interface Room {
  id: string
  name: string
  capacity: number
  description: string | null
}

const config = useRuntimeConfig()
const toast = useToast()
const rooms = ref<{ label: string; value: string; capacity: number }[]>([])
const instructors = ref<{ label: string; value: string }[]>([])
const isSubmittingCourse = ref(false)

const form = reactive({
  name: '',
  courseType: 'regular',
  danceStyleIds: [''],
  level: '',
  instructorIds: [''],
  dateFrom: '',
  dateTo: '',
  capacity: 20,
  balancedRoles: false,
  price: null as number | null,
  note: '',
  sessions: [{
    roomId: '',
    weekday: '',
    startsAt: '',
    endsAt: ''
  }]
})

const courseTypes = [
  { label: 'Běžný kurz', value: 'regular' },
  { label: 'Školní kurz', value: 'school' }
]

const danceStyles = ref<{ label: string; value: string }[]>([])

onMounted(async () => {
  try {
    const [styles, availableRooms, availableUsers] = await Promise.all([
      $fetch<DanceStyle[]>('/api/admin/dancestyles', { credentials: 'include' }),
      $fetch<Room[]>('/api/admin/rooms', { credentials: 'include' }),
      $fetch<UserRow[]>('/api/admin/users', { credentials: 'include' })
    ])

    danceStyles.value = Array.isArray(styles)
      ? styles.map(style => ({ label: style.name, value: style.id }))
      : []

    rooms.value = Array.isArray(availableRooms)
      ? availableRooms.map(room => ({
          label: `${room.name} (${room.capacity} míst)`,
          value: room.id,
          capacity: room.capacity
        }))
      : []

    instructors.value = Array.isArray(availableUsers)
      ? availableUsers
          .filter(user => user.role === 'Lecturer' || user.role === 'Teacher')
          .map(user => ({ label: user.name, value: user.id }))
      : []
  } catch (error) {
    danceStyles.value = []
    rooms.value = []
    instructors.value = []
    console.error('Failed to load dance styles, rooms or instructors', error)
  }
})

function addInstructorField() {
  form.instructorIds.push('')
}

function removeInstructorField(index: number) {
  if (form.instructorIds.length > 1) {
    form.instructorIds.splice(index, 1)
  }
}

function availableInstructors(index: number) {
  const selectedByOtherField = new Set(
    form.instructorIds.filter((instructorId, fieldIndex) => fieldIndex !== index && instructorId)
  )

  return instructors.value.filter(instructor => !selectedByOtherField.has(instructor.value))
}

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

function addDanceStyleField() {
  form.danceStyleIds.push('')
}

function removeDanceStyleField(index: number) {
  if (form.danceStyleIds.length > 1) {
    form.danceStyleIds.splice(index, 1)
  }
}

function addSession() {
  form.sessions.push({
    roomId: '',
    weekday: '',
    startsAt: '',
    endsAt: ''
  })
}

function removeSession(index: number) {
  if (form.sessions.length > 1) {
    form.sessions.splice(index, 1)
  }
}

const maxCourseCapacity = computed(() => {
  const capacities = form.sessions
    .map(session => rooms.value.find(room => room.value === session.roomId)?.capacity)
    .filter((capacity): capacity is number => capacity !== undefined)

  return capacities.length > 0 ? Math.min(...capacities) : null
})

watch(maxCourseCapacity, (maxCapacity) => {
  if (maxCapacity !== null && form.capacity > maxCapacity) {
    form.capacity = maxCapacity
  }
})

async function submitCourse() {
  const danceStyleIds = [...new Set(form.danceStyleIds.filter(Boolean))]

  if (danceStyleIds.length === 0) {
    toast.add({ title: 'Chyba', description: 'Vyberte alespoň jeden taneční styl.', color: 'error' })
    return
  }

  const instructorIds = [...new Set(form.instructorIds.filter(Boolean))]

  if (instructorIds.length === 0) {
    toast.add({ title: 'Chyba', description: 'Vyberte alespoň jednoho lektora.', color: 'error' })
    return
  }

  if (!form.level || !form.dateFrom || !form.dateTo) {
    toast.add({ title: 'Chyba', description: 'Vyplňte všechna povinná pole kurzu.', color: 'error' })
    return
  }

  if (form.sessions.some(session => !session.roomId || !session.weekday || !session.startsAt || !session.endsAt)) {
    toast.add({ title: 'Chyba', description: 'Vyplňte místnost, den a čas u každého dne kurzu.', color: 'error' })
    return
  }

  isSubmittingCourse.value = true
  try {
    await $fetch('/api/admin/courses', {
      method: 'POST',
      body: {
        name: form.name.trim(),
        courseType: form.courseType,
        courseDanceStyles: danceStyleIds.map(danceStyleId => ({ danceStyleId })),
        level: Number(form.level),
        courseInstructors: instructorIds.map(instructorId => ({ instructorId })),
        startDate: form.dateFrom,
        endDate: form.dateTo,
        capacity: form.capacity,
        balancedRoles: form.balancedRoles,
        price: form.price ?? 0,
        description: form.note.trim() || null,
        sessions: form.sessions.map(session => ({
          roomId: session.roomId,
          weekday: session.weekday,
          startsAt: session.startsAt,
          endsAt: session.endsAt
        }))
      },
      credentials: 'include'
    })

    await navigateTo({
      path: '/admin/courses',
      query: { created: '1' }
    })
  } catch (error: any) {
    const errors = error?.data?.errors
    const validationMessage = errors
      ? Object.values(errors).flat().join(' ')
      : null

    toast.add({
      title: 'Chyba',
      description: validationMessage
        ?? error?.data?.detail
        ?? error?.data?.message
        ?? 'Kurz se nepodařilo vytvořit.',
      color: 'error'
    })
  } finally {
    isSubmittingCourse.value = false
  }
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
        <div class="flex gap-3">
          <NuxtLink to="/admin/dance">
            <UButton label="Správa tanců" color="primary" :ui="{ base: 'cursor-pointer' }" />
          </NuxtLink>
          <NuxtLink to="/admin/rooms">
            <UButton label="Správa sálů" color="primary" :ui="{ base: 'cursor-pointer' }" />
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
        <UFormField label="Taneční styly" required>
          <div class="space-y-3">
            <div
              v-for="(_, index) in form.danceStyleIds"
              :key="index"
              class="flex items-center gap-2"
            >
              <USelect
                v-model="form.danceStyleIds[index]"
                :items="danceStyles"
                :ui="{ base: 'w-full cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
                class="w-full"
                placeholder="Vyberte tanec"
              />
              <UButton
                v-if="form.danceStyleIds.length > 1"
                type="button"
                icon="i-lucide-trash-2"
                color="error"
                variant="ghost"
                :ui="{ base: 'cursor-pointer' }"
                aria-label="Odebrat taneční styl"
                @click="removeDanceStyleField(index)"
              />
            </div>

            <UButton
              type="button"
              icon="i-lucide-plus"
              color="primary"
              variant="outline"
              :ui="{ base: 'cursor-pointer' }"
              @click="addDanceStyleField"
            >
              Přidat další taneční styl
            </UButton>
          </div>
        </UFormField>
      </div>

      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <UFormField label="Lektoři" required>
          <div class="space-y-3">
            <div
              v-for="(_, index) in form.instructorIds"
              :key="index"
              class="flex items-center gap-2"
            >
              <USelect
                v-model="form.instructorIds[index]"
                :items="availableInstructors(index)"
                :ui="{ base: 'cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
                class="w-full"
                placeholder="Vyberte lektora"
              />
              <UButton
                v-if="form.instructorIds.length > 1"
                type="button"
                icon="i-lucide-trash-2"
                color="error"
                variant="ghost"
                :ui="{ base: 'cursor-pointer' }"
                aria-label="Odebrat lektora"
                @click="removeInstructorField(index)"
              />
            </div>

            <UButton
              type="button"
              icon="i-lucide-plus"
              color="primary"
              variant="outline"
              :ui="{ base: 'cursor-pointer' }"
              @click="addInstructorField"
            >
              Přidat dalšího lektora
            </UButton>
          </div>
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
          Období kurzu
        </h2>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <UFormField label="Datum od" required>
            <UInput v-model="form.dateFrom" type="date" class="w-full" :ui="{ base: 'cursor-text' }" />
          </UFormField>

          <UFormField label="Datum do" required>
            <UInput v-model="form.dateTo" type="date" class="w-full" :ui="{ base: 'cursor-text' }" />
          </UFormField>
        </div>
      </div>

      <div class="border-t border-default pt-6">
        <h2 class="mb-4 text-lg font-semibold text-highlighted">
          Dny a časy kurzu
        </h2>

        <div class="space-y-4">
          <div
            v-for="(session, index) in form.sessions"
            :key="index"
            class="grid grid-cols-1 items-end gap-4 md:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(0,1.5fr)_auto]"
          >
            <UFormField label="Den" required>
              <USelect
                v-model="session.weekday"
                :items="weekdays"
                :ui="{ base: 'w-full cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
                class="w-full"
                placeholder="Vyberte den"
              />
            </UFormField>

            <UFormField label="Čas od" required>
              <UInput v-model="session.startsAt" type="time" class="w-full" :ui="{ base: 'cursor-text' }" />
            </UFormField>

            <UFormField label="Čas do" required>
              <UInput v-model="session.endsAt" type="time" class="w-full" :ui="{ base: 'cursor-text' }" />
            </UFormField>

            <UFormField label="Sál" required>
              <USelect
                v-model="session.roomId"
                :items="rooms"
                :ui="{ base: 'w-full cursor-pointer data-[state=open]:cursor-pointer', item: 'cursor-pointer', itemLabel: 'cursor-pointer', itemTrailing: 'cursor-pointer', itemTrailingIcon: 'cursor-pointer' }"
                class="w-full"
                placeholder="Vyberte sál"
              />
            </UFormField>

            <UButton
              v-if="form.sessions.length > 1"
              type="button"
              icon="i-lucide-trash-2"
              color="error"
              variant="ghost"
              :ui="{ base: 'cursor-pointer' }"
              aria-label="Odebrat den"
              @click="removeSession(index)"
            />
          </div>
        </div>

        <UButton
          class="mt-5"
          color="neutral"
          variant="soft"
          icon="i-lucide-plus"
          type="button"
          :ui="{ base: 'cursor-pointer' }"
          @click="addSession"
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
              :max="maxCourseCapacity ?? undefined"
              class="w-full"
            />
            <p v-if="maxCourseCapacity !== null" class="mt-1 text-sm text-muted">
              Maximální kapacita podle vybraných sálů: {{ maxCourseCapacity }}
            </p>
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

        <UButton
          type="submit"
          icon="i-lucide-plus"
          :loading="isSubmittingCourse"
          :disabled="isSubmittingCourse"
          :ui="{ base: 'cursor-pointer' }"
        >
          Vytvořit kurz
        </UButton>
      </div>
    </div>
  </form>
    </div>
  </main>
</template>
