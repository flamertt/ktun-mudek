import styles from './LoginCard.module.css'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { loginAsAdmin } from '../../../shared/api/authApi'
import { appConfig } from '../../../shared/config/appConfig'
import { authTexts } from '../../../shared/lib/texts/auth.js'

export function LoginCard() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const handleSubmit = async (event) => {
    event.preventDefault()

    setErrorMessage('')

    if (!username.trim() || !password) {
      setErrorMessage(authTexts.requiredError)
      return
    }

    setIsSubmitting(true)

    try {
      const response = await loginAsAdmin({
        email: username.trim(),
        password,
      })

      const token = response?.token ?? response?.Token
      const user = response?.user ?? response?.User
      if (!token || user == null) {
        throw new Error(
          response?.message ?? response?.Message ?? 'Giriş yanıtı geçersiz (token veya kullanıcı eksik).',
        )
      }

      localStorage.setItem(appConfig.storage.tokenKey, token)
      localStorage.setItem(appConfig.storage.userKey, JSON.stringify(user))
      navigate(appConfig.routes.home)
    } catch (error) {
      setErrorMessage(error.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className={styles.root} aria-label={authTexts.ariaLabel}>
      <div className={styles.panel}>
        <div className={styles.header}>
          <div className={styles.logoWrap}>
            <img className={styles.logo} src="/login_logo.png" alt={authTexts.logoAlt} />
          </div>
          <h1 className={styles.title}>{authTexts.title}</h1>
          <p className={styles.subtitle}>{authTexts.subtitle}</p>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="username">
              {authTexts.usernameLabel}
            </label>
            <input
              className={styles.input}
              id="username"
              name="username"
              type="text"
              autoComplete="username"
              placeholder={authTexts.usernamePlaceholder}
              value={username}
              onChange={(event) => setUsername(event.target.value)}
            />
          </div>

          <div className={styles.field}>
            <div className={styles.passwordLabelRow}>
              <label className={styles.label} htmlFor="password">
                {authTexts.passwordLabel}
              </label>
              <a className={styles.forgotLink} href={authTexts.forgotPasswordHref}>
                {authTexts.forgotPassword}
              </a>
            </div>
            <input
              className={styles.input}
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              placeholder={authTexts.passwordPlaceholder}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          <div className={styles.rememberRow}>
            <input
              className={styles.checkbox}
              id="remember"
              name="remember"
              type="checkbox"
              checked={rememberMe}
              onChange={(event) => setRememberMe(event.target.checked)}
            />
            <label className={styles.rememberLabel} htmlFor="remember">
              {authTexts.rememberLabel}
            </label>
          </div>

          <div className={styles.captcha} aria-label={authTexts.captchaLabel}>
            {authTexts.captchaPlaceholder}
          </div>

          <button className={styles.button} type="submit" disabled={isSubmitting}>
            <span>{isSubmitting ? authTexts.submitting : authTexts.submit}</span>
            <span className={styles.buttonArrow} aria-hidden="true">
              →
            </span>
          </button>

          {errorMessage ? <p className={styles.errorText}>{errorMessage}</p> : null}
        </form>
      </div>

      <div className={styles.trustBadges} aria-hidden>
        <div className={styles.trustItem}>
          <span className={styles.trustIcon} aria-hidden>
            ✓
          </span>
          <span className={styles.trustLabel}>{authTexts.trustSecure}</span>
        </div>
        <div className={styles.trustItem}>
          <span className={styles.trustIcon} aria-hidden>
            ✓
          </span>
          <span className={styles.trustLabel}>{authTexts.trustSsl}</span>
        </div>
      </div>
    </section>
  )
}
