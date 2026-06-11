import { createColumnHelper } from '@tanstack/react-table'
import { Trash2 } from 'lucide-react'
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

// API'den gelen öğrenci objesinden benzersiz ID çek
// UniversityStudentDto: { studentId, studentNumber, fullName }
const getStudentId = (student) =>
  student?.studentId ?? student?.StudentId ?? student?.id ?? student?.Id

// Skor objesinden ilgili öğrenci ID'sini çek
// StudentAssessmentComponentScoreDto: { externalStudentId, id, score, notes }
const getScoreStudentId = (score) =>
  score?.externalStudentId ?? score?.ExternalStudentId ?? score?.enrollmentId ?? score?.EnrollmentId

export function ComponentScoresPage() {
  const { offeringId, evaluationId, componentId } = useParams()
  const navigate = useNavigate()

  const [students, setStudents] = useState([])
  const [scores, setScores] = useState([])

  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [globalFilter, setGlobalFilter] = useState('')

  const [scoreDraftByStudentId, setScoreDraftByStudentId] = useState({})
  const [notesDraftByStudentId, setNotesDraftByStudentId] = useState({})

  const scoreDraftRef = useRef(scoreDraftByStudentId)
  const notesDraftRef = useRef(notesDraftByStudentId)
  scoreDraftRef.current = scoreDraftByStudentId
  notesDraftRef.current = notesDraftByStudentId

  const [saving, setSaving] = useState(false)
  const [submitError, setSubmitError] = useState('')

  // externalStudentId → mevcut skor satırı
  const scoreByStudentId = useMemo(() => {
    const map = new Map()
    for (const s of Array.isArray(scores) ? scores : []) {
      const sid = getScoreStudentId(s)
      if (sid != null) map.set(sid, s)
    }
    return map
  }, [scores])

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId || !componentId) return
    setLoading(true)
    setError('')

    Promise.all([fetchCourseStudents(token, offeringId), fetchScores(token, componentId)])
      .then(([stuData, sc]) => {
        const stuList = Array.isArray(stuData) ? stuData : []
        const scoreRows = Array.isArray(sc) ? sc : []
        setStudents(stuList)
        setScores(scoreRows)

        const scoreDraft = {}
        const notesDraft = {}
        for (const stu of stuList) {
          const sid = getStudentId(stu)
          if (sid == null) continue
          const existing = scoreRows.find((s) => getScoreStudentId(s) === sid)
          scoreDraft[sid] = existing ? existing.score ?? existing.Score ?? '' : ''
          notesDraft[sid] = existing ? existing.notes ?? existing.Notes ?? '' : ''
        }
        setScoreDraftByStudentId(scoreDraft)
        setNotesDraftByStudentId(notesDraft)
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Notlar alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId, componentId])

  useEffect(() => {
    load()
  }, [load])

  const handleSaveOne = useCallback(
    async (student) => {
      const token = getTeacherToken()
      if (!token || !componentId) return
      setSubmitError('')

      const sid = getStudentId(student)
      if (sid == null) { setSubmitError('Öğrenci ID bulunamadı.'); return }

      const existing = scoreByStudentId.get(sid)
      const scoreId = existing?.id ?? existing?.Id

      const raw = scoreDraftRef.current[sid]
      const score = parseMaybeNumber(raw)
      const notes = String(notesDraftRef.current[sid] ?? '').trim() || null

      if (score == null) {
        if (!scoreId) return
        setSaving(true)
        try {
          await deleteScore(token, scoreId)
          await load()
        } catch (e) {
          setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
        } finally {
          setSaving(false)
        }
        return
      }

      setSaving(true)
      try {
        if (scoreId) {
          await updateScore(token, scoreId, { score, notes })
        } else {
          await addScore(token, componentId, { externalStudentId: sid, score, notes })
        }
        await load()
      } catch (e) {
        setSubmitError(e instanceof Error ? e.message : 'Kaydedilemedi.')
      } finally {
        setSaving(false)
      }
    },
    [componentId, load, scoreByStudentId],
  )

  const handleDeleteOne = useCallback(
    async (student) => {
      const token = getTeacherToken()
      if (!token || !componentId) return
      const sid = getStudentId(student)
      const existing = scoreByStudentId.get(sid)
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
    [componentId, load, scoreByStudentId],
  )

  const handleSaveBulk = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !componentId || !offeringId) return

    const scoresItems = []
    for (const stu of students) {
      const sid = getStudentId(stu)
      if (sid == null) continue
      const score = parseMaybeNumber(scoreDraftRef.current[sid])
      if (score == null) continue
      const notes = String(notesDraftRef.current[sid] ?? '').trim() || null
      scoresItems.push({ externalStudentId: sid, score, notes })
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
  }, [componentId, students, load, offeringId])

  const columns = useMemo(
    () => [
      // API'den gelen alan adı: fullName (ASP.NET camelCase → fullName)
      columnHelper.accessor((row) => row?.fullName ?? row?.FullName ?? row?.studentFullName ?? '—', {
        id: 'fullName',
        header: 'Öğrenci',
      }),
      columnHelper.accessor((row) => row?.studentNumber ?? row?.StudentNumber ?? '—', {
        id: 'studentNumber',
        header: 'Numara',
      }),
      columnHelper.display({
        id: 'score',
        header: 'Puan',
        cell: ({ row }) => {
          const stu = row.original
          const sid = getStudentId(stu)
          const value = scoreDraftRef.current[sid] ?? ''
          return (
            <input
              type="text"
              inputMode="decimal"
              autoComplete="off"
              className={formStyles.input}
              style={{ minWidth: '8rem' }}
              placeholder="boş = girmedi"
              value={value}
              onChange={(e) =>
                setScoreDraftByStudentId((prev) => ({
                  ...prev,
                  [sid]: e.target.value,
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
          const stu = row.original
          const sid = getStudentId(stu)
          const value = notesDraftRef.current[sid] ?? ''
          return (
            <input
              type="text"
              autoComplete="off"
              className={formStyles.input}
              style={{ minWidth: '12rem' }}
              placeholder="İsteğe bağlı kısa not"
              value={value}
              onChange={(e) =>
                setNotesDraftByStudentId((prev) => ({
                  ...prev,
                  [sid]: e.target.value,
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
          const stu = row.original
          const sid = getStudentId(stu)
          const existing = scoreByStudentId.get(sid)
          const hasExisting = Boolean(existing?.id ?? existing?.Id)

          return (
            <div className={formStyles.rowActionGroup}>
              <button
                type="button"
                className={`${formStyles.btn} ${formStyles.btnPrimary}`}
                disabled={saving}
                onClick={() => handleSaveOne(stu)}
              >
                Kaydet
              </button>
              {hasExisting ? (
                <button
                  type="button"
                  className={`${formStyles.rowActionText} ${formStyles.rowActionTextDanger}`}
                  disabled={saving}
                  onClick={() => handleDeleteOne(stu)}
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
    [handleDeleteOne, handleSaveOne, saving, scoreByStudentId],
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
          disabled={saving || !students.length}
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
          data={students}
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
