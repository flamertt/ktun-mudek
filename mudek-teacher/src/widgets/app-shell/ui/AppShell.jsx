import { useEffect, useMemo, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  Bell,
  ChevronRight,
  Maximize2,
  Menu,
  MessageSquareMore,
  Minimize2,
  Search,
  Settings,
} from 'lucide-react'

import { logoutCurrentUser } from '../../../shared/api/authApi'
import { appConfig } from '../../../shared/config/appConfig'

import { getNavIcon } from '../lib/navIcons.js'
import styles from './AppShell.module.css'

export function AppShell() {
  const navigate = useNavigate()
  const location = useLocation()
  const profileRef = useRef(null)

  const [isCollapsed, setIsCollapsed] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const [isFullscreen, setIsFullscreen] = useState(Boolean(document.fullscreenElement))
  const [isProfileOpen, setIsProfileOpen] = useState(false)
  const [logoutError, setLogoutError] = useState('')

  const navItems = useMemo(
    () => appConfig.navSections.flatMap((section) => section.items),
    [],
  )

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', 'light')
    localStorage.setItem(appConfig.storage.themeKey, 'light')
  }, [])

  useEffect(() => {
    const handleFullscreen = () => {
      setIsFullscreen(Boolean(document.fullscreenElement))
    }

    document.addEventListener('fullscreenchange', handleFullscreen)
    return () => document.removeEventListener('fullscreenchange', handleFullscreen)
  }, [])

  useEffect(() => {
    const handlePointerDown = (event) => {
      if (!profileRef.current?.contains(event.target)) {
        setIsProfileOpen(false)
      }
    }

    document.addEventListener('pointerdown', handlePointerDown)
    return () => document.removeEventListener('pointerdown', handlePointerDown)
  }, [])

  useEffect(() => {
    if (!isProfileOpen) return
    const handleKeyDown = (event) => {
      if (event.key === 'Escape') setIsProfileOpen(false)
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isProfileOpen])

  const pageTitle = useMemo(() => {
    const exact = navItems.find((item) => item.path === location.pathname)
    if (exact) return exact.label
    const nested = navItems.find(
      (item) => item.path !== appConfig.routes.home && location.pathname.startsWith(`${item.path}/`),
    )
    if (nested) return nested.label
    return appConfig.sidebarTitle
  }, [location.pathname, navItems])

  useEffect(() => {
    document.title = `${pageTitle} | ${appConfig.appTitle}`
  }, [pageTitle])

  const profileName = useMemo(() => {
    const raw = localStorage.getItem(appConfig.storage.userKey)
    if (!raw) return appConfig.ui.profileName

    try {
      const parsed = JSON.parse(raw)
      return parsed?.fullName ?? appConfig.ui.profileName
    } catch {
      return appConfig.ui.profileName
    }
  }, [])

  const toggleFullscreen = async () => {
    if (!document.fullscreenElement) {
      await document.documentElement.requestFullscreen()
      return
    }

    await document.exitFullscreen()
  }

  const handleSecureLogout = async () => {
    setLogoutError('')

    const token = localStorage.getItem(appConfig.storage.tokenKey)
    try {
      await logoutCurrentUser(token)
    } catch {
      setLogoutError(appConfig.ui.logoutError)
    } finally {
      localStorage.removeItem(appConfig.storage.tokenKey)
      localStorage.removeItem(appConfig.storage.userKey)
      navigate('/login', { replace: true })
    }
  }

  const renderItemIcon = (iconName) => {
    const Icon = getNavIcon(iconName)
    return <Icon className={styles.itemIcon} aria-hidden="true" />
  }

  return (
    <div className={styles.shell}>
      <header className={styles.navbar}>
        <div className={styles.navLeft}>
          <div className={`${styles.navBrand} ${isCollapsed ? styles.navBrandCollapsed : ''}`}>
            <img
              className={`${styles.universityLogo} ${isCollapsed ? styles.universityLogoCollapsed : ''}`}
              src={isCollapsed ? '/sidebar_logo_collapsed.png' : '/ktun_logo_koyu_zemin.gif'}
              alt={appConfig.ui.universityLogoAlt}
            />
          </div>
          <div className={styles.leftGroup}>
            <button
              className={styles.toggleButton}
              type="button"
              aria-label={appConfig.ui.toggleSidebarLabel}
              onClick={() => setIsCollapsed((prev) => !prev)}
            >
              <Menu className={styles.navIcon} aria-hidden="true" />
            </button>
            <nav className={styles.breadcrumb} aria-label={appConfig.ui.breadcrumbAriaLabel}>
              <span className={styles.crumbRoot}>{appConfig.ui.breadcrumbRoot}</span>
              <ChevronRight className={styles.crumbSep} aria-hidden="true" />
              <span className={styles.crumbCurrent}>{pageTitle}</span>
            </nav>
          </div>
        </div>

        <div className={styles.navCenter}>
          <label className={styles.searchWrap}>
            <span className={styles.visuallyHidden}>{appConfig.ui.searchInputLabel}</span>
            <Search className={styles.searchIcon} aria-hidden="true" />
            <input
              className={styles.searchInput}
              type="search"
              name="shell-search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={appConfig.ui.searchPlaceholder}
              autoComplete="off"
            />
          </label>
        </div>

        <div className={styles.rightGroup}>
          <button
            className={styles.iconButton}
            type="button"
            aria-label={appConfig.ui.notificationsLabel}
            disabled
          >
            <Bell className={styles.navIcon} aria-hidden="true" />
          </button>

          <button
            className={styles.iconButton}
            type="button"
            aria-label={appConfig.ui.messagesLabel}
            disabled
          >
            <MessageSquareMore className={styles.navIcon} aria-hidden="true" />
          </button>

          <button
            className={styles.iconButton}
            type="button"
            aria-label={appConfig.ui.settingsLabel}
            disabled
          >
            <Settings className={styles.navIcon} aria-hidden="true" />
          </button>

          <span className={styles.rightDivider} aria-hidden="true" />

          <button
            className={styles.iconButton}
            type="button"
            aria-label={appConfig.ui.toggleFullscreenLabel}
            onClick={toggleFullscreen}
          >
            {isFullscreen ? (
              <Minimize2 className={styles.navIcon} aria-hidden="true" />
            ) : (
              <Maximize2 className={styles.navIcon} aria-hidden="true" />
            )}
          </button>

          <div className={styles.profileWrap} ref={profileRef}>
            <button
              className={styles.profileButton}
              type="button"
              aria-label={appConfig.ui.profileLabel}
              aria-expanded={isProfileOpen}
              aria-haspopup="menu"
              aria-controls={isProfileOpen ? 'profile-menu-panel' : undefined}
              onClick={() => setIsProfileOpen((prev) => !prev)}
            >
              <span className={styles.profileAvatar} aria-hidden="true">
                {profileName.slice(0, 1).toUpperCase()}
              </span>
              <span className={styles.profileText}>
                <span className={styles.profileName}>{profileName}</span>
                <span className={styles.profileRole}>{appConfig.ui.profileRole}</span>
              </span>
            </button>

            {isProfileOpen ? (
              <div
                id="profile-menu-panel"
                className={styles.profileDropdown}
                role="region"
                aria-label={appConfig.ui.profileMenuAriaLabel}
              >
                <button
                  className={styles.dropdownItem}
                  type="button"
                  onClick={handleSecureLogout}
                >
                  {appConfig.ui.logoutLabel}
                </button>
                {logoutError ? <p className={styles.dropdownError}>{logoutError}</p> : null}
              </div>
            ) : null}
          </div>
        </div>
      </header>

      <div className={`${styles.body} ${isCollapsed ? styles.bodyCollapsed : ''}`}>
        <aside className={styles.sidebar}>
          <nav className={styles.nav}>
            {appConfig.navSections.map((section) => (
              <div key={section.key} className={styles.sectionGroup}>
                {!isCollapsed && section.title ? (
                  <p className={styles.sectionTitle}>{section.title}</p>
                ) : null}

                <div className={styles.sectionItems}>
                  {section.items.map((item) => (
                    <NavLink
                      key={item.key}
                      to={item.path}
                      title={item.label}
                      aria-label={isCollapsed ? item.label : undefined}
                      className={({ isActive }) =>
                        `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
                      }
                    >
                      {renderItemIcon(item.icon)}
                      {!isCollapsed ? <span className={styles.itemLabel}>{item.label}</span> : null}
                    </NavLink>
                  ))}
                </div>
              </div>
            ))}
          </nav>
        </aside>

        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
