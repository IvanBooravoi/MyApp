<script setup>
import { reactive, ref } from 'vue'

const props = defineProps({
  token: {
    type: String,
    required: true,
  },
})

const files = [
  { name: 'c.csv', description: 'Остатки механиков' },
  { name: 'o.csv', description: 'Остатки на складе' },
  { name: 'p.csv', description: 'Работники' },
]
const selectedFiles = reactive({})
const uploadingFile = ref('')
const messages = reactive({})

function selectFile(fileName, event) {
  selectedFiles[fileName] = event.target.files?.[0] ?? null
  messages[fileName] = ''
}

async function downloadFile(fileName) {
  messages[fileName] = ''
  const response = await fetch(`/api/admin/csv-files/${fileName}`, {
    headers: { Authorization: 'Bearer ' + props.token },
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    messages[fileName] =
      problem?.detail ??
      problem?.title ??
      'Не удалось скачать текущий CSV-файл.'
    return
  }

  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.append(link)
  link.click()
  link.remove()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}

async function replaceFile(fileName) {
  const file = selectedFiles[fileName]
  if (!file) {
    messages[fileName] = 'Выберите CSV-файл.'
    return
  }
  if (!window.confirm(`Заменить текущий файл ${fileName}?`)) return

  uploadingFile.value = fileName
  messages[fileName] = ''
  const body = new FormData()
  body.append('file', file)
  try {
    const response = await fetch(`/api/admin/csv-files/${fileName}`, {
      method: 'POST',
      headers: { Authorization: 'Bearer ' + props.token },
      body,
    })
    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      throw new Error(
        problem?.errors?.file?.[0] ??
          problem?.detail ??
          problem?.title ??
          'Не удалось заменить CSV-файл.',
      )
    }

    selectedFiles[fileName] = null
    messages[fileName] = `${fileName} успешно заменён.`
  } catch (error) {
    messages[fileName] =
      error instanceof Error ? error.message : 'Не удалось заменить CSV-файл.'
  } finally {
    uploadingFile.value = ''
  }
}
</script>

<template>
  <div class="csv-files-settings">
    <div>
      <h2>Загрузка CSV-файлов</h2>
      <p class="profile-hint">
        Новый файл полностью заменяет предыдущий файл на сервере.
      </p>
    </div>
    <div class="csv-file-grid">
      <article v-for="item in files" :key="item.name" class="csv-file-card">
        <div>
          <strong>{{ item.name }}</strong>
          <p>{{ item.description }}</p>
        </div>
        <input
          type="file"
          accept=".csv,text/csv"
          :disabled="uploadingFile !== ''"
          @change="selectFile(item.name, $event)"
        />
        <button
          class="primary-button"
          type="button"
          :disabled="!selectedFiles[item.name] || uploadingFile !== ''"
          @click="replaceFile(item.name)"
        >
          {{ uploadingFile === item.name ? 'Загрузка...' : 'Заменить файл' }}
        </button>
        <a
          class="secondary-link"
          href="#"
          :aria-disabled="uploadingFile !== ''"
          @click.prevent="uploadingFile === '' && downloadFile(item.name)"
        >
          Скачать текущий {{ item.name }}
        </a>
        <p
          v-if="messages[item.name]"
          class="form-message"
          :class="{
            'form-message--success': messages[item.name].endsWith('успешно заменён.'),
            'form-message--error': !messages[item.name].endsWith('успешно заменён.'),
          }"
        >
          {{ messages[item.name] }}
        </p>
      </article>
    </div>
  </div>
</template>
