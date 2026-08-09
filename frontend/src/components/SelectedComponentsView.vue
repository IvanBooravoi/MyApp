<script setup>
import { computed } from 'vue'

const props = defineProps({
  navigationCollapsed: {
    type: Boolean,
    required: true,
  },
  rows: {
    type: Array,
    required: true,
  },
})

const emit = defineEmits(['back', 'clear', 'update:rows'])

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

function isHiddenDatabaseColumn(column) {
  return hiddenDatabaseColumns.has(column.trim().toLowerCase())
}

const columns = computed(() => {
  const names = new Set()
  props.rows.forEach((row) => {
    Object.keys(row)
      .filter((column) => column !== '__quantity' && !isHiddenDatabaseColumn(column))
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

function removeRow(index) {
  emit(
    'update:rows',
    props.rows.filter((_, rowIndex) => rowIndex !== index),
  )
}

</script>

<template>
  <main class="home-page tables-page" :class="{ 'home-page--expanded': navigationCollapsed }">
    <header class="home-header">
      <div>
        <p class="eyebrow">V_MEH_OST</p>
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

      <div class="data-table-scroll">
        <table v-if="rows.length">
          <thead>
            <tr>
              <th v-for="column in columns" :key="column">{{ column }}</th>
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
