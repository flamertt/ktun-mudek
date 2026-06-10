import { Search } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'

import { fetchTeacherCoursesByEmail, fetchUniversityAcademicTerms } from '../../api/adminApi'
import { getAdminToken } from '../../lib/authToken'
import formStyles from '../admin-form/AdminForm.module.css'
import styles from './TeacherCoursePicker.module.css'

/**
 * Öğretmenin derslerini e-posta + dönem üzerinden arar ve seçim yapar.
 *
 * @param {{ onSelect: (course: { courseId: number, programId: number, courseCode: string, courseName: string, courseOfferingId: number }) => void, selectedCourseId?: number }} props
 */
export function TeacherCoursePicker({ onSelect, selectedCourseId }) {
  const [terms, setTerms] = useState([])
  const [termId, setTermId] = useState('')
  const [email, setEmail] = useState('')
  const [courses, setCourses] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [searched, setSearched] = useState(false)

  useEffect(() => {
    const token = getAdminToken()
    if (!token) return
    fetchUniversityAcademicTerms(token)
      .then((data) => {
        const list = Array.isArray(data) ? data : []
        setTerms(list)
        if (list.length) {
          const first = list[0]
          setTermId(String(first?.academicTermId ?? first?.AcademicTermId ?? first?.id ?? first?.Id ?? ''))
        }
      })
      .catch(() => {})
  }, [])

  const handleSearch = useCallback(async () => {
    const token = getAdminToken()
    const trimmedEmail = email.trim()
    if (!token || !trimmedEmail || !termId) {
      setError('E-posta ve dönem zorunludur.')
      return
    }
    setLoading(true)
    setError('')
    setCourses([])
    setSearched(true)
    try {
      const data = await fetchTeacherCoursesByEmail(token, trimmedEmail, Number(termId))
      setCourses(Array.isArray(data) ? data : [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Dersler alınamadı.')
    } finally {
      setLoading(false)
    }
  }, [email, termId])

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') void handleSearch()
  }

  return (
    <div className={styles.root}>
      <p className={styles.hint}>
        Öğretmenin e-postasını ve dönemi seçerek derslerini listeleyin; ardından CLO eklemek istediğiniz derse tıklayın.
      </p>

      <div className={styles.filterRow}>
        <div className={styles.fieldGroup}>
          <label htmlFor="tcp-email" className={styles.label}>Öğretim elemanı e-postası</label>
          <input
            id="tcp-email"
            className={formStyles.input}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="örn. mgunduz"
            style={{ minWidth: '16rem' }}
          />
        </div>

        <div className={styles.fieldGroup}>
          <label htmlFor="tcp-term" className={styles.label}>Dönem</label>
          <select
            id="tcp-term"
            className={formStyles.input}
            value={termId}
            onChange={(e) => setTermId(e.target.value)}
            style={{ minWidth: '12rem' }}
          >
            {terms.length === 0 && <option value="">Yükleniyor…</option>}
            {terms.map((t) => {
              const id = t?.academicTermId ?? t?.AcademicTermId ?? t?.id ?? t?.Id ?? ''
              const name = t?.ad ?? t?.Ad ?? t?.academicTermName ?? t?.AcademicTermName ?? t?.name ?? t?.Name ?? t?.termName ?? t?.TermName ?? String(id)
              return <option key={id} value={id}>{name}</option>
            })}
          </select>
        </div>

        <div className={styles.fieldGroup} style={{ justifyContent: 'flex-end' }}>
          <label style={{ visibility: 'hidden', fontSize: '0.82rem' }}>_</label>
          <button
            type="button"
            className={`${formStyles.btn} ${formStyles.btnPrimary}`}
            onClick={() => void handleSearch()}
            disabled={loading}
            style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
          >
            <Search size={15} aria-hidden />
            {loading ? 'Aranıyor…' : 'Ders ara'}
          </button>
        </div>
      </div>

      {error && <p className={styles.error}>{error}</p>}

      {searched && !loading && courses.length === 0 && !error && (
        <p className={styles.empty}>Bu dönemde bu öğretmene ait ders bulunamadı.</p>
      )}

      {courses.length > 0 && (
        <ul className={styles.courseList}>
          {courses.map((c) => {
            const courseId = c?.courseId ?? c?.CourseId
            const programId = c?.programId ?? c?.ProgramId
            const code = c?.courseCode ?? c?.CourseCode ?? '—'
            const name = c?.courseName ?? c?.CourseName ?? '—'
            const offeringId = c?.courseOfferingId ?? c?.CourseOfferingId
            const isSelected = selectedCourseId != null && Number(selectedCourseId) === Number(courseId)
            return (
              <li
                key={`${courseId}-${offeringId}`}
                className={`${styles.courseItem} ${isSelected ? styles.courseItemSelected : ''}`}
                onClick={() =>
                  onSelect({ courseId: Number(courseId), programId: Number(programId), courseCode: code, courseName: name, courseOfferingId: Number(offeringId) })
                }
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ')
                    onSelect({ courseId: Number(courseId), programId: Number(programId), courseCode: code, courseName: name, courseOfferingId: Number(offeringId) })
                }}
                aria-pressed={isSelected}
              >
                <span className={styles.courseCode}>{code}</span>
                <span className={styles.courseName}>{name}</span>
                <span className={styles.courseMeta}>
                  courseId: <strong>{courseId}</strong>
                  {programId ? <> · programId: <strong>{programId}</strong></> : null}
                </span>
                {isSelected && <span className={styles.selectedBadge}>Seçildi</span>}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
