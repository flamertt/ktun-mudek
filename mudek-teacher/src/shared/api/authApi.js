import { getJson, postJson, postJsonWithAuth } from './httpClient'

const TEACHER_LOGIN_ENDPOINT = '/api/TeacherAuth/login'
const TEACHER_LOGOUT_ENDPOINT = '/api/TeacherAuth/logout'
const TEACHER_DEV_TOKEN_ENDPOINT = '/api/TeacherAuth/dev-token'

/** [Sadece geliştirme] Parametre girmeden hazır test öğretmeni token'ı alır. */
export function getDevToken() {
  return getJson(TEACHER_DEV_TOKEN_ENDPOINT)
}

export function loginAsTeacher({ email, password }) {
  return postJson(TEACHER_LOGIN_ENDPOINT, { email, password })
}

export async function logoutCurrentUser(token) {
  if (!token) return null

  try {
    return await postJsonWithAuth(TEACHER_LOGOUT_ENDPOINT, {}, token)
  } catch {
    return null
  }
}
