import { createColumnHelper } from '@tanstack/react-table'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'

import {
  addComponentCloMapping,
  deleteComponentCloMapping,
  fetchComponentClos,
  fetchOfferingClos,
  updateComponentCloMapping,
} from '../../../shared/api/teacherApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { parseMaybeNumber } from '../../../shared/lib/numberUtils.js'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'

const columnHelper = createColumnHelper()

export function ComponentClosPage() {
  const { offeringId, componentId } = useParams()

  const page = appConfig.pages.evaluations

  const [clos, setClos] = useState([])
  const [mappings, setMappings] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [globalFilter, setGlobalFilter] = useState('')

  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogMode, setDialogMode] = useState('create')
  const [form, setForm] = useState({ courseLearningOutcomeId: '', weight: 0.5 })
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [editingMappingId, setEditingMappingId] = useState(null)

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId || !componentId) return
    setLoading(true)
    setError('')

    Promise.all([fetchOfferingClos(token, offeringId), fetchComponentClos(token, componentId)])
      .then(([closData, mappingsData]) => {
        setClos(Array.isArray(closData) ? closData : [])
        setMappings(Array.isArray(mappingsData) ? mappingsData : [])
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Eşlemeler alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId, componentId])

  useEffect(() => {
    load()
  }, [load])

  const openCreate = useCallback(() => {
    setDialogMode('create')
    setEditingMappingId(null)
    setForm({ courseLearningOutcomeId: clos[0]?.id ?? '', weight: 0.5 })
    setFormError('')
    setDialogOpen(true)
  }, [clos])

  const openEdit = useCallback((row) => {
    setDialogMode('edit')
    setEditingMappingId(row.id)
    setForm({
      courseLearningOutcomeId: row.courseLearningOutcomeId ?? '',
      weight: Number(row.weight ?? 0),
    })
    setFormError('')
    setDialogOpen(true)
  }, [])

  const handleSubmit = async (e) => {
    e.preventDefault()
    const token = getTeacherToken()
    if (!token || !componentId) return

    setFormError('')
    setSaving(true)
    try {
      const cloId = String(form.courseLearningOutcomeId ?? '').trim()
      if (!cloId) {
        setFormError('CLO seçin.')
        return
      }

      const weight = parseMaybeNumber(form.weight) ?? 0
      const bodyBase = { courseLearningOutcomeId: cloId, weight }

      if (dialogMode === 'create') {
        await addComponentCloMapping(token, componentId, bodyBase)
      } else {
        if (!editingMappingId) return
        await updateComponentCloMapping(token, editingMappingId, {
          ...bodyBase,
          assessmentComponentId: componentId,
        })
      }

      setDialogOpen(false)
      load()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Kaydedilemedi.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (row) => {
    const token = getTeacherToken()
    if (!token) return
    const ok = window.confirm('Eşleme silinsin mi?')
    if (!ok) return
    setSaving(true)
    setError('')
    try {
      await deleteComponentCloMapping(token, row.id)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silme başarısız.')
    } finally {
      setSaving(false)
    }
  }

  const columns = useMemo(
    () => [
      columnHelper.accessor('outcomeCode', { header: 'CLO' }),
      columnHelper.accessor('outcomeDescription', { header: 'Açıklama' }),
      columnHelper.accessor('weight', { header: 'Ağırlık', cell: (info) => String(info.getValue() ?? 0) }),
      columnHelper.display({
        id: 'actions',
        header: 'İşlemler',
        cell: ({ row }) => (
          <div className={formStyles.rowActionGroup}>
            <button
              type="button"
              className={formStyles.rowActionText}
              onClick={() => openEdit(row.original)}
              disabled={saving}
            >
              <Pencil size={14} aria-hidden />
              Düzenle
            </button>
            <button
              type="button"
              className={`${formStyles.rowActionText} ${formStyles.rowActionTextDanger}`}
              onClick={() => handleDelete(row.original)}
              disabled={saving}
            >
              <Trash2 size={14} aria-hidden />
              Sil
            </button>
          </div>
        ),
      }),
    ],
    [handleDelete, openEdit, saving],
  )

  return (
    <PageSection title="Bileşen -> CLO Eşleştirme" description={page.title} error={error}>
      <DataTable
        columns={columns}
        data={mappings}
        globalFilter={globalFilter}
        onGlobalFilterChange={setGlobalFilter}
        searchPlaceholder="CLO veya açıklama ara…"
        toolbarExtra={
          <>
            <RefreshIconButton onClick={load} loading={loading} />
            <button type="button" className={`${formStyles.btn} ${formStyles.btnPrimary}`} onClick={openCreate}>
              <Plus size={18} aria-hidden />
              Yeni eşleme
            </button>
          </>
        }
        isLoading={loading}
      />

      <AppDialog
        open={dialogOpen}
        onClose={() => {
          if (!saving) setDialogOpen(false)
        }}
        title={dialogMode === 'create' ? 'Yeni CLO eşlemesi' : 'Eşlemeyi düzenle'}
        size="md"
        footer={
          <>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnGhost}`}
              onClick={() => setDialogOpen(false)}
              disabled={saving}
            >
              Vazgeç
            </button>
            <button
              type="submit"
              form="component-clos-form"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={saving}
            >
              {saving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </>
        }
      >
        <form id="component-clos-form" className={formStyles.form} onSubmit={handleSubmit}>
          {formError ? (
            <p className={sectionStyles.error} role="alert">
              {formError}
            </p>
          ) : null}

          <div className={formStyles.field}>
            <label className={formStyles.label} htmlFor="courseLearningOutcomeId">
              CLO
            </label>
            <select
              id="courseLearningOutcomeId"
              className={formStyles.select}
              value={form.courseLearningOutcomeId}
              onChange={(e) => setForm((f) => ({ ...f, courseLearningOutcomeId: e.target.value }))}
              required
            >
              {clos.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code} · {c.description}
                </option>
              ))}
            </select>
          </div>

          <div className={formStyles.field}>
            <label className={formStyles.label} htmlFor="weight">
              Ağırlık
            </label>
            <input
              id="weight"
              type="number"
              step="0.01"
              className={formStyles.input}
              value={form.weight}
              onChange={(e) => setForm((f) => ({ ...f, weight: e.target.value }))}
            />
          </div>
        </form>
      </AppDialog>
    </PageSection>
  )
}

