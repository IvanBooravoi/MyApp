<script setup>
import { ref } from 'vue'
import AuthLogin from './components/AuthLogin.vue'
import HomeView from './components/HomeView.vue'
import NavigationSidebar from './components/NavigationSidebar.vue'
import SettingsView from './components/SettingsView.vue'
import TablesView from './components/TablesView.vue'

const TOKEN_KEY = 'myapp.authToken'
const ROLE_KEY = 'myapp.userRole'

const storedToken = localStorage.getItem(TOKEN_KEY)
const storedRole = localStorage.getItem(ROLE_KEY)
if (storedToken && !storedRole) {
  localStorage.removeItem(TOKEN_KEY)
}

const token = ref(storedRole ? storedToken : null)
const role = ref(storedRole)
const isNavigationCollapsed = ref(false)
const currentPage = ref('home')
const currentTable = ref('v_full_ost')
const currentSettings = ref('users')

function handleAuthenticated(authData) {
  localStorage.setItem(TOKEN_KEY, authData.token)
  localStorage.setItem(ROLE_KEY, authData.role)
  token.value = authData.token
  role.value = authData.role
}

function handleLogout() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(ROLE_KEY)
  token.value = null
  role.value = null
  isNavigationCollapsed.value = false
  currentPage.value = 'home'
}

function openTable(tableName) {
  currentTable.value = tableName
  currentPage.value = 'tables'
}

function openSettings(section) {
  currentSettings.value = section
  currentPage.value = 'settings'
}
</script>

<template>
  <AuthLogin v-if="!token" @authenticated="handleAuthenticated" />

  <div v-else class="app-shell">
    <NavigationSidebar
      :collapsed="isNavigationCollapsed"
      :active-page="currentPage"
      :active-table="currentTable"
      :active-settings="currentSettings"
      :is-admin="role === 'administrator'"
      @toggle="isNavigationCollapsed = !isNavigationCollapsed"
      @navigate="currentPage = $event"
      @navigate-table="openTable"
      @navigate-settings="openSettings"
      @logout="handleLogout"
    />
    <HomeView
      v-if="currentPage === 'home'"
      :navigation-collapsed="isNavigationCollapsed"
    />
    <TablesView
      v-else-if="currentPage === 'tables'"
      :navigation-collapsed="isNavigationCollapsed"
      :selected-table-id="currentTable"
      :token="token"
      @select-table="currentTable = $event"
    />
    <SettingsView
      v-else-if="currentPage === 'settings' && role === 'administrator'"
      :navigation-collapsed="isNavigationCollapsed"
      :selected-section="currentSettings"
      :token="token"
    />
  </div>
</template>
