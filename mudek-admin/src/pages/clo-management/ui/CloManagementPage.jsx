import { createColumnHelper } from '@tanstack/react-table'
import { useCallback, useEffect, useMemo, useState } from 'react'

import { fetchUniversityCourseClos, fetchUniversityPrograms } from '../../../shared/api/adminApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getAdminToken } from '../../../shared/lib/authToken'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import pageStyles from './CloManagementPage.module.css'

const columnHelper = createColumnHelper()

function programKey(p) {
  return String(p?.programId ?? p?.ProgramId ?? '')
}

function programLabel(p) {
  return p?.programName ?? p?.ProgramName ?? programKey(p)
}

export function CloManagementPage() {
  const page = appConfig.pages.cloManagement
  const [programs, setPrograms] = useState([])
  const [courseIdInput, setCourseIdInput] = useState('')
  const [courseId, setCourseId] = useState('')
  const [rows, setRows] = useState([])
  const [error, setError] = useState('')
  const [loadingPrograms, setLoadingPrograms] = useState(true)
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  const loadPrograms = useCallback(async () => {
    const token = getAdminToken()
    if (!token) return
    setLoadingPrograms(true)
    setError('')
    try {
      const data = await fetchUniversityPrograms(token)
      setPrograms(Array.isArray(data) ? data : [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Programlar yüklenemedi.')
    } finally {
      setLoadingPrograms(false)
    }
  }, [])

  const loadClos = useCallback(async () => {
    const token = getAdminToken()
    const cid = Number(courseId)
    if (!token || !Number.isFinite(cid) || cid <= 0) {
      setRows([])
      return
    }
    setLoading(true)
    setError('')
    try {
      const data = await fetchUniversityCourseClos(token, cid)
      setRows(Array.isArray(data) ? data : [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'CLO listesi alınamadı.')
      setRows([])
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => {
    void loadPrograms()
  }, [loadPrograms])

  useEffect(() => {
    void loadClos()
  }, [loadClos])

  const columns = useMemo(
    () => [
      columnHelper.accessor((r) => r?.cloId ?? r?.CloId ?? '—', { header: 'CLO Id' }),
      columnHelper.accessor((r) => r?.description ?? r?.Description ?? '—', {
        header: 'Açıklama',
        cell: (info) => (
          <span className={pageStyles.hint} style={{ margin: 0 }} title={String(info.getValue())}>
            {String(info.getValue())}
          </span>
        ),
      }),
    ],
    [],
  )

  const applyCourseId = () => {
    setCourseId(courseIdInput.trim())
  }

  return (
    <PageSection
      title={page.title}
      description="Ders öğrenim çıktıları üniversite API’sinden salt okunur gelir. Ders kimliğini (courseId) üniversite sisteminden bularak girin."
      error={error}
    >
      <p className={sectionStyles.muted} style={{ marginBottom: '0.75rem' }}>
        Program listesi (bilgi):{' '}
        {loadingPrograms
          ? 'Yükleniyor…'
          : programs.map((p) => programLabel(p)).join(', ') || '—'}
      </p>

      <div className={pageStyles.filterRow} style={{ maxWidth: '100%' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
          <span>Ders Id (courseId)</span>
          <input
            type="number"
            className={formStyles.input}
            value={courseIdInput}
            onChange={(e) => setCourseIdInput(e.target.value)}
            placeholder="Örn. üniversite katalog ders no"
            style={{ minWidth: '12rem' }}
          />
        </label>
        <button type="button" className={`${formStyles.btn} ${formStyles.btnPrimary}`} onClick={applyCourseId}>
          Listele
        </button>
        <RefreshIconButton onClick={() => void loadClos()} loading={loading} title="Yenile" />
      </div>

      <DataTable
        columns={columns}
        data={rows}
        globalFilter={globalFilter}
        onGlobalFilterChange={setGlobalFilter}
        searchPlaceholder="CLO açıklamasında ara…"
        isLoading={loading}
      />
    </PageSection>
  )
}
