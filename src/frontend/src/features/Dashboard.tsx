import { useQuery } from '@tanstack/react-query'
import { api, date } from '../lib/api'
import type { Change, Page } from '../lib/types'
import { Badge, Button, Empty, ErrorState, Loading, Panel } from '../components/ui'

interface Status { totalAssets: number; activeChanges: number; pendingApprovals: number; criticalFindings: number; staleAssets: number; importedSources: number }
export function Dashboard({ navigate, canCreate }: { navigate: (page: string) => void; canCreate: boolean }) {
  const status = useQuery({ queryKey: ['status'], queryFn: () => api<Status>('/system/status') })
  const changes = useQuery({ queryKey: ['changes', 'live', 1], queryFn: () => api<Page<Change>>('/changes?test=false') })
  if (status.isPending || changes.isPending) return <Loading />
  if (status.error || changes.error) return <ErrorState error={(status.error || changes.error)!} retry={() => { void status.refetch(); void changes.refetch() }} />
  const s = status.data
  return <><header className="page-heading"><div><h1>Değişiklikten önce, etkisini görün.</h1><p>Envanter, bağımlılıklar ve kanıta dayalı risk analizi.</p></div>{canCreate && <Button onClick={() => navigate('changes/new')}>Yeni değişiklik</Button>}</header>
    <div className="metrics">{[['Toplam varlık', s.totalAssets], ['Aktif değişiklik', s.activeChanges], ['Bekleyen onay', s.pendingApprovals], ['Kritik bulgu', s.criticalFindings]].map(([label, value]) => <div key={label}><span>{label}</span><strong>{value}</strong></div>)}</div>
    <Panel title="Son değişiklikler"><div className="table-wrap"><table><thead><tr><th>Değişiklik</th><th>Durum</th><th>Risk</th><th>Güncelleme</th></tr></thead><tbody>{changes.data.items.slice(0, 5).map(c => <tr key={c.id}><td><button className="text-button" onClick={() => navigate(`changes/${c.id}`)}>{c.title}</button></td><td><Badge>{c.state}</Badge></td><td>Analizi aç</td><td>{date(c.updatedAt)}</td></tr>)}</tbody></table></div>{changes.data.items.length === 0 && <Empty title="Henüz değişiklik yok">İlk değişiklik taslağınızı oluşturun.</Empty>}</Panel>
    <div className="two-columns"><Panel title="Veri güvenilirliği">{s.totalAssets === 0 ? <Empty title="Henüz envanter alınmadı" /> : <div className="health-summary"><strong>{s.staleAssets}</strong><p>Güncelliğini yitirmiş varlık</p><p className="muted">Kurum genelindeki envanter kapsamı henüz doğrulanmadı.</p></div>}</Panel><Panel title="Entegrasyon sağlığı"><div className="empty"><h3>{s.importedSources === 0 ? 'Bağlı kaynak yok' : `${s.importedSources} içe aktarma kaynağı`}</h3>{s.importedSources > 0 && <p className="muted">Dosya aktarımı, canlı bağlantı sağlığını doğrulamaz.</p>}<Button variant="outline" onClick={() => navigate('integrations')}>Entegrasyonları aç</Button></div></Panel></div>
    {s.criticalFindings > 0 && <p className="small muted">Kritik bulgular son 20 analiz kaydından hesaplanır; tekrar eden bulgular içerebilir.</p>}
  </>
}
