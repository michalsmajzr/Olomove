<script setup lang="ts">
// page accessible only to administrators
definePageMeta({
  middleware: 'admin'
})

const { isAdmin, currentUser } = useRoleCheck()
</script>

<template>
  <div class="p-6">
    <h1 class="text-3xl font-bold mb-4">Admin Dashboard</h1>

    <div v-if="currentUser" class="bg-blue-50 p-4 rounded-lg mb-4">
      <p class="text-sm text-gray-600">logged in as:</p>
      <p class="font-semibold">{{ currentUser.id }}</p>
      <p class="text-sm text-gray-600 mt-2">roles: {{ currentUser.roles.join(', ') }}</p>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div class="bg-white p-4 rounded-lg shadow">
        <h2 class="font-semibold mb-2">user management</h2>
        <p class="text-gray-600 text-sm">manage accounts and roles</p>
      </div>

      <div class="bg-white p-4 rounded-lg shadow">
        <h2 class="font-semibold mb-2">course management</h2>
        <p class="text-gray-600 text-sm">manage all courses</p>
      </div>
    </div>
  </div>
</template>
