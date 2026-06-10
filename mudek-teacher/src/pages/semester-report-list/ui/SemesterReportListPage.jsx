import { CheckCircle, FileText, Plus } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import {
  createSemesterReport,
  deleteSemesterReport,
  fetchSemesterReports,
} from '../../../shared/api/semesterReportApi'
import { fetchMyEvaluations } from '../../../shared/api/teacherApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import styles from './SemesterReportListPage.module.css'

function fv(obj, ...keys) {
  for (const k of keys) {
    const lo = k[0].toLowerCase() + k.slice(1)
    const up = k[0].toUpperCase() + k.slice(1)
    if (obj[lo] != null) return obj[lo]
    if (obj[up] != null) return obj[up]
  }
  return null
}

function StatusBadge({ status }) {
  const isReady = status === 'Ready' || status === 1
  return (
    <span className={isReady ? styles.badgeReady : styles.badgeDraft}>
      {isReady ? 'Hazır' : 'Taslak'}
    </span>
  )
}

export function SemesterReportListPage() {
  const navigate = useNavigate()

  const [reports, setReports] = useState([])
  const [evaluations, setEvaluations] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [createOpen, setCreateOpen] = useState(false)
  const [createOfferingId, setCreateOfferingId] = useState('')
  const [createTeacherName, setCreateTeacherName] = useState('')
  const [createTeacherTitle, setCreateTeacherTitle] = useState('')
  const [creating, setCreating] = useState(false)

  const load = useCallback(() => {
    const token = getTeacherToken()
    if (!token) return
    setLoading(true)
    setError('')
    Promise.all([fetchSemesterReports(token), fetchMyEvaluations(token)])
      .then(([rpts, evals]) => {
        setReports(Array.isArray(rpts) ? rpts : [])
        setEvaluations(Array.isArray(evals) ? evals : [])
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Veriler alınamadı.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    void Promise.resolve().then(load)
  }, [load])

  // Henüz raporu olmayan değerlendirmeler
  const availableEvals = useMemo(() => {
    const usedOfferingIds = new Set(reports.map((r) => fv(r, 'externalCourseOfferingId')))
    return evaluations.filter((e) => !usedOfferingIds.has(fv(e, 'externalCourseOfferingId')))
  }, [evaluations, reports])

  const selectedEval = useMemo(
    () => availableEvals.find((e) => String(fv(e, 'externalCourseOfferingId')) === createOfferingId) ?? null,
    [availableEvals, createOfferingId],
  )

  const handleOpenCreate = () => {
    const firstId = availableEvals.length === 1 ? String(fv(availableEvals[0], 'externalCourseOfferingId')) : ''
    setCreateOfferingId(firstId)
    setCreateTeacherName('')
    setCreateTeacherTitle('')
    setCreateOpen(true)
  }

  const handleCreate = async () => {
    const token = getTeacherToken()
    if (!token) return
    const offeringId = Number(createOfferingId)
    if (!offeringId) {
      setError('Ders seçimi zorunludur.')
      return
    }
    setCreating(true)
    setError('')
    try {
      const result = await createSemesterReport(token, {
        externalCourseOfferingId: offeringId,
        teacherName: createTeacherName.trim() || null,
        teacherTitle: createTeacherTitle.trim() || null,
      })
      const id = fv(result, 'id')
      setCreateOpen(false)
      if (id) navigate(`${appConfig.routes.semesterReports}/${id}`)
      else load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Rapor oluşturulamadı.')
    } finally {
      setCreating(false)
    }
  }

  const handleDelete = async (id) => {
    if (!window.confirm('Bu raporu silmek istediğinize emin misiniz? Yüklenen tüm dosyalar da silinecektir.')) return
    const token = getTeacherToken()
    if (!token) return
    try {
      await deleteSemesterReport(token, id)
      load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silinemedi.')
    }
  }

  return (
    <PageSection
      title={appConfig.pages.semesterReports.title}
      description={appConfig.pages.semesterReports.description}
      error={error}
      loading={loading}
    >
      <div className={styles.root}>
        <div className={styles.toolbar}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={handleOpenCreate}
            disabled={availableEvals.length === 0 && !loading}
          >
            <Plus size={16} aria-hidden />
            Yeni rapor
          </button>
          {!loading && availableEvals.length === 0 && evaluations.length > 0 && (
            <span className={styles.infoText}>
              Tüm ders değerlendirmeleriniz için rapor oluşturulmuş.
            </span>
          )}
          {!loading && evaluations.length === 0 && (
            <span className={styles.infoText}>
              Rapor oluşturmak için önce ders değerlendirmesi oluşturmanız gerekiyor.
            </span>
          )}
        </div>

        {!loading && reports.length > 0 && (
          <div className={styles.grid}>
            {reports.map((r) => {
              const id = fv(r, 'id') ?? ''
              const code = fv(r, 'courseCode') ?? ''
              const name = fv(r, 'courseName') ?? ''
              const term = fv(r, 'academicTermName') ?? ''
              const teacher = fv(r, 'teacherName') ?? ''
              const status = fv(r, 'status')
              const createdAt = fv(r, 'createdAt')
              const updatedAt = fv(r, 'updatedAt')
              const cardTitle = code && name ? `${code} — ${name}` : name || code || 'Rapor'

              return (
                <div key={id} className={styles.card}>
                  <div className={styles.cardTop}>
                    <StatusBadge status={status} />
                  </div>
                  <h3 className={styles.cardTitle}>{cardTitle}</h3>
                  <div className={styles.cardMeta}>
                    {term && <span>{term}</span>}
                    {teacher && <span>{teacher}</span>}
                    {updatedAt && (
                      <span>
                        Güncellendi: {new Date(updatedAt).toLocaleDateString('tr-TR')}
                      </span>
                    )}
                    {!updatedAt && createdAt && (
                      <span>
                        Oluşturuldu: {new Date(createdAt).toLocaleDateString('tr-TR')}
                      </span>
                    )}
                  </div>
                  <div className={styles.cardActions}>
                    <button
                      type="button"
                      className={styles.actionPrimary}
                      onClick={() => navigate(`${appConfig.routes.semesterReports}/${id}`)}
                    >
                      <FileText size={15} aria-hidden />
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className={styles.actionDanger}
                      onClick={() => handleDelete(id)}
                    >
                      Sil
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        )}

        {!loading && reports.length === 0 && (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon} aria-hidden>
              <CheckCircle size={26} strokeWidth={2} />
            </span>
            <h3 className={styles.emptyTitle}>Henüz rapor yok</h3>
            <p className={styles.emptyText}>
              &quot;Yeni rapor&quot; ile dönem sonu ders değerlendirme raporu oluşturun.
              Raporunuz MÜDEK komisyonuna göndermeden önce tüm bölümleri doldurmanız gerekecektir.
            </p>
          </div>
        )}
      </div>

      <AppDialog
        open={createOpen}
        title="Yeni dönem sonu raporu"
        onClose={() => setCreateOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnGhost}`}
              onClick={() => setCreateOpen(false)}
            >
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={creating || !createOfferingId}
              onClick={() => void handleCreate()}
            >
              {creating ? 'Oluşturuluyor…' : 'Oluştur'}
            </button>
          </div>
        }
      >
        <div className={formStyles.field}>
          <label htmlFor="sr-offering">Ders değerlendirmesi</label>
          <select
            id="sr-offering"
            className={formStyles.select}
            value={createOfferingId}
            onChange={(e) => setCreateOfferingId(e.target.value)}
          >
            <option value="">— Ders seçin —</option>
            {availableEvals.map((e) => {
              const offId = fv(e, 'externalCourseOfferingId')
              const code = fv(e, 'courseCode') ?? ''
              const name = fv(e, 'courseName') ?? ''
              const label = [code, name].filter(Boolean).join(' — ')
              return (
                <option key={offId} value={offId}>
                  {label || `Ders #${offId}`}
                </option>
              )
            })}
          </select>
        </div>

        {/* Seçilen dersin dönemi otomatik gösterilir */}
        {selectedEval && (
          <div className={styles.termInfo}>
            <span className={styles.termInfoLabel}>Akademik dönem</span>
            <span className={styles.termInfoValue}>
              {fv(selectedEval, 'academicTermName') ?? '—'}
            </span>
          </div>
        )}

        <div className={formStyles.field}>
          <label htmlFor="sr-teacher-title">Ünvan</label>
          <input
            id="sr-teacher-title"
            className={formStyles.input}
            value={createTeacherTitle}
            onChange={(e) => setCreateTeacherTitle(e.target.value)}
            placeholder="Dr. Öğr. Üyesi · Doç. Dr. · Prof. Dr."
          />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="sr-teacher-name">Adı Soyadı</label>
          <input
            id="sr-teacher-name"
            className={formStyles.input}
            value={createTeacherName}
            onChange={(e) => setCreateTeacherName(e.target.value)}
            placeholder="Ad Soyad"
          />
        </div>
      </AppDialog>
    </PageSection>
  )
}
