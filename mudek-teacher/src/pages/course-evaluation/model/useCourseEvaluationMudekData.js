import { useCallback, useEffect, useMemo, useState } from 'react'

import {
  calculateMudekEvaluation,
  createEvaluation,
  fetchCourseCloPoMap,
  fetchCourseStudents,
  fetchEvaluation,
  fetchExams,
  fetchMyCourseDetail,
  fetchMudekResults,
  fetchOfferingClos,
  fetchOfferingProgramOutcomes,
} from '../../../shared/api/teacherApi'
import { getTeacherToken } from '../../../shared/lib/authToken'

export function useCourseEvaluationMudekData(offeringId) {
  const [evaluation, setEvaluation] = useState(null)
  const [courseDetail, setCourseDetail] = useState(null)
  const [students, setStudents] = useState([])
  const [clos, setClos] = useState([])
  const [programOutcomes, setProgramOutcomes] = useState([])
  const [exams, setExams] = useState([])
  const [cloPoMap, setCloPoMap] = useState([])

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const [mudekLoading, setMudekLoading] = useState(false)
  const [mudekError, setMudekError] = useState('')
  const [mudekResults, setMudekResults] = useState(null)
  const [mudekCalcRunning, setMudekCalcRunning] = useState(false)

  const evaluationId = evaluation?.id ?? evaluation?.Id ?? null

  const loadEvaluation = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    setLoading(true)
    setError('')
    fetchEvaluation(token, offeringId)
      .then((data) => setEvaluation(data ?? null))
      .catch((e) => {
        const msg = e instanceof Error ? e.message : ''
        const isMissing =
          msg.toLowerCase().includes('henüz') ||
          msg.toLowerCase().includes('bulunmadı') ||
          msg.toLowerCase().includes('oluşturulmamış')
        if (isMissing) {
          createEvaluation(token, offeringId, {
            studentFeedbackEvaluation: null,
            programOutcomeEvaluation: null,
            generalEvaluation: null,
            improvementSuggestions: null,
          })
            .then((created) => setEvaluation(created ?? null))
            .catch(() => setEvaluation(null))
            .finally(() => setLoading(false))
          return
        }
        setError(msg || 'Değerlendirme alınamadı.')
      })
      .finally(() => setLoading(false))
  }, [offeringId])

  const loadCourseDetail = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    fetchMyCourseDetail(token, offeringId)
      .then((d) => setCourseDetail(d ?? null))
      .catch(() => setCourseDetail(null))
  }, [offeringId])

  const loadMudek = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    setMudekLoading(true)
    setMudekError('')
    fetchMudekResults(token, offeringId)
      .then((data) => setMudekResults(data ?? null))
      .catch((e) => setMudekError(e instanceof Error ? e.message : 'MÜDEK sonuçları alınamadı.'))
      .finally(() => setMudekLoading(false))
  }, [offeringId])

  const runMudekCalculation = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    setMudekCalcRunning(true)
    setMudekError('')
    try {
      const data = await calculateMudekEvaluation(token, offeringId)
      setMudekResults(data ?? null)
    } catch (e) {
      setMudekError(e instanceof Error ? e.message : 'MÜDEK hesaplaması başarısız.')
    } finally {
      setMudekCalcRunning(false)
    }
  }, [offeringId])

  const loadLookups = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId) return

    const p1 = fetchCourseStudents(token, offeringId).catch(() => [])
    const p2 = fetchOfferingClos(token, offeringId).catch(() => [])
    const p3 = fetchOfferingProgramOutcomes(token, offeringId).catch(() => [])
    const p4 = evaluationId ? fetchExams(token, evaluationId).catch(() => []) : Promise.resolve([])

    Promise.all([p1, p2, p3, p4]).then(([studentsData, closData, programOutcomesData, examsData]) => {
      setStudents(Array.isArray(studentsData) ? studentsData : [])
      setClos(Array.isArray(closData) ? closData : [])
      setProgramOutcomes(Array.isArray(programOutcomesData) ? programOutcomesData : [])
      setExams(Array.isArray(examsData) ? examsData : [])
    })
  }, [evaluationId, offeringId])

  useEffect(() => {
    if (!offeringId) return
    void Promise.resolve().then(loadEvaluation)
  }, [loadEvaluation, offeringId])

  useEffect(() => {
    if (!offeringId) return
    void Promise.resolve().then(loadCourseDetail)
  }, [loadCourseDetail, offeringId])

  useEffect(() => {
    const token = getTeacherToken()
    const cid = courseDetail?.courseId ?? courseDetail?.CourseId
    if (!token || cid == null || cid === '') {
      setCloPoMap([])
      return undefined
    }
    const n = Number(cid)
    if (!Number.isFinite(n)) {
      setCloPoMap([])
      return undefined
    }
    let cancelled = false
    fetchCourseCloPoMap(token, n)
      .then((data) => {
        if (!cancelled) setCloPoMap(Array.isArray(data) ? data : [])
      })
      .catch(() => {
        if (!cancelled) setCloPoMap([])
      })
    return () => {
      cancelled = true
    }
  }, [courseDetail])

  useEffect(() => {
    if (!offeringId) return
    void Promise.resolve().then(loadMudek)
  }, [loadMudek, offeringId])

  useEffect(() => {
    if (!offeringId) return
    void Promise.resolve().then(loadLookups)
  }, [loadLookups, offeringId])

  const formatDate = useCallback((iso) => {
    if (!iso) return '—'
    try {
      return new Date(iso).toLocaleString('tr-TR')
    } catch {
      return String(iso)
    }
  }, [])

  const shortGuid = useCallback((v) => {
    const s = String(v ?? '')
    if (!s) return '—'
    if (s.length <= 12) return s
    return `${s.slice(0, 8)}…${s.slice(-4)}`
  }, [])

  const title = useMemo(() => {
    const pageTitle = 'Ders değerlendirme'
    const code = courseDetail?.courseCode ?? courseDetail?.CourseCode ?? ''
    const name = courseDetail?.courseName ?? courseDetail?.CourseName ?? ''
    const term = courseDetail?.termName ?? courseDetail?.TermName ?? ''
    const section = courseDetail?.section ?? courseDetail?.Section ?? ''

    const coursePart = code && name ? `${code} · ${name}` : code || name
    const metaPart = [term, section ? `Şube ${section}` : ''].filter(Boolean).join(' · ')

    const left = coursePart ? `${pageTitle} · ${coursePart}` : pageTitle
    if (metaPart) return `${left} (${metaPart})`
    return left
  }, [courseDetail])

  const mudekMeta = useMemo(() => {
    if (!mudekResults) return null
    const lastCalculatedAt = mudekResults.lastCalculatedAt ?? mudekResults.LastCalculatedAt ?? null
    const isCalculationDirty = mudekResults.isCalculationDirty ?? mudekResults.IsCalculationDirty ?? false
    return { lastCalculatedAt, isCalculationDirty }
  }, [mudekResults])

  const studentByEnrollmentId = useMemo(() => {
    const map = new Map()
    for (const s of students ?? []) {
      const enrollmentId = s.enrollmentId ?? s.EnrollmentId ?? s.id ?? s.Id
      if (!enrollmentId) continue
      const fullName = s.studentFullName ?? s.StudentFullName ?? s.fullName ?? s.FullName ?? ''
      const studentNumber = s.studentNumber ?? s.StudentNumber ?? ''
      const label = [fullName, studentNumber ? `(${studentNumber})` : ''].filter(Boolean).join(' ')
      map.set(String(enrollmentId), label || shortGuid(enrollmentId))
    }
    return map
  }, [shortGuid, students])

  const cloById = useMemo(() => {
    const map = new Map()
    for (const c of clos ?? []) {
      const id = c.id ?? c.Id
      if (!id) continue
      const code = c.code ?? c.Code ?? ''
      const desc = c.description ?? c.Description ?? ''
      const label = code ? (desc ? `${code} · ${desc}` : code) : desc
      map.set(String(id), label || shortGuid(id))
    }
    return map
  }, [clos, shortGuid])

  const programOutcomeById = useMemo(() => {
    const map = new Map()
    for (const p of programOutcomes ?? []) {
      const id = p.id ?? p.Id
      if (!id) continue
      const code = p.code ?? p.Code ?? ''
      const title = p.title ?? p.Title ?? ''
      const label = code && title ? `${code} — ${title}` : code || title
      map.set(String(id), label || shortGuid(id))
    }
    return map
  }, [programOutcomes, shortGuid])

  const examById = useMemo(() => {
    const map = new Map()
    for (const e of exams ?? []) {
      const id = e.id ?? e.Id
      if (!id) continue
      const examType = e.examType ?? e.ExamType ?? ''
      map.set(String(id), examType || shortGuid(id))
    }
    return map
  }, [exams, shortGuid])

  const mudekStudentResults = useMemo(() => {
    const list = mudekResults?.studentResults ?? mudekResults?.StudentResults ?? []
    if (!Array.isArray(list)) return []
    return list.map((x) => ({
      id: x.id ?? x.Id,
      enrollmentId: x.enrollmentId ?? x.EnrollmentId,
      midtermScore: x.midtermScore ?? x.MidtermScore,
      finalScore: x.finalScore ?? x.FinalScore,
      makeupScore: x.makeupScore ?? x.MakeupScore,
      usedExamType: x.usedExamType ?? x.UsedExamType,
      successGrade: x.successGrade ?? x.SuccessGrade,
      letterGrade: x.letterGrade ?? x.LetterGrade,
      isPassed: x.isPassed ?? x.IsPassed,
      includedInStatistics: x.includedInStatistics ?? x.IncludedInStatistics,
      updatedAt: x.updatedAt ?? x.UpdatedAt,
    }))
  }, [mudekResults])

  const sortedStudents = useMemo(() => {
    return [...mudekStudentResults].sort((a, b) => {
      const av = Number(a.successGrade ?? -Infinity)
      const bv = Number(b.successGrade ?? -Infinity)
      if (Number.isFinite(av) && Number.isFinite(bv)) return bv - av
      if (Number.isFinite(bv)) return 1
      if (Number.isFinite(av)) return -1
      return 0
    })
  }, [mudekStudentResults])

  const studentStats = useMemo(() => {
    const rows = mudekStudentResults
      .map((r) => ({
        id: r.id,
        successGrade: typeof r.successGrade === 'number' ? r.successGrade : Number(r.successGrade),
      }))
      .filter((x) => x.id && Number.isFinite(x.successGrade))

    if (!rows.length) return { bestId: null, worstId: null, avgId: null, mean: null }

    const sorted = [...rows].sort((a, b) => b.successGrade - a.successGrade)
    const bestId = sorted[0]?.id ?? null
    const worstId = sorted[sorted.length - 1]?.id ?? null
    const mean = sorted.reduce((acc, x) => acc + x.successGrade, 0) / sorted.length

    let avgId = sorted[0]?.id ?? null
    let bestDiff = Infinity
    for (const x of sorted) {
      const d = Math.abs(x.successGrade - mean)
      if (d < bestDiff) {
        bestDiff = d
        avgId = x.id
      }
    }
    return { bestId, worstId, avgId, mean }
  }, [mudekStudentResults])

  const letterGradeDistribution = useMemo(() => {
    const counts = new Map()
    let total = 0
    for (const r of mudekStudentResults) {
      if (!r?.includedInStatistics) continue
      const g = String(r.letterGrade ?? '—')
      counts.set(g, (counts.get(g) ?? 0) + 1)
      total += 1
    }
    const items = [...counts.entries()]
      .map(([grade, count]) => ({ grade, count, ratio: total ? count / total : 0 }))
      .sort((a, b) => b.count - a.count)
    return { total, items }
  }, [mudekStudentResults])

  const passRate = useMemo(() => {
    const rows = mudekStudentResults.filter((r) => r?.includedInStatistics)
    if (!rows.length) return null
    const passed = rows.filter((r) => r.isPassed).length
    return passed / rows.length
  }, [mudekStudentResults])

  const classAverage = useMemo(() => {
    const rows = mudekStudentResults
      .filter((r) => r?.includedInStatistics)
      .map((r) => Number(r.successGrade))
      .filter((x) => Number.isFinite(x))
    if (!rows.length) return null
    return rows.reduce((a, b) => a + b, 0) / rows.length
  }, [mudekStudentResults])

  return {
    evaluation,
    evaluationId,
    courseDetail,
    students,
    clos,
    programOutcomes,
    programOutcomeById,
    cloPoMap,
    exams,
    loading,
    error,
    mudekLoading,
    mudekError,
    mudekResults,
    mudekMeta,
    mudekCalcRunning,
    runMudekCalculation,
    title,
    formatDate,
    shortGuid,
    studentByEnrollmentId,
    cloById,
    examById,
    mudekStudentResults,
    sortedStudents,
    studentStats,
    letterGradeDistribution,
    passRate,
    classAverage,
  }
}

