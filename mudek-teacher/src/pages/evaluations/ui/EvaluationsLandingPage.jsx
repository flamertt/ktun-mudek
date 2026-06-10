import { useCallback, useEffect, useMemo, useState } from 'react'
import { ClipboardCheck, Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

import {
  createEvaluation,
  fetchEvaluation,
  fetchMyCourses,
  fetchMyEvaluations,
  fetchTeacherAcademicTerms,
} from '../../../shared/api/teacherApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { academicTermRowId, academicTermRowLabel } from '../../../shared/lib/teacherAcademicTermMap'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import { RefreshIconButton } from '../../../shared/ui/refresh-icon-button/RefreshIconButton.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import styles from './EvaluationsLandingPage.module.css'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'

export function EvaluationsLandingPage() {
  const page = appConfig.pages.evaluations
  const navigate = useNavigate()

  const [rows, setRows] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [query, setQuery] = useState('')
  const [academicTerms, setAcademicTerms] = useState([])
  const [selectedTermId, setSelectedTermId] = useState('')
  const [allEvaluationsCount, setAllEvaluationsCount] = useState(null)

  const [createOpen, setCreateOpen] = useState(false)
  const [selectedOfferingIds, setSelectedOfferingIds] = useState([])
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')

  const [evalExistsByOfferingId, setEvalExistsByOfferingId] = useState({})
  const [checkingEvals, setCheckingEvals] = useState(false)

  const checkEvaluationExists = useCallback(async (token, offeringId) => {
    try {
      await fetchEvaluation(token, offeringId)
      return true
    } catch {
      return false
    }
  }, [])

  useEffect(() => {
    const token = getTeacherToken()
    if (!token) return undefined
    let cancelled = false
    fetchTeacherAcademicTerms(token)
      .then((data) => {
        if (cancelled || !Array.isArray(data)) return
        setAcademicTerms(
          [...data].sort((a, b) => Number(academicTermRowId(b)) - Number(academicTermRowId(a))),
        )
      })
      .catch(() => {
        if (!cancelled) setAcademicTerms([])
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    const token = getTeacherToken()
    if (!token) return
    fetchMyEvaluations(token)
      .then((data) => setAllEvaluationsCount(Array.isArray(data) ? data.length : 0))
      .catch(() => setAllEvaluationsCount(null))
  }, [])

  const runPool = useCallback(async (items, concurrency, worker) => {
    const results = new Array(items.length)
    let idx = 0
    const runners = new Array(Math.min(concurrency, items.length)).fill(null).map(async () => {
      while (idx < items.length) {
        const current = idx
        idx += 1
        results[current] = await worker(items[current], current)
      }
    })
    await Promise.all(runners)
    return results
  }, [])

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token) return
    setLoading(true)
    setError('')
    fetchMyCourses(token, selectedTermId)
      .then(async (data) => {
        const list = Array.isArray(data) ? data : []
        setRows(list)

        // `my-courses` bazen "hasEvaluation" bayrağını eksik/uyumsuz döndürüyor.
        // Bu yüzden offering bazlı `GET /my-courses/{offeringId}/evaluation` ile var/yok kontrolü yapıyoruz.
        const offeringIds = list
          .map(
            (r) =>
              r?.externalCourseOfferingId ??
              r?.ExternalCourseOfferingId ??
              r?.id ??
              r?.Id ??
              r?.courseOfferingId ??
              r?.CourseOfferingId ??
              r?.offeringId ??
              r?.OfferingId ??
              '',
          )
          .filter(Boolean)

        if (!offeringIds.length) {
          setEvalExistsByOfferingId({})
          return
        }

        setCheckingEvals(true)
        try {
          const pairs = await runPool(offeringIds, 6, async (offeringId) => {
            const exists = await checkEvaluationExists(token, offeringId)
            return [offeringId, exists]
          })
          const next = {}
          for (const p of pairs) {
            if (!p) continue
            const [offeringId, exists] = p
            next[offeringId] = Boolean(exists)
          }
          setEvalExistsByOfferingId(next)
        } finally {
          setCheckingEvals(false)
        }
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Dersler alınamadı.'))
      .finally(() => setLoading(false))
  }, [checkEvaluationExists, runPool, selectedTermId])

  useEffect(() => {
    void Promise.resolve().then(load)
  }, [load])

  const normalizedRows = useMemo(() => {
    return (rows ?? [])
      .map((r) => {
        const id =
          r?.externalCourseOfferingId ??
          r?.ExternalCourseOfferingId ??
          r?.id ??
          r?.Id ??
          r?.courseOfferingId ??
          r?.CourseOfferingId ??
          r?.offeringId ??
          r?.OfferingId ??
          ''
        const courseCode = r?.courseCode ?? r?.CourseCode ?? ''
        const courseName = r?.courseName ?? r?.CourseName ?? ''
        const termName = r?.termName ?? r?.TermName ?? ''
        const section = r?.section ?? r?.Section ?? ''
        const teacherName = r?.teacherName ?? r?.TeacherName ?? ''
        const enrolledCount = r?.enrolledCount ?? r?.EnrolledCount ?? 0
        const isActive = r?.isActive ?? r?.IsActive ?? false
        const hasEvaluation =
          r?.hasEvaluation ??
          r?.HasEvaluation ??
          r?.evaluationExists ??
          r?.EvaluationExists ??
          evalExistsByOfferingId?.[id] ??
          false

        return {
          ...r,
          id,
          courseCode,
          courseName,
          termName,
          section,
          teacherName,
          enrolledCount,
          isActive,
          hasEvaluation,
        }
      })
      .filter((r) => Boolean(r.id))
  }, [evalExistsByOfferingId, rows])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    const evalRows = normalizedRows.filter((r) => r.hasEvaluation)
    if (!q) return evalRows
    return evalRows.filter((r) => {
      const hay = `${r.courseCode ?? ''} ${r.courseName ?? ''} ${r.termName ?? ''} ${r.section ?? ''} ${r.teacherName ?? ''}`.toLowerCase()
      return hay.includes(q)
    })
  }, [normalizedRows, query])

  const openCreate = useCallback(() => {
    setSelectedOfferingIds([])
    setCreateError('')
    setCreateOpen(true)
  }, [])

  const handleCreate = useCallback(async () => {
    const token = getTeacherToken()
    if (!token) return
    const ids = selectedOfferingIds.filter(Boolean)
    if (!ids.length) {
      setCreateError('Lütfen en az bir ders açılışı seç.')
      return
    }

    setCreating(true)
    setCreateError('')
    try {
      for (const offeringId of ids) {
        await createEvaluation(token, offeringId, {
          studentFeedbackEvaluation: null,
          programOutcomeEvaluation: null,
          generalEvaluation: null,
          improvementSuggestions: null,
        })
      }
      setCreateOpen(false)
      await load()
      navigate(`/evaluations/${ids[0]}`)
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : 'Değerlendirme oluşturulamadı.')
    } finally {
      setCreating(false)
    }
  }, [load, navigate, selectedOfferingIds])

  const coursesWithoutEvaluation = useMemo(() => {
    return normalizedRows.filter((r) => !r.hasEvaluation)
  }, [normalizedRows])

  const toggleSelected = useCallback((id) => {
    if (!id) return
    setSelectedOfferingIds((prev) => {
      if (prev.includes(id)) return prev.filter((x) => x !== id)
      return [...prev, id]
    })
  }, [])

  return (
    <PageSection title={page.title} description={page.description} error={error}>
      <div className={styles.root}>
        <div className={styles.toolbar}>
          {academicTerms.length ? (
            <label className={styles.searchLabel} htmlFor="eval-course-term">
              Akademik dönem
              <select
                id="eval-course-term"
                className={sectionStyles.select}
                value={selectedTermId}
                onChange={(e) => setSelectedTermId(e.target.value)}
              >
                <option value="">Aktif dönem (üniversite)</option>
                {academicTerms.map((t) => {
                  const tid = academicTermRowId(t)
                  if (!tid) return null
                  return (
                    <option key={tid} value={tid}>
                      {academicTermRowLabel(t)}
                    </option>
                  )
                })}
              </select>
            </label>
          ) : null}
          <div className={styles.search}>
            <label className={styles.searchLabel} htmlFor="eval-course-search">
              Ders ara
              <input
                id="eval-course-search"
                className={styles.searchInput}
                type="search"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Ders kodu, ders adı, dönem…"
              />
            </label>
          </div>

          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
            <RefreshIconButton onClick={load} loading={loading} title="Yenile" />
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              onClick={openCreate}
              disabled={creating}
              title={
                coursesWithoutEvaluation.length
                  ? 'Derslerimden seçip değerlendirme oluştur'
                  : 'Tüm ders açılışlarında değerlendirme mevcut'
              }
            >
              <Plus size={18} aria-hidden />
              Değerlendirme oluştur
            </button>
            <span className={sectionStyles.muted} style={{ fontSize: '0.85rem' }}>
              {filtered.length} ders (seçili dönem)
              {allEvaluationsCount != null ? ` · veritabanında toplam ${allEvaluationsCount} değerlendirme` : ''}
            </span>
          </div>
        </div>

        {loading ? <p className={sectionStyles.muted}>Değerlendirmeler yükleniyor…</p> : null}
        {!loading && checkingEvals ? (
          <p className={sectionStyles.muted}>Değerlendirme durumları kontrol ediliyor…</p>
        ) : null}

        {!loading && filtered.length ? (
          <div className={styles.grid}>
            {filtered.map((r) => (
              <div key={r.id} className={styles.card}>
                <div className={styles.cardTop}>
                  <div className={styles.cardHeaderRow}>
                    <div className={styles.codeBadge}>{r.courseCode}</div>
                  </div>
                  <div className={styles.cardTitle}>{r.courseName}</div>
                  <p className={styles.cardSub}>
                    {r.termName} · Şube {r.section} {r.teacherName ? `· ${r.teacherName}` : ''}
                  </p>
                </div>

                <div className={styles.cardBottom}>
                  <div className={styles.metric}>
                    <div className={styles.metricValue}>{r.enrolledCount ?? 0}</div>
                    <div className={styles.metricLabel}>Kayıtlı öğrenci</div>
                  </div>
                  <button
                    type="button"
                    className={styles.openBtn}
                    onClick={() => navigate(`/evaluations/${r.id}`)}
                    title="Değerlendirmeyi aç"
                  >
                    <ClipboardCheck size={16} aria-hidden />
                    Değerlendirmeyi aç
                  </button>
                </div>
              </div>
            ))}
          </div>
        ) : null}

        {!loading && !filtered.length ? (
          <div className={styles.empty}>
            Henüz değerlendirmen yok. Üstteki <b>Değerlendirme oluştur</b> butonundan ders seçip oluşturabilirsin.
          </div>
        ) : null}
      </div>

      <AppDialog
        open={createOpen}
        title="Değerlendirme oluştur"
        onClose={() => {
          if (!creating) setCreateOpen(false)
        }}
        size="md"
        footer={
          <>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnGhost}`}
              onClick={() => setCreateOpen(false)}
              disabled={creating}
            >
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              onClick={handleCreate}
              disabled={creating || selectedOfferingIds.length === 0}
            >
              {creating ? 'Oluşturuluyor…' : 'Oluştur'}
            </button>
          </>
        }
      >
        {createError ? (
          <p className={sectionStyles.error} role="alert">
            {createError}
          </p>
        ) : null}
        <p className={sectionStyles.muted} style={{ margin: 0 }}>
          Değerlendirme oluşturmak istediğin ders(ler)i seç.
        </p>

        {coursesWithoutEvaluation.length ? (
          <div className={styles.modalList} style={{ marginTop: '0.75rem' }}>
            {coursesWithoutEvaluation.map((c) => {
              const checked = selectedOfferingIds.includes(c.id)
              return (
                <label key={c.id} className={styles.courseOption}>
                  <input
                    type="checkbox"
                    className={formStyles.check}
                    checked={checked}
                    onChange={() => toggleSelected(c.id)}
                    disabled={creating}
                  />
                  <div className={styles.courseMain}>
                    <div className={styles.courseName}>
                      {c.courseCode} · {c.courseName}
                    </div>
                    <div className={styles.courseMeta}>
                      {c.termName} · Şube {c.section} · Öğrenci: {c.enrolledCount ?? 0}
                    </div>
                  </div>
                </label>
              )
            })}
          </div>
        ) : (
          <p className={sectionStyles.muted} style={{ marginTop: '0.75rem' }}>
            Değerlendirme oluşturulacak ders bulunamadı.
          </p>
        )}
      </AppDialog>
    </PageSection>
  )
}

