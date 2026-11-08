import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { api, resetCsrf } from '../lib/api'
import { Button, ErrorState } from './ui'

const schema = z.object({ username: z.string().min(1, 'Kullanıcı adı gerekli'), password: z.string().min(1, 'Parola gerekli') })
export function Login({ onLogin }: { onLogin: () => void }) {
  const [error, setError] = useState<Error>()
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema) })
  return <main className="login-layout"><div className="login-brand"><span className="brand-mark">B</span><strong>BerImpact</strong></div><form className="login-panel" onSubmit={handleSubmit(async data => { try { setError(undefined); await api('/auth/login', 'POST', data); resetCsrf(); onLogin() } catch (e) { setError(e as Error) } })}>
    <h1>Çalışma alanınıza giriş yapın.</h1><p className="muted">Kurum içi değişiklik etki analizi ve risk yönetimi.</p>
    <label>Kullanıcı adı<input autoComplete="username" {...register('username')} /></label>{errors.username && <p role="alert">{errors.username.message}</p>}
    <label>Parola<input type="password" autoComplete="current-password" {...register('password')} /></label>{errors.password && <p role="alert">{errors.password.message}</p>}
    {error && <ErrorState error={error} />}<Button disabled={isSubmitting}>{isSubmitting ? 'Giriş yapılıyor…' : 'Giriş yap'}</Button>
    <p className="small muted">İlk hesap, sunucuda yetkili operatör tarafından oluşturulur. Genel kayıt kapalıdır.</p>
  </form><p className="small muted">Observe Mode · Altyapı yazma işlemleri kapalı</p></main>
}
