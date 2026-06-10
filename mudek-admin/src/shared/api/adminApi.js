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

/** Admin: bir öğretmenin belirli dönemdeki derslerini çeker (courseId bulmak için). */
export function fetchTeacherCoursesByEmail(token, email, academicTermId) {
  return getJson(`${ADMIN}/university/teacher-courses`, { token: authToken(token), query: { email, academicTermId } })
}

// ═══════════════════════════════════════════════════════════════════════════
// Yerel CLO Yönetimi
// ═══════════════════════════════════════════════════════════════════════════

export function fetchLocalClos(token, externalCourseId) {
  return getJson(`${ADMIN}/courses/${externalCourseId}/local-clos`, { token: authToken(token) })
}

export function fetchMergedClos(token, externalCourseId) {
  return getJson(`${ADMIN}/courses/${externalCourseId}/clos/merged`, { token: authToken(token) })
}

export function createLocalClo(token, body) {
  return postJsonWithAuth(`${ADMIN}/local-clos`, body, authToken(token))
}

export function updateLocalClo(token, id, body) {
  return putJsonWithAuth(`${ADMIN}/local-clos/${id}`, body, authToken(token))
}

export function deleteLocalClo(token, id) {
  return deleteJsonWithAuth(`${ADMIN}/local-clos/${id}`, authToken(token))
}

/** CLO kaynak kilidini sıfırlar — admin tarafı için yardımcı endpoint (TeacherController değil; future admin override). */
export function resetCloLock(token, evaluationId) {
  return deleteJsonWithAuth(`/api/Teacher/evaluations/${evaluationId}/clo-lock`, authToken(token))
}

// ═══════════════════════════════════════════════════════════════════════════
// Yerel CLO–PO Eşleme
// ═══════════════════════════════════════════════════════════════════════════

export function fetchPloMapsByClo(token, cloId) {
  return getJson(`${ADMIN}/local-clos/${cloId}/plo-maps`, { token: authToken(token) })
}

export function fetchPloMapsByCourse(token, externalCourseId) {
  return getJson(`${ADMIN}/courses/${externalCourseId}/local-plo-maps`, { token: authToken(token) })
}

export function createPloMap(token, body) {
  return postJsonWithAuth(`${ADMIN}/local-plo-maps`, body, authToken(token))
}

export function updatePloMap(token, id, body) {
  return putJsonWithAuth(`${ADMIN}/local-plo-maps/${id}`, body, authToken(token))
}

export function deletePloMap(token, id) {
  return deleteJsonWithAuth(`${ADMIN}/local-plo-maps/${id}`, authToken(token))
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
