import { ArrowLeft, BarChart3, ListOrdered, Pencil, Plus, Settings2, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import {
  addTeacherSurveyQuestion,
  deleteTeacherSurveyQuestion,
  fetchOfferingClos,
  fetchTeacherSurveyDetail,
  updateTeacherSurvey,
  updateTeacherSurveyQuestion,
} from '../../../shared/api/teacherApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import styles from './TeacherSurveyDetailPage.module.css'

function gid(x) {
  return x?.id ?? x?.Id ?? ''
}

function gstr(x, a, b) {
  return x?.[a] ?? x?.[b] ?? ''
}

export function TeacherSurveyDetailPage() {
  const { offeringId, surveyId } = useParams()
  const navigate = useNavigate()

  const [detail, setDetail] = useState(null)
  const [clos, setClos] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [savingMeta, setSavingMeta] = useState(false)

  const [qOpen, setQOpen] = useState(false)
  const [editQ, setEditQ] = useState(null)
  const [qText, setQText] = useState('')
  const [qOrder, setQOrder] = useState(1)
  const [qRequired, setQRequired] = useState(true)
  const [qMin, setQMin] = useState(0)
  const [qMax, setQMax] = useState(5)
  const [qClo, setQClo] = useState('')
  const [qSaving, setQSaving] = useState(false)

  const loadDetail = useCallback(() => {
    const token = getTeacherToken()
    if (!token || !surveyId || !offeringId) return
    setLoading(true)
    setError('')
    Promise.all([fetchTeacherSurveyDetail(token, surveyId), fetchOfferingClos(token, offeringId)])
      .then(([d, c]) => {
        setDetail(d ?? null)
        setClos(Array.isArray(c) ? c : [])
        const dd = d ?? {}
        setTitle(gstr(dd, 'title', 'Title'))
        setDescription(gstr(dd, 'description', 'Description') ?? '')
        setIsActive(dd?.isActive ?? dd?.IsActive ?? true)
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Anket yüklenemedi.'))
      .finally(() => setLoading(false))
  }, [offeringId, surveyId])

  useEffect(() => {
    void Promise.resolve().then(loadDetail)
  }, [loadDetail])

  const questions = useMemo(() => {
    const qs = detail?.questions ?? detail?.Questions ?? []
    return Array.isArray(qs) ? [...qs].sort((a, b) => (a?.orderIndex ?? a?.OrderIndex ?? 0) - (b?.orderIndex ?? b?.OrderIndex ?? 0)) : []
  }, [detail])

  const openNewQuestion = () => {
    setEditQ(null)
    setQText('')
    setQOrder((questions?.length ?? 0) + 1)
    setQRequired(true)
    setQMin(0)
    setQMax(5)
    setQClo('')
    setQOpen(true)
  }

  const openEditQuestion = (q) => {
    setEditQ(q)
    setQText(gstr(q, 'text', 'Text'))
    setQOrder(q?.orderIndex ?? q?.OrderIndex ?? 1)
    setQRequired(q?.isRequired ?? q?.IsRequired ?? true)
    setQMin(q?.scaleMin ?? q?.ScaleMin ?? 0)
    setQMax(q?.scaleMax ?? q?.ScaleMax ?? 5)
    const cid = q?.courseLearningOutcomeId ?? q?.CourseLearningOutcomeId
    setQClo(cid ? String(cid) : '')
    setQOpen(true)
  }

  const saveMeta = async () => {
    const token = getTeacherToken()
    if (!token || !surveyId) return
    const t = title.trim()
    if (!t) {
      setError('Başlık zorunludur.')
      return
    }
    setSavingMeta(true)
    setError('')
    try {
      const body = {
        id: surveyId,
        title: t,
        description: description.trim() || null,
        isActive,
      }
      const updated = await updateTeacherSurvey(token, surveyId, body)
      setDetail(updated)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kaydedilemedi.')
    } finally {
      setSavingMeta(false)
    }
  }

  const saveQuestion = async () => {
    const token = getTeacherToken()
    if (!token || !surveyId) return
    const txt = qText.trim()
    if (!txt) {
      setError('Soru metni zorunludur.')
      return
    }
    setQSaving(true)
    setError('')
    try {
      if (editQ) {
        const qid = gid(editQ)
        const body = {
          id: qid,
          text: txt,
          orderIndex: Number(qOrder) || 1,
          isRequired: qRequired,
          scaleMin: Number(qMin) || 0,
          scaleMax: Number(qMax) || 5,
          courseLearningOutcomeId: qClo ? qClo : null,
        }
        await updateTeacherSurveyQuestion(token, surveyId, qid, body)
      } else {
        const body = {
          surveyId,
          text: txt,
          orderIndex: Number(qOrder) || 1,
          isRequired: qRequired,
          scaleMin: Number(qMin) || 0,
          scaleMax: Number(qMax) || 5,
          courseLearningOutcomeId: qClo ? qClo : null,
        }
        await addTeacherSurveyQuestion(token, surveyId, body)
      }
      setQOpen(false)
      loadDetail()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Soru kaydedilemedi.')
    } finally {
      setQSaving(false)
    }
  }

  const removeQuestion = async (q) => {
    if (!window.confirm('Bu soruyu silmek istediğinize emin misiniz?')) return
    const token = getTeacherToken()
    const qid = gid(q)
    if (!token || !surveyId || !qid) return
    try {
      await deleteTeacherSurveyQuestion(token, surveyId, qid)
      loadDetail()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silinemedi.')
    }
  }

  const pageTitle = useMemo(() => {
    const t = detail?.title ?? detail?.Title ?? 'Anket'
    return t
  }, [detail])

  return (
    <PageSection
      title={pageTitle}
      description="Öğrencilerin göreceği başlık ve açıklamayı düzenleyin; Likert sorularını ekleyip sıralayın."
      error={error}
      loading={loading}
    >
      <div className={styles.root}>
        <div className={styles.toolbar}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnGhost}`}
            onClick={() =>
              navigate(offeringId ? `${appConfig.routes.surveyCreate}/${offeringId}` : '/courses')
            }
          >
            <ArrowLeft size={16} aria-hidden />
            Anket listesi
          </button>
          {offeringId && surveyId ? (
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnGhost}`}
              onClick={() =>
                navigate(`${appConfig.routes.surveyCreate}/${offeringId}/${surveyId}/results`)
              }
            >
              <BarChart3 size={16} aria-hidden />
              Sonuçları görüntüle
            </button>
          ) : null}
        </div>

        {!loading && detail ? (
          <>
            <div className={styles.panel}>
              <div className={styles.panelHead}>
                <div className={styles.panelHeadMain}>
                  <span className={styles.panelIcon} aria-hidden>
                    <Settings2 size={20} strokeWidth={2.25} />
                  </span>
                  <div>
                    <h3 className={styles.panelTitle}>Anket bilgileri</h3>
                    <p className={styles.panelSub}>Başlık ve kısa açıklama öğrenci arayüzünde üst bölümde görünür.</p>
                  </div>
                </div>
              </div>
              <div className={formStyles.field}>
                <label htmlFor="sv-t">Başlık</label>
                <input id="sv-t" className={formStyles.input} value={title} onChange={(e) => setTitle(e.target.value)} />
              </div>
              <div className={formStyles.field}>
                <label htmlFor="sv-d">Açıklama</label>
                <textarea
                  id="sv-d"
                  className={formStyles.textarea}
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Örn. Bu anket dönem sonu geri bildirimi içindir."
                />
              </div>
              <div className={formStyles.checkRow}>
                <input
                  id="sv-active"
                  type="checkbox"
                  className={formStyles.check}
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                />
                <label htmlFor="sv-active">Öğrencilere açık (aktif)</label>
              </div>
              <div className={formStyles.actions}>
                <button
                  type="button"
                  className={`${formStyles.btn} ${formStyles.btnPrimary}`}
                  disabled={savingMeta}
                  onClick={() => void saveMeta()}
                >
                  {savingMeta ? 'Kaydediliyor…' : 'Bilgileri kaydet'}
                </button>
              </div>
            </div>

            <div className={styles.panel}>
              <div className={styles.panelHead}>
                <div className={styles.panelHeadMain}>
                  <span className={styles.panelIcon} aria-hidden>
                    <ListOrdered size={20} strokeWidth={2.25} />
                  </span>
                  <div>
                    <h3 className={styles.panelTitle}>Likert soruları</h3>
                    <p className={styles.panelSub}>
                      Her soru için ölçek uçları ve isteğe bağlı DÖÇ eşlemesi tanımlayın.
                    </p>
                  </div>
                </div>
                <button
                  type="button"
                  className={`${formStyles.btn} ${formStyles.btnPrimary}`}
                  onClick={openNewQuestion}
                >
                  <Plus size={17} aria-hidden />
                  Soru ekle
                </button>
              </div>
              {questions.length ? (
                <ul className={styles.qList}>
                  {questions.map((q) => {
                    const qid = gid(q)
                    const clo = q?.cloCode ?? q?.CloCode
                    const req = q?.isRequired ?? q?.IsRequired
                    return (
                      <li key={qid} className={styles.qItem}>
                        <div className={styles.qBody}>
                          <div className={styles.qMain}>
                            <span className={styles.qIdx}>{q?.orderIndex ?? q?.OrderIndex ?? '—'}.</span>
                            <span>{gstr(q, 'text', 'Text')}</span>
                          </div>
                          <div className={styles.qMeta}>
                            <span className={styles.qChip}>
                              Ölçek {q?.scaleMin ?? q?.ScaleMin ?? 0}–{q?.scaleMax ?? q?.ScaleMax ?? 5}
                            </span>
                            <span className={styles.qChip}>{req ? 'Zorunlu' : 'İsteğe bağlı'}</span>
                            {clo ? <span className={styles.qChip}>{clo}</span> : null}
                          </div>
                        </div>
                        <div className={styles.qActions}>
                          <button
                            type="button"
                            className={styles.iconBtn}
                            onClick={() => openEditQuestion(q)}
                            title="Düzenle"
                            aria-label="Soruyu düzenle"
                          >
                            <Pencil size={17} aria-hidden />
                          </button>
                          <button
                            type="button"
                            className={styles.iconBtnDanger}
                            onClick={() => void removeQuestion(q)}
                            title="Sil"
                            aria-label="Soruyu sil"
                          >
                            <Trash2 size={17} aria-hidden />
                          </button>
                        </div>
                      </li>
                    )
                  })}
                </ul>
              ) : (
                <p className={styles.emptyHint}>
                  Henüz soru eklenmedi. Öğrenciler anketi görebilse bile yanıtlayamazlar — &quot;Soru ekle&quot; ile
                  başlayın.
                </p>
              )}
            </div>

          </>
        ) : null}
      </div>

      <AppDialog
        open={qOpen}
        title={editQ ? 'Soruyu düzenle' : 'Yeni Likert sorusu'}
        onClose={() => setQOpen(false)}
        size="lg"
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setQOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={qSaving}
              onClick={() => void saveQuestion()}
            >
              {qSaving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        }
      >
        <div className={styles.dialogForm}>
          <p className={styles.dialogHint}>
            Öğrenciler bu soruyu verdiğiniz tam sayı ölçeği üzerinden değerlendirir (örn. 0–5). İsteğe bağlı sorularda 0
            genelde &quot;cevapsız&quot; anlamına gelir.
          </p>
          <div className={formStyles.field}>
            <label htmlFor="qq-text">Soru metni</label>
            <textarea
              id="qq-text"
              className={formStyles.textarea}
              rows={3}
              value={qText}
              onChange={(e) => setQText(e.target.value)}
            />
          </div>
          <div className={styles.row2}>
            <div className={formStyles.field}>
              <label htmlFor="qq-ord">Sıra</label>
              <input
                id="qq-ord"
                type="number"
                className={formStyles.input}
                value={qOrder}
                onChange={(e) => setQOrder(Number(e.target.value))}
                min={1}
              />
            </div>
            <div className={styles.checkField}>
              <span className={styles.checkFieldLabel}>Zorunluluk</span>
              <div className={formStyles.checkRow}>
                <input
                  id="qq-req"
                  type="checkbox"
                  className={formStyles.check}
                  checked={qRequired}
                  onChange={(e) => setQRequired(e.target.checked)}
                />
                <label htmlFor="qq-req">Zorunlu soru</label>
              </div>
            </div>
          </div>
          <div className={styles.row2}>
            <div className={formStyles.field}>
              <label htmlFor="qq-min">Ölçek min</label>
              <input
                id="qq-min"
                type="number"
                className={formStyles.input}
                value={qMin}
                onChange={(e) => setQMin(Number(e.target.value))}
                min={0}
                max={9}
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="qq-max">Ölçek max</label>
              <input
                id="qq-max"
                type="number"
                className={formStyles.input}
                value={qMax}
                onChange={(e) => setQMax(Number(e.target.value))}
                min={1}
                max={10}
              />
            </div>
          </div>
          <div className={formStyles.field}>
            <label htmlFor="qq-clo">DÖÇ eşlemesi (isteğe bağlı)</label>
            <select id="qq-clo" className={formStyles.input} value={qClo} onChange={(e) => setQClo(e.target.value)}>
              <option value="">— Yok —</option>
              {clos.map((c) => {
                const id = gid(c)
                const code = gstr(c, 'code', 'Code')
                const desc = (gstr(c, 'description', 'Description') || '').slice(0, 80)
                return (
                  <option key={id} value={id}>
                    {code} {desc ? `— ${desc}` : ''}
                  </option>
                )
              })}
            </select>
          </div>
        </div>
      </AppDialog>
    </PageSection>
  )
}
