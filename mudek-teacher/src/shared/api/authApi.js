import { postJson, postJsonWithAuth } from './httpClient'

const TEACHER_LOGIN_ENDPOINT = '/api/TeacherAuth/login'
const TEACHER_LOGOUT_ENDPOINT = '/api/TeacherAuth/logout'

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
