import { createColumnHelper } from '@tanstack/react-table'
import { useCallback, useEffect, useMemo, useState } from 'react'

import {
  fetchUniversityCourseCloPoMap,
  fetchUniversityCourseClos,
  fetchUniversityProgramOutcomes,
  fetchUniversityPrograms,
} from '../../../shared/api/adminApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getAdminToken } from '../../../shared/lib/authToken'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import pageStyles from './CloPoMappingPage.module.css'

const columnHelper = createColumnHelper()

function programKey(p) {
  return String(p?.programId ?? p?.ProgramId ?? '')
}

function programLabel(p) {
  return p?.programName ?? p?.ProgramName ?? programKey(p)
}

export function CloPoMappingPage() {
  const page = appConfig.pages.cloPoMapping
  const [programs, setPrograms] = useState([])
  const [programId, setProgramId] = useState('')
  const [courseIdInput, setCourseIdInput] = useState('')
  const [courseId, setCourseId] = useState('')

  const [clos, setClos] = useState([])
  const [outcomes, setOutcomes] = useState([])
  const [maps, setMaps] = useState([])
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
      const list = Array.isArray(data) ? data : []
      setPrograms(list)
      setProgramId((prev) => {
        if (!list.length) return ''
        const keys = list.map(programKey).filter(Boolean)
        if (prev && keys.includes(prev)) return prev
        return keys[0] ?? ''
      })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Programlar yüklenemedi.')
    } finally {
      setLoadingPrograms(false)
    }
  }, [])

  const loadMatrices = useCallback(async () => {
    const token = getAdminToken()
    const pid = Number(programId)
    const cid = Number(courseId)
    if (!token || !Number.isFinite(pid) || pid <= 0) {
      setOutcomes([])
      setClos([])
      setMaps([])
      return
    }
    if (!Number.isFinite(cid) || cid <= 0) {
      setOutcomes([])
      setClos([])
      setMaps([])
      return
    }
    setLoading(true)
    setError('')
    try {
      const [poList, cloList, mapList] = await Promise.all([
        fetchUniversityProgramOutcomes(token, pid),
        fetchUniversityCourseClos(token, cid),
        fetchUniversityCourseCloPoMap(token, cid),
      ])
      setOutcomes(Array.isArray(poList) ? poList : [])
      setClos(Array.isArray(cloList) ? cloList : [])
      setMaps(Array.isArray(mapList) ? mapList : [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Veriler yüklenemedi.')
      setOutcomes([])
      setClos([])
      setMaps([])
    } finally {
      setLoading(false)
    }
  }, [programId, courseId])

  useEffect(() => {
    void loadPrograms()
  }, [loadPrograms])

  useEffect(() => {
    void loadMatrices()
  }, [loadMatrices])

  const mapColumns = useMemo(
    () => [
      columnHelper.accessor((r) => r?.cloId ?? r?.CloId ?? '—', { header: 'CLO Id' }),
      columnHelper.accessor((r) => r?.programOutcomeId ?? r?.ProgramOutcomeId ?? '—', { header: 'PÇ Id' }),
      columnHelper.accessor((r) => r?.weight ?? r?.Weight ?? '—', { header: 'Ağırlık' }),
    ],
    [],
  )

  return (
    <PageSection
      title={page.title}
      description="CLO–program çıktısı matrisi üniversite API’sinden salt okunur. Program seçin, ardından ders Id (courseId) girin."
      error={error}
    >
      <div className={pageStyles.filterRow} style={{ marginBottom: '1rem' }}>
        <label>
          Program
          <select
            className={sectionStyles.select}
            value={programId}
            onChange={(e) => setProgramId(e.target.value)}
            disabled={loadingPrograms || !programs.length}
          >
            {programs.map((p) => (
              <option key={programKey(p)} value={programKey(p)}>
                {programLabel(p)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Ders Id (courseId)
          <input
            type="number"
            className={formStyles.input}
            value={courseIdInput}
            onChange={(e) => setCourseIdInput(e.target.value)}
            placeholder="Katalog ders no"
          />
        </label>
        <button type="button" className={`${formStyles.btn} ${formStyles.btnPrimary}`} onClick={() => setCourseId(courseIdInput.trim())}>
          Yükle
        </button>
        <RefreshIconButton onClick={() => void loadMatrices()} loading={loading} title="Yenile" />
      </div>

      <p className={sectionStyles.muted}>
        PÇ sayısı: {outcomes.length} · CLO sayısı: {clos.length} · Matris satırı: {maps.length}
      </p>

      <h3 className={sectionStyles.muted} style={{ marginTop: '1rem', fontSize: '1rem' }}>
        Matris
      </h3>
      <DataTable
        columns={mapColumns}
        data={maps}
        globalFilter={globalFilter}
        onGlobalFilterChange={setGlobalFilter}
        searchPlaceholder="CLO / PÇ Id ara…"
        isLoading={loading}
      />
    </PageSection>
  )
}
