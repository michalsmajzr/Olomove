<script setup lang="ts">
// page accessible to all authenticated users
definePageMeta({
  middleware: 'user'
})

const { currentUser, hasRole, isAdmin } = useRoleCheck()
</script>

<template>
  <div class="p-6">
    <h1 class="text-3xl font-bold mb-4">Můj profil</h1>

    <div v-if="currentUser" class="bg-gray-50 p-4 rounded-lg mb-4">
      <p class="text-sm text-gray-600">logged in as:</p>
      <p class="font-semibold">{{ currentUser.id }}</p>
      <p class="text-sm text-gray-600 mt-2">roles: {{ currentUser.roles.join(', ') }}</p>
    </div>

    <div class="space-y-4">
      <div class="bg-white p-4 rounded-lg shadow">
        <h2 class="font-semibold mb-2">my courses</h2>
        <p class="text-gray-600 text-sm">courses you are enrolled in</p>
      </div>

      <div v-if="isAdmin()" class="bg-blue-50 p-4 rounded-lg border-l-4 border-blue-500">
        <p class="text-sm font-semibold text-blue-600">you have admin access - special options available</p>
      </div>
    </div>
  </div>
</template>
