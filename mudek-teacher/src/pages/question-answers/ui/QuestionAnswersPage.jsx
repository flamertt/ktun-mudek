import { createColumnHelper } from '@tanstack/react-table'
import { Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import {
  addAnswer,
  addAnswersBulk,
  deleteAnswer,
  fetchAnswers,
  fetchCourseStudents,
  updateAnswer,
} from '../../../shared/api/teacherApi'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { parseMaybeNumber } from '../../../shared/lib/numberUtils.js'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { DataTable } from '../../../shared/ui/data-table/DataTable.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'

const columnHelper = createColumnHelper()

export function QuestionAnswersPage() {
  const { offeringId, evaluationId, questionId } = useParams()
  const navigate = useNavigate()

  const [enrollments, setEnrollments] = useState([])
  const [answers, setAnswers] = useState([])

  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  const [scoreDraftByEnrollmentId, setScoreDraftByEnrollmentId] = useState({})
  const scoreDraftRef = useRef(scoreDraftByEnrollmentId)
  scoreDraftRef.current = scoreDraftByEnrollmentId

  const [saving, setSaving] = useState(false)
  const [submitError, setSubmitError] = useState('')

  const answerByEnrollmentId = useMemo(() => {
    const map = new Map()
    for (const a of Array.isArray(answers) ? answers : []) {
      map.set(a.enrollmentId ?? a.EnrollmentId, a)
    }
    return map
  }, [answers])

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId || !questionId) return
    setLoading(true)
    setError('')

    Promise.all([fetchCourseStudents(token, offeringId), fetchAnswers(token, questionId)])
      .then(([enr, ans]) => {
        const enroll = Array.isArray(enr) ? enr : []
        const answerRows = Array.isArray(ans) ? ans : []
        setEnrollments(enroll)
        setAnswers(answerRows)
        const draft = {}
        for (const en of enroll) {
          const enrollmentId = en.id ?? en.Id
          const existing = answerRows.find((a) => (a.enrollmentId ?? a.EnrollmentId) === enrollmentId)
          draft[enrollmentId] = existing ? existing.score ?? existing.Score : ''
        }
        setScoreDraftByEnrollmentId(draft)
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Cevaplar alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId, questionId])

  useEffect(() => {
    load()
  }, [load])

  const handleSaveOne = useCallback(
    async (enrollment) => {
      const token = getTeacherToken()
      if (!token || !questionId) return

      const enrollmentId = enrollment.id ?? enrollment.Id
      const existing = answerByEnrollmentId.get(enrollmentId)

      const score = parseMaybeNumber(scoreDraftRef.current[enrollmentId])
      if (score == null) {
        setSubmitError('Puan zorunlu.')
        return
      }

      setSubmitError('')
      setSaving(true)
      try {
        if (existing?.id ?? existing?.Id) {
          const answerId = existing.id ?? existing.Id
          await updateAnswer(token, answerId, { score })
        } else {
          await addAnswer(token, questionId, { enrollmentId, score })
        }
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
      } finally {
        setSaving(false)
      }
    },
    [answerByEnrollmentId, load, questionId],
  )

  const handleDeleteOne = useCallback(
    async (enrollment) => {
      const token = getTeacherToken()
      if (!token || !questionId) return

      const enrollmentId = enrollment.id ?? enrollment.Id
      const existing = answerByEnrollmentId.get(enrollmentId)
      const answerId = existing?.id ?? existing?.Id
      if (!answerId) return

      const ok = window.confirm('Bu öğrencinin cevap kaydını silmek istiyor musunuz?')
      if (!ok) return

      setSubmitError('')
      setSaving(true)
      try {
        await deleteAnswer(token, answerId)
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Silinemedi.')
      } finally {
        setSaving(false)
      }
    },
    [answerByEnrollmentId, load, questionId],
  )

  const handleSaveBulk = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !questionId || !offeringId) return

    const items = []
    for (const en of enrollments) {
      const enrollmentId = en.id ?? en.Id
      const score = parseMaybeNumber(scoreDraftRef.current[enrollmentId])
      if (score == null) continue
      items.push({ enrollmentId, score })
    }

    if (!items.length) {
      setSubmitError('Toplu kaydet için en az bir satırda puan girin.')
      return
    }

    setSubmitError('')
    setSaving(true)
    try {
      await addAnswersBulk(token, questionId, { items })
      await load()
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Toplu kaydet başarısız.')
    } finally {
      setSaving(false)
    }
  }, [enrollments, load, offeringId, questionId])

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
              style={{ minWidth: '9rem' }}
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
        id: 'actions',
        header: 'İşlemler',
        cell: ({ row }) => {
          const en = row.original
          const enrollmentId = en.id ?? en.Id
          const existing = answerByEnrollmentId.get(enrollmentId)
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
    [answerByEnrollmentId, handleDeleteOne, handleSaveOne, saving],
  )

  return (
    <PageSection
      title="Öğrenci cevap puanları"
      description="Puan: bu soru için sayısal not. Yazarken tablo sütunları yenilenmez; kayıt yalnızca Kaydet veya Toplu kaydet ile sunucuya gider."
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
            navigate(`/evaluations/${offeringId}/evaluation/${evaluationId}/questions/${questionId}/clos`)
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
