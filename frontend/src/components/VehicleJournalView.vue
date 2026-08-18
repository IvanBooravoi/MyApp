<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import UserAvatar from './UserAvatar.vue'

const props = defineProps({
  navigationCollapsed: { type: Boolean, required: true },
  token: { type: String, required: true },
})

const tabs = [
  { id: 'defects', label: 'Неисправности' },
  { id: 'works', label: 'Ремонт' },
  { id: 'purchases', label: 'Заявки на приобретение' },
  { id: 'hours', label: 'Моточасы' },
]
const today = new Date().toISOString().slice(0, 10)
const vehicles = ref([])
const selectedVehicleId = ref('')
const journal = ref(null)
const activeTab = ref('defects')
const vehicleQuery = ref('')
const vehicleSearchMatches = ref([])
const fromDate = ref('')
const toDate = ref('')
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const pendingWorkPhotos = ref([])
const pendingDefectPhotos = ref([])
const photoViewerUrl = ref('')
const photoViewerName = ref('')
const hoursImportResult = ref(null)

const forms = reactive({
  purchases: {
    requestDate: today,
    requestNumber: '',
    itemName: '',
    quantity: 1,
    status: 'Создана',
    note: '',
  },
  defects: {
    nodeName: '',
    failureReason: '',
  },
  hours: {
    readingDate: today,
    engineHours: '',
    note: '',
  },
  works: {
    defectId: '',
    description: '',
    purchaseRequestNumber: '',
  },
})

const vehicleOptions = computed(() =>
  vehicles.value.flatMap((vehicle) => [
    ...(vehicle.garageNumber === null
      ? []
      : [{ value: String(vehicle.garageNumber), vehicle }]),
    ...(vehicle.stateNumber
      ? [{ value: vehicle.stateNumber, vehicle }]
      : []),
  ]),
)
const currentEntries = computed(() => journal.value?.[activeTab.value] ?? [])
const selectedVehicle = computed(() =>
  vehicles.value.find((vehicle) => vehicle.id === selectedVehicleId.value),
)
const latestHours = computed(() => {
  const values = journal.value?.hours ?? []
  return values.length ? values[0].engineHours : null
})
const defectCount = computed(() => journal.value?.defects?.length ?? 0)

onMounted(loadVehicles)
onBeforeUnmount(closePhoto)

function authHeaders(json = false) {
  return {
    Authorization: `Bearer ${props.token}`,
    ...(json ? { 'Content-Type': 'application/json' } : {}),
  }
}

async function loadVehicles() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const response = await fetch('/api/vehicles', {
      headers: authHeaders(),
    })
    if (!response.ok) throw new Error('Не удалось загрузить список техники.')
    vehicles.value = await response.json()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isLoading.value = false
  }
}

function selectVehicle() {
  const query = normalizeVehicleNumber(vehicleQuery.value)
  if (!query) {
    errorMessage.value = 'Введите гаражный или государственный номер.'
    return
  }
  const exactMatches = vehicles.value.filter((vehicle) =>
    normalizeVehicleNumber(vehicle.garageNumber) === query ||
    normalizeVehicleNumber(vehicle.stateNumber) === query,
  )
  const matches = exactMatches.length
    ? exactMatches
    : vehicles.value.filter((vehicle) =>
        normalizeVehicleNumber(vehicle.garageNumber).includes(query) ||
        normalizeVehicleNumber(vehicle.stateNumber).includes(query),
      )
  if (matches.length !== 1) {
    selectedVehicleId.value = ''
    journal.value = null
    vehicleSearchMatches.value = matches
    errorMessage.value = matches.length
      ? ''
      : 'Техника с таким номером не найдена.'
    return
  }
  chooseVehicle(matches[0])
}

async function chooseVehicle(vehicle) {
  vehicleSearchMatches.value = []
  errorMessage.value = ''
  vehicleQuery.value = vehicle.stateNumber ||
    String(vehicle.garageNumber ?? '')
  selectedVehicleId.value = vehicle.id
  await loadJournal()
}

function normalizeVehicleNumber(value) {
  const lookalikes = {
    А: 'A', В: 'B', Е: 'E', К: 'K', М: 'M', Н: 'H',
    О: 'O', Р: 'P', С: 'C', Т: 'T', У: 'Y', Х: 'X',
  }
  return String(value ?? '')
    .toLocaleUpperCase('ru-RU')
    .replace(/[АВЕКМНОРСТУХ]/g, (letter) => lookalikes[letter])
    .replace(/[^A-Z0-9]/g, '')
}

async function loadJournal() {
  if (!selectedVehicleId.value) {
    journal.value = null
    return
  }
  isLoading.value = true
  errorMessage.value = ''
  try {
    const query = new URLSearchParams()
    if (fromDate.value) query.set('from', fromDate.value)
    if (toDate.value) query.set('to', toDate.value)
    const suffix = query.size ? `?${query}` : ''
    const response = await fetch(
      `/api/vehicles/${selectedVehicleId.value}/journal${suffix}`,
      { headers: authHeaders() },
    )
    if (!response.ok) throw new Error('Не удалось загрузить журнал техники.')
    journal.value = await response.json()
  } catch (error) {
    journal.value = null
    errorMessage.value = error.message
  } finally {
    isLoading.value = false
  }
}

async function addEntry() {
  if (!selectedVehicleId.value) return
  isSaving.value = true
  errorMessage.value = ''
  try {
    const payload = { ...forms[activeTab.value] }
    const response = await fetch(
      `/api/vehicles/${selectedVehicleId.value}/${activeTab.value}`,
      {
        method: 'POST',
        headers: authHeaders(true),
        body: JSON.stringify(payload),
      },
    )
    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      const validation = problem?.errors
        ? Object.values(problem.errors).flat()[0]
        : null
      throw new Error(validation ?? problem?.detail ?? 'Не удалось сохранить запись.')
    }
    const created = await response.json()
    if (activeTab.value === 'defects' && pendingDefectPhotos.value.length) {
      try {
        await uploadEntryPhotos('defects', created.id, pendingDefectPhotos.value)
      } catch (error) {
        resetForm(activeTab.value)
        await loadJournal()
        throw new Error(`Дефект сохранён. ${error.message}`)
      }
    }

    async function importHours(event) {
      const file = event.target.files[0]
      event.target.value = ''
      if (!file) return
      isSaving.value = true
      errorMessage.value = ''
      hoursImportResult.value = null
      try {
        const data = new FormData()
        data.append('file', file)
        const response = await fetch('/api/vehicles/hours/import', {
          method: 'POST',
          headers: authHeaders(),
          body: data,
        })
        const result = await response.json().catch(() => null)
        if (!response.ok) {
          const validation = result?.errors
            ? Object.values(result.errors).flat()[0]
            : null
          throw new Error(
            validation ?? result?.detail ?? 'Не удалось импортировать моточасы.',
          )
        }
        hoursImportResult.value = result
        await loadJournal()
      } catch (error) {
        errorMessage.value = error.message
      } finally {
        isSaving.value = false
      }
    }
    if (activeTab.value === 'works' && pendingWorkPhotos.value.length) {
      try {
        await uploadEntryPhotos('works', created.id, pendingWorkPhotos.value)
      } catch (error) {
        resetForm(activeTab.value)
        await loadJournal()
        throw new Error(`Работа сохранена. ${error.message}`)
      }
    }
    resetForm(activeTab.value)
    await loadJournal()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isSaving.value = false
  }
}

function selectWorkPhotos(event) {
  const files = [...event.target.files]
  const validation = validatePhotoFiles(files, 0)
  if (validation) {
    event.target.value = ''
    pendingWorkPhotos.value = []
    errorMessage.value = validation
    return
  }
  errorMessage.value = ''
  pendingWorkPhotos.value = files
}

function selectDefectPhotos(event) {
  const files = [...event.target.files]
  const validation = validatePhotoFiles(files, 0)
  if (validation) {
    event.target.value = ''
    pendingDefectPhotos.value = []
    errorMessage.value = validation
    return
  }
  errorMessage.value = ''
  pendingDefectPhotos.value = files
}

async function uploadEntryPhotos(category, entryId, files) {
  for (const file of files) {
    const data = new FormData()
    data.append('file', file)
    const response = await fetch(`/api/vehicles/${category}/${entryId}/photos`, {
      method: 'POST',
      headers: authHeaders(),
      body: data,
    })
    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      const validation = problem?.errors
        ? Object.values(problem.errors).flat()[0]
        : null
      throw new Error(
        validation ?? problem?.detail ?? `Не удалось загрузить «${file.name}».`,
      )
    }
  }
}

async function addPhotosToWork(workId, event) {
  const files = [...event.target.files]
  event.target.value = ''
  if (!files.length) return
  const work = journal.value?.works.find((item) => item.id === workId)
  const validation = validatePhotoFiles(files, work?.photos.length ?? 0)
  if (validation) {
    errorMessage.value = validation
    return
  }
  isSaving.value = true
  errorMessage.value = ''
  try {
    await uploadEntryPhotos('works', workId, files)
    await loadJournal()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isSaving.value = false
  }

}

async function addPhotosToDefect(defectId, event) {
  const files = [...event.target.files]
  event.target.value = ''
  if (!files.length) return
  const defect = journal.value?.defects.find((item) => item.id === defectId)
  const validation = validatePhotoFiles(files, defect?.photos.length ?? 0)
  if (validation) {
    errorMessage.value = validation
    return
  }
  isSaving.value = true
  errorMessage.value = ''
  try {
    await uploadEntryPhotos('defects', defectId, files)
    await loadJournal()
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    isSaving.value = false
  }
}

function validatePhotoFiles(files, existingCount) {
  if (existingCount + files.length > 10) {
    return 'Для одной записи можно сохранить не более 10 фотографий.'
  }
  const allowedTypes = ['image/jpeg', 'image/png', 'image/webp']
  if (files.some((file) => !allowedTypes.includes(file.type) || file.size > 8 * 1024 * 1024)) {
    return 'Разрешены JPEG, PNG и WebP размером не более 8 МБ.'
  }
  return ''
}

async function openPhoto(photo, category = 'work') {
  closePhoto()
  const response = await fetch(`/api/vehicles/${category}-photos/${photo.id}`, {
    headers: authHeaders(),
  })
  if (!response.ok) {
    errorMessage.value = 'Не удалось загрузить фотографию.'
    return
  }
  photoViewerUrl.value = URL.createObjectURL(await response.blob())
  photoViewerName.value = photo.fileName
}

function closePhoto() {
  if (photoViewerUrl.value) URL.revokeObjectURL(photoViewerUrl.value)
  photoViewerUrl.value = ''
  photoViewerName.value = ''
}

async function deletePhoto(photoId, category = 'work') {
  if (!window.confirm('Удалить фотографию?')) return
  const response = await fetch(`/api/vehicles/${category}-photos/${photoId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!response.ok) {
    errorMessage.value = 'Не удалось удалить фотографию.'
    return
  }
  closePhoto()
  await loadJournal()
}

async function deleteEntry(id) {
  if (!window.confirm('Удалить выбранную запись?')) return
  const response = await fetch(`/api/vehicles/${activeTab.value}/${id}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!response.ok) {
    errorMessage.value = 'Не удалось удалить запись.'
    return
  }
  await loadJournal()
}

function resetForm(category) {
  if (category === 'purchases') {
    Object.assign(forms.purchases, {
      requestDate: today,
      requestNumber: '',
      itemName: '',
      quantity: 1,
      status: 'Создана',
      note: '',
    })
  } else if (category === 'defects') {
    Object.assign(forms.defects, {
      nodeName: '',
      failureReason: '',
    })
    pendingDefectPhotos.value = []
  } else if (category === 'hours') {
    Object.assign(forms.hours, {
      readingDate: today,
      engineHours: '',
      note: '',
    })
  } else {
    Object.assign(forms.works, {
      defectId: '',
      description: '',
      purchaseRequestNumber: '',
    })
    pendingWorkPhotos.value = []
  }
}

function formatDate(value) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('ru-RU').format(
    new Date(`${value}T00:00:00`),
  )
}

function formatNumber(value) {
  return Number(value).toLocaleString('ru-RU', { maximumFractionDigits: 2 })
}

function printReport() {
  window.print()
}
</script>

<template>
  <main
    class="home-page vehicle-page"
    :class="{ 'home-page--expanded': navigationCollapsed }"
  >
    <header class="home-header vehicle-screen-only">
      <div>
        <p class="eyebrow">УЧЁТ И ИСТОРИЯ</p>
        <h1>Техника</h1>
      </div>
      <UserAvatar :token="token" />
    </header>

    <section class="vehicle-layout">
      <div class="vehicle-content">
        <form
          class="vehicle-search macos-glass-panel vehicle-screen-only"
          @submit.prevent="selectVehicle"
        >
          <label class="vehicle-field">
            <span>Гаражный или государственный номер</span>
            <input
              v-model="vehicleQuery"
              list="vehicle-number-options"
              placeholder="Например: 901 или 5113 CC 65"
              type="search"
            />
            <datalist id="vehicle-number-options">
              <option
                v-for="option in vehicleOptions"
                :key="`${option.vehicle.id}-${option.value}`"
                :value="option.value"
              >
                {{ option.vehicle.modelName }}
              </option>
            </datalist>
          </label>
          <button class="primary-button" type="submit">Найти технику</button>
          <div
            v-if="vehicleSearchMatches.length"
            class="vehicle-search-results"
          >
            <span>Найдено несколько машин. Выберите нужную:</span>
            <button
              v-for="vehicle in vehicleSearchMatches"
              :key="vehicle.id"
              type="button"
              @click="chooseVehicle(vehicle)"
            >
              <strong>{{ vehicle.modelName }}</strong>
              <span>
                Гар. № {{ vehicle.garageNumber ?? '—' }} ·
                {{ vehicle.stateNumber || 'без гос. номера' }}
              </span>
            </button>
          </div>
        </form>

        <p v-if="errorMessage" class="table-message table-message--error">
          {{ errorMessage }}
        </p>
        <p v-else-if="isLoading && !journal" class="table-message">
          Загрузка...
        </p>

        <template v-if="selectedVehicle && journal">
          <section class="vehicle-card macos-glass-panel">
            <div>
              <p class="eyebrow">{{ selectedVehicle.groupName }}</p>
              <h2>{{ selectedVehicle.modelName }}</h2>
              <p>
                {{ selectedVehicle.typeName }} · Гар. №
                {{ selectedVehicle.garageNumber ?? '—' }}
              </p>
            </div>
            <div class="vehicle-identifiers">
              <span>Гос. номер <strong>{{ selectedVehicle.stateNumber || '—' }}</strong></span>
              <span>VIN <strong>{{ selectedVehicle.vin || '—' }}</strong></span>
            </div>
          </section>

          <section class="vehicle-summary">
            <article class="macos-glass-panel">
              <span>Последние моточасы</span>
              <strong>{{ latestHours === null ? '—' : formatNumber(latestHours) }}</strong>
            </article>
            <article class="macos-glass-panel">
              <span>Неисправности за период</span>
              <strong>{{ defectCount }}</strong>
            </article>
            <article class="macos-glass-panel">
              <span>Заявки за период</span>
              <strong>{{ journal.purchases.length }}</strong>
            </article>
            <article class="macos-glass-panel">
              <span>Ремонты за период</span>
              <strong>{{ journal.works.length }}</strong>
            </article>
          </section>

          <section class="vehicle-controls macos-glass-panel vehicle-screen-only">
            <div class="vehicle-period">
              <label class="vehicle-field">
                <span>Период с</span>
                <input v-model="fromDate" type="date" />
              </label>
              <label class="vehicle-field">
                <span>по</span>
                <input v-model="toDate" type="date" />
              </label>
              <button class="secondary-button" type="button" @click="loadJournal">
                Применить
              </button>
              <button class="secondary-button" type="button" @click="printReport">
                Создать отчёт
              </button>
            </div>

            <div class="vehicle-tabs">
              <button
                v-for="tab in tabs"
                :key="tab.id"
                type="button"
                :class="{ 'vehicle-tab--active': activeTab === tab.id }"
                @click="activeTab = tab.id"
              >
                {{ tab.label }}
              </button>
            </div>

            <form class="vehicle-entry-form" @submit.prevent="addEntry">
              <template v-if="activeTab === 'purchases'">
                <label class="vehicle-field">
                  <span>Дата</span>
                  <input v-model="forms.purchases.requestDate" required type="date" />
                </label>
                <label class="vehicle-field">
                  <span>Номер заявки</span>
                  <input v-model="forms.purchases.requestNumber" maxlength="100" />
                </label>
                <label class="vehicle-field vehicle-field--wide">
                  <span>Что приобрести</span>
                  <input v-model="forms.purchases.itemName" required maxlength="500" />
                </label>
                <label class="vehicle-field">
                  <span>Количество</span>
                  <input v-model.number="forms.purchases.quantity" required min="0.01" step="0.01" type="number" />
                </label>
                <label class="vehicle-field">
                  <span>Статус</span>
                  <select v-model="forms.purchases.status">
                    <option>Создана</option>
                    <option>Согласована</option>
                    <option>Заказано</option>
                    <option>Получено</option>
                    <option>Отменена</option>
                  </select>
                </label>
                <label class="vehicle-field vehicle-field--wide">
                  <span>Примечание</span>
                  <input v-model="forms.purchases.note" />
                </label>
              </template>

              <template v-else-if="activeTab === 'defects'">
                <label class="vehicle-field vehicle-field--full">
                  <span>Узел</span>
                  <textarea v-model="forms.defects.nodeName" required rows="2"></textarea>
                </label>
                <label class="vehicle-field vehicle-field--full">
                  <span>Причина неисправности</span>
                  <textarea v-model="forms.defects.failureReason" required rows="3"></textarea>
                </label>
                <label class="vehicle-field vehicle-field--full">
                  <span>Фото (до 10 шт., JPEG, PNG, WebP до 8 МБ)</span>
                  <input
                    accept="image/jpeg,image/png,image/webp"
                    multiple
                    type="file"
                    @change="selectDefectPhotos"
                  />
                  <small v-if="pendingDefectPhotos.length">
                    Выбрано: {{ pendingDefectPhotos.length }}
                  </small>
                </label>
              </template>

              <template v-else-if="activeTab === 'hours'">
                <label class="vehicle-field">
                  <span>Дата показания</span>
                  <input v-model="forms.hours.readingDate" required type="date" />
                </label>
                <label class="vehicle-field">
                  <span>Наработка, м/ч</span>
                  <input v-model.number="forms.hours.engineHours" required min="0" step="0.1" type="number" />
                </label>
                <label class="vehicle-field vehicle-field--wide">
                  <span>Примечание</span>
                  <input v-model="forms.hours.note" />
                </label>
                <div class="vehicle-hours-import vehicle-field--full">
                  <div>
                    <strong>Импорт из CSV</strong>
                    <span>
                      Формат UTF-8:
                      <code>garage_number;model;engine_hours</code>.
                      Дата — сегодня, существующее показание заменяется.
                    </span>
                  </div>
                  <label class="secondary-button">
                    Загрузить CSV
                    <input
                      accept=".csv,text/csv"
                      type="file"
                      @change="importHours"
                    />
                  </label>
                </div>
                <div
                  v-if="hoursImportResult"
                  class="vehicle-import-result vehicle-field--full"
                  :class="{ 'vehicle-import-result--warning': hoursImportResult.errors.length }"
                >
                  <strong>Импортировано: {{ hoursImportResult.imported }}</strong>
                  <ul v-if="hoursImportResult.errors.length">
                    <li v-for="error in hoursImportResult.errors" :key="`${error.line}-${error.message}`">
                      Строка {{ error.line }}: {{ error.message }}
                    </li>
                  </ul>
                </div>
              </template>

              <template v-else>
                <label class="vehicle-field vehicle-field--full">
                  <span>Неисправность</span>
                  <select v-model="forms.works.defectId" required>
                    <option value="" disabled>Выберите неисправность</option>
                    <option
                      v-for="defect in journal.defects"
                      :key="defect.id"
                      :value="defect.id"
                    >
                      {{ defect.nodeName }} — {{ defect.failureReason }}
                    </option>
                  </select>
                </label>
                <label class="vehicle-field vehicle-field--full">
                  <span>Выполненные работы</span>
                  <textarea v-model="forms.works.description" required rows="3"></textarea>
                </label>
                <label class="vehicle-field vehicle-field--full">
                  <span>Номер заявки на приобретение (если необходима)</span>
                  <input v-model="forms.works.purchaseRequestNumber" maxlength="100" />
                </label>
                <label class="vehicle-field vehicle-field--full">
                  <span>Фотографии (JPEG, PNG, WebP до 8 МБ)</span>
                  <input
                    accept="image/jpeg,image/png,image/webp"
                    multiple
                    type="file"
                    @change="selectWorkPhotos"
                  />
                  <small v-if="pendingWorkPhotos.length">
                    Выбрано: {{ pendingWorkPhotos.length }}
                  </small>
                </label>
              </template>

              <button class="primary-button vehicle-save-button" type="submit" :disabled="isSaving">
                {{ isSaving ? 'Сохранение...' : 'Добавить запись' }}
              </button>
            </form>
          </section>

          <section class="vehicle-journal macos-glass-panel vehicle-screen-journal">
            <div class="vehicle-journal-header">
              <div>
                <p class="eyebrow">ЖУРНАЛ</p>
                <h2>{{ tabs.find((tab) => tab.id === activeTab)?.label }}</h2>
              </div>
              <span>{{ currentEntries.length }} записей</span>
            </div>

            <div class="data-table-scroll vehicle-table">
              <table v-if="activeTab === 'purchases'">
                <thead><tr><th>Дата</th><th>№ заявки</th><th>Наименование</th><th>Кол-во</th><th>Статус</th><th>Примечание</th><th class="vehicle-screen-only"></th></tr></thead>
                <tbody>
                  <tr v-for="item in currentEntries" :key="item.id">
                    <td>{{ formatDate(item.requestDate) }}</td><td>{{ item.requestNumber || '—' }}</td>
                    <td>{{ item.itemName }}</td><td>{{ formatNumber(item.quantity) }}</td>
                    <td>{{ item.status || '—' }}</td><td>{{ item.note || '—' }}</td>
                    <td class="vehicle-screen-only"><button class="table-action-button table-action-button--danger" @click="deleteEntry(item.id)">Удалить</button></td>
                  </tr>
                </tbody>
              </table>
              <table v-else-if="activeTab === 'defects'" class="vehicle-defects-table">
                <thead><tr><th>Узел</th><th>Причина неисправности</th><th>Фото</th><th class="vehicle-screen-only"></th></tr></thead>
                <tbody>
                  <tr v-for="item in currentEntries" :key="item.id">
                    <td>{{ item.nodeName }}</td>
                    <td>{{ item.failureReason }}</td>
                    <td>
                      <div class="vehicle-photo-actions">
                        <span v-if="!item.photos.length">Нет</span>
                        <span v-for="photo in item.photos" :key="photo.id" class="vehicle-photo-chip">
                          <button type="button" @click="openPhoto(photo, 'defect')">
                            {{ photo.fileName }}
                          </button>
                          <button
                            class="vehicle-photo-delete"
                            type="button"
                            aria-label="Удалить фотографию"
                            @click="deletePhoto(photo.id, 'defect')"
                          >
                            ×
                          </button>
                        </span>
                        <label class="vehicle-photo-upload">
                          + Добавить
                          <input
                            accept="image/jpeg,image/png,image/webp"
                            multiple
                            type="file"
                            @change="addPhotosToDefect(item.id, $event)"
                          />
                        </label>
                      </div>
                    </td>
                    <td class="vehicle-screen-only"><button class="table-action-button table-action-button--danger" @click="deleteEntry(item.id)">Удалить</button></td>
                  </tr>
                </tbody>
              </table>
              <table v-else-if="activeTab === 'hours'">
                <thead><tr><th>Дата</th><th>Моточасы</th><th>Примечание</th><th class="vehicle-screen-only"></th></tr></thead>
                <tbody>
                  <tr v-for="item in currentEntries" :key="item.id">
                    <td>{{ formatDate(item.readingDate) }}</td><td>{{ formatNumber(item.engineHours) }}</td>
                    <td>{{ item.note || '—' }}</td>
                    <td class="vehicle-screen-only"><button class="table-action-button table-action-button--danger" @click="deleteEntry(item.id)">Удалить</button></td>
                  </tr>
                </tbody>
              </table>
              <table v-else class="vehicle-repairs-table">
                <thead><tr><th>Неисправность / узел</th><th>Выполненные работы</th><th>№ заявки</th><th>Фото</th><th class="vehicle-screen-only"></th></tr></thead>
                <tbody>
                  <tr v-for="item in currentEntries" :key="item.id">
                    <td>{{ item.defectNodeName || 'Старая запись без привязки' }}</td>
                    <td>{{ item.description }}</td>
                    <td>{{ item.purchaseRequestNumber || '—' }}</td>
                    <td>
                      <div class="vehicle-photo-actions">
                        <span v-if="!item.photos.length">Нет</span>
                        <span v-for="photo in item.photos" :key="photo.id" class="vehicle-photo-chip">
                          <button type="button" @click="openPhoto(photo)">
                            {{ photo.fileName }}
                          </button>
                          <button
                            class="vehicle-photo-delete"
                            type="button"
                            aria-label="Удалить фотографию"
                            @click="deletePhoto(photo.id)"
                          >
                            ×
                          </button>
                        </span>
                        <label class="vehicle-photo-upload">
                          + Добавить
                          <input
                            accept="image/jpeg,image/png,image/webp"
                            multiple
                            type="file"
                            @change="addPhotosToWork(item.id, $event)"
                          />
                        </label>
                      </div>
                    </td>
                    <td class="vehicle-screen-only"><button class="table-action-button table-action-button--danger" @click="deleteEntry(item.id)">Удалить</button></td>
                  </tr>
                </tbody>
              </table>
              <p v-if="!currentEntries.length" class="vehicle-empty">
                За выбранный период записей нет
              </p>
            </div>
          </section>

          <section class="vehicle-print-report">
            <h1>Отчёт по технике: {{ selectedVehicle.modelName }}</h1>
            <p>
              Гаражный № {{ selectedVehicle.garageNumber ?? '—' }};
              гос. номер {{ selectedVehicle.stateNumber || '—' }};
              период {{ fromDate ? formatDate(fromDate) : 'за всё время' }}
              {{ toDate ? `— ${formatDate(toDate)}` : '' }}
            </p>

            <h2>Заявки на приобретение</h2>
            <table>
              <thead><tr><th>Дата</th><th>№</th><th>Наименование</th><th>Кол-во</th><th>Статус</th><th>Примечание</th></tr></thead>
              <tbody><tr v-for="item in journal.purchases" :key="item.id"><td>{{ formatDate(item.requestDate) }}</td><td>{{ item.requestNumber || '—' }}</td><td>{{ item.itemName }}</td><td>{{ formatNumber(item.quantity) }}</td><td>{{ item.status }}</td><td>{{ item.note || '—' }}</td></tr></tbody>
            </table>

            <h2>Неисправности</h2>
            <table>
              <thead><tr><th>Узел</th><th>Причина неисправности</th><th>Фото</th></tr></thead>
              <tbody><tr v-for="item in journal.defects" :key="item.id"><td>{{ item.nodeName }}</td><td>{{ item.failureReason }}</td><td>{{ item.photos.length }}</td></tr></tbody>
            </table>

            <h2>Наработанные моточасы</h2>
            <table>
              <thead><tr><th>Дата</th><th>Моточасы</th><th>Примечание</th></tr></thead>
              <tbody><tr v-for="item in journal.hours" :key="item.id"><td>{{ formatDate(item.readingDate) }}</td><td>{{ formatNumber(item.engineHours) }}</td><td>{{ item.note || '—' }}</td></tr></tbody>
            </table>

            <h2>Ремонты</h2>
            <table>
              <thead><tr><th>Неисправность / узел</th><th>Работы</th><th>№ заявки</th><th>Фото</th></tr></thead>
              <tbody><tr v-for="item in journal.works" :key="item.id"><td>{{ item.defectNodeName || 'Старая запись без привязки' }}</td><td>{{ item.description }}</td><td>{{ item.purchaseRequestNumber || '—' }}</td><td>{{ item.photos.length }}</td></tr></tbody>
            </table>
          </section>
        </template>
      </div>
    </section>

    <div
      v-if="photoViewerUrl"
      class="vehicle-photo-viewer"
      role="dialog"
      aria-modal="true"
      :aria-label="photoViewerName"
      @click.self="closePhoto"
    >
      <div class="vehicle-photo-viewer-card">
        <div>
          <strong>{{ photoViewerName }}</strong>
          <button type="button" aria-label="Закрыть" @click="closePhoto">×</button>
        </div>
        <img :src="photoViewerUrl" :alt="photoViewerName" />
      </div>
    </div>
  </main>
</template>
