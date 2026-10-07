<script setup lang="ts">
// page accessible to instructors and teachers only
definePageMeta({
  middleware: 'auth-role',
  requiredRoles: ['Instructor', 'Teacher']
})

const { isInstructor, isTeacher, hasRole, currentUser } = useRoleCheck()
</script>

<template>
  <div class="p-6">
    <h1 class="text-3xl font-bold mb-4">Instruktor / Učitel Dashboard</h1>

    <div v-if="currentUser" class="bg-green-50 p-4 rounded-lg mb-4">
      <p class="text-sm text-gray-600">logged in as:</p>
      <p class="font-semibold">{{ currentUser.id }}</p>
      <p class="text-sm text-gray-600 mt-2">roles: {{ currentUser.roles.join(', ') }}</p>
    </div>

    <div class="space-y-4">
      <div v-if="isInstructor()" class="bg-yellow-50 p-4 rounded-lg">
        <h2 class="font-semibold mb-2">my courses (instructor)</h2>
        <p class="text-gray-600 text-sm">manage courses as instructor</p>
      </div>

      <div v-if="isTeacher()" class="bg-purple-50 p-4 rounded-lg">
        <h2 class="font-semibold mb-2">student management (teacher)</h2>
        <p class="text-gray-600 text-sm">manage students and their progress</p>
      </div>
    </div>
  </div>
</template>
