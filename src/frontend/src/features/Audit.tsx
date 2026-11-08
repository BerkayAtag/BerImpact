import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api, date } from '../lib/api'
import type { Page } from '../lib/types'
import { ErrorState, Loading, Pager, Panel } from '../components/ui'
interface Event { id: number; at: string; actorId: string; action: string; entityId: string; detail: string }
export function Audit() {
  const [page, setPage] = useState(1); const q = useQuery({ queryKey: ['audit', page], queryFn: () => api<Page<Event>>(`/audit/events?page=${page}`) })
  return <><header className="page-heading"><div><h1>Denetim Kaydı</h1><p>Uygulama işlemlerinin eklemeli olay geçmişi.</p></div></header>{q.isPending ? <Loading /> : q.error ? <ErrorState error={q.error} retry={() => void q.refetch()} /> : <Panel title="Olaylar"><div className="table-wrap"><table><thead><tr><th>Zaman</th><th>İşlem</th><th>Aktör</th><th>Ayrıntı</th></tr></thead><tbody>{q.data.items.map(e => <tr key={e.id}><td>{date(e.at)}</td><td>{e.action}<small>{e.entityId}</small></td><td className="code">{e.actorId}</td><td>{e.detail || '—'}</td></tr>)}</tbody></table></div><Pager page={page} total={q.data.total} size={50} setPage={setPage} /></Panel>}</>
}
