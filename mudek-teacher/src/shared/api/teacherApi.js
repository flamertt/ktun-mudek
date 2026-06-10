import {
  deleteJsonWithAuth,
  getJson,
  patchJsonWithAuth,
  postJsonWithAuth,
  putJsonWithAuth,
} from './httpClient'

const TEACHER = '/api/Teacher'

// ═════════════════════════════════════════════
// Akademik dönemler (üniversite)
// ═════════════════════════════════════════════

/** Tüm akademik dönemler (`id` / `ad`). */
export function fetchTeacherAcademicTerms(token) {
  return getJson(`${TEACHER}/academic-terms`, { token })
}

// ═════════════════════════════════════════════
// Courses / My courses
// ═════════════════════════════════════════════

/** Aktif dönem: `GET my-courses`. Belirli dönem: `GET my-courses/academic-terms?termId=`. */
export function fetchMyCourses(token, termId) {
  if (termId != null && termId !== '') {
    return getJson(`${TEACHER}/my-courses/academic-terms`, {
      token,
      query: { termId },
    })
  }
  return getJson(`${TEACHER}/my-courses`, { token })
}

export function fetchMyCourseDetail(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/detail`, { token })
}

export function fetchCourseStudents(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/students`, { token })
}

export function fetchEvaluation(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/evaluation`, { token })
}

export function createEvaluation(token, offeringId, body) {
  return postJsonWithAuth(`${TEACHER}/my-courses/${offeringId}/evaluation`, body, token)
}

export function updateEvaluation(token, evaluationId, body) {
  return putJsonWithAuth(`${TEACHER}/evaluations/${evaluationId}`, body, token)
}

export function deleteEvaluation(token, evaluationId) {
  return deleteJsonWithAuth(`${TEACHER}/evaluations/${evaluationId}`, token)
}

/** Bu öğretmenin tüm ders değerlendirme kayıtları (özet liste). */
export function fetchMyEvaluations(token) {
  return getJson(`${TEACHER}/my-evaluations`, { token })
}

/** Değerlendirme + sınavlar + bileşenler (öğretmen yetkisiyle). */
export function fetchEvaluationFullDetail(token, evaluationId) {
  return getJson(`${TEACHER}/evaluations/${evaluationId}/full-detail`, { token })
}

/** `evaluationId` üzerinden MÜDEK yeniden hesaplama (offering bilinmiyorsa). */
export function postEvaluationMudekCalculate(token, evaluationId) {
  return postJsonWithAuth(`${TEACHER}/evaluations/${evaluationId}/mudek-evaluation/calculate`, {}, token)
}

// ═════════════════════════════════════════════
// Exams / Questions
// ═════════════════════════════════════════════

export function fetchExams(token, evaluationId) {
  return getJson(`${TEACHER}/evaluations/${evaluationId}/exams`, { token })
}

export function fetchExamById(token, examId) {
  return getJson(`${TEACHER}/exams/${examId}`, { token })
}

export function createExam(token, evaluationId, body) {
  return postJsonWithAuth(`${TEACHER}/evaluations/${evaluationId}/exams`, body, token)
}

export function updateExam(token, examId, body) {
  return putJsonWithAuth(`${TEACHER}/exams/${examId}`, body, token)
}

export function deleteExam(token, examId) {
  return deleteJsonWithAuth(`${TEACHER}/exams/${examId}`, token)
}

export function fetchQuestions(token, examId) {
  return getJson(`${TEACHER}/exams/${examId}/questions`, { token })
}

export function fetchQuestionById(token, questionId) {
  return getJson(`${TEACHER}/exam-questions/${questionId}`, { token })
}

export function createQuestion(token, examId, body) {
  return postJsonWithAuth(`${TEACHER}/exams/${examId}/questions`, body, token)
}

export function updateQuestion(token, questionId, body) {
  return putJsonWithAuth(`${TEACHER}/exam-questions/${questionId}`, body, token)
}

export function deleteQuestion(token, questionId) {
  return deleteJsonWithAuth(`${TEACHER}/exam-questions/${questionId}`, token)
}

// ═════════════════════════════════════════════
// Question CLO mappings
// ═════════════════════════════════════════════

export function fetchQuestionClos(token, questionId) {
  return getJson(`${TEACHER}/exam-questions/${questionId}/clos`, { token })
}

export function addQuestionCloMapping(token, questionId, body) {
  return postJsonWithAuth(`${TEACHER}/exam-questions/${questionId}/clos`, body, token)
}

export function updateQuestionCloMapping(token, mappingId, body) {
  return putJsonWithAuth(
    `${TEACHER}/exam-question-outcome-mappings/${mappingId}`,
    body,
    token,
  )
}

export function deleteQuestionCloMapping(token, mappingId) {
  return deleteJsonWithAuth(`${TEACHER}/exam-question-outcome-mappings/${mappingId}`, token)
}

// ═════════════════════════════════════════════
// Answers (student answers) - question based
// ═════════════════════════════════════════════

export function fetchAnswers(token, questionId) {
  return getJson(`${TEACHER}/exam-questions/${questionId}/answers`, { token })
}

export function addAnswer(token, questionId, body) {
  return postJsonWithAuth(`${TEACHER}/exam-questions/${questionId}/answers`, body, token)
}

export function addAnswersBulk(token, questionId, body) {
  return postJsonWithAuth(`${TEACHER}/exam-questions/${questionId}/answers/bulk`, body, token)
}

export function updateAnswer(token, answerId, body) {
  return putJsonWithAuth(`${TEACHER}/student-answers/${answerId}`, body, token)
}

export function deleteAnswer(token, answerId) {
  return deleteJsonWithAuth(`${TEACHER}/student-answers/${answerId}`, token)
}

// ═════════════════════════════════════════════
// Assessment components
// ═════════════════════════════════════════════

export function fetchComponents(token, examId) {
  return getJson(`${TEACHER}/exams/${examId}/components`, { token })
}

export function fetchComponent(token, componentId) {
  return getJson(`${TEACHER}/assessment-components/${componentId}`, { token })
}

export function createComponent(token, examId, body) {
  return postJsonWithAuth(`${TEACHER}/exams/${examId}/components`, body, token)
}

export function updateComponent(token, componentId, body) {
  return putJsonWithAuth(`${TEACHER}/assessment-components/${componentId}`, body, token)
}

export function deleteComponent(token, componentId) {
  return deleteJsonWithAuth(`${TEACHER}/assessment-components/${componentId}`, token)
}

// ═════════════════════════════════════════════
// Component CLO mappings
// ═════════════════════════════════════════════

export function fetchComponentClos(token, componentId) {
  return getJson(`${TEACHER}/assessment-components/${componentId}/clos`, { token })
}

export function addComponentCloMapping(token, componentId, body) {
  return postJsonWithAuth(`${TEACHER}/assessment-components/${componentId}/clos`, body, token)
}

export function updateComponentCloMapping(token, mappingId, body) {
  return putJsonWithAuth(
    `${TEACHER}/assessment-component-outcome-mappings/${mappingId}`,
    body,
    token,
  )
}

export function deleteComponentCloMapping(token, mappingId) {
  return deleteJsonWithAuth(`${TEACHER}/assessment-component-outcome-mappings/${mappingId}`, token)
}

// ═════════════════════════════════════════════
// Scores (student assessment component scores)
// ═════════════════════════════════════════════

export function fetchScores(token, componentId) {
  return getJson(`${TEACHER}/assessment-components/${componentId}/scores`, { token })
}

export function addScore(token, componentId, body) {
  return postJsonWithAuth(`${TEACHER}/assessment-components/${componentId}/scores`, body, token)
}

export function addScoresBulk(token, componentId, body) {
  return postJsonWithAuth(
    `${TEACHER}/assessment-components/${componentId}/scores/bulk`,
    body,
    token,
  )
}

export function updateScore(token, scoreId, body) {
  return putJsonWithAuth(
    `${TEACHER}/student-assessment-component-scores/${scoreId}`,
    body,
    token,
  )
}

export function deleteScore(token, scoreId) {
  return deleteJsonWithAuth(`${TEACHER}/student-assessment-component-scores/${scoreId}`, token)
}

// ═════════════════════════════════════════════
// CLO list (Offering clos)
// ═════════════════════════════════════════════

export function fetchOfferingClos(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/clos`, { token })
}

/** Ders programına ait PÇ listesi (kod, başlık). */
export function fetchOfferingProgramOutcomes(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/program-outcomes`, { token })
}

/** Katalog `courseId` ile CLO–program çıktısı matrisi (üniversite). */
export function fetchCourseCloPoMap(token, courseId) {
  return getJson(`${TEACHER}/courses/${courseId}/clo-po-map`, { token })
}

// ═════════════════════════════════════════════
// MÜDEK results & calculate
// ═════════════════════════════════════════════

export function fetchMudekResults(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/mudek-evaluation/results`, { token })
}

/** Kayıtlı snapshot’ı siler, zinciri baştan hesaplar; güncel özeti döner. */
export function calculateMudekEvaluation(token, offeringId) {
  return postJsonWithAuth(`${TEACHER}/my-courses/${offeringId}/mudek-evaluation/calculate`, {}, token)
}

// ═════════════════════════════════════════════
// Surveys (Likert)
// ═════════════════════════════════════════════

export function fetchTeacherSurveys(token, offeringId) {
  return getJson(`${TEACHER}/my-courses/${offeringId}/surveys`, { token })
}

export function fetchTeacherSurveyDetail(token, surveyId) {
  return getJson(`${TEACHER}/surveys/${surveyId}`, { token })
}

export function createTeacherSurvey(token, body) {
  return postJsonWithAuth(`${TEACHER}/surveys`, body, token)
}

export function updateTeacherSurvey(token, surveyId, body) {
  return putJsonWithAuth(`${TEACHER}/surveys/${surveyId}`, body, token)
}

export function deleteTeacherSurvey(token, surveyId) {
  return deleteJsonWithAuth(`${TEACHER}/surveys/${surveyId}`, token)
}

export function toggleTeacherSurveyActive(token, surveyId) {
  return patchJsonWithAuth(`${TEACHER}/surveys/${surveyId}/toggle-active`, token)
}

export function addTeacherSurveyQuestion(token, surveyId, body) {
  return postJsonWithAuth(`${TEACHER}/surveys/${surveyId}/questions`, body, token)
}

export function updateTeacherSurveyQuestion(token, surveyId, questionId, body) {
  return putJsonWithAuth(`${TEACHER}/surveys/${surveyId}/questions/${questionId}`, body, token)
}

export function deleteTeacherSurveyQuestion(token, surveyId, questionId) {
  return deleteJsonWithAuth(`${TEACHER}/surveys/${surveyId}/questions/${questionId}`, token)
}

export function fetchTeacherSurveyResults(token, surveyId) {
  return getJson(`${TEACHER}/surveys/${surveyId}/results`, { token })
}

