import { createColumnHelper } from '@tanstack/react-table'
import { RefreshCw, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import {
  addScore,
  addScoresBulk,
  deleteScore,
  fetchCourseStudents,
  fetchScores,
  updateScore,
} from '../../../shared/api/teacherApi'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { parseMaybeNumber } from '../../../shared/lib/numberUtils.js'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'

const columnHelper = createColumnHelper()

export function ComponentScoresPage() {
  const { offeringId, evaluationId, componentId } = useParams()
  const navigate = useNavigate()

  const [enrollments, setEnrollments] = useState([])
  const [scores, setScores] = useState([])

  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  const [scoreDraftByEnrollmentId, setScoreDraftByEnrollmentId] = useState({})
  const [notesDraftByEnrollmentId, setNotesDraftByEnrollmentId] = useState({})

  const scoreDraftRef = useRef(scoreDraftByEnrollmentId)
  const notesDraftRef = useRef(notesDraftByEnrollmentId)
  scoreDraftRef.current = scoreDraftByEnrollmentId
  notesDraftRef.current = notesDraftByEnrollmentId

  const [saving, setSaving] = useState(false)
  const [submitError, setSubmitError] = useState('')

  const scoreByEnrollmentId = useMemo(() => {
    const map = new Map()
    for (const s of Array.isArray(scores) ? scores : []) {
      map.set(s.enrollmentId ?? s.EnrollmentId, s)
    }
    return map
  }, [scores])

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId || !componentId) return
    setLoading(true)
    setError('')

    Promise.all([fetchCourseStudents(token, offeringId), fetchScores(token, componentId)])
      .then(([enr, sc]) => {
        const enroll = Array.isArray(enr) ? enr : []
        const scoreRows = Array.isArray(sc) ? sc : []
        setEnrollments(enroll)
        setScores(scoreRows)

        const scoreDraft = {}
        const notesDraft = {}
        for (const en of enroll) {
          const enrollmentId = en.id ?? en.Id
          const existing = scoreRows.find((s) => (s.enrollmentId ?? s.EnrollmentId) === enrollmentId)
          scoreDraft[enrollmentId] = existing ? existing.score ?? existing.Score : ''
          notesDraft[enrollmentId] = existing ? existing.notes ?? existing.Notes ?? '' : ''
        }
        setScoreDraftByEnrollmentId(scoreDraft)
        setNotesDraftByEnrollmentId(notesDraft)
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Notlar alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId, componentId])

  useEffect(() => {
    load()
  }, [load])

  const handleSaveOne = useCallback(
    async (enrollment) => {
      const token = getTeacherToken()
      if (!token || !componentId) return
      setSubmitError('')

      const enrollmentId = enrollment.id ?? enrollment.Id
      const existing = scoreByEnrollmentId.get(enrollmentId)

      const score = parseMaybeNumber(scoreDraftRef.current[enrollmentId])
      const notes = String(notesDraftRef.current[enrollmentId] ?? '').trim() || null

      if (score == null) {
        setSubmitError('Puan zorunlu.')
        return
      }

      setSaving(true)
      try {
        if (existing?.id ?? existing?.Id) {
          const scoreId = existing.id ?? existing.Id
          await updateScore(token, scoreId, { score, notes })
        } else {
          await addScore(token, componentId, { enrollmentId, score, notes })
        }
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
      } finally {
        setSaving(false)
      }
    },
    [componentId, load, scoreByEnrollmentId],
  )

  const handleDeleteOne = useCallback(
    async (enrollment) => {
      const token = getTeacherToken()
      if (!token || !componentId) return
      const enrollmentId = enrollment.id ?? enrollment.Id
      const existing = scoreByEnrollmentId.get(enrollmentId)
      const scoreId = existing?.id ?? existing?.Id
      if (!scoreId) return

      const ok = window.confirm('Bu öğrencinin puan kaydını silmek istiyor musunuz?')
      if (!ok) return

      setSubmitError('')
      setSaving(true)
      try {
        await deleteScore(token, scoreId)
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Silinemedi.')
      } finally {
        setSaving(false)
      }
    },
    [componentId, load, scoreByEnrollmentId],
  )

  const handleSaveBulk = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !componentId || !offeringId) return

    const scoresItems = []
    for (const en of enrollments) {
      const enrollmentId = en.id ?? en.Id
      const score = parseMaybeNumber(scoreDraftRef.current[enrollmentId])
      if (score == null) continue
      const notes = String(notesDraftRef.current[enrollmentId] ?? '').trim() || null
      scoresItems.push({ enrollmentId, score, notes })
    }

    if (!scoresItems.length) {
      setSubmitError('Toplu kaydet için en az bir satırda puan girin.')
      return
    }

    setSubmitError('')
    setSaving(true)
    try {
      await addScoresBulk(token, componentId, { assessmentComponentId: componentId, scores: scoresItems })
      await load()
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Toplu kaydet başarısız.')
    } finally {
      setSaving(false)
    }
  }, [componentId, enrollments, load, offeringId])

  const columns = useMemo(
    () => [
      columnHelper.accessor('studentFullName', { header: 'Öğrenci' }),
      columnHelper.accessor('studentNumber', { header: 'Numara' }),
      columnHelper.accessor('status', { header: 'Durum', cell: (info) => info.getValue() ?? '—' }),
      columnHelper.display({
        id: 'score',
        header: 'Puan',
        cell: ({ row }) => {
          const en = row.original
          const enrollmentId = en.id ?? en.Id
          const value = scoreDraftRef.current[enrollmentId] ?? ''
          return (
            <input
              type="text"
              inputMode="decimal"
              autoComplete="off"
              className={formStyles.input}
              style={{ minWidth: '8rem' }}
              value={value}
              onChange={(e) =>
                setScoreDraftByEnrollmentId((prev) => ({
                  ...prev,
                  [enrollmentId]: e.target.value,
                }))
              }
            />
          )
        },
      }),
      columnHelper.display({
        id: 'notes',
        header: 'Açıklama',
        cell: ({ row }) => {
          const en = row.original
          const enrollmentId = en.id ?? en.Id
          const value = notesDraftRef.current[enrollmentId] ?? ''
          return (
            <input
              type="text"
              autoComplete="off"
              className={formStyles.input}
              style={{ minWidth: '12rem' }}
              placeholder="İsteğe bağlı kısa not"
              value={value}
              onChange={(e) =>
                setNotesDraftByEnrollmentId((prev) => ({
                  ...prev,
                  [enrollmentId]: e.target.value,
                }))
              }
            />
          )
        },
      }),
      columnHelper.display({
        id: 'actions',
        header: 'İşlemler',
        cell: ({ row }) => {
          const en = row.original
          const enrollmentId = en.id ?? en.Id
          const existing = scoreByEnrollmentId.get(enrollmentId)
          const hasExisting = Boolean(existing?.id ?? existing?.Id)

          return (
            <div className={formStyles.rowActionGroup}>
              <button
                type="button"
                className={`${formStyles.btn} ${formStyles.btnPrimary}`}
                disabled={saving}
                onClick={() => handleSaveOne(en)}
              >
                Kaydet
              </button>
              {hasExisting ? (
                <button
                  type="button"
                  className={`${formStyles.rowActionText} ${formStyles.rowActionTextDanger}`}
                  disabled={saving}
                  onClick={() => handleDeleteOne(en)}
                >
                  <Trash2 size={14} aria-hidden />
                  Kaydı sil
                </button>
              ) : null}
            </div>
          )
        },
      }),
    ],
    [handleDeleteOne, handleSaveOne, saving, scoreByEnrollmentId],
  )

  return (
    <PageSection
      title="Bileşen puanları"
      description="Puan: bu sınav/bileşen için sayısal not (ör. 30, 75.5). Açıklama: harf notu değil; isteğe bağlı kısa metin. Değişiklikler yalnızca Kaydet veya Toplu kaydet ile sunucuya yazılır."
      error={error || submitError}
    >
      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <RefreshIconButton onClick={load} loading={loading} title="Yenile" />
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnPrimary}`}
          disabled={saving || !enrollments.length}
          onClick={handleSaveBulk}
        >
          Toplu kaydet
        </button>
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnGhost}`}
          disabled={saving}
          onClick={() =>
            navigate(`/evaluations/${offeringId}/evaluation/${evaluationId}/components/${componentId}/clos`)
          }
        >
          DÖÇ eşlemesi sayfası
        </button>
      </div>

      <div style={{ marginTop: '0.75rem' }}>
        <DataTable
          columns={columns}
          data={enrollments}
          globalFilter={globalFilter}
          onGlobalFilterChange={setGlobalFilter}
          searchPlaceholder="Öğrenci ara…"
          isLoading={loading}
        />
      </div>

      {submitError && !loading ? <p className={sectionStyles.error}>{submitError}</p> : null}
    </PageSection>
  )
}

