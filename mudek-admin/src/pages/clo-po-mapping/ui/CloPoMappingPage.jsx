import { createColumnHelper } from '@tanstack/react-table'
import { useCallback, useEffect, useMemo, useState } from 'react'

import {
  createPloMap,
  deletePloMap,
  fetchLocalClos,
  fetchPloMapsByCourse,
  fetchUniversityCourseCloPoMap,
  fetchUniversityCourseClos,
  fetchUniversityProgramOutcomes,
  fetchUniversityPrograms,
  updatePloMap,
} from '../../../shared/api/adminApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getAdminToken } from '../../../shared/lib/authToken'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import { TeacherCoursePicker } from '../../../shared/ui/teacher-course-picker/TeacherCoursePicker.jsx'
import pageStyles from './CloPoMappingPage.module.css'

const columnHelper = createColumnHelper()

const EMPTY_FORM = { courseCloId: '', externalPloId: '', ploCode: '', weight: '0.5' }

function programKey(p) { return String(p?.programId ?? p?.ProgramId ?? '') }
function programLabel(p) { return p?.programName ?? p?.ProgramName ?? programKey(p) }

export function CloPoMappingPage() {
  const page = appConfig.pages.cloPoMapping
  const [programs, setPrograms] = useState([])
  const [programId, setProgramId] = useState('')

  // course selection — either via picker or manual input
  const [courseIdInput, setCourseIdInput] = useState('')
  const [courseId, setCourseId] = useState('')
  const [selectedCourseMeta, setSelectedCourseMeta] = useState(null) // { courseCode, courseName }

  // Üniversite API verileri
  const [apiClos, setApiClos] = useState([])
  const [apiMaps, setApiMaps] = useState([])
  const [outcomes, setOutcomes] = useState([])

  // Yerel veriler
  const [localClos, setLocalClos] = useState([])
  const [localMaps, setLocalMaps] = useState([])

  const [error, setError] = useState('')
  const [loadingPrograms, setLoadingPrograms] = useState(true)
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  // form
  const [formOpen, setFormOpen] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')

  const loadPrograms = useCallback(async () => {
    const token = getAdminToken()
    if (!token) return
    setLoadingPrograms(true)
    try {
      const data = await fetchUniversityPrograms(token)
      const list = Array.isArray(data) ? data : []
      setPrograms(list)
      setProgramId((prev) => {
        const keys = list.map(programKey).filter(Boolean)
        if (prev && keys.includes(prev)) return prev
        return keys[0] ?? ''
      })
    } catch { /* ignore */ } finally { setLoadingPrograms(false) }
  }, [])

  const loadData = useCallback(async () => {
    const token = getAdminToken()
    const pid = Number(programId)
    const cid = Number(courseId)
    if (!token || !Number.isFinite(pid) || pid <= 0 || !Number.isFinite(cid) || cid <= 0) {
      setApiClos([]); setApiMaps([]); setOutcomes([]); setLocalClos([]); setLocalMaps([])
      return
    }
    setLoading(true); setError('')
    try {
      const [poList, apiCloList, apiMapList, dbCloList, dbMapList] = await Promise.all([
        fetchUniversityProgramOutcomes(token, pid),
        fetchUniversityCourseClos(token, cid),
        fetchUniversityCourseCloPoMap(token, cid),
        fetchLocalClos(token, cid),
        fetchPloMapsByCourse(token, cid),
      ])
      setOutcomes(Array.isArray(poList) ? poList : [])
      setApiClos(Array.isArray(apiCloList) ? apiCloList : [])
      setApiMaps(Array.isArray(apiMapList) ? apiMapList : [])
      setLocalClos(Array.isArray(dbCloList) ? dbCloList : [])
      setLocalMaps(Array.isArray(dbMapList) ? dbMapList : [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Veriler yüklenemedi.')
    } finally { setLoading(false) }
  }, [programId, courseId])

  useEffect(() => { void loadPrograms() }, [loadPrograms])
  useEffect(() => { void loadData() }, [loadData])

  /** Picker'dan ders seçildiğinde courseId ve programId'yi otomatik doldur. */
  const handleCourseSelect = (course) => {
    const cid = String(course.courseId)
    const pid = String(course.programId)
    setCourseId(cid)
    setCourseIdInput(cid)
    setSelectedCourseMeta({ courseCode: course.courseCode, courseName: course.courseName })
    if (pid && pid !== '0') setProgramId(pid)
  }

  const openCreate = () => {
    setEditRow(null)
    setForm(EMPTY_FORM)
    setFormError('')
    setFormOpen(true)
  }

  const openEdit = (row) => {
    setEditRow(row)
    setForm({
      courseCloId: String(row.courseCloId),
      externalPloId: String(row.externalPloId),
      ploCode: row.ploCode ?? '',
      weight: String(row.weight),
    })
    setFormError('')
    setFormOpen(true)
  }

  const handleSave = async () => {
    const token = getAdminToken()
    const w = parseFloat(form.weight)
    if (isNaN(w) || w < 0 || w > 1) { setFormError('Ağırlık 0.0–1.0 arasında olmalı.'); return }
    setSaving(true); setFormError('')
    try {
      if (editRow) {
        await updatePloMap(token, editRow.id, { id: editRow.id, ploCode: form.ploCode || null, weight: w })
      } else {
        await createPloMap(token, {
          courseCloId: Number(form.courseCloId),
          externalPloId: Number(form.externalPloId),
          ploCode: form.ploCode || null,
          weight: w,
        })
      }
      setFormOpen(false)
      await loadData()
    } catch (e) {
      setFormError(e instanceof Error ? e.message : 'Kayıt başarısız.')
    } finally { setSaving(false) }
  }

  const handleDelete = async (row) => {
    if (!window.confirm('Bu CLO–PO eşlemesi silinsin mi?')) return
    const token = getAdminToken()
    try { await deletePloMap(token, row.id); await loadData() }
    catch (e) { setError(e instanceof Error ? e.message : 'Silme başarısız.') }
  }

  const apiMapColumns = useMemo(() => [
    columnHelper.accessor((r) => r?.cloId ?? '—', { header: 'CLO Id' }),
    columnHelper.accessor((r) => r?.programOutcomeId ?? '—', { header: 'PÇ Id' }),
    columnHelper.accessor((r) => r?.weight ?? '—', { header: 'Ağırlık' }),
  ], [])

  const localMapColumns = useMemo(() => [
    columnHelper.accessor('id', { header: 'Id' }),
    columnHelper.accessor('courseCloId', { header: 'CLO Id' }),
    columnHelper.accessor(
      (r) => r.cloCode ? `${r.cloCode} — ${r.cloDescription ?? ''}` : r.cloDescription ?? '—',
      { header: 'CLO' },
    ),
    columnHelper.accessor('externalPloId', { header: 'PÇ Id' }),
    columnHelper.accessor('ploCode', { header: 'PÇ Kodu', cell: (i) => i.getValue() ?? '—' }),
    columnHelper.accessor('weight', { header: 'Ağırlık', cell: (i) => i.getValue()?.toFixed(2) }),
    columnHelper.display({
      id: 'actions',
      header: 'İşlem',
      cell: ({ row }) => (
        <span style={{ display: 'flex', gap: '0.4rem' }}>
          <button
            className={`${formStyles.btn} ${formStyles.btnSecondary}`}
            style={{ padding: '0.2rem 0.5rem', fontSize: '0.78rem' }}
            onClick={() => openEdit(row.original)}
          >
            Düzenle
          </button>
          <button
            className={formStyles.btn}
            style={{ padding: '0.2rem 0.5rem', fontSize: '0.78rem', background: 'var(--color-danger,#e53)', color: '#fff' }}
            onClick={() => handleDelete(row.original)}
          >
            Sil
          </button>
        </span>
      ),
    }),
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [])

  const apiEmpty = apiClos.length === 0 && apiMaps.length === 0

  return (
    <PageSection
      title={page.title}
      description="CLO–program çıktısı matrisi yönetimi. Üniversite API'si boş dönerse yerel kayıtlar hesaplamada kullanılır."
      error={error}
    >
      {/* ── Öğretmen ders arama ─────────────────────────────────────── */}
      <TeacherCoursePicker onSelect={handleCourseSelect} selectedCourseId={Number(courseId) || undefined} />

      {/* ── Manuel giriş ────────────────────────────────────────────── */}
      <div className={pageStyles.filterRow} style={{ marginBottom: '1rem' }}>
        <label>
          <span style={{ display: 'block', fontSize: '0.82rem', color: 'var(--color-muted)', marginBottom: '0.2rem' }}>Program</span>
          <select
            className={sectionStyles.select}
            value={programId}
            onChange={(e) => setProgramId(e.target.value)}
            disabled={loadingPrograms || !programs.length}
          >
            {programs.map((p) => (
              <option key={programKey(p)} value={programKey(p)}>{programLabel(p)}</option>
            ))}
          </select>
        </label>
        <label>
          <span style={{ display: 'block', fontSize: '0.82rem', color: 'var(--color-muted)', marginBottom: '0.2rem' }}>Ders Id (courseId)</span>
          <input
            type="number"
            className={formStyles.input}
            value={courseIdInput}
            onChange={(e) => { setCourseIdInput(e.target.value); setSelectedCourseMeta(null) }}
            placeholder="Katalog ders no"
          />
        </label>
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnPrimary}`}
          onClick={() => { setCourseId(courseIdInput.trim()); setSelectedCourseMeta(null) }}
        >
          Yükle
        </button>
        <RefreshIconButton onClick={() => void loadData()} loading={loading} title="Yenile" />
      </div>

      {courseId && (
        <>
          {selectedCourseMeta && (
            <p className={sectionStyles.muted} style={{ marginBottom: '0.5rem' }}>
              Seçili ders: <strong>{selectedCourseMeta.courseCode} — {selectedCourseMeta.courseName}</strong>
              {' · '}courseId: <strong>{courseId}</strong>
            </p>
          )}

          <p className={sectionStyles.muted}>
            PÇ sayısı: {outcomes.length} · API CLO: {apiClos.length} · API matris satırı: {apiMaps.length}
            {apiEmpty && (
              <span style={{ color: 'var(--color-warning,#c90)', marginLeft: '0.75rem' }}>
                ⚠ API boş — yerel kayıtlar devreye giriyor
              </span>
            )}
          </p>

          <h3 className={sectionStyles.muted} style={{ marginTop: '1rem', fontSize: '1rem' }}>Üniversite API Matrisi</h3>
          <DataTable
            columns={apiMapColumns}
            data={apiMaps}
            globalFilter=""
            onGlobalFilterChange={() => {}}
            searchPlaceholder="API matris ara…"
            isLoading={loading}
          />

          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginTop: '1.5rem', marginBottom: '0.5rem' }}>
            <h3 className={sectionStyles.muted} style={{ margin: 0, fontSize: '1rem' }}>
              Yerel CLO–PO Eşlemeleri ({localMaps.length})
            </h3>
            {localClos.length > 0 ? (
              <button
                type="button"
                className={`${formStyles.btn} ${formStyles.btnPrimary}`}
                onClick={openCreate}
                style={{ fontSize: '0.85rem' }}
              >
                + Yeni Eşleme
              </button>
            ) : (
              <span className={sectionStyles.muted} style={{ fontSize: '0.85rem' }}>
                Önce CLO Yönetim sayfasından yerel CLO ekleyin.
              </span>
            )}
          </div>
          <DataTable
            columns={localMapColumns}
            data={localMaps}
            globalFilter={globalFilter}
            onGlobalFilterChange={setGlobalFilter}
            searchPlaceholder="Eşleme ara…"
            isLoading={loading}
          />
        </>
      )}

      <AppDialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        title={editRow ? 'Eşleme Düzenle' : 'Yeni CLO–PO Eşlemesi'}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          {!editRow && (
            <>
              <label className={formStyles.label}>
                CLO *
                <select
                  className={formStyles.input}
                  value={form.courseCloId}
                  onChange={(e) => setForm((f) => ({ ...f, courseCloId: e.target.value }))}
                >
                  <option value="">CLO seçin</option>
                  {localClos.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.code ? `${c.code} — ` : ''}{c.description}
                    </option>
                  ))}
                </select>
              </label>
              <label className={formStyles.label}>
                Program Çıktısı (PÇ) *
                <select
                  className={formStyles.input}
                  value={form.externalPloId}
                  onChange={(e) => setForm((f) => ({ ...f, externalPloId: e.target.value }))}
                >
                  <option value="">PÇ seçin</option>
                  {outcomes.map((o) => {
                    const id = o?.programOutcomeId ?? o?.ProgramOutcomeId
                    const code = o?.programOutcomeCode ?? o?.ProgramOutcomeCode ?? id
                    const desc = o?.description ?? o?.Description ?? ''
                    return (
                      <option key={id} value={id}>
                        {code} — {desc}
                      </option>
                    )
                  })}
                </select>
              </label>
            </>
          )}
          <label className={formStyles.label}>
            PÇ Kodu (opsiyonel)
            <input
              className={formStyles.input}
              value={form.ploCode}
              onChange={(e) => setForm((f) => ({ ...f, ploCode: e.target.value }))}
              placeholder="örn. PÇ-3"
            />
          </label>
          <label className={formStyles.label}>
            Ağırlık (0.0 – 1.0) *
            <input
              type="number"
              step="0.05"
              min="0"
              max="1"
              className={formStyles.input}
              value={form.weight}
              onChange={(e) => setForm((f) => ({ ...f, weight: e.target.value }))}
            />
          </label>
          {formError && (
            <p style={{ color: 'var(--color-danger,#e53)', margin: 0, fontSize: '0.85rem' }}>{formError}</p>
          )}
          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
            <button className={formStyles.btn} onClick={() => setFormOpen(false)}>İptal</button>
            <button
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              onClick={handleSave}
              disabled={saving}
            >
              {saving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </div>
      </AppDialog>
    </PageSection>
  )
}
