<script setup>
import { ref } from 'vue'
import AuthLogin from './components/AuthLogin.vue'
import HomeView from './components/HomeView.vue'
import MaintenanceView from './components/MaintenanceView.vue'
import NavigationSidebar from './components/NavigationSidebar.vue'
import ProfileView from './components/ProfileView.vue'
import RequirementsJournalView from './components/RequirementsJournalView.vue'
import SelectedComponentsView from './components/SelectedComponentsView.vue'
import SettingsView from './components/SettingsView.vue'
import TablesView from './components/TablesView.vue'
import VehicleJournalView from './components/VehicleJournalView.vue'

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
const currentVehicleSection = ref('defects')
const selectedComponentRows = ref([])
const selectedResponsibleEmployee = ref(null)
const profileVersion = ref(0)

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
  selectedComponentRows.value = []
  selectedResponsibleEmployee.value = null
}

function openTable(tableName) {
  if (
    tableName !== currentTable.value &&
    (tableName === 'v_full_ost' || tableName === 'v_meh_ost')
  ) {
    selectedResponsibleEmployee.value = null
  }
  currentTable.value = tableName
  currentPage.value = 'tables'
}

function openSettings(section) {
  currentSettings.value = section
  currentPage.value = 'settings'
}

function openVehicleSection(section) {
  currentVehicleSection.value = section
  currentPage.value = 'vehicles'
}

function openSelectedComponents() {
  currentPage.value = 'selected-components'
}

function applyMaintenanceTemplate(rows) {
  currentTable.value = 'v_full_ost'
  updateSelectedComponentRows(rows)
}

function updateResponsibleEmployee(employee) {
  if (
    employee &&
    selectedComponentRows.value.some(
      (row) => row.__sourceTable !== employee.sourceTable,
    )
  ) {
    selectedComponentRows.value = []
  }
  selectedResponsibleEmployee.value = employee
}

function updateSelectedComponentRows(rows) {
  selectedComponentRows.value = rows
  const sourceTable = rows[0]?.__sourceTable
  if (
    sourceTable &&
    selectedResponsibleEmployee.value?.sourceTable !== sourceTable
  ) {
    selectedResponsibleEmployee.value = null
  }
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
      :active-vehicle-section="currentVehicleSection"
      :is-admin="role === 'administrator'"
      :selected-components-count="selectedComponentRows.length"
      :token="token"
      :profile-version="profileVersion"
      @toggle="isNavigationCollapsed = !isNavigationCollapsed"
      @navigate="currentPage = $event"
      @navigate-table="openTable"
      @navigate-selected-components="openSelectedComponents"
      @navigate-settings="openSettings"
      @navigate-vehicle="openVehicleSection"
      @logout="handleLogout"
    />
    <HomeView
      v-if="currentPage === 'home'"
      :navigation-collapsed="isNavigationCollapsed"
      :token="token"
    />
    <TablesView
      v-else-if="currentPage === 'tables'"
      :navigation-collapsed="isNavigationCollapsed"
      :selected-table-id="currentTable"
      :selected-component-rows="selectedComponentRows"
      :selected-responsible-employee="selectedResponsibleEmployee"
      :token="token"
      @select-table="currentTable = $event"
      @show-selected-components="openSelectedComponents"
      @update:selected-component-rows="updateSelectedComponentRows"
      @update:selected-responsible-employee="updateResponsibleEmployee"
    />
    <SelectedComponentsView
      v-else-if="currentPage === 'selected-components'"
      :navigation-collapsed="isNavigationCollapsed"
      :rows="selectedComponentRows"
      :responsible-employee="selectedResponsibleEmployee"
      :token="token"
      @back="openTable(currentTable)"
      @clear="selectedComponentRows = []"
      @update:rows="updateSelectedComponentRows"
    />
    <RequirementsJournalView
      v-else-if="currentPage === 'requirements'"
      :navigation-collapsed="isNavigationCollapsed"
      :token="token"
    />
    <MaintenanceView
      v-else-if="currentPage === 'maintenance'"
      :navigation-collapsed="isNavigationCollapsed"
      :selected-count="selectedComponentRows.length"
      :token="token"
      @apply-template="applyMaintenanceTemplate"
      @show-selected="openSelectedComponents"
    />
    <VehicleJournalView
      v-else-if="currentPage === 'vehicles'"
      :navigation-collapsed="isNavigationCollapsed"
      :role="role"
      :section="currentVehicleSection"
      :token="token"
      @navigate-section="openVehicleSection"
    />
    <SettingsView
      v-else-if="currentPage === 'settings' && role === 'administrator'"
      :navigation-collapsed="isNavigationCollapsed"
      :selected-section="currentSettings"
      :token="token"
    />
    <ProfileView
      v-else-if="currentPage === 'profile'"
      :navigation-collapsed="isNavigationCollapsed"
      :token="token"
      @profile-updated="profileVersion++"
    />
  </div>
</template>
