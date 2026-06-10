import { LoginCard } from '../../../widgets/login-card/ui/LoginCard.jsx'

import styles from './LoginPage.module.css'

const bg = `url(${import.meta.env.BASE_URL}background.jpg)`

export function LoginPage() {
  return (
    <main
      className={styles.root}
      style={{ backgroundImage: `linear-gradient(155deg, color-mix(in srgb, var(--md-primary-container) 22%, transparent) 0%, rgba(38,24,22,0.42) 42%, rgba(26,12,12,0.58) 100%), ${bg}` }}
    >
      <LoginCard />
    </main>
  )
}

