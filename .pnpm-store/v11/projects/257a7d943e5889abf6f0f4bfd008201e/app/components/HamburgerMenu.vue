<script setup lang="ts">
defineProps<{
  items: Array<{ label: string, to: string }>
  authenticated: boolean
}>()

const emit = defineEmits<{ logout: [] }>()
const open = ref(false)

</script>

<template>
  <UButton
    class="text-highlighted hover:bg-transparent hover:opacity-80 md:hidden"
    icon="i-lucide-menu"
    aria-label="Otevřít navigaci"
    :aria-expanded="open"
    aria-controls="mobile-navigation"
    variant="ghost"
    color="neutral"
    :ui="{ base: 'size-8 p-0', leadingIcon: 'size-6' }"
    @click="open = true"
  />

  <div
    v-if="open"
    id="mobile-navigation"
    class="fixed inset-0 z-50 flex flex-col bg-default md:hidden"
    role="dialog"
    aria-modal="true"
    aria-label="Mobilní navigace"
  >
    <div class="border-b border-slate-200 bg-white">
      <div class="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
        <NuxtLink to="/" @click="open = false">
          <AppLogo class="h-14 pt-2 pb-4 w-auto" />
        </NuxtLink>

        <UButton
          class="text-highlighted hover:bg-transparent hover:opacity-80"
          icon="i-lucide-x"
          aria-label="Zavřít navigaci"
          variant="ghost"
          color="neutral"
          :ui="{ base: 'size-8 p-0', leadingIcon: 'size-6' }"
          @click="open = false"
        />
      </div>
    </div>

    <div class="flex flex-1 flex-col p-6">
      <UNavigationMenu
        :items="items"
        orientation="vertical"
        class="w-full"
        :ui="{
          list: 'gap-2',
          link: 'rounded-lg px-3 py-1.5 text-sm font-medium leading-6 text-muted hover:bg-elevated hover:text-primary data-[active]:bg-elevated data-[active]:text-primary'
        }"
        @click="open = false"
      />

      <div class="mt-auto flex gap-2 border-t border-default pt-4">
        <UButton
        v-if="authenticated"
        to="/profile"
        label="Můj profil"
        icon="i-lucide-circle-user-round"
        variant="ghost"
        color="neutral"
      />
      <UButton
        v-if="authenticated"
        label="Odhlásit se"
        icon="i-lucide-log-out"
        variant="ghost"
        color="neutral"
        class="cursor-pointer"
        @click="emit('logout')"
      />
      <UButton
        v-if="!authenticated"
        to="https://www.instagram.com/"
        target="_blank"
        label="Instagram"
        icon="i-lucide-instagram"
        variant="ghost"
        color="neutral"
      />
      <UButton
        v-if="!authenticated"
        to="/login"
        label="Přihlášení"
        icon="i-lucide-log-in"
        variant="ghost"
        color="neutral"
      />
      </div>
    </div>
  </div>
</template>
