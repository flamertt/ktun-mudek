import { ArrowLeft, BarChart3, ClipboardList, Plus } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import {
  createTeacherSurvey,
  deleteTeacherSurvey,
  fetchMyCourseDetail,
  fetchTeacherSurveys,
  toggleTeacherSurveyActive,
} from '../../../shared/api/teacherApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import styles from './CourseSurveysPage.module.css'

function sid(x) {
  return x?.id ?? x?.Id ?? ''
}

export function CourseSurveysPage() {
  const { offeringId } = useParams()
  const navigate = useNavigate()

  const [course, setCourse] = useState(null)
  const [surveys, setSurveys] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [createOpen, setCreateOpen] = useState(false)
  const [createTitle, setCreateTitle] = useState('')
  const [createDesc, setCreateDesc] = useState('')
  const [createActive, setCreateActive] = useState(true)
  const [creating, setCreating] = useState(false)

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    setLoading(true)
    setError('')
    Promise.all([fetchMyCourseDetail(token, offeringId), fetchTeacherSurveys(token, offeringId)])
      .then(([detail, list]) => {
        setCourse(detail ?? null)
        setSurveys(Array.isArray(list) ? list : [])
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Veriler alınamadı.'))
      .finally(() => setLoading(false))
  }, [offeringId])

  useEffect(() => {
    void Promise.resolve().then(load)
  }, [load])

  const title = useMemo(() => {
    if (!course) return 'Ders anketleri'
    const code = course.courseCode ?? course.CourseCode ?? ''
    const name = course.courseName ?? course.CourseName ?? ''
    return code && name ? `${code} — ${name}` : 'Ders anketleri'
  }, [course])

  const handleCreate = async () => {
    const token = getTeacherToken()
    if (!token || !offeringId) return
    const t = createTitle.trim()
    if (!t) {
      setError('Anket başlığı zorunludur.')
      return
    }
    setCreating(true)
    setError('')
    try {
      await createTeacherSurvey(token, {
        courseOfferingId: offeringId,
        title: t,
        description: createDesc.trim() || null,
        isActive: createActive,
      })
      setCreateOpen(false)
      setCreateTitle('')
      setCreateDesc('')
      setCreateActive(true)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Anket oluşturulamadı.')
    } finally {
      setCreating(false)
    }
  }

  const handleToggle = async (surveyId) => {
    const token = getTeacherToken()
    if (!token) return
    try {
      await toggleTeacherSurveyActive(token, surveyId)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Durum değiştirilemedi.')
    }
  }

  const handleDelete = async (surveyId) => {
    if (!window.confirm('Bu anketi silmek istediğinize emin misiniz?')) return
    const token = getTeacherToken()
    if (!token) return
    try {
      await deleteTeacherSurvey(token, surveyId)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silinemedi.')
    }
  }

  return (
    <PageSection
      title={title}
      description="Bu ders açılışı için anketleri yönetin; soruları düzenleme ve sonuçları ayrı sayfalarda açabilirsiniz."
      error={error}
      loading={loading}
    >
      <div className={styles.root}>
        <div className={styles.toolbar}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnGhost}`}
            onClick={() => navigate(appConfig.routes.surveyCreate)}
          >
            <ArrowLeft size={16} aria-hidden />
            Anketlerim
          </button>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={() => setCreateOpen(true)}
          >
            <Plus size={16} aria-hidden />
            Yeni anket
          </button>
        </div>

        {!loading && surveys.length ? (
          <div className={styles.grid}>
            {surveys.map((s) => {
              const id = sid(s)
              const st = s?.title ?? s?.Title ?? '—'
              const active = s?.isActive ?? s?.IsActive ?? false
              const qCount = s?.questionCount ?? s?.QuestionCount ?? 0
              const subCount = s?.submissionCount ?? s?.SubmissionCount ?? 0
              return (
                <div key={id} className={styles.card}>
                  <div className={styles.cardTop}>
                    <span className={active ? styles.badgeOn : styles.badgeOff}>
                      {active ? 'Aktif' : 'Pasif'}
                    </span>
                  </div>
                  <h3 className={styles.cardTitle}>{st}</h3>
                  <div className={styles.cardMeta} aria-label="Anket özeti">
                    <span>{qCount} soru</span>
                    <span>{subCount} gönderim</span>
                  </div>
                  <div className={styles.cardActions}>
                    <button
                      type="button"
                      className={styles.actionPrimary}
                      onClick={() => navigate(`${appConfig.routes.surveyCreate}/${offeringId}/${id}`)}
                    >
                      <ClipboardList size={16} aria-hidden />
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className={styles.actionPrimary}
                      onClick={() => navigate(`${appConfig.routes.surveyCreate}/${offeringId}/${id}/results`)}
                    >
                      <BarChart3 size={16} aria-hidden />
                      Sonuçlar
                    </button>
                    <button type="button" className={styles.actionSecondary} onClick={() => handleToggle(id)}>
                      {active ? 'Pasifleştir' : 'Aktifleştir'}
                    </button>
                    <button type="button" className={styles.actionDanger} onClick={() => handleDelete(id)}>
                      Sil
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        ) : null}

        {!loading && !surveys.length ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon} aria-hidden>
              <ClipboardList size={26} strokeWidth={2} />
            </span>
            <h3 className={styles.emptyTitle}>Henüz anket yok</h3>
            <p className={styles.emptyText}>
              &quot;Yeni anket&quot; ile başlayın; ardından soruları ekleyip anketi aktifleştirebilirsiniz.
            </p>
          </div>
        ) : null}
      </div>

      <AppDialog
        open={createOpen}
        title="Yeni anket"
        onClose={() => setCreateOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setCreateOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={creating}
              onClick={() => void handleCreate()}
            >
              {creating ? 'Kaydediliyor…' : 'Oluştur'}
            </button>
          </div>
        }
      >
        <div className={formStyles.field}>
          <label htmlFor="sv-title">Başlık</label>
          <input
            id="sv-title"
            className={formStyles.input}
            value={createTitle}
            onChange={(e) => setCreateTitle(e.target.value)}
            placeholder="Örn. Dönem sonu ders anketi"
          />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="sv-desc">Açıklama (isteğe bağlı)</label>
          <textarea
            id="sv-desc"
            className={formStyles.textarea}
            rows={3}
            value={createDesc}
            onChange={(e) => setCreateDesc(e.target.value)}
          />
        </div>
        <div className={formStyles.checkRow}>
          <input
            id="sv-create-active"
            type="checkbox"
            className={formStyles.check}
            checked={createActive}
            onChange={(e) => setCreateActive(e.target.checked)}
          />
          <label htmlFor="sv-create-active">Oluşturulduğunda aktif olsun</label>
        </div>
      </AppDialog>
    </PageSection>
  )
}
