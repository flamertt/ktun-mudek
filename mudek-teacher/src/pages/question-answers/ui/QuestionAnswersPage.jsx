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

  // Üniversite API'si ExternalStudentId (= studentId) ile çalışır; enrollment ID kavramı yok
  const answerByStudentId = useMemo(() => {
    const map = new Map()
    for (const a of Array.isArray(answers) ? answers : []) {
      const sid = a.externalStudentId ?? a.ExternalStudentId ?? a.enrollmentId ?? a.EnrollmentId
      map.set(Number(sid), a)
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
          const studentId = en.studentId ?? en.StudentId ?? en.id ?? en.Id
          const existing = answerRows.find(
            (a) => Number(a.externalStudentId ?? a.ExternalStudentId) === Number(studentId),
          )
          draft[studentId] = existing ? existing.score ?? existing.Score : ''
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

      const studentId = enrollment.studentId ?? enrollment.StudentId ?? enrollment.id ?? enrollment.Id
      const existing = answerByStudentId.get(Number(studentId))
      const answerId = existing?.id ?? existing?.Id

      const raw = scoreDraftRef.current[studentId]
      const score = parseMaybeNumber(raw)

      // Boş = sınava girmemiş: kayıt varsa sil, yoksa hiçbir şey yapma
      if (score == null) {
        if (!answerId) return
        setSubmitError('')
        setSaving(true)
        try {
          await deleteAnswer(token, answerId)
          await load()
        } catch (e) {
          setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
        } finally {
          setSaving(false)
        }
        return
      }

      setSubmitError('')
      setSaving(true)
      try {
        if (answerId) {
          await updateAnswer(token, answerId, { score })
        } else {
          await addAnswer(token, questionId, { externalStudentId: Number(studentId), score })
        }
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
      } finally {
        setSaving(false)
      }
    },
    [answerByStudentId, deleteAnswer, load, questionId],
  )

  const handleDeleteOne = useCallback(
    async (enrollment) => {
      const token = getTeacherToken()
      if (!token || !questionId) return

      const studentId = enrollment.studentId ?? enrollment.StudentId ?? enrollment.id ?? enrollment.Id
      const existing = answerByStudentId.get(Number(studentId))
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
    [answerByStudentId, load, questionId],
  )

  const handleSaveBulk = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !questionId || !offeringId) return

    const items = []
    for (const en of enrollments) {
      const studentId = en.studentId ?? en.StudentId ?? en.id ?? en.Id
      const score = parseMaybeNumber(scoreDraftRef.current[studentId])
      if (score == null) continue
      items.push({ externalStudentId: Number(studentId), score })
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
      columnHelper.accessor(
        (r) => r?.fullName ?? r?.FullName ?? r?.studentFullName ?? r?.StudentFullName ?? '—',
        { header: 'Öğrenci' },
      ),
      columnHelper.accessor(
        (r) => r?.studentNumber ?? r?.StudentNumber ?? r?.numara ?? r?.Numara ?? '—',
        { header: 'Numara' },
      ),
      columnHelper.display({
        id: 'score',
        header: 'Puan',
        cell: ({ row }) => {
          const en = row.original
          const studentId = en.studentId ?? en.StudentId ?? en.id ?? en.Id
          const value = scoreDraftRef.current[studentId] ?? ''
          return (
            <input
              type="text"
              inputMode="decimal"
              autoComplete="off"
              className={formStyles.input}
              style={{ minWidth: '9rem' }}
              placeholder="boş = girmedi"
              value={value}
              onChange={(e) =>
                setScoreDraftByEnrollmentId((prev) => ({
                  ...prev,
                  [studentId]: e.target.value,
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
          const studentId = en.studentId ?? en.StudentId ?? en.id ?? en.Id
          const existing = answerByStudentId.get(Number(studentId))
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
    [answerByStudentId, handleDeleteOne, handleSaveOne, saving],
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
