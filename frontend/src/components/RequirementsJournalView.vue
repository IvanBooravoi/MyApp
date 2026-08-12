<script setup>
import { computed, onMounted, ref } from 'vue'
import { renderPdfDocument } from '../utils/pdfPreview'

const props = defineProps({
  navigationCollapsed: {
    type: Boolean,
    required: true,
  },
  token: {
    type: String,
    required: true,
  },
})

const requirements = ref([])
const isLoading = ref(false)
const errorMessage = ref('')
const activePdfId = ref(null)
const deletingId = ref(null)
const dateFilter = ref('')
const itemNameFilter = ref('')
const recipientFilter = ref('')

const dateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'short',
  timeStyle: 'medium',
})
const quantityFormatter = new Intl.NumberFormat('ru-RU', {
  maximumFractionDigits: 3,
})

const filteredRequirements = computed(() => {
  const itemName = itemNameFilter.value.trim().toLocaleLowerCase('ru-RU')
  const recipient = recipientFilter.value.trim().toLocaleLowerCase('ru-RU')

  return requirements.value.filter((requirement) => {
    if (
      dateFilter.value &&
      formatDateInputValue(requirement.createdAt) !== dateFilter.value
    ) {
      return false
    }

    if (
      recipient &&
      !requirement.authorName.toLocaleLowerCase('ru-RU').includes(recipient)
    ) {
      return false
    }

    return (
      !itemName ||
      requirement.items.some((item) =>
        item.name.toLocaleLowerCase('ru-RU').includes(itemName),
      )
    )
  })
})

function formatDateTime(value) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : dateTimeFormatter.format(date)
}

function formatQuantity(value) {
  const quantity = Number(value)
  return Number.isFinite(quantity) ? quantityFormatter.format(quantity) : value
}

function formatDateInputValue(value) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function clearFilters() {
  dateFilter.value = ''
  itemNameFilter.value = ''
  recipientFilter.value = ''
}

async function loadRequirements() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const response = await fetch('/api/requirements', {
      headers: {
        Authorization: `Bearer ${props.token}`,
      },
    })
    if (!response.ok) {
      throw new Error(
        response.status === 401
          ? 'Сессия истекла. Выполните вход повторно.'
          : 'Не удалось загрузить выписанные требования.',
      )
    }
    requirements.value = await response.json()
  } catch (error) {
    requirements.value = []
    errorMessage.value =
      error instanceof Error
        ? error.message
        : 'Не удалось загрузить выписанные требования.'
  } finally {
    isLoading.value = false
  }
}

async function openPdf(requirement) {
  errorMessage.value = ''
  const previewWindow = window.open('', '_blank')
  if (!previewWindow) {
    errorMessage.value = 'Разрешите открытие всплывающих окон для просмотра PDF.'
    return
  }

  previewWindow.document.body.textContent = 'Формирование PDF...'
  activePdfId.value = requirement.id
  try {
    const response = await fetch(`/api/requirements/${requirement.id}/pdf`, {
      headers: {
        Authorization: `Bearer ${props.token}`,
      },
    })
    if (!response.ok) {
      throw new Error('Не удалось сформировать PDF.')
    }
    renderPdfDocument(previewWindow, await response.json())
  } catch (error) {
    previewWindow.close()
    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось сформировать PDF.'
  } finally {
    activePdfId.value = null
  }
}

async function deleteRequirement(requirement) {
  if (!window.confirm(`Удалить требование для техники «${requirement.vehicleNumber}»?`)) {
    return
  }

  errorMessage.value = ''
  deletingId.value = requirement.id
  try {
    const response = await fetch(`/api/requirements/${requirement.id}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${props.token}`,
      },
    })
    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      throw new Error(
        problem?.detail ??
          problem?.title ??
          'Не удалось удалить требование.',
      )
    }
    requirements.value = requirements.value.filter(
      (item) => item.id !== requirement.id,
    )
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : 'Не удалось удалить требование.'
  } finally {
    deletingId.value = null
  }
}

onMounted(loadRequirements)
</script>

<template>
  <main class="home-page tables-page" :class="{ 'home-page--expanded': navigationCollapsed }">
    <header class="home-header">
      <div>
        <p class="eyebrow">ЖУРНАЛ</p>
        <h1>Выписанные требования</h1>
      </div>
      <div class="user-avatar" aria-label="Профиль пользователя">П</div>
    </header>

    <section class="data-table-panel macos-glass-panel">
      <div class="data-table-header">
        <div>
          <p class="eyebrow">ИСТОРИЯ СОЗДАНИЯ</p>
          <h2>Требования</h2>
        </div>
        <div class="requirements-journal-actions">
          <span class="record-count">
            {{ filteredRequirements.length }} из {{ requirements.length }}
          </span>
          <button
            class="secondary-button"
            type="button"
            :disabled="isLoading"
            @click="loadRequirements"
          >
            Обновить
          </button>
        </div>
      </div>

      <div class="requirements-journal-filters">
        <label>
          <span>Дата создания</span>
          <input v-model="dateFilter" type="date" />
        </label>
        <label>
          <span>Наименование</span>
          <input
            v-model="itemNameFilter"
            type="search"
            placeholder="Поиск по наименованию"
          />
        </label>
        <label>
          <span>Кто получил</span>
          <input
            v-model="recipientFilter"
            type="search"
            placeholder="ФИО получателя"
          />
        </label>
        <button
          class="secondary-button"
          type="button"
          :disabled="!dateFilter && !itemNameFilter && !recipientFilter"
          @click="clearFilters"
        >
          Сбросить
        </button>
      </div>

      <p v-if="errorMessage" class="table-message table-message--error" role="alert">
        {{ errorMessage }}
      </p>
      <p v-else-if="isLoading" class="table-message">Загрузка требований...</p>

      <div v-else class="data-table-scroll requirements-journal-table">
        <table>
          <thead>
            <tr>
              <th>Дата и время создания</th>
              <th>Кто создал</th>
              <th>Кто выдал</th>
              <th>Номер техники</th>
              <th>Наименования, единицы и количество</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="requirement in filteredRequirements" :key="requirement.id">
              <td class="requirements-date">{{ formatDateTime(requirement.createdAt) }}</td>
              <td>{{ requirement.authorName }}</td>
              <td>{{ requirement.issuerName }}</td>
              <td>{{ requirement.vehicleNumber }}</td>
              <td>
                <div class="requirement-items">
                  <div
                    v-for="(item, index) in requirement.items"
                    :key="`${requirement.id}-${index}`"
                    class="requirement-item"
                  >
                    <span class="requirement-item-name">{{ item.name }}</span>
                    <span class="requirement-item-value">
                      {{ item.unit }} — {{ formatQuantity(item.quantity) }}
                    </span>
                  </div>
                </div>
              </td>
              <td>
                <div class="requirements-row-actions">
                  <button
                    class="secondary-button"
                    type="button"
                    :disabled="activePdfId === requirement.id"
                    @click="openPdf(requirement)"
                  >
                    {{
                      activePdfId === requirement.id
                        ? 'Формирование...'
                        : 'Сформировать PDF'
                    }}
                  </button>
                  <button
                    class="table-action-button table-action-button--danger"
                    type="button"
                    :disabled="deletingId === requirement.id"
                    @click="deleteRequirement(requirement)"
                  >
                    {{ deletingId === requirement.id ? 'Удаление...' : 'Удалить' }}
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="filteredRequirements.length === 0">
              <td class="empty-table" colspan="6">
                {{
                  requirements.length === 0
                    ? 'Выписанных требований пока нет'
                    : 'По заданным условиям ничего не найдено'
                }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </main>
</template>
