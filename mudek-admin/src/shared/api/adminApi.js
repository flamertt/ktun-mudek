import { getAdminToken } from '../lib/authToken'
import { deleteJsonWithAuth, getJson, postJsonWithAuth, putJsonWithAuth } from './httpClient'

const ADMIN = '/api/Admin'

/** Açıkça verilen token yoksa localStorage’daki admin token’ı kullanır (Bearer’sız istek / 401 önlemi). */
function authToken(explicit) {
  if (explicit != null && String(explicit).trim() !== '') return String(explicit).trim()
  return getAdminToken()?.trim() || undefined
}

// ═══════════════════════════════════════════════════════════════════════════
// Üniversite API (salt okuma) + aktif dönem senkronu
// ═══════════════════════════════════════════════════════════════════════════

export function fetchUniversityPrograms(token) {
  return getJson(`${ADMIN}/university/programs`, { token: authToken(token) })
}

export function fetchUniversityAcademicTerms(token) {
  return getJson(`${ADMIN}/university/academic-terms`, { token: authToken(token) })
}

export function fetchUniversityActiveAcademicTerm(token) {
  return getJson(`${ADMIN}/university/academic-terms/active`, { token: authToken(token) })
}

/** Üniversiteden aktif dönemi çekip DB'ye yazar. */
export function syncUniversityActiveAcademicTerm(token) {
  return postJsonWithAuth(`${ADMIN}/university/academic-terms/sync`, {}, authToken(token))
}

/** DB'deki aktif dönem (önce sync önerilir). */
export function fetchDbActiveAcademicTerm(token) {
  return getJson(`${ADMIN}/active-term`, { token: authToken(token) })
}

export function fetchUniversityProgramOutcomes(token, programId) {
  return getJson(`${ADMIN}/university/programs/${programId}/outcomes`, { token: authToken(token) })
}

export function fetchUniversityCourseClos(token, courseId) {
  return getJson(`${ADMIN}/university/courses/${courseId}/clos`, { token: authToken(token) })
}

export function fetchUniversityCourseCloPoMap(token, courseId) {
  return getJson(`${ADMIN}/university/courses/${courseId}/clo-po-map`, { token: authToken(token) })
}

// ═══════════════════════════════════════════════════════════════════════════
// MÜDEK ders değerlendirmeleri (CourseEvaluation)
// ═══════════════════════════════════════════════════════════════════════════

export function fetchAllCourseEvaluations(token) {
  return getJson(`${ADMIN}/course-evaluations`, { token: authToken(token) })
}

export function fetchCourseEvaluationById(token, id) {
  return getJson(`${ADMIN}/course-evaluations/${id}`, { token: authToken(token) })
}

export function fetchCourseEvaluationByOffering(token, externalCourseOfferingId) {
  return getJson(`${ADMIN}/course-evaluations/by-offering/${externalCourseOfferingId}`, {
    token: authToken(token),
  })
}

export function fetchCourseEvaluationsByTeacher(token, externalTeacherId) {
  return getJson(`${ADMIN}/course-evaluations/by-teacher/${externalTeacherId}`, { token: authToken(token) })
}

// ═══════════════════════════════════════════════════════════════════════════
// Harf notu kuralları (ExternalProgramId = üniversite programId)
// ═══════════════════════════════════════════════════════════════════════════

export function fetchAllLetterGradeRules(token) {
  return getJson(`${ADMIN}/letter-grade-rules`, { token: authToken(token) })
}

export function fetchLetterGradeRulesByProgram(token, externalProgramId) {
  return getJson(`${ADMIN}/programs/${externalProgramId}/letter-grade-rules`, { token: authToken(token) })
}

export function fetchLetterGradeRuleById(token, id) {
  return getJson(`${ADMIN}/letter-grade-rules/${id}`, { token: authToken(token) })
}

/**
 * @param {string} token
 * @param {{ externalProgramId: number, letterGrade: string, minScore: number, maxScore: number, isPassing?: boolean, minimumFinalScore?: number|null, description?: string|null }} body
 */
export function createLetterGradeRule(token, body) {
  return postJsonWithAuth(`${ADMIN}/letter-grade-rules`, body, authToken(token))
}

/**
 * @param {string} token
 * @param {string} id Guid
 * @param {{ id: string, letterGrade: string, minScore: number, maxScore: number, isPassing?: boolean, minimumFinalScore?: number|null, description?: string|null }} body
 */
export function updateLetterGradeRule(token, id, body) {
  return putJsonWithAuth(`${ADMIN}/letter-grade-rules/${id}`, body, authToken(token))
}

export function deleteLetterGradeRule(token, id) {
  return deleteJsonWithAuth(`${ADMIN}/letter-grade-rules/${id}`, authToken(token))
}
