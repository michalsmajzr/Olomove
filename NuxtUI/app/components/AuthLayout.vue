<template>
  <main class="min-h-screen px-6 py-10 sm:px-12">
    <UButton
      to="/"
      icon="i-lucide-arrow-left"
      aria-label="Zpět na úvodní stránku"
      variant="ghost"
      color="neutral"
      class="rounded-full text-highlighted transition-colors hover:bg-elevated"
      :ui="{ base: 'size-12 justify-center p-0', leadingIcon: 'size-7' }"
    />
    <section class="mx-auto flex min-h-[calc(100vh-10rem)] max-w-md items-center py-10">
      <UCard class="w-full border border-slate-200 shadow-none"><slot /></UCard>
    </section>
  </main>
</template>
