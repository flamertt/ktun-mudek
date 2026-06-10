import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  FileUp,
  Loader2,
  Save,
  Trash2,
} from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import { API_BASE_URL } from '../../../shared/api/httpClient.js'
import {
  deleteReportFile,
  deleteWeeklyResource,
  downloadReportFile,
  fetchCloNotes,
  fetchPloNotes,
  fetchReportFiles,
  fetchReportPreview,
  fetchSemesterReport,
  fetchStudentSamples,
  fetchWeeklyResources,
  updateSemesterReport,
  uploadReportFile,
  upsertCloNote,
  upsertPloNote,
  upsertWeeklyResource,
  validateReport,
} from '../../../shared/api/semesterReportApi'
import { appConfig } from '../../../shared/config/appConfig'
import { getTeacherToken } from '../../../shared/lib/authToken'
import { AppDialog } from '../../../shared/ui/dialog/AppDialog.jsx'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import formStyles from '../../../shared/ui/admin-form/AdminForm.module.css'
import styles from './SemesterReportDetailPage.module.css'

// ────────────────────────────────────────────────────────────────────
// Yardımcılar
// ────────────────────────────────────────────────────────────────────

function fv(obj, ...keys) {
  if (!obj) return null
  for (const k of keys) {
    const lo = k[0].toLowerCase() + k.slice(1)
    const up = k[0].toUpperCase() + k.slice(1)
    if (obj[lo] != null) return obj[lo]
    if (obj[up] != null) return obj[up]
  }
  return null
}

function pct(v) {
  if (v == null) return '—'
  return `%${Number(v).toFixed(1)}`
}

function statusLabel(s) {
  if (s === 'Ready' || s === 1) return 'Hazır'
  return 'Taslak'
}

function achievementClass(score) {
  if (score == null) return ''
  if (score >= 70) return styles.scoreGood
  if (score >= 60) return styles.scoreWarn
  return styles.scoreBad
}

const SECTION_CATEGORIES = {
  ExamPaper: 'Sınav Soruları',
  AnswerKey: 'Cevap Anahtarı',
  StudentPaper_High: 'Öğrenci Kağıdı (En Yüksek)',
  StudentPaper_Mid: 'Öğrenci Kağıdı (Orta)',
  StudentPaper_Low: 'Öğrenci Kağıdı (En Düşük)',
  AttendanceSheet: 'Devam Çizelgesi',
  GradeList: 'OBS Başarı Notları Listesi',
  SurveyExcel: 'Anket Excel Dosyası',
  OtherTool: 'Diğer Ölçme Aracı',
  Other: 'Diğer',
}

// Harf notuna göre renk (pasta grafik)
const GRADE_COLORS = {
  AA: '#2e7d32', BA: '#388e3c', BB: '#66bb6a',
  CB: '#aed581', CC: '#fff176', DC: '#ffb74d',
  DD: '#ef6c00', FD: '#e53935', FF: '#b71c1c',
}
function gradeColor(grade) {
  return GRADE_COLORS[grade?.toUpperCase()] ?? '#90a4ae'
}

// Anket bar segmenti için renk (düşük=kırmızı, yüksek=yeşil)
function surveyBarColor(val, scaleMax) {
  const ratio = scaleMax > 0 ? val / scaleMax : 0
  if (ratio >= 0.8) return '#2e7d32'
  if (ratio >= 0.6) return '#66bb6a'
  if (ratio >= 0.4) return '#ffb74d'
  if (ratio >= 0.2) return '#ef6c00'
  return '#e53935'
}

// SVG Donut chart bileşeni
function LetterGradeDonut({ distribution, passed, failed }) {
  const entries = Object.entries(distribution).filter(([, v]) => v > 0)
  const total = entries.reduce((s, [, v]) => s + v, 0)
  if (total === 0) return null

  const R = 52
  const cx = 70
  const cy = 70
  const strokeW = 24
  const circ = 2 * Math.PI * R

  let cumulative = 0
  const slices = entries.map(([grade, cnt]) => {
    const frac = cnt / total
    const offset = circ * (1 - cumulative)
    const dash = circ * frac
    cumulative += frac
    return { grade, cnt, dash, offset }
  })

  return (
    <div className={styles.donutWrap}>
      <svg width={140} height={140} viewBox="0 0 140 140" className={styles.donut}>
        {slices.map(({ grade, dash, offset }) => (
          <circle
            key={grade}
            cx={cx}
            cy={cy}
            r={R}
            fill="none"
            stroke={gradeColor(grade)}
            strokeWidth={strokeW}
            strokeDasharray={`${dash} ${circ - dash}`}
            strokeDashoffset={offset}
            transform={`rotate(-90 ${cx} ${cy})`}
          >
            <title>{grade}: {slices.find((s) => s.grade === grade)?.cnt ?? 0}</title>
          </circle>
        ))}
        <text x={cx} y={cy - 6} textAnchor="middle" className={styles.donutTotal}>{total}</text>
        <text x={cx} y={cy + 12} textAnchor="middle" className={styles.donutLabel}>öğrenci</text>
      </svg>
      {(passed != null || failed != null) && (
        <div className={styles.donutStats}>
          <span className={styles.donutPassed}>{passed ?? 0} geçti</span>
          <span className={styles.donutDivider}>·</span>
          <span className={styles.donutFailed}>{failed ?? 0} kaldı</span>
        </div>
      )}
    </div>
  )
}

const TABS = [
  { key: 'overview', label: 'Genel Bakış' },
  { key: 'cover-a-b', label: 'A · B Bölümleri' },
  { key: 'files-c-g', label: 'C–G Dosyalar' },
  { key: 'eval-i-j', label: 'H · İ · J Değerlendirme' },
  { key: 'results-k-l-m', label: 'K · L · M Sonuçlar' },
  { key: 'preview', label: 'Önizleme' },
]

// ────────────────────────────────────────────────────────────────────
// Küçük yardımcı bileşenler
// ────────────────────────────────────────────────────────────────────

function SectionCard({ title, children, defaultOpen = true }) {
  const [open, setOpen] = useState(defaultOpen)
  return (
    <div className={styles.sectionCard}>
      <button
        type="button"
        className={styles.sectionCardHeader}
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
      >
        <span className={styles.sectionCardTitle}>{title}</span>
        {open ? <ChevronUp size={16} aria-hidden /> : <ChevronDown size={16} aria-hidden />}
      </button>
      {open && <div className={styles.sectionCardBody}>{children}</div>}
    </div>
  )
}

function SaveRow({ saving, onSave, label = 'Kaydet' }) {
  return (
    <div className={styles.saveRow}>
      <button
        type="button"
        className={`${formStyles.btn} ${formStyles.btnPrimary}`}
        onClick={onSave}
        disabled={saving}
      >
        {saving ? <Loader2 size={14} className={styles.spin} aria-hidden /> : <Save size={14} aria-hidden />}
        {saving ? 'Kaydediliyor…' : label}
      </button>
    </div>
  )
}

function FileItem({ file, onDownload, onDelete }) {
  const name = fv(file, 'originalFileName') ?? 'Dosya'
  const cat = SECTION_CATEGORIES[fv(file, 'fileCategory')] ?? fv(file, 'fileCategory') ?? '—'
  const size = fv(file, 'fileSize')
  const kb = size ? `${(size / 1024).toFixed(1)} KB` : ''
  return (
    <div className={styles.fileItem}>
      <div className={styles.fileInfo}>
        <span className={styles.fileName}>{name}</span>
        <span className={styles.fileMeta}>
          {cat}
          {kb ? ` · ${kb}` : ''}
        </span>
      </div>
      <div className={styles.fileActions}>
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnGhost}`}
          style={{ fontSize: '0.78rem', padding: '0.35rem 0.65rem' }}
          onClick={() => onDownload(file)}
        >
          İndir
        </button>
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnDanger}`}
          style={{ fontSize: '0.78rem', padding: '0.35rem 0.65rem' }}
          onClick={() => onDelete(fv(file, 'id'))}
        >
          <Trash2 size={13} aria-hidden />
        </button>
      </div>
    </div>
  )
}

// ────────────────────────────────────────────────────────────────────
// Ana bileşen
// ────────────────────────────────────────────────────────────────────

export function SemesterReportDetailPage() {
  const { reportId } = useParams()
  const navigate = useNavigate()

  const [report, setReport] = useState(null)
  const [preview, setPreview] = useState(null)
  const [validation, setValidation] = useState(null)
  const [weeklyResources, setWeeklyResources] = useState([])
  const [files, setFiles] = useState([])
  const [cloNotes, setCloNotes] = useState([])
  const [ploNotes, setPloNotes] = useState([])
  const [studentSamples, setStudentSamples] = useState(null)

  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState('overview')

  // ─── Form state ─────────────────────────────────────────────────
  const [universityName, setUniversityName] = useState('')
  const [facultyName, setFacultyName] = useState('')
  const [departmentName, setDepartmentName] = useState('')
  const [teacherName, setTeacherName] = useState('')
  const [teacherTitle, setTeacherTitle] = useState('')
  // Bölüm A: Ders bilgileri
  const [courseSemester, setCourseSemester] = useState('')
  const [courseCredit, setCourseCredit] = useState('')
  const [courseEcts, setCourseEcts] = useState('')
  const [courseTheoryHours, setCourseTheoryHours] = useState('')
  const [coursePracticeHours, setCoursePracticeHours] = useState('')
  const [courseObjective, setCourseObjective] = useState('')
  const [courseContent, setCourseContent] = useState('')
  const [sectionANotes, setSectionANotes] = useState('')
  // Tartışma/yorum alanları
  const [sectionGDiscussion, setSectionGDiscussion] = useState('')
  const [sectionHCommentary, setSectionHCommentary] = useState('')
  const [sectionIEval, setSectionIEval] = useState('')
  const [sectionJChanges, setSectionJChanges] = useState('')
  const [sectionMImprovement, setSectionMImprovement] = useState('')
  const [signatureName, setSignatureName] = useState('')
  const [signatureDate, setSignatureDate] = useState('')
  const [reportStatus, setReportStatus] = useState('Draft')
  const [saving, setSaving] = useState(false)

  // ─── Haftalık kaynak dialog ──────────────────────────────────────
  const [weekOpen, setWeekOpen] = useState(false)
  const [weekNum, setWeekNum] = useState(1)
  const [weekTopic, setWeekTopic] = useState('')
  const [weekResType, setWeekResType] = useState('')
  const [weekResInfo, setWeekResInfo] = useState('')
  const [weekChapter, setWeekChapter] = useState('')
  const [weekDesc, setWeekDesc] = useState('')
  const [weekSummary, setWeekSummary] = useState('')
  const [weekSaving, setWeekSaving] = useState(false)

  // ─── Dosya yükleme ───────────────────────────────────────────────
  const fileInputRef = useRef(null)
  const [uploadSection, setUploadSection] = useState('C')
  const [uploadCategory, setUploadCategory] = useState('ExamPaper')
  const [uploadExamTypeLabel, setUploadExamTypeLabel] = useState('Vize')
  const [uploadNotes, setUploadNotes] = useState('')
  const [uploading, setUploading] = useState(false)
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)

  // ─── CLO not dialog ──────────────────────────────────────────────
  const [cloNoteOpen, setCloNoteOpen] = useState(false)
  const [cloNoteData, setCloNoteData] = useState(null)
  const [cloNoteText, setCloNoteText] = useState('')
  const [cloImprovement, setCloImprovement] = useState('')
  const [cloNoteSaving, setCloNoteSaving] = useState(false)

  // ─── PLO not dialog ──────────────────────────────────────────────
  const [ploNoteOpen, setPloNoteOpen] = useState(false)
  const [ploNoteData, setPloNoteData] = useState(null)
  const [ploNoteText, setPloNoteText] = useState('')
  const [ploImprovement, setPloImprovement] = useState('')
  const [ploNoteSaving, setPloNoteSaving] = useState(false)

  // ────────────────────────────────────────────────────────────────
  // Veri Yükleme
  // ────────────────────────────────────────────────────────────────

  const loadReport = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !reportId) return
    setLoading(true)
    setError('')
    try {
      const [r, w, f, cn, pn] = await Promise.all([
        fetchSemesterReport(token, reportId),
        fetchWeeklyResources(token, reportId),
        fetchReportFiles(token, reportId),
        fetchCloNotes(token, reportId),
        fetchPloNotes(token, reportId),
      ])
      setReport(r)
      setWeeklyResources(Array.isArray(w) ? w : [])
      setFiles(Array.isArray(f) ? f : [])
      setCloNotes(Array.isArray(cn) ? cn : [])
      setPloNotes(Array.isArray(pn) ? pn : [])

      // Form state sync
      setUniversityName(fv(r, 'universityName') ?? '')
      setFacultyName(fv(r, 'facultyName') ?? '')
      setDepartmentName(fv(r, 'departmentName') ?? '')
      setTeacherName(fv(r, 'teacherName') ?? '')
      setTeacherTitle(fv(r, 'teacherTitle') ?? '')
      setCourseSemester(fv(r, 'courseSemester') ?? '')
      setCourseCredit(fv(r, 'courseCredit') ?? '')
      setCourseEcts(fv(r, 'courseEcts') ?? '')
      setCourseTheoryHours(String(fv(r, 'courseTheoryHours') ?? ''))
      setCoursePracticeHours(String(fv(r, 'coursePracticeHours') ?? ''))
      setCourseObjective(fv(r, 'courseObjective') ?? '')
      setCourseContent(fv(r, 'courseContent') ?? '')
      setSectionANotes(fv(r, 'sectionANotes') ?? '')
      setSectionGDiscussion(fv(r, 'sectionGDiscussion') ?? '')
      setSectionHCommentary(fv(r, 'sectionHCommentary') ?? '')
      setSectionIEval(fv(r, 'sectionIGeneralEvaluation') ?? '')
      setSectionJChanges(fv(r, 'sectionJChangesFromPrevious') ?? '')
      setSectionMImprovement(fv(r, 'sectionMImprovement') ?? '')
      setSignatureName(fv(r, 'signatureName') ?? '')
      const sd = fv(r, 'signatureDate')
      setSignatureDate(sd ? sd.slice(0, 10) : '')
      setReportStatus(fv(r, 'status') === 1 || fv(r, 'status') === 'Ready' ? 'Ready' : 'Draft')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Rapor yüklenemedi.')
    } finally {
      setLoading(false)
    }
  }, [reportId])

  const loadPreview = useCallback(async () => {
    const token = getTeacherToken()
    if (!token || !reportId) return
    try {
      const [p, v, ss] = await Promise.all([
        fetchReportPreview(token, reportId),
        validateReport(token, reportId),
        fetchStudentSamples(token, reportId).catch(() => null),
      ])
      setPreview(p)
      setValidation(v)
      setStudentSamples(ss)
    } catch {
      // önizleme başarısız olsa da sessiz kal
    }
  }, [reportId])

  useEffect(() => {
    void loadReport()
    void loadPreview()
  }, [loadReport, loadPreview])

  const refreshAll = useCallback(() => {
    void loadReport()
    void loadPreview()
  }, [loadReport, loadPreview])

  // ────────────────────────────────────────────────────────────────
  // Rapor kaydetme
  // ────────────────────────────────────────────────────────────────

  const handleSave = async () => {
    const token = getTeacherToken()
    if (!token) return
    setSaving(true)
    setError('')
    try {
      await updateSemesterReport(token, reportId, {
        universityName: universityName.trim() || null,
        facultyName: facultyName.trim() || null,
        departmentName: departmentName.trim() || null,
        teacherName: teacherName.trim() || null,
        teacherTitle: teacherTitle.trim() || null,
        courseSemester: courseSemester.trim() || null,
        courseCredit: courseCredit.trim() || null,
        courseEcts: courseEcts.trim() || null,
        courseTheoryHours: courseTheoryHours ? parseInt(courseTheoryHours) : null,
        coursePracticeHours: coursePracticeHours ? parseInt(coursePracticeHours) : null,
        courseObjective: courseObjective.trim() || null,
        courseContent: courseContent.trim() || null,
        sectionANotes: sectionANotes.trim() || null,
        sectionGDiscussion: sectionGDiscussion.trim() || null,
        sectionHCommentary: sectionHCommentary.trim() || null,
        sectionIGeneralEvaluation: sectionIEval.trim() || null,
        sectionJChangesFromPrevious: sectionJChanges.trim() || null,
        sectionMImprovement: sectionMImprovement.trim() || null,
        signatureName: signatureName.trim() || null,
        signatureDate: signatureDate || null,
        status: reportStatus === 'Ready' ? 1 : 0,
      })
      refreshAll()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kaydedilemedi.')
    } finally {
      setSaving(false)
    }
  }

  // ────────────────────────────────────────────────────────────────
  // Haftalık kaynak
  // ────────────────────────────────────────────────────────────────

  const openWeekDialog = (existing = null) => {
    if (existing) {
      setWeekNum(fv(existing, 'weekNumber') ?? 1)
      setWeekTopic(fv(existing, 'topic') ?? '')
      setWeekResType(fv(existing, 'resourceType') ?? '')
      setWeekResInfo(fv(existing, 'resourceInfo') ?? '')
      setWeekChapter(fv(existing, 'chapterPage') ?? '')
      setWeekDesc(fv(existing, 'description') ?? '')
      setWeekSummary(fv(existing, 'contentSummary') ?? '')
    } else {
      const usedWeeks = weeklyResources.map((w) => fv(w, 'weekNumber') ?? 0)
      const nextWeek = Array.from({ length: 14 }, (_, i) => i + 1).find((w) => !usedWeeks.includes(w)) ?? 1
      setWeekNum(nextWeek)
      setWeekTopic('')
      setWeekResType('')
      setWeekResInfo('')
      setWeekChapter('')
      setWeekDesc('')
      setWeekSummary('')
    }
    setWeekOpen(true)
  }

  const handleSaveWeek = async () => {
    const token = getTeacherToken()
    if (!token) return
    setWeekSaving(true)
    try {
      await upsertWeeklyResource(token, reportId, {
        weekNumber: Number(weekNum),
        topic: weekTopic.trim() || null,
        resourceType: weekResType.trim() || null,
        resourceInfo: weekResInfo.trim() || null,
        chapterPage: weekChapter.trim() || null,
        description: weekDesc.trim() || null,
        contentSummary: weekSummary.trim() || null,
      })
      setWeekOpen(false)
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kaynak kaydedilemedi.')
    } finally {
      setWeekSaving(false)
    }
  }

  const handleDeleteWeek = async (resourceId) => {
    if (!window.confirm('Bu kaynağı silmek istiyor musunuz?')) return
    const token = getTeacherToken()
    if (!token) return
    try {
      await deleteWeeklyResource(token, reportId, resourceId)
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Silinemedi.')
    }
  }

  // ────────────────────────────────────────────────────────────────
  // Dosya yükleme
  // ────────────────────────────────────────────────────────────────

  const handleUpload = async () => {
    const file = fileInputRef.current?.files?.[0]
    if (!file) {
      setError('Lütfen bir dosya seçin.')
      return
    }
    const token = getTeacherToken()
    if (!token) return
    setUploading(true)
    setError('')
    try {
      await uploadReportFile(token, reportId, {
        sectionCode: uploadSection,
        fileCategory: uploadCategory,
        examTypeLabel: uploadSection === 'C' ? uploadExamTypeLabel : undefined,
        notes: uploadNotes.trim() || undefined,
      }, file)
      setUploadDialogOpen(false)
      setUploadNotes('')
      if (fileInputRef.current) fileInputRef.current.value = ''
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Dosya yüklenemedi.')
    } finally {
      setUploading(false)
    }
  }

  const handleDownload = async (file) => {
    const token = getTeacherToken()
    if (!token) return
    try {
      await downloadReportFile(token, reportId, fv(file, 'id'), fv(file, 'originalFileName'))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'İndirilemedi.')
    }
  }

  const handleDeleteFile = async (fileId) => {
    if (!window.confirm('Bu dosyayı silmek istiyor musunuz?')) return
    const token = getTeacherToken()
    if (!token) return
    try {
      await deleteReportFile(token, reportId, fileId)
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Dosya silinemedi.')
    }
  }

  // ────────────────────────────────────────────────────────────────
  // CLO notları
  // ────────────────────────────────────────────────────────────────

  const openCloNote = (clo) => {
    const existing = cloNotes.find((n) => fv(n, 'externalCloId') === fv(clo, 'externalCloId'))
    setCloNoteData(clo)
    setCloNoteText(fv(existing, 'teacherNote') ?? '')
    setCloImprovement(fv(existing, 'improvementSuggestion') ?? '')
    setCloNoteOpen(true)
  }

  const handleSaveCloNote = async () => {
    const token = getTeacherToken()
    if (!token || !cloNoteData) return
    setCloNoteSaving(true)
    try {
      await upsertCloNote(token, reportId, {
        externalCloId: fv(cloNoteData, 'externalCloId'),
        cloCode: fv(cloNoteData, 'cloCode'),
        cloDescription: fv(cloNoteData, 'cloDescription'),
        teacherNote: cloNoteText.trim() || null,
        improvementSuggestion: cloImprovement.trim() || null,
      })
      setCloNoteOpen(false)
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Not kaydedilemedi.')
    } finally {
      setCloNoteSaving(false)
    }
  }

  // ────────────────────────────────────────────────────────────────
  // PLO notları
  // ────────────────────────────────────────────────────────────────

  const openPloNote = (plo) => {
    const existing = ploNotes.find((n) => fv(n, 'externalPloId') === fv(plo, 'externalPloId'))
    setPloNoteData(plo)
    setPloNoteText(fv(existing, 'teacherNote') ?? '')
    setPloImprovement(fv(existing, 'improvementSuggestion') ?? '')
    setPloNoteOpen(true)
  }

  const handleSavePloNote = async () => {
    const token = getTeacherToken()
    if (!token || !ploNoteData) return
    setPloNoteSaving(true)
    try {
      await upsertPloNote(token, reportId, {
        externalPloId: fv(ploNoteData, 'externalPloId'),
        ploCode: fv(ploNoteData, 'ploCode'),
        ploDescription: fv(ploNoteData, 'ploDescription'),
        teacherNote: ploNoteText.trim() || null,
        improvementSuggestion: ploImprovement.trim() || null,
      })
      setPloNoteOpen(false)
      void loadReport()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Not kaydedilemedi.')
    } finally {
      setPloNoteSaving(false)
    }
  }

  // ────────────────────────────────────────────────────────────────
  // Başlık
  // ────────────────────────────────────────────────────────────────

  const pageTitle = (() => {
    if (!report) return 'Dönem Sonu Raporu'
    const code = fv(report, 'courseCode') ?? ''
    const name = fv(report, 'courseName') ?? ''
    return code && name ? `${code} — ${name}` : name || code || 'Dönem Sonu Raporu'
  })()

  // ────────────────────────────────────────────────────────────────
  // Sekmeler render
  // ────────────────────────────────────────────────────────────────

  function renderOverview() {
    const missingItems = fv(validation, 'missingItems') ?? []
    const warnings = fv(validation, 'warnings') ?? []
    const isComplete = fv(validation, 'isComplete') ?? false
    const status = fv(report, 'status')

    return (
      <div className={styles.overviewGrid}>
        {/* Rapor bilgileri */}
        <SectionCard title="Rapor Bilgileri">
          <div className={styles.infoGrid}>
            <div className={styles.infoRow}>
              <span className={styles.infoLabel}>Ders</span>
              <span className={styles.infoValue}>{pageTitle}</span>
            </div>
            <div className={styles.infoRow}>
              <span className={styles.infoLabel}>Dönem</span>
              <span className={styles.infoValue}>{fv(report, 'academicTermName') ?? '—'}</span>
            </div>
            <div className={styles.infoRow}>
              <span className={styles.infoLabel}>Öğretim Üyesi</span>
              <span className={styles.infoValue}>
                {[fv(report, 'teacherTitle'), fv(report, 'teacherName')].filter(Boolean).join(' ') || '—'}
              </span>
            </div>
            <div className={styles.infoRow}>
              <span className={styles.infoLabel}>Durum</span>
              <span className={status === 'Ready' || status === 1 ? styles.badgeReady : styles.badgeDraft}>
                {statusLabel(status)}
              </span>
            </div>
          </div>
        </SectionCard>

        {/* Doğrulama */}
        <SectionCard title="Eksiklik Kontrolü">
          {isComplete ? (
            <div className={styles.validComplete}>
              <CheckCircle2 size={18} aria-hidden />
              Rapor tüm zorunlu bölümleri içeriyor.
            </div>
          ) : (
            <div className={styles.validIssueList}>
              {missingItems.map((item, i) => (
                <div key={i} className={styles.validMissing}>
                  <AlertCircle size={14} aria-hidden />
                  <span>
                    <strong>Bölüm {fv(item, 'section')}</strong>: {fv(item, 'message')}
                  </span>
                </div>
              ))}
              {warnings.map((item, i) => (
                <div key={`w${i}`} className={styles.validWarning}>
                  <AlertCircle size={14} aria-hidden />
                  <span>
                    <strong>Bölüm {fv(item, 'section')}</strong>: {fv(item, 'message')}
                  </span>
                </div>
              ))}
            </div>
          )}
        </SectionCard>

        {/* Öğrenci örnekleri */}
        {studentSamples && (
          <SectionCard title="Bölüm C — Önerilen Öğrenci Örnekleri">
            <p className={styles.helpText}>
              Sınav kağıdı yüklemek için aşağıdaki öğrencilerin kağıtlarını taratarak <em>C–G Dosyalar</em> sekmesinden yükleyin.
            </p>
            <div className={styles.samplesGrid}>
              {[
                { key: 'highest', label: 'En Yüksek', sample: fv(studentSamples, 'highest') },
                { key: 'middle', label: 'Orta', sample: fv(studentSamples, 'middle') },
                { key: 'lowest', label: 'En Düşük', sample: fv(studentSamples, 'lowest') },
              ].filter((x) => x.sample).map(({ key, label, sample }) => (
                <div key={key} className={styles.sampleCard}>
                  <span className={styles.sampleLevel}>{label}</span>
                  <span className={styles.sampleName}>{fv(sample, 'studentName') ?? '—'}</span>
                  <span className={styles.sampleMeta}>{fv(sample, 'studentNumber')} · {fv(sample, 'letterGrade')} · {pct(fv(sample, 'successGrade'))}</span>
                </div>
              ))}
            </div>
          </SectionCard>
        )}
      </div>
    )
  }

  function renderCoverAB() {
    const sorted = [...weeklyResources].sort((a, b) => (fv(a, 'weekNumber') ?? 0) - (fv(b, 'weekNumber') ?? 0))

    return (
      <div className={styles.colStack}>
        {/* Bölüm A: Kapak + ders bilgileri */}
        <SectionCard title="Kapak Bilgileri ve Bölüm A — Ders Tanıtım Notları">

          {/* Kurumsal Bilgiler */}
          <h4 className={styles.subSectionTitle}>Kurumsal Bilgiler</h4>
          <div className={styles.formGrid1}>
            <div className={formStyles.field}>
              <label htmlFor="sr-university">Üniversite Adı</label>
              <input
                id="sr-university"
                className={formStyles.input}
                value={universityName}
                onChange={(e) => setUniversityName(e.target.value)}
                placeholder="KONYA TEKNİK ÜNİVERSİTESİ"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-faculty">Fakülte Adı</label>
              <input
                id="sr-faculty"
                className={formStyles.input}
                value={facultyName}
                onChange={(e) => setFacultyName(e.target.value)}
                placeholder="BİLGİSAYAR ve BİLİŞİM BİLİMLERİ FAKÜLTESİ"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-dept">
                Bölüm Adı
                {!departmentName && (
                  <span className={styles.optLabel}> (API&apos;den otomatik doldurulamadıysa girin)</span>
                )}
              </label>
              <input
                id="sr-dept"
                className={formStyles.input}
                value={departmentName}
                onChange={(e) => setDepartmentName(e.target.value)}
                placeholder="BİLGİSAYAR MÜHENDİSLİĞİ BÖLÜMÜ"
              />
            </div>
          </div>

          {/* Öğretim Üyesi */}
          <h4 className={styles.subSectionTitle} style={{ marginTop: '1rem' }}>Öğretim Üyesi</h4>
          <div className={styles.formGrid2}>
            <div className={formStyles.field}>
              <label htmlFor="sr-title">Ünvan</label>
              <input
                id="sr-title"
                className={formStyles.input}
                value={teacherTitle}
                onChange={(e) => setTeacherTitle(e.target.value)}
                placeholder="Dr. Öğr. Üyesi"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-name">Adı Soyadı</label>
              <input
                id="sr-name"
                className={formStyles.input}
                value={teacherName}
                onChange={(e) => setTeacherName(e.target.value)}
                placeholder="Ad Soyad"
              />
            </div>
          </div>

          {/* Ders Tanıtım Bilgileri */}
          <h4 className={styles.subSectionTitle} style={{ marginTop: '1rem' }}>Ders Tanıtım Bilgileri</h4>
          <div className={styles.formGrid3}>
            <div className={formStyles.field}>
              <label htmlFor="sr-semester">Yarıyıl</label>
              <input
                id="sr-semester"
                className={formStyles.input}
                value={courseSemester}
                onChange={(e) => setCourseSemester(e.target.value)}
                placeholder="örn: 5"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-credit">Kredi</label>
              <input
                id="sr-credit"
                className={formStyles.input}
                value={courseCredit}
                onChange={(e) => setCourseCredit(e.target.value)}
                placeholder="örn: 3"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-ects">AKTS</label>
              <input
                id="sr-ects"
                className={formStyles.input}
                value={courseEcts}
                onChange={(e) => setCourseEcts(e.target.value)}
                placeholder="örn: 5"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-theory">Teorik Saat/Hafta</label>
              <input
                id="sr-theory"
                type="number"
                min="0"
                className={formStyles.input}
                value={courseTheoryHours}
                onChange={(e) => setCourseTheoryHours(e.target.value)}
                placeholder="3"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-practice">Uygulama Saat/Hafta</label>
              <input
                id="sr-practice"
                type="number"
                min="0"
                className={formStyles.input}
                value={coursePracticeHours}
                onChange={(e) => setCoursePracticeHours(e.target.value)}
                placeholder="2"
              />
            </div>
          </div>
          <div className={formStyles.field} style={{ marginTop: '0.75rem' }}>
            <label htmlFor="sr-objective">Dersin Amacı</label>
            <textarea
              id="sr-objective"
              className={formStyles.textarea}
              rows={3}
              value={courseObjective}
              onChange={(e) => setCourseObjective(e.target.value)}
              placeholder="Bu dersin amacı, öğrencilerin…"
            />
          </div>
          <div className={formStyles.field}>
            <label htmlFor="sr-content">Dersin İçeriği</label>
            <textarea
              id="sr-content"
              className={formStyles.textarea}
              rows={4}
              value={courseContent}
              onChange={(e) => setCourseContent(e.target.value)}
              placeholder="Hafta hafta işlenecek konular…"
            />
          </div>

          {/* Ders Öğrenim Çıktıları (DÖÇ) — önizlemeden otomatik */}
          {(() => {
            const cloList = fv(preview, 'cloResults') ?? []
            if (!cloList.length) return null
            return (
              <div style={{ marginTop: '0.75rem' }}>
                <h4 className={styles.subSectionTitle}>Ders Öğrenim Çıktıları (DÖÇ)</h4>
                <div className={styles.cloList}>
                  {cloList.map((c, i) => (
                    <div key={i} className={styles.cloListItem}>
                      <span className={styles.cloCode}>{fv(c, 'cloCode') ?? `DÖÇ${i + 1}`}</span>
                      <span className={styles.cloDesc}>{fv(c, 'cloDescription') ?? '—'}</span>
                    </div>
                  ))}
                </div>
              </div>
            )
          })()}

          {/* Ek Notlar */}
          <div className={formStyles.field} style={{ marginTop: '0.75rem' }}>
            <label htmlFor="sr-a-notes">
              Bölüm A Ek Notları{' '}
              <span className={styles.optLabel}>(isteğe bağlı)</span>
            </label>
            <textarea
              id="sr-a-notes"
              className={formStyles.textarea}
              rows={3}
              value={sectionANotes}
              onChange={(e) => setSectionANotes(e.target.value)}
              placeholder="Ders bilgileri ile ilgili ek açıklamalar…"
            />
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>

        {/* Bölüm B: Haftalık kaynaklar */}
        <SectionCard title="Bölüm B — Derste Kullanılan Kaynaklar (Haftalık)">
          <div className={styles.sectionToolbar}>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              onClick={() => openWeekDialog()}
            >
              + Hafta Ekle
            </button>
          </div>

          {sorted.length > 0 ? (
            <>
              <div className={styles.weekTable}>
                <div className={styles.weekTableHeader}>
                  <span>Hafta</span>
                  <span>Konu</span>
                  <span>Kaynak</span>
                  <span>İçerik Özeti</span>
                  <span></span>
                </div>
                {sorted.map((w) => {
                  const wid = fv(w, 'id')
                  return (
                    <div key={wid} className={styles.weekTableRow}>
                      <span className={styles.weekNum}>{fv(w, 'weekNumber')}</span>
                      <span>{fv(w, 'topic') ?? '—'}</span>
                      <span>{[fv(w, 'resourceType'), fv(w, 'resourceInfo')].filter(Boolean).join(' · ') || '—'}</span>
                      <span className={styles.weekSummary}>{fv(w, 'contentSummary') ?? '—'}</span>
                      <div className={styles.weekActions}>
                        <button
                          type="button"
                          className={`${formStyles.rowActionText}`}
                          onClick={() => openWeekDialog(w)}
                        >
                          Düzenle
                        </button>
                        <button
                          type="button"
                          className={`${formStyles.rowActionText} ${formStyles.rowActionTextDanger}`}
                          onClick={() => handleDeleteWeek(wid)}
                        >
                          Sil
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
              {/* Haftalık İçerik Özeti (otomatik oluşturulan metin) */}
              {sorted.some((w) => fv(w, 'contentSummary')) && (
                <div className={styles.weekSummaryBlock}>
                  <h5 className={styles.weekSummaryTitle}>Haftalık İçerik Özeti</h5>
                  {sorted.filter((w) => fv(w, 'contentSummary')).map((w) => {
                    const topic = fv(w, 'topic')
                    const summary = fv(w, 'contentSummary')
                    return (
                      <p key={fv(w, 'id')} className={styles.weekSummaryLine}>
                        <strong>Hafta {fv(w, 'weekNumber')}{topic ? ` (${topic})` : ''}:</strong>{' '}
                        {summary}
                      </p>
                    )
                  })}
                </div>
              )}
            </>
          ) : (
            <p className={styles.emptyInfo}>Henüz haftalık kaynak girilmemiş. &quot;Hafta Ekle&quot; ile başlayın.</p>
          )}
        </SectionCard>
      </div>
    )
  }

  function renderFilesCG() {
    const fileSectionLabels = { C: 'C', D: 'D', E: 'E' }
    const fileSectionDescriptions = {
      C: 'Yazılı Sınav Kağıtları ve Cevap Anahtarları',
      D: 'Kullanılan Diğer Ölçme Araçları',
      E: 'Ders Devam Çizelgeleri',
    }

    const bySection = {}
    for (const sec of Object.keys(fileSectionLabels)) bySection[sec] = []
    for (const f of files) {
      const sec = (fv(f, 'sectionCode') ?? '').toUpperCase()
      if (bySection[sec]) bySection[sec].push(f)
    }

    // Section F — not listesi
    const studentGrades = fv(preview, 'studentGrades') ?? []
    const successStats = fv(preview, 'successStats')
    const letterDist = fv(successStats, 'letterGradeDistribution') ?? {}

    // Section G — anket soruları
    const surveyQuestions = fv(preview, 'surveyQuestions') ?? []
    const passingCount = fv(preview, 'surveyPassingStudentCount') ?? 0
    const submissionCount = fv(preview, 'surveyTotalSubmissions') ?? 0

    return (
      <div className={styles.colStack}>
        {/* C, D, E — Dosya yükleme */}
        <div className={styles.sectionToolbar}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={() => setUploadDialogOpen(true)}
          >
            <FileUp size={15} aria-hidden />
            Dosya Yükle
          </button>
        </div>

        {/* Bölüm C — Vize/Final alt kategorileriyle */}
        <SectionCard title="Bölüm C — Sınav Kağıtları ve Cevap Anahtarları" defaultOpen>
          {(() => {
            const cFiles = bySection['C'] ?? []
            const EXAM_TYPES = ['Vize', 'Final', 'Bütünleme']
            const CAT_ORDER = ['ExamPaper', 'AnswerKey', 'StudentPaper_High', 'StudentPaper_Mid', 'StudentPaper_Low']
            const CAT_LABELS = {
              ExamPaper: 'Sınav Soruları',
              AnswerKey: 'Cevap Anahtarı',
              StudentPaper_High: 'En Yüksek Notlu Öğrenci Kağıdı',
              StudentPaper_Mid: 'Orta Düzey Öğrenci Kağıdı',
              StudentPaper_Low: 'En Düşük Notlu Öğrenci Kağıdı',
            }
            // Sınav türüne göre sub-number hesapla
            const subNumberFor = (examType, cat) => {
              const eIdx = EXAM_TYPES.indexOf(examType)
              const cIdx = CAT_ORDER.indexOf(cat)
              if (eIdx < 0 || cIdx < 0) return null
              return eIdx * CAT_ORDER.length + cIdx + 1
            }
            // Dosyaları grupla
            const grouped = {}
            for (const et of EXAM_TYPES) {
              grouped[et] = {}
              for (const cat of CAT_ORDER) grouped[et][cat] = []
            }
            const uncategorized = []
            for (const f of cFiles) {
              const etl = fv(f, 'examTypeLabel')
              const cat = fv(f, 'fileCategory')
              if (etl && grouped[etl] && cat && grouped[etl][cat]) {
                grouped[etl][cat].push(f)
              } else {
                uncategorized.push(f)
              }
            }
            const hasAny = cFiles.length > 0
            return (
              <>
                {hasAny ? (
                  EXAM_TYPES.map((et) => {
                    const etFiles = Object.values(grouped[et]).flat()
                    if (!etFiles.length) return null
                    return (
                      <div key={et} className={styles.cSubSection}>
                        <h5 className={styles.cSubTitle}>{et} Sınavı</h5>
                        {CAT_ORDER.map((cat) => {
                          const catFiles = grouped[et][cat]
                          if (!catFiles.length) return null
                          const subNum = subNumberFor(et, cat)
                          return (
                            <div key={cat} className={styles.cCatGroup}>
                              <span className={styles.cCatLabel}>
                                C.{subNum} — {CAT_LABELS[cat]}
                              </span>
                              {catFiles.map((f) => (
                                <FileItem
                                  key={fv(f, 'id')}
                                  file={f}
                                  onDownload={handleDownload}
                                  onDelete={handleDeleteFile}
                                />
                              ))}
                            </div>
                          )
                        })}
                      </div>
                    )
                  })
                ) : (
                  <p className={styles.emptyInfo}>Bu bölüm için dosya yüklenmemiş.</p>
                )}
                {uncategorized.length > 0 && (
                  <div className={styles.cSubSection}>
                    <h5 className={styles.cSubTitle}>Diğer Sınav Dosyaları</h5>
                    {uncategorized.map((f) => (
                      <FileItem key={fv(f, 'id')} file={f} onDownload={handleDownload} onDelete={handleDeleteFile} />
                    ))}
                  </div>
                )}
              </>
            )
          })()}
        </SectionCard>

        {/* Bölüm D */}
        <SectionCard title="Bölüm D — Kullanılan Diğer Ölçme Araçları" defaultOpen={false}>
          {(bySection['D'] ?? []).length > 0 ? (
            <div className={styles.fileList}>
              {(bySection['D'] ?? []).map((f) => (
                <FileItem key={fv(f, 'id')} file={f} onDownload={handleDownload} onDelete={handleDeleteFile} />
              ))}
            </div>
          ) : (
            <p className={styles.emptyInfo}>Bu bölüm için dosya yüklenmemiş.</p>
          )}
        </SectionCard>

        {/* Bölüm E */}
        <SectionCard title="Bölüm E — Ders Devam Çizelgeleri" defaultOpen={false}>
          {(bySection['E'] ?? []).length > 0 ? (
            <div className={styles.fileList}>
              {(bySection['E'] ?? []).map((f) => (
                <FileItem key={fv(f, 'id')} file={f} onDownload={handleDownload} onDelete={handleDeleteFile} />
              ))}
            </div>
          ) : (
            <p className={styles.emptyInfo}>Bu bölüm için dosya yüklenmemiş.</p>
          )}
        </SectionCard>

        {/* F — Başarı Notları Tablosu (otomatik) */}
        <SectionCard title="Bölüm F — Sonuçlandırılmış Başarı Notları (Sistemden Otomatik)" defaultOpen>
          {studentGrades.length > 0 ? (
            <>
              {/* Pasta grafik + istatistik */}
              {successStats && Object.keys(letterDist).length > 0 && (
                <div className={styles.gradeStatRow}>
                  <LetterGradeDonut
                    distribution={letterDist}
                    passed={fv(successStats, 'passedStudents')}
                    failed={fv(successStats, 'failedStudents')}
                  />
                  <div className={styles.gradeStatMeta}>
                    <div className={styles.statItem}>
                      <span className={styles.statVal}>{fv(successStats, 'totalStudents')}</span>
                      <span className={styles.statLbl}>Toplam</span>
                    </div>
                    <div className={styles.statItem}>
                      <span className={`${styles.statVal} ${styles.scoreGood}`}>
                        {fv(successStats, 'passedStudents')}
                      </span>
                      <span className={styles.statLbl}>Geçti</span>
                    </div>
                    <div className={styles.statItem}>
                      <span className={`${styles.statVal} ${styles.scoreBad}`}>
                        {fv(successStats, 'failedStudents')}
                      </span>
                      <span className={styles.statLbl}>Kaldı</span>
                    </div>
                    <div className={styles.statItem}>
                      <span className={styles.statVal}>
                        %{Number(fv(successStats, 'successPercentage') ?? 0).toFixed(1)}
                      </span>
                      <span className={styles.statLbl}>Başarı oranı</span>
                    </div>
                    <div className={styles.gradeDistLegend}>
                      {Object.entries(letterDist)
                        .filter(([, cnt]) => cnt > 0)
                        .map(([grade, cnt]) => (
                          <span key={grade} className={styles.gradeDistItem}>
                            <span
                              className={styles.gradeDistSwatch}
                              style={{ background: gradeColor(grade) }}
                            />
                            {grade}: {cnt}
                          </span>
                        ))}
                    </div>
                  </div>
                </div>
              )}

              {/* Not tablosu */}
              {(() => {
                const sorted = [...studentGrades].sort((a, b) =>
                  (fv(a, 'studentNumber') ?? '').localeCompare(fv(b, 'studentNumber') ?? ''),
                )
                const passedCount = sorted.filter((s) => fv(s, 'isPassed')).length
                const failedCount = sorted.length - passedCount
                return (
                  <>
                    <div className={styles.gradeSummaryRow}>
                      <span>Toplam <strong>{sorted.length}</strong> öğrenci</span>
                      <span className={styles.scoreGood}><strong>{passedCount}</strong> geçti</span>
                      <span className={styles.scoreBad}><strong>{failedCount}</strong> kaldı</span>
                    </div>
                    <div className={styles.gradeTable}>
                      <div className={`${styles.gradeRow} ${styles.gradeHeader}`}>
                        <span>#</span>
                        <span>Öğrenci No</span>
                        <span>Adı Soyadı</span>
                        <span>Vize</span>
                        <span>Final</span>
                        <span>Büt.</span>
                        <span>Başarı</span>
                        <span>Harf</span>
                        <span>Durum</span>
                      </div>
                      {sorted.map((s, i) => {
                        const failed = !fv(s, 'isPassed')
                        return (
                          <div
                            key={fv(s, 'externalStudentId') ?? i}
                            className={`${styles.gradeRow} ${failed ? styles.gradeRowFailed : ''}`}
                          >
                            <span className={styles.gradeSeq}>{i + 1}</span>
                            <span className={styles.gradeNum}>{fv(s, 'studentNumber') ?? '—'}</span>
                            <span>{fv(s, 'studentName') ?? '—'}</span>
                            <span>{fv(s, 'midtermScore') != null ? Number(fv(s, 'midtermScore')).toFixed(1) : '—'}</span>
                            <span>{fv(s, 'finalScore') != null ? Number(fv(s, 'finalScore')).toFixed(1) : '—'}</span>
                            <span>{fv(s, 'makeupScore') != null ? Number(fv(s, 'makeupScore')).toFixed(1) : '—'}</span>
                            <span>{fv(s, 'successGrade') != null ? Number(fv(s, 'successGrade')).toFixed(1) : '—'}</span>
                            <span className={styles.gradeLetterCell}>{fv(s, 'letterGrade') ?? '—'}</span>
                            <span className={failed ? styles.failedBadge : styles.passedBadge}>
                              {failed ? 'Kaldı' : 'Geçti'}
                            </span>
                          </div>
                        )
                      })}
                    </div>
                    <p className={styles.helpText} style={{ marginTop: '0.5rem' }}>
                      Tüm öğrenciler listelenmektedir (geçen ve kalan dahil). Veriler MÜDEK hesaplama sonuçlarından otomatik alınmıştır.
                    </p>
                  </>
                )
              })()}
            </>
          ) : (
            <p className={styles.emptyInfo}>
              Başarı notu listesi yok. Önce MÜDEK hesaplamayı çalıştırın.
            </p>
          )}
        </SectionCard>

        {/* G — Anket Sonuçları (otomatik, sadece geçen öğrenciler) */}
        <SectionCard title="Bölüm G — Öğrenci Ders Değerlendirme Anket Sonuçları (Sistemden Otomatik)" defaultOpen>
          {surveyQuestions.length > 0 ? (
            <>
              <p className={styles.helpText}>
                Sadece <strong>geçen öğrencilerin</strong> yanıtları dahil edilmiştir.
                Geçen öğrenci: {passingCount} kişi · Değerlendirilen anket: {submissionCount} adet
              </p>

              <div className={styles.surveyTable}>
                <div className={`${styles.surveyRow} ${styles.surveyHeader}`}>
                  <span>#</span>
                  <span>Soru</span>
                  <span>İlgili DÖÇ</span>
                  <span>Yanıt</span>
                  <span>Ort.</span>
                  <span>%</span>
                  <span>Dağılım</span>
                </div>
                {surveyQuestions.map((q, i) => {
                  const dist = fv(q, 'scoreDistribution') ?? {}
                  const scaleMax = fv(q, 'scaleMax') ?? 5
                  const distEntries = Object.entries(dist).map(([k, v]) => [Number(k), Number(v)])
                  const total = distEntries.reduce((s, [, v]) => s + v, 0)
                  return (
                    <div key={i} className={styles.surveyRow}>
                      <span>{fv(q, 'orderIndex') ?? i + 1}</span>
                      <span className={styles.surveyQText}>{fv(q, 'questionText') ?? '—'}</span>
                      <span className={styles.surveyCloBadge}>{fv(q, 'cloCode') ?? '—'}</span>
                      <span>{fv(q, 'responseCount') ?? 0}</span>
                      <span>{fv(q, 'averageScore') != null ? Number(fv(q, 'averageScore')).toFixed(2) : '—'} / {scaleMax}</span>
                      <span>{fv(q, 'scorePercentage') != null ? `%${Number(fv(q, 'scorePercentage')).toFixed(1)}` : '—'}</span>
                      <span>
                        <div className={styles.distBar}>
                          {distEntries.map(([val, cnt]) => {
                            const w = total > 0 ? (cnt / total) * 100 : 0
                            return (
                              <div
                                key={val}
                                className={styles.distBarSegment}
                                style={{ width: `${w}%`, background: surveyBarColor(val, scaleMax) }}
                                title={`${val}: ${cnt} kişi (%${w.toFixed(1)})`}
                              />
                            )
                          })}
                        </div>
                        <div className={styles.distBarLabels}>
                          {distEntries.filter(([, cnt]) => cnt > 0).map(([val, cnt]) => (
                            <span key={val}>{val}:{cnt}</span>
                          ))}
                        </div>
                      </span>
                    </div>
                  )
                })}
              </div>
            </>
          ) : (
            <p className={styles.emptyInfo}>
              Bu ders için henüz anket yanıtı bulunmamaktadır veya geçen öğrenci kaydı mevcut değildir.
            </p>
          )}
          {/* Anket tartışma metni */}
          <div className={formStyles.field} style={{ marginTop: '1rem' }}>
            <label htmlFor="sr-g-disc">
              Anket Sonuçları Tartışma Metni{' '}
              <span className={styles.optLabel}>(isteğe bağlı — manuel)</span>
            </label>
            <textarea
              id="sr-g-disc"
              className={formStyles.textarea}
              rows={5}
              value={sectionGDiscussion}
              onChange={(e) => setSectionGDiscussion(e.target.value)}
              placeholder="Anket sonuçları incelendiğinde öğrencilerin genel memnuniyetinin yüksek olduğu görülmektedir…"
            />
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>
      </div>
    )
  }

  function renderEvalIJ() {
    return (
      <div className={styles.colStack}>

        {/* H — Başarı istatistikleri yorumu */}
        <SectionCard title="Bölüm H — Sınav İstatistikleri ve Başarı Oranı Yorumu">
          {(() => {
            const examStats = fv(preview, 'examStats') ?? []
            const successStats = fv(preview, 'successStats')
            return (
              <>
                {examStats.length > 0 && (
                  <div className={styles.resultsTable}>
                    <div className={`${styles.resultsRow} ${styles.resultsHeader}`}>
                      <span>Sınav</span>
                      <span>Katılım</span>
                      <span>Maks.</span>
                      <span>Min.</span>
                      <span>Ort.</span>
                    </div>
                    {examStats.map((es, i) => (
                      <div key={i} className={styles.resultsRow}>
                        <span>{fv(es, 'examType') ?? '—'}</span>
                        <span>{fv(es, 'participantCount') ?? '—'}</span>
                        <span>{fv(es, 'maxScore') != null ? Number(fv(es, 'maxScore')).toFixed(1) : '—'}</span>
                        <span>{fv(es, 'minScore') != null ? Number(fv(es, 'minScore')).toFixed(1) : '—'}</span>
                        <span>{fv(es, 'averageScore') != null ? Number(fv(es, 'averageScore')).toFixed(1) : '—'}</span>
                      </div>
                    ))}
                  </div>
                )}
                {successStats && (
                  <div className={styles.statCards}>
                    <div className={styles.statCard}>
                      <span className={styles.statCardVal}>{fv(successStats, 'totalStudents') ?? '—'}</span>
                      <span className={styles.statCardLbl}>Toplam</span>
                    </div>
                    <div className={styles.statCard}>
                      <span className={`${styles.statCardVal} ${styles.scoreGood}`}>{fv(successStats, 'passedStudents') ?? '—'}</span>
                      <span className={styles.statCardLbl}>Geçti</span>
                    </div>
                    <div className={styles.statCard}>
                      <span className={`${styles.statCardVal} ${styles.scoreBad}`}>{fv(successStats, 'failedStudents') ?? '—'}</span>
                      <span className={styles.statCardLbl}>Kaldı</span>
                    </div>
                    <div className={styles.statCard}>
                      <span className={styles.statCardVal}>%{Number(fv(successStats, 'successPercentage') ?? 0).toFixed(1)}</span>
                      <span className={styles.statCardLbl}>Başarı Oranı</span>
                    </div>
                  </div>
                )}
                <div className={formStyles.field} style={{ marginTop: '0.75rem' }}>
                  <label htmlFor="sr-h-comment">
                    Başarı Oranı Yorumu{' '}
                    <span className={styles.optLabel}>(teknik değerlendirme)</span>
                  </label>
                  <textarea
                    id="sr-h-comment"
                    className={formStyles.textarea}
                    rows={5}
                    value={sectionHCommentary}
                    onChange={(e) => setSectionHCommentary(e.target.value)}
                    placeholder="Dersin genel başarı oranı %X olarak gerçekleşmiştir. Bu sonuç geçen döneme kıyasla…"
                  />
                </div>
                <SaveRow saving={saving} onSave={handleSave} />
              </>
            )
          })()}
        </SectionCard>

        <SectionCard title="Bölüm İ — Öğretim Üyesinin Genel Değerlendirmesi ve Öneriler">
          <p className={styles.helpText}>
            Dönem boyunca edindiğiniz tecrübeler, öğrenci geri bildirimleri, program çıktılarının sağlanma
            düzeyleri ve istatistiki başarı verileri ışığında kapsamlı değerlendirmenizi yazın.
          </p>
          <div className={formStyles.field}>
            <label htmlFor="sr-eval-i">Genel değerlendirme metni</label>
            <textarea
              id="sr-eval-i"
              className={formStyles.textarea}
              rows={10}
              value={sectionIEval}
              onChange={(e) => setSectionIEval(e.target.value)}
              placeholder="Ders kapsamında yapılan ölçme ve değerlendirme sonuçları incelendiğinde…"
            />
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>

        <SectionCard title="Bölüm J — Geçmiş Dönemden Farklı Yapılan Değişiklikler">
          <p className={styles.helpText}>
            Geçmiş döneme kıyasla müfredat, dersin işlenişi veya ölçme-değerlendirme sisteminde yapılan
            yenilikleri ve bunların etkilerini özetleyin.
          </p>
          <div className={formStyles.field}>
            <label htmlFor="sr-eval-j">Değişiklikler metni</label>
            <textarea
              id="sr-eval-j"
              className={formStyles.textarea}
              rows={7}
              value={sectionJChanges}
              onChange={(e) => setSectionJChanges(e.target.value)}
              placeholder="Geçmiş döneme kıyasla ders kapsamında…"
            />
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>

        <SectionCard title="İmza ve Durum">
          <div className={styles.formGrid2}>
            <div className={formStyles.field}>
              <label htmlFor="sr-sig-name">İmzalayan adı soyadı</label>
              <input
                id="sr-sig-name"
                className={formStyles.input}
                value={signatureName}
                onChange={(e) => setSignatureName(e.target.value)}
                placeholder="Ad Soyad"
              />
            </div>
            <div className={formStyles.field}>
              <label htmlFor="sr-sig-date">Tarih</label>
              <input
                id="sr-sig-date"
                type="date"
                className={formStyles.input}
                value={signatureDate}
                onChange={(e) => setSignatureDate(e.target.value)}
              />
            </div>
          </div>
          <div className={formStyles.field} style={{ marginTop: '0.5rem' }}>
            <label htmlFor="sr-status">Rapor durumu</label>
            <select
              id="sr-status"
              className={formStyles.select}
              value={reportStatus}
              onChange={(e) => setReportStatus(e.target.value)}
            >
              <option value="Draft">Taslak</option>
              <option value="Ready">Hazır — MÜDEK komisyonuna gönderilebilir</option>
            </select>
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>
      </div>
    )
  }

  function renderResultsKLM() {
    const cloResults = fv(preview, 'cloResults') ?? []
    const ploResults_ = fv(preview, 'ploResults') ?? []
    const comparisons = fv(preview, 'cloComparisons') ?? []
    const questionResults = fv(preview, 'questionResults') ?? []
    const cloPlomMatrix = fv(preview, 'cloPlomMatrix')

    const examOrder = { Vize: 0, Final: 1, Bütünleme: 2 }
    const sortedQR = [...questionResults].sort((a, b) => {
      const ea = examOrder[fv(a, 'examType')] ?? 99
      const eb = examOrder[fv(b, 'examType')] ?? 99
      if (ea !== eb) return ea - eb
      return (fv(a, 'questionNumber') ?? 0) - (fv(b, 'questionNumber') ?? 0)
    })

    return (
      <div className={styles.colStack}>
        {/* Bölüm K: PÇ başarı + DÖÇ-PÇ matrisi */}
        <SectionCard title="Bölüm K — Program Çıktıları (PÇ) Başarı ve İlişki Matrisi">
          {/* K.1 — DÖÇ-PÇ İlişki Matrisi */}
          <h4 className={styles.subSectionTitle}>K.1 — DÖÇ–PÇ İlişki Matrisi</h4>
          {cloPlomMatrix && (fv(cloPlomMatrix, 'rows') ?? []).length > 0 ? (
            <div className={styles.scrollTable}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>DÖÇ</th>
                    {(fv(cloPlomMatrix, 'ploCodes') ?? []).map((code, ci) => (
                      <th key={ci} title={(fv(cloPlomMatrix, 'ploDescriptions') ?? [])[ci] ?? ''}>{code}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {(fv(cloPlomMatrix, 'rows') ?? []).map((row, ri) => (
                    <tr key={ri}>
                      <td className={styles.resultCode}>{fv(row, 'cloCode')}</td>
                      {(fv(row, 'weights') ?? []).map((w, wi) => (
                        <td key={wi} className={styles.matrixCell}>
                          {w != null && w > 0 ? (
                            <span className={styles.matrixVal}>{Number(w).toFixed(2)}</span>
                          ) : (
                            <span className={styles.matrixEmpty}>—</span>
                          )}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className={styles.emptyInfo}>DÖÇ-PÇ ilişki matrisi bulunamadı. Yönetici panelinden DÖÇ-PÇ eşleştirmesi yapın.</p>
          )}

          {/* K.2 — PÇ Başarı Tablosu */}
          <h4 className={styles.subSectionTitle} style={{ marginTop: '1rem' }}>K.2 — PÇ Başarı Tablosu</h4>
          {ploResults_.length === 0 ? (
            <p className={styles.emptyInfo}>PÇ sonuçları henüz hesaplanmamış. MÜDEK hesaplamasını çalıştırın.</p>
          ) : (
            <div className={styles.resultsTable}>
              <div className={`${styles.resultsRow} ${styles.resultsHeader}`}>
                <span>PÇ Kodu</span>
                <span>Başarı</span>
                <span>Durum</span>
                <span>Notlar</span>
              </div>
              {ploResults_.map((plo) => {
                const extId = fv(plo, 'externalPloId')
                const note = ploNotes.find((n) => fv(n, 'externalPloId') === extId)
                const score = fv(plo, 'achievementScore')
                return (
                  <div key={extId} className={styles.resultsRow}>
                    <span className={styles.resultCode}>
                      {fv(plo, 'ploCode') ?? `PÇ#${extId}`}
                    </span>
                    <span className={achievementClass(score)}>{pct(score)}</span>
                    <span className={achievementClass(score)}>{fv(plo, 'status') ?? '—'}</span>
                    <div className={styles.noteCell}>
                      {fv(note, 'teacherNote') && (
                        <p className={styles.existingNote}>{fv(note, 'teacherNote')}</p>
                      )}
                      <button
                        type="button"
                        className={`${formStyles.rowActionText}`}
                        onClick={() => openPloNote(plo)}
                      >
                        {note ? 'Notu Düzenle' : 'Not Ekle'}
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </SectionCard>

        {/* Bölüm L: DÖÇ */}
        <SectionCard title="Bölüm L — DÖÇ Başarı Analizi">

          {/* L.1 — Soru-DÖÇ İlişki Tablosu */}
          <h4 className={styles.subSectionTitle}>L.1 — Soru–DÖÇ İlişki Tablosu</h4>
          {sortedQR.length === 0 ? (
            <p className={styles.emptyInfo}>Soru-DÖÇ eşleştirmesi henüz yok.</p>
          ) : (
            <div className={styles.scrollTable}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>Ölçme Aracı</th>
                    <th>Soru No</th>
                    <th>İlişkili DÖÇ(ler)</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedQR.map((q, i) => {
                    const codes = fv(q, 'linkedCloCodes') ?? []
                    const qLabel = fv(q, 'componentName') ?? (fv(q, 'questionNumber') != null ? `S${fv(q, 'questionNumber')}` : `#${i + 1}`)
                    return (
                      <tr key={i}>
                        <td>{fv(q, 'examType') ?? '—'}</td>
                        <td>{qLabel}</td>
                        <td>{codes.length ? codes.join(', ') : '—'}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* L.2 — Soru Başarı Analiz Tablosu */}
          <h4 className={styles.subSectionTitle} style={{ marginTop: '1rem' }}>L.2 — Soru Başarı Analiz Tablosu</h4>
          {sortedQR.length === 0 ? (
            <p className={styles.emptyInfo}>Soru başarı verisi henüz hesaplanmamış.</p>
          ) : (
            <div className={styles.scrollTable}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>Ölçme Aracı</th>
                    <th>Soru</th>
                    <th>Max Puan</th>
                    <th>Ortalama</th>
                    <th>Başarı %</th>
                    <th>DÖÇ</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedQR.map((q, i) => {
                    const codes = fv(q, 'linkedCloCodes') ?? []
                    const qLabel = fv(q, 'componentName') ?? (fv(q, 'questionNumber') != null ? `Soru ${fv(q, 'questionNumber')}` : `#${i + 1}`)
                    const achievement = fv(q, 'achievementRate')
                    return (
                      <tr key={i} className={achievement != null && achievement < 60 ? styles.gradeRowFailed : ''}>
                        <td>{fv(q, 'examType') ?? '—'}</td>
                        <td>{qLabel}</td>
                        <td>{fv(q, 'maxScore') != null ? Number(fv(q, 'maxScore')).toFixed(1) : '—'}</td>
                        <td>{fv(q, 'averageScore') != null ? Number(fv(q, 'averageScore')).toFixed(1) : '—'}</td>
                        <td className={achievementClass(achievement)}>
                          {achievement != null ? `%${Number(achievement).toFixed(1)}` : '—'}
                        </td>
                        <td>{codes.length ? codes.join(', ') : '—'}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* L.3 — DÖÇ Başarı Tablosu */}
          <h4 className={styles.subSectionTitle} style={{ marginTop: '1rem' }}>L.3 — DÖÇ Başarı Tablosu</h4>
          {cloResults.length === 0 ? (
            <p className={styles.emptyInfo}>DÖÇ sonuçları henüz hesaplanmamış. MÜDEK hesaplamasını çalıştırın.</p>
          ) : (
            <div className={styles.resultsTable}>
              <div className={`${styles.resultsRowClo} ${styles.resultsHeader}`}>
                <span>DÖÇ</span>
                <span>Vize</span>
                <span>Final</span>
                <span>Bütünleme</span>
                <span>Birleşik</span>
                <span>Durum</span>
                <span>Notlar</span>
              </div>
              {cloResults.map((clo) => {
                const extId = fv(clo, 'externalCloId')
                const note = cloNotes.find((n) => fv(n, 'externalCloId') === extId)
                const combined = fv(clo, 'combinedAchievement')
                const makeup = fv(clo, 'makeupAchievement')
                return (
                  <div key={extId} className={styles.resultsRowClo}>
                    <span className={styles.resultCode} title={fv(clo, 'cloDescription') ?? ''}>
                      {fv(clo, 'cloCode') ?? `DÖÇ#${extId}`}
                    </span>
                    <span>{pct(fv(clo, 'midtermAchievement'))}</span>
                    <span>{pct(fv(clo, 'finalAchievement'))}</span>
                    <span>{makeup != null ? pct(makeup) : '—'}</span>
                    <span className={achievementClass(combined)}>{pct(combined)}</span>
                    <span className={achievementClass(combined)}>{fv(clo, 'status') ?? '—'}</span>
                    <div className={styles.noteCell}>
                      {fv(note, 'teacherNote') && (
                        <p className={styles.existingNote}>{fv(note, 'teacherNote')}</p>
                      )}
                      <button
                        type="button"
                        className={`${formStyles.rowActionText}`}
                        onClick={() => openCloNote(clo)}
                      >
                        {note ? 'Notu Düzenle' : 'Not Ekle'}
                      </button>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </SectionCard>

        {/* Bölüm M: Anket-Ölçme Karşılaştırması */}
        <SectionCard title="Bölüm M — Anket ve Ölçme Sonuçlarının Karşılaştırması">
          {comparisons.length === 0 ? (
            <p className={styles.emptyInfo}>Karşılaştırma verisi henüz oluşturulmamış. MÜDEK hesaplaması ve anket gerekli.</p>
          ) : (
            <div className={styles.resultsTable}>
              <div className={`${styles.resultsRow} ${styles.resultsHeader}`}>
                <span>DÖÇ</span>
                <span>Ölçme</span>
                <span>Anket</span>
                <span>Fark</span>
                <span>Değerlendirme</span>
              </div>
              {comparisons.map((c) => {
                const extId = fv(c, 'externalCloId')
                const diff = fv(c, 'difference')
                const diffClass = diff == null ? '' : diff <= 10 ? styles.scoreGood : diff <= 20 ? styles.scoreWarn : styles.scoreBad
                return (
                  <div key={extId} className={styles.resultsRow}>
                    <span className={styles.resultCode}>{fv(c, 'cloCode') ?? `DÖÇ#${extId}`}</span>
                    <span>{pct(fv(c, 'measurementResult'))}</span>
                    <span>{pct(fv(c, 'surveyResult'))}</span>
                    <span className={diffClass}>{diff != null ? diff.toFixed(1) : '—'}</span>
                    <span>{fv(c, 'evaluation') ?? '—'}</span>
                  </div>
                )
              })}
            </div>
          )}
          {/* İyileştirme önerileri */}
          <div className={formStyles.field} style={{ marginTop: '1rem' }}>
            <label htmlFor="sr-m-improve">
              İyileştirme Önerileri{' '}
              <span className={styles.optLabel}>(fark analizi ve öneriler)</span>
            </label>
            <textarea
              id="sr-m-improve"
              className={formStyles.textarea}
              rows={6}
              value={sectionMImprovement}
              onChange={(e) => setSectionMImprovement(e.target.value)}
              placeholder="Ölçme sonuçları ile anket sonuçları arasındaki farklar incelendiğinde… Sonraki dönem için önerimiz…"
            />
          </div>
          <SaveRow saving={saving} onSave={handleSave} />
        </SectionCard>
      </div>
    )
  }

  async function handleExportPdf() {
    if (!preview || !report) return

    const token = getTeacherToken()
    const r = report
    const p = preview

    const esc = (s) => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

    const th = (...cols) => cols.map((c) => `<th>${esc(c)}</th>`).join('')
    const td = (...cols) => `<tr>${cols.map((c) => `<td>${esc(c)}</td>`).join('')}</tr>`

    const studentGrades = fv(p, 'studentGrades') ?? []
    const examStats = fv(p, 'examStats') ?? []
    const successStats = fv(p, 'successStats')
    const surveyQuestions = fv(p, 'surveyQuestions') ?? []
    const surveyCloResults = fv(p, 'surveyCloResults') ?? []
    const cloResults = fv(p, 'cloResults') ?? []
    const ploResults = fv(p, 'ploResults') ?? []
    const cloComparisons = fv(p, 'cloComparisons') ?? []
    const questionResults = fv(p, 'questionResults') ?? []

    const examOrder2 = { Vize: 0, Final: 1, Bütünleme: 2 }
    const sortedQR2 = [...questionResults].sort((a, b) => {
      const ea = examOrder2[fv(a, 'examType')] ?? 99
      const eb = examOrder2[fv(b, 'examType')] ?? 99
      if (ea !== eb) return ea - eb
      return (fv(a, 'questionNumber') ?? 0) - (fv(b, 'questionNumber') ?? 0)
    })
    const weeklyResources = fv(p, 'weeklyResources') ?? []
    const assessmentTools = fv(p, 'assessmentTools') ?? []
    const letterDist = fv(successStats, 'letterGradeDistribution') ?? {}

    // ── PDF için inline SVG donut chart ─────────────────────────────
    const PDF_GRADE_COLORS = {
      AA: '#2e7d32', BA: '#388e3c', BB: '#66bb6a',
      CB: '#aed581', CC: '#fff176', DC: '#ffb74d',
      DD: '#ef6c00', FD: '#e53935', FF: '#b71c1c',
    }
    function buildDonutSvg(dist, passed, failed) {
      const entries = Object.entries(dist).filter(([, v]) => v > 0)
      const total = entries.reduce((s, [, v]) => s + v, 0)
      if (total === 0) return ''
      const R = 52; const cx = 70; const cy = 70; const strokeW = 24
      const circ = 2 * Math.PI * R
      let cum = 0
      const slices = entries.map(([grade, cnt]) => {
        const frac = cnt / total
        const offset = circ * (1 - cum)
        const dash = circ * frac
        cum += frac
        const color = PDF_GRADE_COLORS[grade?.toUpperCase()] ?? '#90a4ae'
        return `<circle cx="${cx}" cy="${cy}" r="${R}" fill="none" stroke="${color}"
          stroke-width="${strokeW}" stroke-dasharray="${dash} ${circ - dash}"
          stroke-dashoffset="${offset}" transform="rotate(-90 ${cx} ${cy})">
          <title>${grade}: ${cnt}</title></circle>`
      }).join('')
      const legendItems = entries.map(([grade, cnt]) => {
        const color = PDF_GRADE_COLORS[grade?.toUpperCase()] ?? '#90a4ae'
        const pct = ((cnt / total) * 100).toFixed(1)
        return `<div style="display:flex;align-items:center;gap:5px;font-size:9pt;margin:2px 0">
          <span style="display:inline-block;width:12px;height:12px;border-radius:2px;background:${color}"></span>
          <span>${grade}: ${cnt} öğrenci (${pct}%)</span>
        </div>`
      }).join('')
      const statsLine = (passed != null || failed != null)
        ? `<p style="font-size:9pt;margin:4px 0"><span style="color:#1a7a1a;font-weight:600">${passed ?? 0} geçti</span>
           &nbsp;·&nbsp;<span style="color:#c00;font-weight:600">${failed ?? 0} kaldı</span></p>` : ''
      return `<div style="display:flex;align-items:flex-start;gap:1.5cm;margin:0.4cm 0;page-break-inside:avoid">
        <div style="text-align:center">
          <svg width="140" height="140" viewBox="0 0 140 140">
            ${slices}
            <text x="${cx}" y="${cy - 6}" text-anchor="middle" font-size="16" font-weight="bold" fill="#333">${total}</text>
            <text x="${cx}" y="${cy + 12}" text-anchor="middle" font-size="10" fill="#666">öğrenci</text>
          </svg>
          ${statsLine}
        </div>
        <div style="padding-top:10px">${legendItems}</div>
      </div>`
    }

    const gradeRows = studentGrades
      .sort((a, b) => (fv(a, 'studentNumber') ?? '').localeCompare(fv(b, 'studentNumber') ?? ''))
      .map((s) => td(
        fv(s, 'studentNumber') ?? '—',
        fv(s, 'studentName') ?? '—',
        fv(s, 'midtermScore') != null ? Number(fv(s, 'midtermScore')).toFixed(1) : '—',
        fv(s, 'finalScore') != null ? Number(fv(s, 'finalScore')).toFixed(1) : '—',
        fv(s, 'makeupScore') != null ? Number(fv(s, 'makeupScore')).toFixed(1) : '—',
        fv(s, 'successGrade') != null ? Number(fv(s, 'successGrade')).toFixed(1) : '—',
        fv(s, 'letterGrade') ?? '—',
        fv(s, 'isPassed') ? 'Geçti' : 'Kaldı',
      )).join('')

    const donutSvgHtml = buildDonutSvg(
      letterDist,
      fv(successStats, 'passedStudents'),
      fv(successStats, 'failedStudents'),
    )

    // ── Resim dosyalarını base64'e çevir, bölüm bazında grupla ─────
    const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg']
    const imageFiles = files.filter((f) => {
      const name = (fv(f, 'originalFileName') ?? '').toLowerCase()
      return imageExts.some((ext) => name.endsWith('.' + ext))
    })
    const imageBySec = {}
    for (const f of imageFiles) {
      try {
        const url = `${API_BASE_URL}/api/SemesterReport/${reportId}/files/${fv(f, 'id')}/download`
        const resp = await fetch(url, { headers: { Authorization: `Bearer ${token}` } })
        if (!resp.ok) continue
        const blob = await resp.blob()
        const dataUrl = await new Promise((res) => {
          const reader = new FileReader()
          reader.onload = (e) => res(e.target.result)
          reader.readAsDataURL(blob)
        })
        const sec = (fv(f, 'sectionCode') ?? 'X').toUpperCase()
        if (!imageBySec[sec]) imageBySec[sec] = []
        imageBySec[sec].push({
          name: esc(fv(f, 'originalFileName') ?? 'Dosya'),
          dataUrl,
          examTypeLabel: fv(f, 'examTypeLabel') ?? null,
          fileCategory: fv(f, 'fileCategory') ?? null,
        })
      } catch { /* atla */ }
    }

    // C bölümü: examTypeLabel (Vize/Final/Bütünleme) + fileCategory → "C.X Vize Sınav Soruları"
    const C_NUM_MAP = {
      'Vize|ExamPaper': 'C.1', 'Vize|AnswerKey': 'C.2',
      'Vize|StudentPaper_High': 'C.3', 'Vize|StudentPaper_Mid': 'C.4', 'Vize|StudentPaper_Low': 'C.5',
      'Final|ExamPaper': 'C.6', 'Final|AnswerKey': 'C.7',
      'Final|StudentPaper_High': 'C.8', 'Final|StudentPaper_Mid': 'C.9', 'Final|StudentPaper_Low': 'C.10',
      'Bütünleme|ExamPaper': 'C.11', 'Bütünleme|AnswerKey': 'C.12',
      'Bütünleme|StudentPaper_High': 'C.13', 'Bütünleme|StudentPaper_Mid': 'C.14', 'Bütünleme|StudentPaper_Low': 'C.15',
    }
    const CAT_NAME = {
      ExamPaper: 'Sınav Soruları', AnswerKey: 'Cevap Anahtarı',
      StudentPaper_High: 'En Yüksek Notlu Öğrenci Kağıdı',
      StudentPaper_Mid: 'Orta Düzey Öğrenci Kağıdı',
      StudentPaper_Low: 'En Düşük Notlu Öğrenci Kağıdı',
    }

    const buildImgLabel = (img, sec, idx) => {
      if (sec === 'C') {
        const examType = img.examTypeLabel ?? ''
        const cat = img.fileCategory ?? ''
        const cNum = C_NUM_MAP[`${examType}|${cat}`] ?? `C.${idx + 1}`
        const catName = CAT_NAME[cat] ?? cat
        return catName ? `${cNum} ${examType} ${catName}` : `${cNum} ${examType}`
      }
      if (sec === 'E') {
        const n = idx + 1
        return n > 1 ? `Ders Devam Çizelgesi ${n}` : 'Ders Devam Çizelgesi'
      }
      return img.name
    }

    // Her resim tam sayfa (A4 içinde max boyut, page-break-before: always)
    const renderSectionImages = (sec) => {
      const imgs = imageBySec[sec] ?? []
      if (!imgs.length) return ''
      return imgs.map((img, idx) => {
        const label = buildImgLabel(img, sec, idx)
        return `<div style="page-break-before:always;display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:calc(29.7cm - 3cm);padding:0.5cm 0">
          <p style="font-size:12pt;font-weight:bold;margin-bottom:0.3cm;align-self:flex-start;color:#222">${esc(label)}</p>
          <img src="${img.dataUrl}" style="max-width:100%;max-height:calc(29.7cm - 4cm);object-fit:contain" />
        </div>`
      }).join('')
    }

    const cloPlomMatrix = fv(p, 'cloPlomMatrix')

    const html = `<!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8">
<title></title>
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{font-family:'Times New Roman',serif;font-size:11pt;color:#111;background:#fff;padding:1.5cm 2cm}
  h1{font-size:14pt;text-align:center;margin-bottom:0.3cm}
  h2{font-size:12pt;margin:0.8cm 0 0.3cm;border-bottom:1px solid #333;padding-bottom:2px;break-after:avoid}
  h3{font-size:11pt;margin:0.5cm 0 0.2cm;break-after:avoid}
  h4{font-size:10.5pt;margin:0.4cm 0 0.15cm;color:#444;break-after:avoid}
  p{margin:0.2cm 0;line-height:1.5}
  table{width:100%;border-collapse:collapse;font-size:9.5pt;margin:0.3cm 0;break-inside:auto}
  thead{display:table-header-group}
  tr{break-inside:avoid;page-break-inside:avoid}
  th,td{border:1px solid #777;padding:3px 6px;text-align:left}
  th{background:#f0f0f0;font-weight:bold}
  .cover{text-align:center;margin-bottom:1cm;break-inside:avoid}
  .cover p{margin:0.15cm 0;font-size:11pt}
  .stats{display:flex;gap:1cm;flex-wrap:wrap;margin:0.3cm 0;break-inside:avoid}
  .stat{text-align:center}
  .stat .val{font-size:14pt;font-weight:bold}
  .stat .lbl{font-size:9pt;color:#555}
  .passed{color:#1a7a1a}
  .failed{color:#c00}
  .note-box{border:1px solid #ccc;padding:0.3cm;margin:0.2cm 0;white-space:pre-wrap;font-size:10pt;line-height:1.5;break-inside:avoid}
  .info-grid{display:grid;grid-template-columns:auto 1fr;gap:0.1cm 0.5cm;margin:0.2cm 0;font-size:10pt;break-inside:avoid}
  .info-label{font-weight:bold;white-space:nowrap}
  .no-break{break-inside:avoid;page-break-inside:avoid}
  @page{size:A4;margin:0}
  @media print{body{padding:1.5cm 2cm;-webkit-print-color-adjust:exact;print-color-adjust:exact}}
</style></head><body>

<div class="cover">
  ${fv(p, 'universityName') ? `<p style="font-size:12pt;font-weight:bold;letter-spacing:0.05em">${esc(fv(p, 'universityName'))}</p>` : ''}
  ${fv(p, 'facultyName') ? `<p style="font-size:11pt;font-weight:bold">${esc(fv(p, 'facultyName'))}</p>` : ''}
  ${fv(p, 'departmentName') ? `<p style="font-size:11pt;font-weight:bold;margin-bottom:0.4cm">${esc(fv(p, 'departmentName'))}</p>` : ''}
  <h1>DÖNEM SONU DERS DEĞERLENDİRME RAPORU</h1>
  <p><b>${esc(fv(r, 'courseCode'))} — ${esc(fv(r, 'courseName'))}</b></p>
  <p>Akademik Dönem: ${esc(fv(r, 'academicTermName') ?? '—')}</p>
  <p>Öğretim Üyesi: ${esc([fv(r, 'teacherTitle'), fv(r, 'teacherName')].filter(Boolean).join(' ') || '—')}</p>
  <p>Tarih: ${fv(r, 'signatureDate') ? esc(new Date(fv(r, 'signatureDate')).toLocaleDateString('tr-TR')) : '—'}</p>
</div>

${/* Bölüm A */ ''}
<h2>Bölüm A — Ders Tanıtım Bilgileri</h2>
<div class="info-grid">
  <span class="info-label">Ders Kodu:</span><span>${esc(fv(r, 'courseCode') ?? '—')}</span>
  <span class="info-label">Ders Adı:</span><span>${esc(fv(r, 'courseName') ?? '—')}</span>
  <span class="info-label">Akademik Dönem:</span><span>${esc(fv(r, 'academicTermName') ?? '—')}</span>
  ${fv(p, 'courseSemester') ? `<span class="info-label">Yarıyıl:</span><span>${esc(fv(p, 'courseSemester'))}</span>` : ''}
  ${fv(p, 'courseCredit') ? `<span class="info-label">Kredi:</span><span>${esc(fv(p, 'courseCredit'))}</span>` : ''}
  ${fv(p, 'courseEcts') ? `<span class="info-label">AKTS:</span><span>${esc(fv(p, 'courseEcts'))}</span>` : ''}
  ${(fv(p, 'courseTheoryHours') != null || fv(p, 'coursePracticeHours') != null) ? `<span class="info-label">Teorik / Uygulama:</span><span>${fv(p, 'courseTheoryHours') ?? '—'} / ${fv(p, 'coursePracticeHours') ?? '—'} saat/hafta</span>` : ''}
</div>
${fv(p, 'courseObjective') ? `<h4>Dersin Amacı</h4><p>${esc(fv(p, 'courseObjective'))}</p>` : ''}
${fv(p, 'courseContent') ? `<h4>Dersin İçeriği</h4><p>${esc(fv(p, 'courseContent'))}</p>` : ''}
${cloResults.length ? `<h4>Ders Öğrenim Çıktıları (DÖÇ)</h4>
<table><thead><tr>${th('DÖÇ Kodu', 'Açıklama')}</tr></thead><tbody>
${cloResults.map((c) => td(fv(c, 'cloCode') ?? '—', fv(c, 'cloDescription') ?? '—')).join('')}
</tbody></table>` : ''}
${fv(p, 'sectionANotes') ? `<h4>Ek Notlar</h4><div class="note-box">${esc(fv(p, 'sectionANotes'))}</div>` : ''}

${/* Bölüm B */ ''}
${weeklyResources.length ? `<h2>Bölüm B — Derste Kullanılan Kaynaklar</h2>
<table><thead><tr>${th('Hafta', 'Konu', 'Kaynak Türü', 'Kaynak Bilgisi', 'Bölüm / Sayfa', 'İçerik Özeti')}</tr></thead><tbody>
${weeklyResources.map((w) => td(fv(w, 'weekNumber'), fv(w, 'topic') ?? '—', fv(w, 'resourceType') ?? '—', fv(w, 'resourceInfo') ?? '—', fv(w, 'chapterPage') ?? '—', fv(w, 'contentSummary') ?? '—')).join('')}
</tbody></table>
${weeklyResources.some((w) => fv(w, 'contentSummary')) ? `<h4>Haftalık İçerik Özeti</h4>${weeklyResources.filter((w) => fv(w, 'contentSummary')).map((w) => `<p><strong>Hafta ${fv(w, 'weekNumber')}${fv(w, 'topic') ? ` (${esc(fv(w, 'topic'))})` : ''}:</strong> ${esc(fv(w, 'contentSummary'))}</p>`).join('')}` : ''}` : ''}

${/* Bölüm C */ ''}
${imageBySec['C']?.length ? `<h2>Bölüm C — Sınav Soruları ve Cevap Anahtarları</h2>${renderSectionImages('C')}` : ''}

${/* Bölüm D */ ''}
${assessmentTools.length ? `<div class="no-break"><h2>Bölüm D — Kullanılan Diğer Ölçme Araçları</h2>
<table><thead><tr>${th('Ad', 'Tür', 'Ağırlık (%)')}</tr></thead><tbody>
${assessmentTools.map((t) => td(fv(t, 'name') ?? '—', fv(t, 'type') ?? '—', fv(t, 'weightPercentage') != null ? `%${Number(fv(t, 'weightPercentage')).toFixed(0)}` : '—')).join('')}
</tbody></table></div>` : ''}

${/* Bölüm E */ ''}
${imageBySec['E']?.length ? `<h2>Bölüm E — Ders Devam Çizelgeleri</h2>${renderSectionImages('E')}` : ''}

${/* Bölüm F */ ''}
${studentGrades.length ? `<h2>Bölüm F — Sonuçlandırılmış Başarı Notları</h2>
${successStats ? `<div class="stats">
  <div class="stat"><div class="val">${fv(successStats, 'totalStudents')}</div><div class="lbl">Toplam</div></div>
  <div class="stat"><div class="val passed">${fv(successStats, 'passedStudents')}</div><div class="lbl">Geçti</div></div>
  <div class="stat"><div class="val failed">${fv(successStats, 'failedStudents')}</div><div class="lbl">Kaldı</div></div>
  <div class="stat"><div class="val">%${Number(fv(successStats, 'successPercentage') ?? 0).toFixed(1)}</div><div class="lbl">Başarı Oranı</div></div>
</div>${donutSvgHtml}` : ''}
<table><thead><tr>${th('Öğrenci No', 'Adı Soyadı', 'Vize', 'Final', 'Bütünleme', 'Başarı Notu', 'Harf', 'Durum')}</tr></thead>
<tbody>${gradeRows}</tbody></table>` : ''}

${/* Bölüm G */ ''}
${surveyQuestions.length ? `<h2>Bölüm G — Öğrenci Ders Değerlendirme Anket Sonuçları</h2>
<p style="font-size:9.5pt">Sadece geçen öğrencilerin yanıtları dahildir. Geçen öğrenci: ${fv(p, 'surveyPassingStudentCount') ?? 0} kişi · Değerlendirilen anket: ${fv(p, 'surveyTotalSubmissions') ?? 0} adet</p>
<table><thead><tr>${th('#', 'Soru', 'DÖÇ', 'Yanıt', 'Ortalama', '%')}</tr></thead><tbody>
${surveyQuestions.map((q) => td(fv(q, 'orderIndex') ?? '', fv(q, 'questionText') ?? '—', fv(q, 'cloCode') ?? '—', fv(q, 'responseCount') ?? 0, fv(q, 'averageScore') != null ? `${Number(fv(q, 'averageScore')).toFixed(2)} / ${fv(q, 'scaleMax')}` : '—', fv(q, 'scorePercentage') != null ? `%${Number(fv(q, 'scorePercentage')).toFixed(1)}` : '—')).join('')}
</tbody></table>
${fv(p, 'sectionGDiscussion') ? `<h4>Anket Sonuçları Tartışması</h4><div class="note-box">${esc(fv(p, 'sectionGDiscussion'))}</div>` : ''}` : ''}

${/* Bölüm H */ ''}
${examStats.length ? `<div class="no-break"><h2>Bölüm H — Sınav İstatistikleri ve Başarı Oranı Yorumu</h2>
<table><thead><tr>${th('Sınav', 'Katılan', 'En Yüksek', 'En Düşük', 'Ortalama')}</tr></thead><tbody>
${examStats.map((e) => td(fv(e, 'examType') ?? '—', fv(e, 'participantCount'), fv(e, 'maxScore') != null ? Number(fv(e, 'maxScore')).toFixed(1) : '—', fv(e, 'minScore') != null ? Number(fv(e, 'minScore')).toFixed(1) : '—', fv(e, 'averageScore') != null ? Number(fv(e, 'averageScore')).toFixed(1) : '—')).join('')}
</tbody></table>
${fv(p, 'sectionHCommentary') ? `<h4>Başarı Oranı Yorumu</h4><div class="note-box">${esc(fv(p, 'sectionHCommentary'))}</div>` : ''}</div>` : ''}

${/* Bölüm İ */ ''}
${fv(p, 'sectionIGeneralEvaluation') ? `<h2>Bölüm İ — Genel Değerlendirme ve Öneriler</h2><div class="note-box">${esc(fv(p, 'sectionIGeneralEvaluation'))}</div>` : ''}

${/* Bölüm J */ ''}
${fv(p, 'sectionJChangesFromPrevious') ? `<h2>Bölüm J — Geçmiş Dönemden Farklı Yapılan Değişiklikler</h2><div class="note-box">${esc(fv(p, 'sectionJChangesFromPrevious'))}</div>` : ''}

${/* Bölüm K */ ''}
${(ploResults.length || (cloPlomMatrix && (fv(cloPlomMatrix, 'rows') ?? []).length > 0)) ? `<h2>Bölüm K — Program Çıktıları (PÇ)</h2>` : ''}
${cloPlomMatrix && (fv(cloPlomMatrix, 'rows') ?? []).length > 0 ? (() => {
  const ploCodes = fv(cloPlomMatrix, 'ploCodes') ?? []
  const rows = fv(cloPlomMatrix, 'rows') ?? []
  return `<div class="no-break"><h3>K.1 — DÖÇ–PÇ İlişki Matrisi</h3>
<table><thead><tr><th>DÖÇ</th>${ploCodes.map((c) => `<th>${esc(c)}</th>`).join('')}</tr></thead><tbody>
${rows.map((row) => `<tr><td><b>${esc(fv(row, 'cloCode'))}</b></td>${(fv(row, 'weights') ?? []).map((w) => `<td style="text-align:center">${w != null && w > 0 ? Number(w).toFixed(2) : '—'}</td>`).join('')}</tr>`).join('')}
</tbody></table></div>`
})() : ''}
${ploResults.length ? `<div class="no-break"><h3>K.2 — PÇ Başarı Tablosu</h3>
<table><thead><tr>${th('PÇ Kodu', 'Açıklama', 'Başarı %', 'Durum')}</tr></thead><tbody>
${ploResults.map((p2) => td(fv(p2, 'ploCode') ?? '—', fv(p2, 'ploTitle') ?? '—', fv(p2, 'achievementScore') != null ? `%${Number(fv(p2, 'achievementScore')).toFixed(1)}` : '—', fv(p2, 'status') ?? '—')).join('')}
</tbody></table></div>` : ''}

${/* Bölüm L */ ''}
${(sortedQR2.length || cloResults.length) ? `<h2>Bölüm L — DÖÇ Başarı Analizi</h2>` : ''}
${sortedQR2.length ? `<h3>L.1 — Soru–DÖÇ İlişki Tablosu</h3>
<table><thead><tr>${th('Ölçme Aracı', 'Soru', 'İlişkili DÖÇ(ler)', 'Ağırlık')}</tr></thead><tbody>
${sortedQR2.map((q, i) => {
  const codes = fv(q, 'linkedCloCodes') ?? []
  const weights = fv(q, 'linkedCloWeights') ?? {}
  const qLabel = fv(q, 'componentName') ?? (fv(q, 'questionNumber') != null ? `Soru ${fv(q, 'questionNumber')}` : `#${i + 1}`)
  const cloStr = codes.length ? codes.join(', ') : '—'
  const wStr = codes.length
    ? codes.map((c) => weights[c] != null ? `${c}: ${Number(weights[c]).toFixed(2)}` : c).join(', ')
    : '—'
  return td(fv(q, 'examType') ?? '—', qLabel, cloStr, wStr)
}).join('')}
</tbody></table>
<h3>L.2 — Soru Başarı Analiz Tablosu</h3>
<table><thead><tr>${th('Ölçme Aracı', 'Soru', 'Max Puan', 'Ortalama', 'Başarı %', 'DÖÇ (Ağırlık)')}</tr></thead><tbody>
${sortedQR2.map((q, i) => {
  const codes = fv(q, 'linkedCloCodes') ?? []
  const weights = fv(q, 'linkedCloWeights') ?? {}
  const qLabel = fv(q, 'componentName') ?? (fv(q, 'questionNumber') != null ? `Soru ${fv(q, 'questionNumber')}` : `#${i + 1}`)
  const cloWithW = codes.length
    ? codes.map((c) => weights[c] != null ? `${c}(${Number(weights[c]).toFixed(2)})` : c).join(', ')
    : '—'
  return td(fv(q, 'examType') ?? '—', qLabel, fv(q, 'maxScore') != null ? Number(fv(q, 'maxScore')).toFixed(1) : '—', fv(q, 'averageScore') != null ? Number(fv(q, 'averageScore')).toFixed(1) : '—', fv(q, 'achievementRate') != null ? `%${Number(fv(q, 'achievementRate')).toFixed(1)}` : '—', cloWithW)
}).join('')}
</tbody></table>` : ''}
${cloResults.length ? `<div class="no-break"><h3>L.3 — DÖÇ Başarı Tablosu</h3>
<table><thead><tr>${th('DÖÇ', 'Açıklama', 'Vize %', 'Final %', 'Büt. %', 'Birleşik %', 'Durum')}</tr></thead><tbody>
${cloResults.map((c) => td(fv(c, 'cloCode') ?? '—', fv(c, 'cloDescription') ?? '—', fv(c, 'midtermAchievement') != null ? `%${Number(fv(c, 'midtermAchievement')).toFixed(1)}` : '—', fv(c, 'finalAchievement') != null ? `%${Number(fv(c, 'finalAchievement')).toFixed(1)}` : '—', fv(c, 'makeupAchievement') != null ? `%${Number(fv(c, 'makeupAchievement')).toFixed(1)}` : '—', fv(c, 'combinedAchievement') != null ? `%${Number(fv(c, 'combinedAchievement')).toFixed(1)}` : '—', fv(c, 'status') ?? '—')).join('')}
</tbody></table></div>` : ''}

${/* Bölüm M */ ''}
${cloComparisons.length ? `<div class="no-break"><h2>Bölüm M — Anket–Ölçme Karşılaştırması ve İyileştirme Önerileri</h2>
<table><thead><tr>${th('DÖÇ', 'Ölçme %', 'Anket %', 'Fark', 'Değerlendirme')}</tr></thead><tbody>
${cloComparisons.map((c) => td(fv(c, 'cloCode') ?? '—', fv(c, 'measurementResult') != null ? `%${Number(fv(c, 'measurementResult')).toFixed(1)}` : '—', fv(c, 'surveyResult') != null ? `%${Number(fv(c, 'surveyResult')).toFixed(1)}` : '—', fv(c, 'difference') != null ? Number(fv(c, 'difference')).toFixed(1) : '—', fv(c, 'evaluation') ?? '—')).join('')}
</tbody></table></div>
${fv(p, 'sectionMImprovement') ? `<div class="note-box">${esc(fv(p, 'sectionMImprovement'))}</div>` : ''}` : ''}

<div style="margin-top:2cm;border-top:1px solid #333;padding-top:0.5cm">
  <p>İmzalayan: ${esc(fv(r, 'signatureName') ?? '—')}</p>
  <p>Tarih: ${fv(r, 'signatureDate') ? esc(new Date(fv(r, 'signatureDate')).toLocaleDateString('tr-TR')) : '—'}</p>
</div>

<script>
  window.onload = () => {
    // Kısa gecikme → tüm görsellerin yüklenmesini bekle
    setTimeout(() => window.print(), 300)
  }
<\/script>
</body></html>`

    const finalHtml = html

    const win = window.open('', '_blank')
    if (!win) { alert('Popup engellendi. Tarayıcınızın popup engelleyicisini devre dışı bırakın.'); return }
    win.document.write(finalHtml)
    win.document.close()
  }

  function renderPreview() {
    if (!preview) return <p className={styles.emptyInfo}>Önizleme yükleniyor…</p>

    const studentGrades = fv(preview, 'studentGrades') ?? []
    const examStats = fv(preview, 'examStats') ?? []
    const successStats = fv(preview, 'successStats')
    const surveyResults = fv(preview, 'surveyCloResults') ?? []

    return (
      <div className={styles.colStack}>
        <div className={styles.previewToolbar}>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={handleExportPdf}
          >
            PDF'e Aktar / Yazdır
          </button>
          <span className={styles.previewToolbarHint}>
            Yazdır diyaloğu açıldığında: <strong>Diğer ayarlar → Üst bilgi ve alt bilgi</strong> seçeneğini{' '}
            <strong>kaldırın</strong>, ardından &quot;PDF olarak kaydet&quot; seçin.
          </span>
        </div>

        {/* Bölüm F: Öğrenci notları */}
        <SectionCard title="Bölüm F — Öğrenci Başarı Notları" defaultOpen={false}>
          {studentGrades.length === 0 ? (
            <p className={styles.emptyInfo}>Henüz not verisi yok.</p>
          ) : (
            <div className={styles.scrollTable}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>Öğrenci No</th>
                    <th>Ad Soyad</th>
                    <th>Vize</th>
                    <th>Final</th>
                    <th>Bütünleme</th>
                    <th>Başarı Notu</th>
                    <th>Harf</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {studentGrades.map((s) => (
                    <tr key={fv(s, 'externalStudentId')}>
                      <td>{fv(s, 'studentNumber') ?? '—'}</td>
                      <td>{fv(s, 'studentName') ?? '—'}</td>
                      <td>{fv(s, 'midtermScore') != null ? Number(fv(s, 'midtermScore')).toFixed(1) : '—'}</td>
                      <td>{fv(s, 'finalScore') != null ? Number(fv(s, 'finalScore')).toFixed(1) : '—'}</td>
                      <td>{fv(s, 'makeupScore') != null ? Number(fv(s, 'makeupScore')).toFixed(1) : '—'}</td>
                      <td>{fv(s, 'successGrade') != null ? Number(fv(s, 'successGrade')).toFixed(1) : '—'}</td>
                      <td>{fv(s, 'letterGrade') ?? '—'}</td>
                      <td>{fv(s, 'isPassed') ? 'Geçti' : 'Kaldı'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </SectionCard>

        {/* Bölüm H: Başarı istatistikleri */}
        <SectionCard title="Bölüm H — Ders Başarı Oranları" defaultOpen={false}>
          {successStats ? (
            <div className={styles.statsGrid}>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>Toplam Öğrenci</span>
                <span className={styles.statValue}>{fv(successStats, 'totalStudents')}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>Geçen</span>
                <span className={`${styles.statValue} ${styles.scoreGood}`}>{fv(successStats, 'passedStudents')}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>Kalan</span>
                <span className={`${styles.statValue} ${styles.scoreBad}`}>{fv(successStats, 'failedStudents')}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>Başarı Yüzdesi</span>
                <span className={styles.statValue}>{pct(fv(successStats, 'successPercentage'))}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>FF/FD</span>
                <span className={styles.statValue}>{fv(successStats, 'gradeFF_FD') ?? fv(successStats, 'gradeFFFD')}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>DD/DC</span>
                <span className={styles.statValue}>{fv(successStats, 'gradeDD_DC') ?? fv(successStats, 'gradeDDDC')}</span>
              </div>
              <div className={styles.statBox}>
                <span className={styles.statLabel}>CC ve Üstü</span>
                <span className={`${styles.statValue} ${styles.scoreGood}`}>{fv(successStats, 'gradeCC_Above') ?? fv(successStats, 'gradeCCAbove')}</span>
              </div>
            </div>
          ) : (
            <p className={styles.emptyInfo}>Başarı istatistikleri henüz hesaplanmamış.</p>
          )}

          {examStats.length > 0 && (
            <div className={styles.scrollTable} style={{ marginTop: '1rem' }}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>Sınav</th>
                    <th>Katılan</th>
                    <th>En Yüksek</th>
                    <th>En Düşük</th>
                    <th>Ortalama</th>
                  </tr>
                </thead>
                <tbody>
                  {examStats.map((e, i) => (
                    <tr key={i}>
                      <td>{fv(e, 'examType') ?? '—'}</td>
                      <td>{fv(e, 'participantCount')}</td>
                      <td>{fv(e, 'maxScore') != null ? Number(fv(e, 'maxScore')).toFixed(1) : '—'}</td>
                      <td>{fv(e, 'minScore') != null ? Number(fv(e, 'minScore')).toFixed(1) : '—'}</td>
                      <td>{fv(e, 'averageScore') != null ? Number(fv(e, 'averageScore')).toFixed(1) : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </SectionCard>

        {/* Bölüm G: Anket sonuçları */}
        <SectionCard title="Bölüm G — Öğrenci Ders Değerlendirme Anket Sonuçları" defaultOpen={false}>
          {surveyResults.length === 0 ? (
            <p className={styles.emptyInfo}>Anket sonuçları veya DÖÇ eşleştirmesi henüz mevcut değil.</p>
          ) : (
            <div className={styles.scrollTable}>
              <table className={styles.previewTable}>
                <thead>
                  <tr>
                    <th>DÖÇ</th>
                    <th>Likert Ort.</th>
                    <th>Yüzde</th>
                    <th>Yorum</th>
                  </tr>
                </thead>
                <tbody>
                  {surveyResults.map((s) => (
                    <tr key={fv(s, 'externalCloId')}>
                      <td>{fv(s, 'cloCode') ?? '—'}</td>
                      <td>{fv(s, 'likertAverage') != null ? Number(fv(s, 'likertAverage')).toFixed(2) : '—'}</td>
                      <td>{pct(fv(s, 'surveyPercentage'))}</td>
                      <td>{fv(s, 'comment') ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </SectionCard>
      </div>
    )
  }

  // ────────────────────────────────────────────────────────────────
  // Render
  // ────────────────────────────────────────────────────────────────

  return (
    <PageSection
      title={pageTitle}
      description={`Dönem Sonu Ders Değerlendirme Raporu · ${fv(report, 'academicTermName') ?? ''}`}
      error={error}
      loading={loading}
    >
      {/* Geri butonu */}
      <div className={styles.backRow}>
        <button
          type="button"
          className={`${formStyles.btn} ${formStyles.btnGhost}`}
          onClick={() => navigate(appConfig.routes.semesterReports)}
        >
          <ArrowLeft size={15} aria-hidden />
          Raporlarım
        </button>
      </div>

      {/* Sekme çubuğu */}
      <div className={styles.tabBar} role="tablist">
        {TABS.map((t) => (
          <button
            key={t.key}
            type="button"
            role="tab"
            aria-selected={activeTab === t.key}
            className={activeTab === t.key ? `${styles.tab} ${styles.tabActive}` : styles.tab}
            onClick={() => setActiveTab(t.key)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Sekme içerikleri */}
      <div className={styles.tabContent}>
        {activeTab === 'overview' && renderOverview()}
        {activeTab === 'cover-a-b' && renderCoverAB()}
        {activeTab === 'files-c-g' && renderFilesCG()}
        {activeTab === 'eval-i-j' && renderEvalIJ()}
        {activeTab === 'results-k-l-m' && renderResultsKLM()}
        {activeTab === 'preview' && renderPreview()}
      </div>

      {/* ──── Dialog: Haftalık Kaynak ──────────────────────────────────── */}
      <AppDialog
        open={weekOpen}
        title={`Hafta ${weekNum} Kaynağı`}
        onClose={() => setWeekOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setWeekOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={weekSaving}
              onClick={() => void handleSaveWeek()}
            >
              {weekSaving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        }
      >
        <div className={styles.formGrid2}>
          <div className={formStyles.field}>
            <label htmlFor="w-week">Hafta numarası</label>
            <select id="w-week" className={formStyles.select} value={weekNum} onChange={(e) => setWeekNum(Number(e.target.value))}>
              {Array.from({ length: 16 }, (_, i) => i + 1).map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
          </div>
          <div className={formStyles.field}>
            <label htmlFor="w-type">Kaynak türü</label>
            <input id="w-type" className={formStyles.input} value={weekResType} onChange={(e) => setWeekResType(e.target.value)} placeholder="Ders Kitabı / Ders Notu / Sunum" />
          </div>
        </div>
        <div className={formStyles.field}>
          <label htmlFor="w-topic">İşlenen konu</label>
          <input id="w-topic" className={formStyles.input} value={weekTopic} onChange={(e) => setWeekTopic(e.target.value)} placeholder="Konunun başlığı" />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="w-info">Kaynak bilgisi</label>
          <input id="w-info" className={formStyles.input} value={weekResInfo} onChange={(e) => setWeekResInfo(e.target.value)} placeholder="Yazar, Kitap Adı, Yayınevi, Yıl" />
        </div>
        <div className={styles.formGrid2}>
          <div className={formStyles.field}>
            <label htmlFor="w-chapter">Bölüm / Sayfa</label>
            <input id="w-chapter" className={formStyles.input} value={weekChapter} onChange={(e) => setWeekChapter(e.target.value)} placeholder="Bölüm 1, s.1–15" />
          </div>
          <div className={formStyles.field}>
            <label htmlFor="w-desc">Kısa açıklama</label>
            <input id="w-desc" className={formStyles.input} value={weekDesc} onChange={(e) => setWeekDesc(e.target.value)} placeholder="Teorik anlatım" />
          </div>
        </div>
        <div className={formStyles.field}>
          <label htmlFor="w-summary">Haftalık içerik özeti</label>
          <textarea id="w-summary" className={formStyles.textarea} rows={4} value={weekSummary} onChange={(e) => setWeekSummary(e.target.value)} placeholder="Bu haftada işlenen konular kısaca…" />
        </div>
      </AppDialog>

      {/* ──── Dialog: Dosya Yükleme ────────────────────────────────────── */}
      <AppDialog
        open={uploadDialogOpen}
        title="Dosya Yükle"
        onClose={() => setUploadDialogOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setUploadDialogOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={uploading}
              onClick={() => void handleUpload()}
            >
              {uploading ? 'Yükleniyor…' : 'Yükle'}
            </button>
          </div>
        }
      >
        <div className={formStyles.field}>
          <label htmlFor="up-section">Bölüm</label>
          <select id="up-section" className={formStyles.select} value={uploadSection} onChange={(e) => setUploadSection(e.target.value)}>
            <option value="C">C — Sınav Kağıtları ve Cevap Anahtarları</option>
            <option value="D">D — Diğer Ölçme Araçları</option>
            <option value="E">E — Ders Devam Çizelgeleri</option>
          </select>
        </div>
        {uploadSection === 'C' && (
          <div className={formStyles.field}>
            <label htmlFor="up-exam-type">Sınav Türü</label>
            <select id="up-exam-type" className={formStyles.select} value={uploadExamTypeLabel} onChange={(e) => setUploadExamTypeLabel(e.target.value)}>
              <option value="Vize">Vize (C.1–C.5)</option>
              <option value="Final">Final (C.6–C.10)</option>
              <option value="Bütünleme">Bütünleme (C.11–C.15)</option>
            </select>
          </div>
        )}
        <div className={formStyles.field}>
          <label htmlFor="up-category">Dosya kategorisi</label>
          <select id="up-category" className={formStyles.select} value={uploadCategory} onChange={(e) => setUploadCategory(e.target.value)}>
            {uploadSection === 'C' ? (
              <>
                <option value="ExamPaper">Sınav Soruları</option>
                <option value="AnswerKey">Cevap Anahtarı</option>
                <option value="StudentPaper_High">En Yüksek Notlu Öğrenci Kağıdı</option>
                <option value="StudentPaper_Mid">Orta Düzey Öğrenci Kağıdı</option>
                <option value="StudentPaper_Low">En Düşük Notlu Öğrenci Kağıdı</option>
              </>
            ) : uploadSection === 'D' ? (
              <>
                <option value="OtherTool">Diğer Ölçme Aracı</option>
                <option value="Other">Diğer</option>
              </>
            ) : (
              <option value="AttendanceSheet">Devam Çizelgesi</option>
            )}
          </select>
        </div>
        <div className={formStyles.field}>
          <label htmlFor="up-file">Dosya seçin (PDF, JPG, PNG, DOC, XLS)</label>
          <input
            id="up-file"
            type="file"
            ref={fileInputRef}
            className={formStyles.input}
            accept=".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"
          />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="up-notes">Not (isteğe bağlı)</label>
          <input id="up-notes" className={formStyles.input} value={uploadNotes} onChange={(e) => setUploadNotes(e.target.value)} placeholder="Ek açıklama" />
        </div>
      </AppDialog>

      {/* ──── Dialog: DÖÇ Notu ────────────────────────────────────────── */}
      <AppDialog
        open={cloNoteOpen}
        title={`DÖÇ Notu — ${fv(cloNoteData, 'cloCode') ?? ''}`}
        onClose={() => setCloNoteOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setCloNoteOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={cloNoteSaving}
              onClick={() => void handleSaveCloNote()}
            >
              {cloNoteSaving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        }
      >
        {cloNoteData && (
          <p className={styles.noteHint}>{fv(cloNoteData, 'cloDescription')}</p>
        )}
        <div className={formStyles.field}>
          <label htmlFor="clo-note-text">Öğretmen yorumu</label>
          <textarea
            id="clo-note-text"
            className={formStyles.textarea}
            rows={5}
            value={cloNoteText}
            onChange={(e) => setCloNoteText(e.target.value)}
            placeholder="Bu DÖÇ için ölçme sonucuna dair değerlendirmeniz…"
          />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="clo-improvement">İyileştirme önerisi</label>
          <textarea
            id="clo-improvement"
            className={formStyles.textarea}
            rows={4}
            value={cloImprovement}
            onChange={(e) => setCloImprovement(e.target.value)}
            placeholder="Gelecek dönem için öneriler…"
          />
        </div>
      </AppDialog>

      {/* ──── Dialog: PÇ Notu ─────────────────────────────────────────── */}
      <AppDialog
        open={ploNoteOpen}
        title={`PÇ Notu — ${fv(ploNoteData, 'ploCode') ?? ''}`}
        onClose={() => setPloNoteOpen(false)}
        footer={
          <div className={formStyles.actions}>
            <button type="button" className={`${formStyles.btn} ${formStyles.btnGhost}`} onClick={() => setPloNoteOpen(false)}>
              Vazgeç
            </button>
            <button
              type="button"
              className={`${formStyles.btn} ${formStyles.btnPrimary}`}
              disabled={ploNoteSaving}
              onClick={() => void handleSavePloNote()}
            >
              {ploNoteSaving ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        }
      >
        {ploNoteData && (
          <p className={styles.noteHint}>{fv(ploNoteData, 'ploDescription')}</p>
        )}
        <div className={formStyles.field}>
          <label htmlFor="plo-note-text">Öğretmen yorumu</label>
          <textarea
            id="plo-note-text"
            className={formStyles.textarea}
            rows={5}
            value={ploNoteText}
            onChange={(e) => setPloNoteText(e.target.value)}
            placeholder="Bu PÇ için başarı oranına dair değerlendirmeniz…"
          />
        </div>
        <div className={formStyles.field}>
          <label htmlFor="plo-improvement">İyileştirme önerisi</label>
          <textarea
            id="plo-improvement"
            className={formStyles.textarea}
            rows={4}
            value={ploImprovement}
            onChange={(e) => setPloImprovement(e.target.value)}
            placeholder="Gelecek dönem için öneriler…"
          />
        </div>
      </AppDialog>
    </PageSection>
  )
}
