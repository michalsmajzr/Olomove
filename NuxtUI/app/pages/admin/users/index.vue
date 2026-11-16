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

type PaymentRow = {
  id: string
  amount: number
  variableSymbol: string
  status: 'Pending' | 'Credited' | 'Cancelled'
  createdAt: string
  creditedAt: string | null
  user: { id: string; name: string; email: string | null }
}

const users = ref<UserRow[]>([])
const payments = ref<PaymentRow[]>([])
const isPaymentsLoading = ref(true)
const paymentError = ref('')
const creditingPaymentId = ref<string | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')
const isUpdating = ref<string | null>(null)
const toast = useToast()
const adjustingUserId = ref<string | null>(null)
const creditAdjustmentAmount = ref<number>(100)
const isAdjustingCredit = ref(false)
const deletingUserId = ref<string | null>(null)
const cancelingPaymentId = ref<string | null>(null)

const columns = [
  { accessorKey: 'name', header: 'Jméno' },
  { accessorKey: 'email', header: 'E-mail' },
  { accessorKey: 'role', header: 'Role' },
  { accessorKey: 'credit', header: 'Kredit' },
  { accessorKey: 'actions', header: 'Akce' }
]

const roleLabels: Record<string, string> = {
  Admin: 'Administrátor',
  Instructor: 'Lektor',
  Teacher: 'Učitel',
  User: 'Klient'
}

const roleOptions = [
  { label: 'Administrátor', value: 'Admin' },
  { label: 'Lektor', value: 'Instructor' },
  { label: 'Uživatel', value: 'User' }
]

  const tableRows = computed(() => users.value.map(user => ({
    ...user,
    originalRole: user.role,
    roleLabel: roleLabels[user.role] ?? user.role,
    credit: user.role === 'User' ? `${user.credit.toLocaleString('cs-CZ')} Kč` : '—'
  })))

  const paymentStatusLabels: Record<PaymentRow['status'], string> = {
    Pending: 'Čeká na kontrolu',
    Credited: 'Připsáno',
    Cancelled: 'Zrušeno'
  }

  const paymentRows = computed(() => payments.value.map(payment => ({
    ...payment,
    amountLabel: `${payment.amount.toLocaleString('cs-CZ')} Kč`,
    statusLabel: paymentStatusLabels[payment.status] ?? payment.status,
    dateLabel: new Date(payment.createdAt).toLocaleString('cs-CZ')
  })))

onMounted(async () => {
  const results = await Promise.allSettled([
    $fetch<UserRow[]>('/api/admin/users', { credentials: 'include' }),
    $fetch<PaymentRow[]>('/api/admin/payments', { credentials: 'include' })
  ])
  if (results[0]?.status === 'fulfilled') {
    users.value = results[0].value
  } else {
    errorMessage.value = 'Seznam uživatelů se nepodařilo načíst.'
  }
  if (results[1]?.status === 'fulfilled') {
    payments.value = results[1].value
  } else {
    paymentError.value = 'Seznam plateb se nepodařilo načíst.'
  }
  isLoading.value = false
  isPaymentsLoading.value = false
})

function formatPaymentAmount(amount: number) {
  return amount.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
}

async function creditPayment(payment: PaymentRow) {
  if (payment.status !== 'Pending' || creditingPaymentId.value) {
    return
  }
  creditingPaymentId.value = payment.id
  try {
    await $fetch(`/api/admin/payments/${payment.id}/credit`, {
      method: 'POST', credentials: 'include'
    })
      payment.status = 'Credited'
    payment.creditedAt = new Date().toISOString()

    const index = payments.value.findIndex(p => p.id === payment.id)
      if (index !== -1) {
          payments.value[index] = {
              ...payments.value[index]!,
              status: 'Credited',
              creditedAt: new Date().toISOString()
      }
    }

    const user = users.value.find(item => item.id === payment.user.id)
    if (user) {
      user.credit += payment.amount
    }
    toast.add({ title: 'Platba připsána', description: `${formatPaymentAmount(payment.amount)} Kč bylo připsáno uživateli.`, color: 'success' })
  } catch (error: any) {
    toast.add({ title: 'Chyba', description: error?.data?.detail || error?.data || 'Platbu se nepodařilo připsat.', color: 'error' })
  } finally {
    creditingPaymentId.value = null
  }
}

  async function cancelPayment(payment: PaymentRow) {
    if (payment.status !== 'Pending' || cancelingPaymentId.value) {
      return
    }
    if (!confirm(`Opravdu chcete zrušit platbu ${formatPaymentAmount(payment.amount)} Kč (VS: ${payment.variableSymbol})?`)) {
      return
    }

    cancelingPaymentId.value = payment.id
    try {
      await $fetch(`/api/admin/payments/${payment.id}/cancel`, {
        method: 'DELETE', credentials: 'include'
      })
      payments.value = payments.value.filter(p => p.id !== payment.id)
      toast.add({ title: 'Platba zrušena', description: 'Platební příkaz byl úspěšně zrušen.', color: 'neutral' })
    } catch (error: any) {
      toast.add({ title: 'Chyba', description: error?.data?.detail || error?.data || 'Platbu se nepodařilo zrušit.', color: 'error' })
    } finally {
      cancelingPaymentId.value = null
    }
  }

  async function adjustCredit(userId: string, isAdd: boolean) {
    const rawAmount = Number(creditAdjustmentAmount.value)
    if (!Number.isFinite(rawAmount) || rawAmount <= 0) {
      toast.add({ title: 'Chyba', description: 'Zadejte platnou kladnou částku.', color: 'error' })
      return
    }

    const amount = isAdd ? rawAmount : -rawAmount
    isAdjustingCredit.value = true

    try {
      const result = await $fetch<{ credit: number }>(`/api/admin/users/${userId}/adjust-credit`, {
        method: 'POST',
        body: { amount },
        credentials: 'include'
      })

      const user = users.value.find(u => u.id === userId)
      if (user) {
        user.credit = result.credit
      }

      toast.add({
        title: 'Kredit upraven',
        description: `Změna o ${amount > 0 ? '+' : ''}${amount.toLocaleString('cs-CZ')} Kč byla úspěšně provedena.`,
        color: 'success'
      })
      adjustingUserId.value = null
    } catch (error: any) {
      toast.add({
        title: 'Chyba',
        description: error?.data || 'Kredit se nepodařilo upravit.',
        color: 'error'
      })
    } finally {
      isAdjustingCredit.value = false
    }
  }

  async function deleteUser(user: UserRow) {
    if (user.role === 'Admin') {
      toast.add({ title: 'Akce zakázána', description: 'Administrátora nelze smazat.', color: 'error' })
      return
    }

    if (!confirm(`Opravdu chcete trvale smazat uživatele ${user.name}?`)) {
      return
    }

    deletingUserId.value = user.id
    try {
      await $fetch(`/api/admin/users/${user.id}`, {
        method: 'DELETE',
        credentials: 'include'
      })

      users.value = users.value.filter(u => u.id !== user.id)

      toast.add({
        title: 'Uživatel smazán',
        description: `Uživatel ${user.name} byl úspěšně odstraněn.`,
        color: 'success'
      })
    } catch (error: any) {
      toast.add({
        title: 'Chyba',
        description: error?.data || 'Uživatele se nepodařilo smazat.',
        color: 'error'
      })
    } finally {
      deletingUserId.value = null
    }
  }

async function updateRole(userId: string, selectedRole: string) {
  if (!selectedRole) return

  isUpdating.value = userId
  try {
    await $fetch(`/api/admin/users/${userId}/role`, {
      method: 'PUT',
      body: { role: selectedRole },
      credentials: 'include'
    })
    const user = users.value.find(u => u.id === userId)
    if (user) {
      user.role = selectedRole
    }
    toast.add({
      title: 'Úspěch',
      description: 'Role uživatele byla úspěšně změněna.',
      color: 'success'
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
        ?? 'Roli uživatele se nepodařilo změnit.',
      color: 'error'
    })
  } finally {
    isUpdating.value = null
  }
}
</script>

<template>
  <div>
    <AppNavbar />

    <main class="bg-muted/30">
      <div class="mx-auto min-h-[calc(100vh-5rem)] max-w-7xl px-6 py-10">
        <div class="mb-8">
          <h1 class="text-3xl font-semibold tracking-tight text-highlighted">
            Uživatelé
          </h1>
          <p class="mt-2 text-base text-muted">
            Správa klientů a rolí
          </p>
        </div>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <h2 class="text-lg font-semibold text-highlighted">
              Seznam uživatelů
            </h2>
          </template>

          <UTable v-if="!isLoading && !errorMessage"
                  :data="tableRows"
                  :columns="columns"
                  class="w-full">
            <template #role-cell="{ row }">
              <span v-if="row.original.originalRole === 'Admin'" class="font-medium text-slate-500">
                {{ row.original.roleLabel }}
              </span>

              <USelect v-else
                       :model-value="row.original.originalRole"
                       :items="roleOptions"
                       label-key="label"
                       value-key="value"
                       :loading="isUpdating === row.original.id"
                       :disabled="isUpdating === row.original.id"
                       class="w-40"
                       @update:model-value="updateRole(row.original.id, $event)" />
            </template>

            <template #actions-cell="{ row }">
              <div class="flex items-center gap-2">
                <UButton v-if="row.original.originalRole === 'User'"
                         size="xs"
                         color="neutral"
                         variant="outline"
                         :ui="{ base: 'cursor-pointer'}"
                         @click="adjustingUserId = adjustingUserId === row.original.id ? null : row.original.id">
                  Upravit kredit
                </UButton>

                <UButton v-if="row.original.originalRole !== 'Admin'"
                         size="xs"
                         color="error"
                         variant="outline"
                         :loading="deletingUserId === row.original.id"
                         :ui="{ base: 'cursor-pointer'}"
                         @click="deleteUser(row.original)">
                  Smazat
                </UButton>
              </div>

              <div v-if="adjustingUserId === row.original.id && row.original.originalRole === 'User'"
                   class="mt-2 inline-flex flex-wrap items-center gap-2 rounded-lg border border-default bg-muted/20 p-2">
                <UInput v-model.number="creditAdjustmentAmount"
                        type="number"
                        min="1"
                        size="xs"
                        class="w-24"
                        placeholder="Částka" />
                <UButton size="xs"
                         color="success"
                         variant="outline"
                         :loading="isAdjustingCredit"
                         :ui="{ base: 'cursor-pointer'}"
                         @click="adjustCredit(row.original.id, true)">
                  Přidat
                </UButton>
                <UButton size="xs"
                         color="error"
                         variant="outline"
                         :loading="isAdjustingCredit"
                         :ui="{ base: 'cursor-pointer'}"
                         @click="adjustCredit(row.original.id, false)">
                  Odebrat
                </UButton>
              </div>
            </template>
          </UTable>

          <div v-else class="px-6 py-10 text-sm text-muted">
            {{ errorMessage || 'Načítání uživatelů…' }}
          </div>
        </UCard>

        <UCard class="mt-8" :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <div class="flex items-center justify-between gap-3">
              <div>
                <h2 class="text-lg font-semibold text-highlighted">Dobití kreditu</h2>
                <p class="mt-1 text-sm text-muted">Po kontrole příchozí platby připíšete částku uživateli.</p>
              </div>
            </div>
          </template>
          <div v-if="isPaymentsLoading" class="px-6 py-10 text-sm text-muted">Načítání plateb…</div>
          <UAlert v-else-if="paymentError" color="error" :description="paymentError" class="m-6" />
          <UTable v-else :data="paymentRows" :columns="[
            { accessorKey: 'dateLabel', header: 'Datum' },
            { accessorKey: 'user.name', header: 'Uživatel' },
            { accessorKey: 'amountLabel', header: 'Částka' },
            { accessorKey: 'variableSymbol', header: 'Variabilní symbol' },
            { accessorKey: 'statusLabel', header: 'Stav' },
            { accessorKey: 'action', header: '' }
          ]" class="w-full">
            <template #user.name-cell="{ row }"><div>{{ row.original.user.name }}</div><div class="text-xs text-muted">{{ row.original.user.email }}</div></template>
            <template #statusLabel-cell="{ row }">
              <UBadge :color="row.original.status === 'Credited' ? 'success' : row.original.status === 'Cancelled' ? 'neutral' : 'warning'"
                      variant="subtle">
                {{ row.original.statusLabel }}
              </UBadge>
            </template>
            <template #action-cell="{ row }">
              <div v-if="row.original.status === 'Pending'" class="flex items-center gap-2">
                <UButton size="sm"
                         :ui="{ base: 'cursor-pointer'}"
                         :loading="creditingPaymentId === row.original.id"
                         @click="creditPayment(row.original)">
                  Připsat kredit
                </UButton>
                <UButton size="sm"
                         color="neutral"
                         variant="outline"
                         :loading="cancelingPaymentId === row.original.id"
                         :ui="{ base: 'cursor-pointer'}"
                         @click="cancelPayment(row.original)">
                  Zrušit
                </UButton>
              </div>
              
            </template>
          </UTable>
        </UCard>
      </div>
    </main>
  </div>
</template>
