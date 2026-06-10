import { createColumnHelper } from '@tanstack/react-table'
import { useCallback, useEffect, useMemo, useState } from 'react'

import {
  createLocalClo,
  deleteLocalClo,
  fetchMergedClos,
  fetchUniversityPrograms,
  updateLocalClo,
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
import pageStyles from './CloManagementPage.module.css'

const columnHelper = createColumnHelper()

const EMPTY_FORM = { code: '', description: '', orderIndex: 0, isActive: true }

function programKey(p) { return String(p?.programId ?? p?.ProgramId ?? '') }
function programLabel(p) { return p?.programName ?? p?.ProgramName ?? programKey(p) }

export function CloManagementPage() {
  const page = appConfig.pages.cloManagement
  const [programs, setPrograms] = useState([])
  const [selectedProgramId, setSelectedProgramId] = useState('')

  // course selection — either via picker or manual input
  const [courseIdInput, setCourseIdInput] = useState('')
  const [courseId, setCourseId] = useState('')
  const [selectedCourseMeta, setSelectedCourseMeta] = useState(null) // { courseCode, courseName }

  const [apiClos, setApiClos] = useState([])
  const [localClos, setLocalClos] = useState([])
  const [apiEmpty, setApiEmpty] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  // form
  const [formOpen, setFormOpen] = useState(false)
  const [editRow, setEditRow] = useState(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')

  useEffect(() => {
    const token = getAdminToken()
    if (!token) return
    fetchUniversityPrograms(token)
      .then((data) => {
        const list = Array.isArray(data) ? data : []
        setPrograms(list)
        if (list.length) setSelectedProgramId(programKey(list[0]))
      })
      .catch(() => {})
  }, [])

  const loadData = useCallback(async () => {
    const token = getAdminToken()
    const cid = Number(courseId)
    if (!token || !Number.isFinite(cid) || cid <= 0) {
      setApiClos([]); setLocalClos([]); setApiEmpty(false)
      return
    }
    setLoading(true); setError('')
    try {
      const data = await fetchMergedClos(token, cid)
      setApiClos(Array.isArray(data?.fromApi) ? data.fromApi : [])
      setLocalClos(Array.isArray(data?.fromDb) ? data.fromDb : [])
      setApiEmpty(data?.apiEmpty ?? true)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Veriler yüklenemedi.')
    } finally {
      setLoading(false)
    }
  }, [courseId])

  useEffect(() => { void loadData() }, [loadData])

  /** Picker'dan ders seçildiğinde courseId ve programId'yi otomatik doldur. */
  const handleCourseSelect = (course) => {
    const cid = String(course.courseId)
    const pid = String(course.programId)
    setCourseId(cid)
    setCourseIdInput(cid)
    setSelectedCourseMeta({ courseCode: course.courseCode, courseName: course.courseName })
    if (pid && pid !== '0') setSelectedProgramId(pid)
  }

  const openCreate = () => {
    setEditRow(null)
    setForm(EMPTY_FORM)
    setFormError('')
    setFormOpen(true)
  }

  const openEdit = (row) => {
    setEditRow(row)
    setForm({ code: row.code ?? '', description: row.description, orderIndex: row.orderIndex, isActive: row.isActive })
    setFormError('')
    setFormOpen(true)
  }

  const handleSave = async () => {
    const token = getAdminToken()
    const cid = Number(courseId)
    if (!form.description.trim()) { setFormError('Açıklama zorunludur.'); return }
    setSaving(true); setFormError('')
    try {
      if (editRow) {
        await updateLocalClo(token, editRow.id, {
          id: editRow.id,
          code: form.code || null,
          description: form.description,
          orderIndex: Number(form.orderIndex),
          isActive: form.isActive,
        })
      } else {
        await createLocalClo(token, {
          externalCourseId: cid,
          externalProgramId: Number(selectedProgramId) || 0,
          code: form.code || null,
          description: form.description,
          orderIndex: Number(form.orderIndex),
        })
      }
      setFormOpen(false)
      await loadData()
    } catch (e) {
      setFormError(e instanceof Error ? e.message : 'Kayıt başarısız.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (row) => {
    if (!window.confirm(`"${row.description}" silinsin mi?`)) return
    const token = getAdminToken()
    try { await deleteLocalClo(token, row.id); await loadData() }
    catch (e) { setError(e instanceof Error ? e.message : 'Silme başarısız.') }
  }

  const apiColumns = useMemo(() => [
    columnHelper.accessor((r) => r?.cloId ?? '—', { header: 'CLO Id' }),
    columnHelper.accessor((r) => r?.description ?? '—', { header: 'Açıklama' }),
  ], [])

  const localColumns = useMemo(() => [
    columnHelper.accessor('id', { header: 'Id' }),
    columnHelper.accessor('code', { header: 'Kod', cell: (i) => i.getValue() ?? '—' }),
    columnHelper.accessor('description', { header: 'Açıklama' }),
    columnHelper.accessor('orderIndex', { header: 'Sıra' }),
    columnHelper.accessor('isActive', { header: 'Aktif', cell: (i) => i.getValue() ? '✓' : '—' }),
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
  ], [courseId])

  return (
    <PageSection
      title={page.title}
      description="Üniversite API'si boş döndüğünde hesaplamada kullanılacak yerel CLO'ları yönetin."
      error={error}
    >
      {/* ── Öğretmen ders arama ─────────────────────────────────────── */}
      <TeacherCoursePicker onSelect={handleCourseSelect} selectedCourseId={Number(courseId) || undefined} />

      {/* ── Manuel giriş (isteğe bağlı) ─────────────────────────────── */}
      <div className={pageStyles.filterRow} style={{ marginBottom: '1rem', flexWrap: 'wrap', gap: '0.75rem' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
          <span style={{ fontSize: '0.82rem', color: 'var(--color-muted)' }}>Program (ExternalProgramId)</span>
          <select
            className={formStyles.input}
            value={selectedProgramId}
            onChange={(e) => setSelectedProgramId(e.target.value)}
            style={{ minWidth: '12rem' }}
          >
            {programs.length === 0 && <option value="">Yükleniyor…</option>}
            {programs.map((p) => (
              <option key={programKey(p)} value={programKey(p)}>{programLabel(p)}</option>
            ))}
          </select>
        </label>
        <label style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
          <span style={{ fontSize: '0.82rem', color: 'var(--color-muted)' }}>Ders Id (courseId)</span>
          <input
            type="number"
            className={formStyles.input}
            value={courseIdInput}
            onChange={(e) => { setCourseIdInput(e.target.value); setSelectedCourseMeta(null) }}
            placeholder="Üniversite katalog ders no"
            style={{ minWidth: '12rem' }}
          />
        </label>
        <div style={{ alignSelf: 'flex-end', display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={() => { setCourseId(courseIdInput.trim()); setSelectedCourseMeta(null) }}
          >
            Listele
          </button>
          <RefreshIconButton onClick={() => void loadData()} loading={loading} title="Yenile" />
        </div>
      </div>

      {courseId && (
        <>
          {selectedCourseMeta && (
            <p className={sectionStyles.muted} style={{ marginBottom: '0.5rem' }}>
              Seçili ders: <strong>{selectedCourseMeta.courseCode} — {selectedCourseMeta.courseName}</strong>
              {' · '}courseId: <strong>{courseId}</strong>
            </p>
          )}

          {/* Üniversite API'sinden gelen CLO'lar */}
          <h3 className={sectionStyles.muted} style={{ marginTop: '1rem', fontSize: '1rem' }}>
            Üniversite API CLO'ları
            {apiEmpty && (
              <span style={{ color: 'var(--color-warning,#c90)', marginLeft: '0.5rem' }}>
                ⚠ Boş — yerel kayıt devreye giriyor
              </span>
            )}
          </h3>
          <DataTable
            columns={apiColumns}
            data={apiClos}
            isLoading={loading}
            searchPlaceholder="API CLO ara…"
            globalFilter=""
            onGlobalFilterChange={() => {}}
          />

          {/* Yerel CLO'lar */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginTop: '1.5rem', marginBottom: '0.5rem' }}>
            <h3 className={sectionStyles.muted} style={{ margin: 0, fontSize: '1rem' }}>
              Yerel CLO'lar ({localClos.length})
            </h3>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              onClick={openCreate}
              style={{ fontSize: '0.85rem' }}
            >
              + Yeni CLO
            </button>
          </div>
          <DataTable
            columns={localColumns}
            data={localClos}
            globalFilter={globalFilter}
            onGlobalFilterChange={setGlobalFilter}
            searchPlaceholder="CLO ara…"
            isLoading={loading}
          />
        </>
      )}

      <AppDialog open={formOpen} onClose={() => setFormOpen(false)} title={editRow ? 'CLO Düzenle' : 'Yeni CLO Ekle'}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          <label className={formStyles.label}>
            Kod (opsiyonel)
            <input
              className={formStyles.input}
              value={form.code}
              onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
              placeholder="örn. ÖÇ-1"
            />
          </label>
          <label className={formStyles.label}>
            Açıklama *
            <textarea
              className={formStyles.input}
              value={form.description}
              onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
              rows={3}
              placeholder="CLO açıklaması"
            />
          </label>
          <label className={formStyles.label}>
            Sıra no
            <input
              type="number"
              className={formStyles.input}
              value={form.orderIndex}
              onChange={(e) => setForm((f) => ({ ...f, orderIndex: e.target.value }))}
            />
          </label>
          {editRow && (
            <label style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
              />
              Aktif
            </label>
          )}
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
