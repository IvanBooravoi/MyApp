<script setup>
import { ref } from 'vue'

const props = defineProps({
  collapsed: {
    type: Boolean,
    required: true,
  },
  activePage: {
    type: String,
    required: true,
  },
  activeTable: {
    type: String,
    required: true,
  },
  activeSettings: {
    type: String,
    required: true,
  },
  isAdmin: {
    type: Boolean,
    required: true,
  },
})

const emit = defineEmits([
  'toggle',
  'navigate',
  'navigate-table',
  'navigate-settings',
  'logout',
])
const isTablesExpanded = ref(false)
const isSettingsExpanded = ref(false)

function toggleTables() {
  if (props.collapsed) {
    emit('toggle')
    isTablesExpanded.value = true
    return
  }

  isTablesExpanded.value = !isTablesExpanded.value
}

function toggleSettings() {
  if (props.collapsed) {
    emit('toggle')
    isSettingsExpanded.value = true
    return
  }

  isSettingsExpanded.value = !isSettingsExpanded.value
}
</script>

<template>
  <aside class="sidebar" :class="{ 'sidebar--collapsed': collapsed }">
    <div class="sidebar-header">
      <div class="sidebar-brand">
        <span class="brand-mark brand-mark--small" aria-hidden="true">M</span>
        <span class="sidebar-label brand-name">MyApp</span>
      </div>
      <button
        v-if="collapsed"
        class="icon-button"
        type="button"
        aria-label="Развернуть меню"
        :aria-expanded="false"
        @click="$emit('toggle')"
      >
        <span class="hamburger" aria-hidden="true">
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>
    </div>

    <button
      v-if="!collapsed"
      class="collapse-handle"
      type="button"
      aria-label="Свернуть меню"
      :aria-expanded="true"
      @click="$emit('toggle')"
    >
      <span aria-hidden="true">‹</span>
    </button>

    <nav class="sidebar-nav" aria-label="Основная навигация">
      <button
        class="nav-link"
        :class="{ 'nav-link--active': activePage === 'home' }"
        type="button"
        :aria-current="activePage === 'home' ? 'page' : undefined"
        @click="$emit('navigate', 'home')"
      >
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M3 10.8 12 3l9 7.8V21h-6v-6H9v6H3V10.8Z" />
        </svg>
        <span class="sidebar-label">Главная</span>
      </button>
      <button
        class="nav-link"
        :class="{ 'nav-link--active': activePage === 'tables' }"
        type="button"
        :aria-expanded="isTablesExpanded"
        aria-controls="tables-submenu"
        @click="toggleTables"
      >
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M4 5h16v14H4V5Zm0 5h16M9 5v14" />
        </svg>
        <span class="sidebar-label">Таблицы</span>
        <span
          class="sidebar-label submenu-chevron"
          :class="{ 'submenu-chevron--expanded': isTablesExpanded }"
          aria-hidden="true"
        >
          ›
        </span>
      </button>
      <div
        v-if="!collapsed && isTablesExpanded"
        id="tables-submenu"
        class="nav-submenu"
      >
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'tables' && activeTable === 'v_full_ost' }"
          type="button"
          @click="$emit('navigate-table', 'v_full_ost')"
        >
          Остатки на складе
        </button>
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'tables' && activeTable === 'v_meh_ost' }"
          type="button"
          @click="$emit('navigate-table', 'v_meh_ost')"
        >
          Остатки механиков
        </button>
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'tables' && activeTable === 'v_workers' }"
          type="button"
          @click="$emit('navigate-table', 'v_workers')"
        >
          Работники
        </button>
      </div>
      <button
        v-if="isAdmin"
        class="nav-link"
        :class="{ 'nav-link--active': activePage === 'settings' }"
        type="button"
        :aria-expanded="isSettingsExpanded"
        aria-controls="settings-submenu"
        @click="toggleSettings"
      >
        <svg viewBox="0 0 24 24" aria-hidden="true">
          <path d="M12 15.5A3.5 3.5 0 1 0 12 8a3.5 3.5 0 0 0 0 7.5Zm8-3.5 2-1-2-3.5-2.2.6a8 8 0 0 0-1.8-1L15.5 5h-4L11 7.1a8 8 0 0 0-1.8 1L7 7.5 5 11l2 1a8 8 0 0 0 0 2l-2 1 2 3.5 2.2-.6a8 8 0 0 0 1.8 1l.5 2.1h4l.5-2.1a8 8 0 0 0 1.8-1l2.2.6 2-3.5-2-1a8 8 0 0 0 0-2Z" />
        </svg>
        <span class="sidebar-label">Настройки</span>
        <span
          class="sidebar-label submenu-chevron"
          :class="{ 'submenu-chevron--expanded': isSettingsExpanded }"
          aria-hidden="true"
        >
          ›
        </span>
      </button>
      <div
        v-if="isAdmin && !collapsed && isSettingsExpanded"
        id="settings-submenu"
        class="nav-submenu"
      >
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'settings' && activeSettings === 'users' }"
          type="button"
          @click="$emit('navigate-settings', 'users')"
        >
          Пользователи
        </button>
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'settings' && activeSettings === 'registration' }"
          type="button"
          @click="$emit('navigate-settings', 'registration')"
        >
          Регистрация пользователя
        </button>
        <button
          class="nav-submenu-link"
          :class="{ 'nav-submenu-link--active': activePage === 'settings' && activeSettings === 'professions' }"
          type="button"
          @click="$emit('navigate-settings', 'professions')"
        >
          Профессии
        </button>
      </div>
    </nav>

    <button class="nav-link logout-button" type="button" @click="$emit('logout')">
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M10 5H4v14h6M14 8l4 4-4 4m4-4H9" />
      </svg>
      <span class="sidebar-label">Выйти</span>
    </button>
  </aside>
</template>
