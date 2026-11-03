<script setup lang="ts">
  type CurrentUser = { roles: string[] }
  const currentUser = useState<CurrentUser | null>('auth-user', () => null)
  const isAdmin = computed(() => currentUser.value?.roles.includes('Admin') ?? false)

  type AboutPage = {
    id: number
    title: string
    content: string
    updatedAt: string
  }

  // get
  const { data: aboutData, refresh } = await useAsyncData<AboutPage>('about-page', () =>
    $fetch<AboutPage>('/api/about')
  )

  const title = ref(aboutData.value?.title ?? 'Nadpis')
  const content = ref(aboutData.value?.content ?? 'Obsah')

  watch(aboutData, (newData) => {
    if (newData) {
      title.value = newData.title
      content.value = newData.content
    }
  }, { immediate: true })

  const isSaving = ref(false)

  async function saveContent() {
    isSaving.value = true
    try {
      // put do databaze
      await $fetch('/api/admin/about', {
        method: 'PUT',
        body: {
          title: title.value,
          content: content.value
        }
      })

      await refresh()

      console.log('Success')
    } catch (error) {
      console.error('Error:', error)
    } finally {
      isSaving.value = false
    }
  }
</script>


<template>
  <div>
    <AppNavbar />

    <div class="py-12 px-4">
      <div class="mx-auto max-w-3xl text-center">
        <!-- nadpis -->
        <h1 class="text-4xl font-extrabold text-orange-500 mb-10">
          O nás
        </h1>

        <!-- karta -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-8 sm:p-12 shadow-sm text-center">

          <!-- editace pro admina -->
          <div v-if="isAdmin" class="space-y-6">
            <div>
              <label class="block text-xs font-semibold text-slate-400 mb-1 text-left">Nadpis</label>
              <UInput v-model="title"
                      size="lg"
                      class="w-full text-center font-bold text-slate-800" />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-400 mb-1 text-left">Text</label>
              <UTextarea v-model="content"
                         :rows="12"
                         class="w-full text-center text-slate-600 leading-relaxed" />
            </div>

            <div class="pt-2 flex justify-center">
              <UButton label="Uložit změny"
                       color="primary"
                       size="md"
                       icon="i-lucide-check"
                       :loading="isSaving"
                       class="px-6 cursor-pointer"
                       @click="saveContent" />
            </div>
          </div>

          <!-- zobrazeni pro ostatni -->
          <div v-else class="space-y-6">
            <h2 class="text-xl font-bold text-slate-800">
              {{ title }}
            </h2>

            <div class="whitespace-pre-line text-sm sm:text-base leading-relaxed text-slate-600">
              {{ content }}
            </div>
          </div>

        </div>
      </div>
    </div>
  </div>
</template>
