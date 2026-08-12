<script setup>
import { computed, ref } from 'vue'
import { renderPdfDocument } from '../utils/pdfPreview'

const props = defineProps({
  navigationCollapsed: {
    type: Boolean,
    required: true,
  },
  rows: {
    type: Array,
    required: true,
  },
  token: {
    type: String,
    required: true,
  },
  responsibleEmployee: {
    type: Object,
    default: null,
  },
})

const emit = defineEmits(['back', 'clear', 'update:rows'])
const vehicleNumber = ref('')
const documentDate = ref(formatLocalDate(new Date()))
const documentError = ref('')
const isGeneratingDocument = ref(false)

const databaseQuantityColumns = new Set([
  'amount',
  'count',
  'qty',
  'quantity',
  'количество',
  'кол-во',
  'колво',
])

const hiddenDatabaseColumns = new Set([
  ...databaseQuantityColumns,
  'cost',
  'price',
  'стоимость',
  'цена',
])

const columnLabels = {
  name: 'Наименование',
  unit: 'Ед. изм.',
}

function isHiddenDatabaseColumn(column) {
  return hiddenDatabaseColumns.has(column.trim().toLowerCase())
}

const columns = computed(() => {
  const names = new Set()
  props.rows.forEach((row) => {
    Object.keys(row)
      .filter((column) => !column.startsWith('__') && !isHiddenDatabaseColumn(column))
      .forEach((column) => names.add(column))
  })
  return [...names]
})

function formatCell(value) {
  if (value === null || value === undefined || value === '') {
    return '—'
  }

  return typeof value === 'object' ? JSON.stringify(value) : String(value)
}

function getRowValue(row, aliases) {
  const normalizedAliases = new Set(aliases.map((alias) => alias.toLowerCase()))
  const key = Object.keys(row).find((column) =>
    normalizedAliases.has(column.toLowerCase()),
  )
  return key ? row[key] : ''
}

function formatLocalDate(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function removeRow(index) {
  emit(
    'update:rows',
    props.rows.filter((_, rowIndex) => rowIndex !== index),
  )
}

async function generateDocument() {
  documentError.value = ''
  if (!vehicleNumber.value.trim()) {
    documentError.value = 'Укажите номер техники.'
    return
  }

  if (!props.responsibleEmployee) {
    documentError.value = 'Выберите ответственное лицо за выдачу.'
    return
  }

  const sourceTable = props.rows[0]?.__sourceTable
  if (
    !sourceTable ||
    props.rows.some((row) => row.__sourceTable !== sourceTable) ||
    props.responsibleEmployee.sourceTable !== sourceTable
  ) {
    documentError.value =
      'Ответственное лицо не соответствует источнику выбранных компонентов.'
    return
  }

  if (props.rows.length > 100) {
    documentError.value = 'Можно сформировать не более 100 компонентов за один раз.'
    return
  }

  const previewWindow = window.open('', '_blank')
  if (!previewWindow) {
    documentError.value = 'Разрешите открытие всплывающих окон для просмотра PDF.'
    return
  }

  previewWindow.document.body.textContent = 'Формирование PDF...'
  isGeneratingDocument.value = true
  try {
    const response = await fetch('/api/documents/components', {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${props.token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        date: documentDate.value,
        vehicleNumber: vehicleNumber.value.trim(),
        sourceTable,
        responsibleEmployee: {
          firstName: props.responsibleEmployee.firstName,
          patronymic: props.responsibleEmployee.patronymic,
          lastName: props.responsibleEmployee.lastName,
        },
        items: props.rows.map((row) => ({
          name: String(getRowValue(row, ['name', 'Наименование']) ?? ''),
          unit: String(getRowValue(row, ['unit', 'Ед.изм.', 'Ед. изм.']) ?? ''),
          quantity: Number(row.__quantity ?? 1),
          availableQuantity: Number(
            row.__availableQuantity ??
              getRowValue(row, ['amount', 'Количество']) ??
              0,
          ),
        })),
      }),
    })

    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      const validationMessage = problem?.errors
        ? Object.values(problem.errors).flat()[0]
        : null
      throw new Error(
        validationMessage ??
          problem?.detail ??
          problem?.title ??
          'Не удалось сформировать PDF.',
      )
    }

    const result = await response.json()
    if (result.documents.length !== 1) {
      throw new Error('Сервер вернул некорректное количество PDF-файлов.')
    }
    renderPdfDocument(previewWindow, result.documents[0])
  } catch (error) {
    previewWindow.close()
    documentError.value =
      error instanceof Error ? error.message : 'Не удалось сформировать PDF.'
  } finally {
    isGeneratingDocument.value = false
  }
}

</script>

<template>
  <main class="home-page tables-page" :class="{ 'home-page--expanded': navigationCollapsed }">
    <header class="home-header">
      <div>
        <p class="eyebrow">FULL_OST / MEH_OST</p>
        <h1>Выбранные компоненты</h1>
      </div>
      <div class="user-avatar" aria-label="Профиль пользователя">П</div>
    </header>

    <section class="data-table-panel macos-glass-panel">
      <div class="data-table-header selected-components-header">
        <div>
          <p class="eyebrow">РЕЗУЛЬТАТ ВЫБОРА</p>
          <h2>Компоненты</h2>
        </div>
        <span class="record-count">{{ rows.length }} выбрано</span>
      </div>

      <div class="selected-components-actions">
        <button class="secondary-button" type="button" @click="$emit('back')">
          ← Вернуться к выбору
        </button>
        <button
          class="table-action-button table-action-button--danger"
          type="button"
          :disabled="rows.length === 0"
          @click="$emit('clear')"
        >
          Очистить список
        </button>
      </div>

      <form class="document-form" @submit.prevent="generateDocument">
        <label>
          <span>Дата</span>
          <input v-model="documentDate" type="date" required />
        </label>
        <label>
          <span>Номер техники</span>
          <input
            v-model="vehicleNumber"
            type="text"
            maxlength="100"
            placeholder="Введите номер техники"
            required
          />
        </label>
        <button
          class="primary-button"
          type="submit"
          :disabled="rows.length === 0 || isGeneratingDocument"
        >
          {{ isGeneratingDocument ? 'Формирование...' : 'Просмотреть PDF' }}
        </button>
      </form>
      <p v-if="documentError" class="form-error" role="alert">{{ documentError }}</p>

      <div class="data-table-scroll">
        <table v-if="rows.length">
          <thead>
            <tr>
              <th v-for="column in columns" :key="column">
                {{ columnLabels[column] ?? column }}
              </th>
              <th class="quantity-column">Количество</th>
              <th class="selection-remove-column">Действие</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, rowIndex) in rows" :key="rowIndex">
              <td v-for="column in columns" :key="column">
                {{ formatCell(row[column]) }}
              </td>
              <td class="quantity-column">{{ formatCell(row.__quantity ?? 1) }}</td>
              <td class="selection-remove-column">
                <button
                  class="table-action-button table-action-button--danger"
                  type="button"
                  @click="removeRow(rowIndex)"
                >
                  Удалить
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-else class="table-message">
          Компоненты ещё не выбраны. Вернитесь в таблицу и отметьте нужные строки.
        </p>
      </div>
    </section>
  </main>
</template>
