import { useEffect, useState } from 'react'
import { QueryClient, QueryClientProvider, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowRightLeft, FileText, House, Layers, LogOut, Menu, Moon, Play, Plug, Sun, UserRound, ShieldCheck } from 'lucide-react'
import { api, ApiError, resetCsrf } from './lib/api'
import { allowed, type User } from './lib/types'
import { Login } from './components/Login'
import { Button, ErrorState, Loading } from './components/ui'
import { Dashboard } from './features/Dashboard'
import { Inventory } from './features/Inventory'
import { Changes } from './features/Changes'
import { Integrations } from './features/Integrations'
import { Simulation } from './features/Simulation'
import { Audit } from './features/Audit'

const client = new QueryClient({ defaultOptions: { queries: { retry: (count, error) => !(error instanceof ApiError && [401, 403].includes(error.status)) && count < 1, staleTime: 10000 } } })
const navigation = [{ id: 'dashboard', title: 'Genel Bakış', icon: House }, { id: 'inventory', title: 'Envanter', icon: Layers }, { id: 'changes', title: 'Değişiklikler', icon: ArrowRightLeft }, { id: 'simulation', title: 'Simülasyon', icon: Play }, { id: 'integrations', title: 'Entegrasyonlar', icon: Plug }, { id: 'audit', title: 'Denetim Kaydı', icon: FileText }]
function Console() {
  const cache = useQueryClient(); const [path, setPath] = useState(location.hash.slice(1) || 'dashboard'); const [dark, setDark] = useState(localStorage.getItem('berimpact-theme') === 'dark'); const [menu, setMenu] = useState(false); const [error, setError] = useState<Error>()
  const me = useQuery({ queryKey: ['me'], queryFn: () => api<User>('/auth/me'), retry: false, refetchInterval: 60000 })
  useEffect(() => { document.documentElement.dataset.theme = dark ? 'dark' : 'light'; localStorage.setItem('berimpact-theme', dark ? 'dark' : 'light') }, [dark])
  useEffect(() => { const change = () => setPath(location.hash.slice(1) || 'dashboard'); window.addEventListener('hashchange', change); return () => window.removeEventListener('hashchange', change) }, [])
  const navigate = (next: string) => { location.hash = next; setPath(next); setMenu(false); window.scrollTo(0, 0) }
  if (me.isPending) return <Loading />
  if (me.error instanceof ApiError && me.error.status === 401) return <Login onLogin={() => { cache.clear(); void me.refetch() }} />
  if (me.error) return <main className="login-layout"><ErrorState error={me.error} retry={() => void me.refetch()} /></main>
  const user = me.data!; const active = path.split('/')[0]; const nav = navigation.filter(n => n.id !== 'audit' || allowed(user, 'PlatformAdministrator', 'Auditor'))
  return <div className="app-shell"><a className="skip-link" href="#main-content" onClick={event => { event.preventDefault(); document.getElementById('main-content')?.focus() }}>İçeriğe geç</a><aside className={menu ? 'sidebar open' : 'sidebar'}><a className="brand" href="#dashboard" onClick={() => setMenu(false)}><span className="brand-mark">B</span><strong>BerImpact</strong></a><nav aria-label="Ana menü">{nav.map(n => <a key={n.id} href={`#${n.id}`} className={active === n.id ? 'active' : ''} aria-current={active === n.id ? 'page' : undefined} onClick={() => setMenu(false)}><n.icon size={23} strokeWidth={1.8} /><span>{n.title}</span></a>)}</nav><div className="mode"><div><strong>Observe Mode</strong><ShieldCheck size={21} /></div><p>Altyapı yazma işlemleri kapalı</p></div></aside><div className="workspace"><header className="topbar"><Button variant="ghost" className="menu-toggle" aria-label="Menüyü aç" onClick={() => setMenu(!menu)}><Menu size={22} /></Button><span className="breadcrumb">Çalışma alanı <span>/</span> <strong>{navigation.find(n => n.id === active)?.title || 'Genel Bakış'}</strong></span><div className="top-actions"><button className="theme-switch" aria-label={dark ? 'Açık temaya geç' : 'Koyu temaya geç'} onClick={() => setDark(!dark)}>{dark ? <Moon size={22} /> : <Sun size={22} />}<span className={dark ? 'switch on' : 'switch'} /></button><span className="account"><UserRound size={22} />{user.username}</span><Button variant="ghost" aria-label="Çıkış yap" onClick={async () => { try { await api('/auth/logout', 'POST'); resetCsrf(); cache.clear(); void me.refetch() } catch (e) { setError(e as Error) } }}><LogOut size={18} /></Button></div></header><main id="main-content" tabIndex={-1} className="main-content">{error && <ErrorState error={error} />}{active === 'dashboard' && <Dashboard navigate={navigate} canCreate={allowed(user, 'PlatformAdministrator', 'ChangeManager')} />}{active === 'inventory' && <Inventory />}{active === 'changes' && <Changes path={path} user={user} navigate={navigate} />}{active === 'simulation' && <Simulation user={user} />}{active === 'integrations' && <Integrations user={user} />}{active === 'audit' && <Audit />}{!navigation.some(n => n.id === active) && <ErrorState error={new Error('Sayfa bulunamadı.')} />}</main><footer>BerImpact · Kurum içi çalışma alanı</footer></div></div>
}
export default function App() { return <QueryClientProvider client={client}><Console /></QueryClientProvider> }
