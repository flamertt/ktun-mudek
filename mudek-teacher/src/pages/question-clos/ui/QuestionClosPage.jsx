import { createColumnHelper } from '@tanstack/react-table'
import { Pencil, Trash2, Plus } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'

import {
  addQuestionCloMapping,
  deleteQuestionCloMapping,
  fetchOfferingClos,
  fetchQuestionClos,
  updateQuestionCloMapping,
} from '../../../shared/api/teacherApi'
import { getTeacherToken } from '../../../shared/lib/authToken'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { appConfig } from '../../../shared/config/appConfig'
import { parseMaybeNumber } from '../../../shared/lib/numberUtils.js'

const columnHelper = createColumnHelper()

const emptyForm = {
  courseLearningOutcomeId: '',
  weight: 0.5,
}

export function QuestionClosPage() {
  const { offeringId, questionId } = useParams()

  const page = appConfig.pages.evaluations

  const [clos, setClos] = useState([])
  const [mappings, setMappings] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [globalFilter, setGlobalFilter] = useState('')

  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogMode, setDialogMode] = useState('create')
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)
  const [editingMappingId, setEditingMappingId] = useState(null)

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId || !questionId) return
    setLoading(true)
    setError('')

    Promise.all([fetchOfferingClos(token, offeringId), fetchQuestionClos(token, questionId)])
      .then(([closData, mappingsData]) => {
        // fetchOfferingClos artık { source, clos } objesi dönüyor; geriye uyumlu düz dizi de desteklenir
        const cloList = Array.isArray(closData) ? closData : (Array.isArray(closData?.clos) ? closData.clos : [])
        setClos(cloList)
        setMappings(Array.isArray(mappingsData) ? mappingsData : [])
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'CLO eşlemeleri alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId, questionId])

  useEffect(() => {
    load()
  }, [load])

  const openCreate = useCallback(() => {
    setDialogMode('create')
    setEditingMappingId(null)
    setForm({
      courseLearningOutcomeId: clos[0]?.id ?? '',
      weight: 0.5,
    })
    setFormError('')
    setDialogOpen(true)
  }, [clos])

  const openEdit = useCallback((row) => {
    setDialogMode('edit')
    setEditingMappingId(row.id ?? row.Id)
    // cloKey üzerinden seçili CLO'yu bul; yoksa externalCloId + source ile oluştur
    const key = row.cloKey ?? row.CloKey
      ?? `${row.cloSource ?? row.CloSource ?? 'api'}:${row.externalCloId ?? row.ExternalCloId ?? ''}`
    const match = clos.find((c) => {
      const src = c?.source ?? c?.sourceType ?? 'api'
      const id = c?.id ?? c?.cloId ?? ''
      return `${src}:${id}` === key || String(c?.id) === String(row.externalCloId ?? row.ExternalCloId)
    })
    setForm({
      courseLearningOutcomeId: match ? String(match.id ?? match.cloId) : '',
      weight: Number(row.weight ?? 0),
    })
    setFormError('')
    setDialogOpen(true)
  }, [clos])

  const handleSubmit = async (e) => {
    e.preventDefault()
    const token = getTeacherToken()
    if (!token || !questionId) return

    setFormError('')
    setSaving(true)
    try {
      const cloId = String(form.courseLearningOutcomeId ?? '').trim()
      if (!cloId) {
        setFormError('CLO seçin.')
        return
      }

      const weight = parseMaybeNumber(form.weight) ?? 0

      // Seçilen CLO'nun kaynağını bul (api|db)
      const selectedClo = clos.find((c) => String(c.id) === cloId || String(c.cloId) === cloId)
      const cloSource = selectedClo?.source ?? 'api'
      const externalCloId = Number(selectedClo?.cloId ?? cloId)

      const body = {
        examQuestionId: questionId,
        externalCloId,
        cloSource,
        cloCode: selectedClo?.code ?? null,
        cloDescription: selectedClo?.description ?? null,
        weight,
      }

      if (dialogMode === 'create') {
        await addQuestionCloMapping(token, questionId, body)
      } else {
        if (!editingMappingId) return
        await updateQuestionCloMapping(token, editingMappingId, body)
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
      await deleteQuestionCloMapping(token, row.id)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silme başarısız.')
    } finally {
      setSaving(false)
    }
  }

  const columns = useMemo(
    () => [
      columnHelper.accessor((r) => r?.cloCode ?? r?.CloCode ?? '—', { header: 'CLO Kodu' }),
      columnHelper.accessor((r) => r?.cloDescription ?? r?.CloDescription ?? '—', { header: 'Açıklama' }),
      columnHelper.accessor((r) => r?.cloSource ?? r?.CloSource ?? 'api', {
        header: 'Kaynak',
        cell: (info) => info.getValue() === 'db'
          ? <span style={{ fontSize: '0.8rem', color: 'var(--color-warning,#c90)' }}>Yerel</span>
          : <span style={{ fontSize: '0.8rem' }}>API</span>,
      }),
      columnHelper.accessor('weight', {
        header: 'Ağırlık',
        cell: (info) => String(info.getValue() ?? 0),
      }),
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
    <PageSection title="Soru -> CLO Eşleştirme" description={page.title} error={error}>
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
            <button type="submit" form="question-clos-form" className={`${formStyles.btn} ${formStyles.btnPrimary}`} disabled={saving}>
              {saving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </>
        }
      >
        <form id="question-clos-form" className={formStyles.form} onSubmit={handleSubmit}>
          {formError ? (
            <p className={sectionStyles.error} role="alert">
              {formError}
            </p>
          ) : null}

          <div className={formStyles.field}>
            <label className={formStyles.label} htmlFor="courseLearningOutcomeId">
              CLO {clos[0]?.source === 'db' && <span style={{ fontSize: '0.75rem', color: 'var(--color-warning,#c90)' }}>(yerel kayıt)</span>}
            </label>
            <select
              id="courseLearningOutcomeId"
              className={formStyles.select}
              value={form.courseLearningOutcomeId}
              onChange={(e) => setForm((f) => ({ ...f, courseLearningOutcomeId: e.target.value }))}
              required
            >
              {clos.map((c) => (
                <option key={c.id ?? c.cloId} value={c.id ?? String(c.cloId)}>
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

