<script setup lang="ts">
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)
const showPassword = ref(false)
const form = reactive({ email: '', password: '', rememberMe: false })

type LoginResponse = { roles: string[] }

async function login() {
  loading.value = true
  try {
    const user = await $fetch<LoginResponse>(`${config.public.apiBase}/api/auth/login`, {
      method: 'POST', credentials: 'include', body: form
    })
    await navigateTo(user.roles.includes('Admin') ? '/admin/users' : '/')
  } catch (error: unknown) {
    const response = error as { data?: { message?: string } }

    toast.add({
      title: 'Přihlášení se nepodařilo',
      description: response.data?.message ?? 'E-mail nebo heslo není správné.',
      color: 'error'
    })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <div class="space-y-7 px-2 py-4 sm:px-4">
      <header class="text-center">
        <UIcon name="i-lucide-circle-user-round" class="size-10" />
        <h1 class="mt-3 text-2xl font-bold">Přihlášení</h1>
        <p class="mt-2 text-lg text-slate-500">Zadejte své přihlašovací údaje pro přístup ke svému účtu.</p>
      </header>
      <UForm :state="form" class="space-y-5" @submit="login">
        <UFormField label="Email" name="email" required>
          <UInput v-model="form.email" type="email" autocomplete="email" placeholder="Zadejte svůj e-mail" class="w-full" />
        </UFormField>
        <UFormField label="Heslo" name="password" required>
          <UInput
            v-model="form.password"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="current-password"
            placeholder="Zadejte své heslo"
            class="w-full"
          >
            <template #trailing>
              <UButton
                :icon="showPassword ? 'i-lucide-eye-off' : 'i-lucide-eye'"
                :aria-label="showPassword ? 'Skrýt heslo' : 'Zobrazit heslo'"
                color="neutral"
                variant="link"
                size="sm"
                class="cursor-pointer"
                @click="showPassword = !showPassword"
              />
            </template>
          </UInput>
        </UFormField>
        <!--<div class="flex items-center justify-between">
          <UCheckbox v-model="form.rememberMe" label="Pamatovat si mě" class="cursor-pointer" />
          <NuxtLink to="/forgot-password" class="cursor-pointer text-primary underline">Zapomenuté heslo?</NuxtLink>
        </div>-->
        <UButton block type="submit" size="lg" :loading="loading" label="Přihlásit se" class="cursor-pointer" />
      </UForm>
      <p class="text-center text-slate-500">Nemáte účet? <NuxtLink to="/signup" class="cursor-pointer text-primary underline">Zaregistrujte se</NuxtLink></p>
      <p class="text-center text-sm text-slate-500">Přihlášením souhlasíte s našimi obchodními podmínkami.</p>
    </div>
  </AuthLayout>
</template>
