<script setup lang="ts">
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)
const showPassword = ref(false)
const showPasswordConfirmation = ref(false)
const form = reactive({ firstName: '', lastName: '', email: '', password: '', passwordConfirmation: '' })

async function register() {
  if (form.password !== form.passwordConfirmation) {
    toast.add({ title: 'Hesla se neshodují', description: 'Zadejte stejné heslo do obou polí.', color: 'error' })
    return
  }

  loading.value = true
  try {
    await $fetch(`${config.public.apiBase}/api/auth/register`, {
      method: 'POST',
      credentials: 'include',
      body: {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        password: form.password
      }
    })
    await navigateTo('/')
  } catch (error: unknown) {
    const response = error as {
      data?: {
        message?: string
        errors?: Record<string, string[]>
      }
    }
    const validationMessages = response.data?.errors
      ? Object.values(response.data.errors).flat().join(' ')
      : undefined

    toast.add({
      title: 'Registrace se nepodařila',
      description: validationMessages ?? response.data?.message ?? 'Zkontrolujte prosím zadané údaje.',
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
        <UIcon name="i-lucide-user-round-plus" class="size-10" />
        <h1 class="mt-3 text-2xl font-bold">Vytvořit účet</h1>
        <p class="mt-2 text-lg text-slate-500">Zaregistrujte se pomocí e-mailu a hesla.</p>
      </header>
      <UForm :state="form" class="space-y-5" @submit="register">
        <div class="grid gap-5 sm:grid-cols-2">
          <UFormField label="Jméno" name="firstName" required><UInput v-model="form.firstName" autocomplete="given-name" class="w-full" /></UFormField>
          <UFormField label="Příjmení" name="lastName" required><UInput v-model="form.lastName" autocomplete="family-name" class="w-full" /></UFormField>
        </div>
        <UFormField label="Email" name="email" required><UInput v-model="form.email" type="email" autocomplete="email" class="w-full" /></UFormField>
        <UFormField label="Heslo" name="password" required hint="Minimálně 8 znaků">
          <UInput v-model="form.password" :type="showPassword ? 'text' : 'password'" autocomplete="new-password" class="w-full">
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
        <UFormField label="Potvrzení hesla" name="passwordConfirmation" required>
          <UInput v-model="form.passwordConfirmation" :type="showPasswordConfirmation ? 'text' : 'password'" autocomplete="new-password" class="w-full">
            <template #trailing>
              <UButton
                :icon="showPasswordConfirmation ? 'i-lucide-eye-off' : 'i-lucide-eye'"
                :aria-label="showPasswordConfirmation ? 'Skrýt heslo' : 'Zobrazit heslo'"
                color="neutral"
                variant="link"
                size="sm"
                class="cursor-pointer"
                @click="showPasswordConfirmation = !showPasswordConfirmation"
              />
            </template>
          </UInput>
        </UFormField>
        <UButton block type="submit" size="lg" :loading="loading" label="Zaregistrovat se" class="cursor-pointer" />
      </UForm>
      <p class="text-center text-slate-500">Už máte účet? <NuxtLink to="/login" class="cursor-pointer text-primary underline">Přihlaste se</NuxtLink></p>
    </div>
  </AuthLayout>
</template>
