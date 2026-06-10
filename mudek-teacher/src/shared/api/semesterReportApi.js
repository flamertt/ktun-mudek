import {
  deleteJsonWithAuth,
  getJson,
  postFormDataWithAuth,
  postJsonWithAuth,
  putJsonWithAuth,
  API_BASE_URL,
} from './httpClient'

const REPORT = '/api/SemesterReport'

// ═════════════════════════════════════════════
// Temel CRUD
// ═════════════════════════════════════════════

/** Öğretmenin tüm dönem sonu raporları. */
export function fetchSemesterReports(token) {
  return getJson(REPORT, { token })
}

/** Rapor detayı (manuel alanlar + navigation). */
export function fetchSemesterReport(token, id) {
  return getJson(`${REPORT}/${id}`, { token })
}

/**
 * Yeni rapor oluştur.
 * @param {string} token
 * @param {{ externalCourseOfferingId: number, teacherName?: string, teacherTitle?: string }} body
 */
export function createSemesterReport(token, body) {
  return postJsonWithAuth(REPORT, body, token)
}

/**
 * Rapor manuel alanlarını güncelle.
 * @param {string} token
 * @param {string} id
 * @param {object} body
 */
export function updateSemesterReport(token, id, body) {
  return putJsonWithAuth(`${REPORT}/${id}`, body, token)
}

/** Raporu sil. */
export function deleteSemesterReport(token, id) {
  return deleteJsonWithAuth(`${REPORT}/${id}`, token)
}

// ═════════════════════════════════════════════
// Önizleme & Doğrulama
// ═════════════════════════════════════════════

/** Raporun tam önizlemesi (otomatik + manuel). */
export function fetchReportPreview(token, id) {
  return getJson(`${REPORT}/${id}/preview`, { token })
}

/** Raporun eksiksizlik kontrolü. */
export function validateReport(token, id) {
  return getJson(`${REPORT}/${id}/validation`, { token })
}

// ═════════════════════════════════════════════
// Bölüm B: Haftalık Kaynaklar
// ═════════════════════════════════════════════

export function fetchWeeklyResources(token, id) {
  return getJson(`${REPORT}/${id}/weekly-resources`, { token })
}

/**
 * Haftalık kaynağı ekle/güncelle (upsert by weekNumber).
 * @param {object} dto — { weekNumber, topic, resourceType, resourceInfo, chapterPage, description, contentSummary }
 */
export function upsertWeeklyResource(token, id, dto) {
  return putJsonWithAuth(`${REPORT}/${id}/weekly-resources`, dto, token)
}

export function deleteWeeklyResource(token, id, resourceId) {
  return deleteJsonWithAuth(`${REPORT}/${id}/weekly-resources/${resourceId}`, token)
}

// ═════════════════════════════════════════════
// Dosya Yükleme (C, D, E, F, G)
// ═════════════════════════════════════════════

export function fetchReportFiles(token, id) {
  return getJson(`${REPORT}/${id}/files`, { token })
}

/**
 * Dosya yükle (multipart/form-data).
 * @param {string} token
 * @param {string} id  rapor ID
 * @param {{ sectionCode: string, fileCategory: string, examId?: string, notes?: string }} meta
 * @param {File} file
 */
export function uploadReportFile(token, id, { sectionCode, fileCategory, examId, examTypeLabel, notes }, file) {
  const fd = new FormData()
  fd.append('sectionCode', sectionCode)
  fd.append('fileCategory', fileCategory)
  if (examId) fd.append('examId', examId)
  if (examTypeLabel) fd.append('examTypeLabel', examTypeLabel)
  if (notes) fd.append('notes', notes)
  fd.append('file', file)
  return postFormDataWithAuth(`${REPORT}/${id}/files`, fd, token)
}

export function deleteReportFile(token, id, fileId) {
  return deleteJsonWithAuth(`${REPORT}/${id}/files/${fileId}`, token)
}

/**
 * Dosya indirme URL'si (direkt tarayıcı ile fetch/anchor için).
 * Token header ile gönderilmesi gerektiğinden fetch kullanılır.
 */
export async function downloadReportFile(token, id, fileId, originalFileName) {
  const url = `${API_BASE_URL}${REPORT}/${id}/files/${fileId}/download`
  const resp = await fetch(url, {
    headers: { Authorization: `Bearer ${token}` },
  })
  if (!resp.ok) throw new Error(`Dosya indirilemedi (${resp.status})`)
  const blob = await resp.blob()
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = originalFileName ?? 'dosya'
  link.click()
  URL.revokeObjectURL(link.href)
}

// ═════════════════════════════════════════════
// DÖÇ Notları (Bölüm L, M)
// ═════════════════════════════════════════════

export function fetchCloNotes(token, id) {
  return getJson(`${REPORT}/${id}/clo-notes`, { token })
}

/**
 * DÖÇ notunu ekle/güncelle (upsert by externalCloId).
 * @param {object} dto — { externalCloId, cloCode, cloDescription, teacherNote, improvementSuggestion }
 */
export function upsertCloNote(token, id, dto) {
  return putJsonWithAuth(`${REPORT}/${id}/clo-notes`, dto, token)
}

// ═════════════════════════════════════════════
// PÇ Notları (Bölüm K)
// ═════════════════════════════════════════════

export function fetchPloNotes(token, id) {
  return getJson(`${REPORT}/${id}/plo-notes`, { token })
}

/**
 * PÇ notunu ekle/güncelle (upsert by externalPloId).
 * @param {object} dto — { externalPloId, ploCode, ploDescription, teacherNote, improvementSuggestion }
 */
export function upsertPloNote(token, id, dto) {
  return putJsonWithAuth(`${REPORT}/${id}/plo-notes`, dto, token)
}

// ═════════════════════════════════════════════
// Bölüm C: Öğrenci Örnekleri
// ═════════════════════════════════════════════

/** Dönem sonu notuna göre yüksek/orta/düşük öğrenci önerileri. */
export function fetchStudentSamples(token, id) {
  return getJson(`${REPORT}/${id}/student-samples`, { token })
}
