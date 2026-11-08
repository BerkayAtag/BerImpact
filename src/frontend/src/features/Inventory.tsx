import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { api, date } from '../lib/api'
import type { Asset, Page } from '../lib/types'
import { Badge, Button, Empty, ErrorState, Loading, Pager, Panel } from '../components/ui'

interface Edge { id: string; sourceId: string; targetId: string; type: string; evidence: string; state: string; observedAt: string }
export function Inventory() {
  const [now] = useState(() => Date.now())
  const [search, setSearch] = useState(''); const [term, setTerm] = useState(''); const [page, setPage] = useState(1); const [test, setTest] = useState(false); const [selected, setSelected] = useState<Asset>()
  const q = useQuery({ queryKey: ['assets', term, page, test], queryFn: () => api<Page<Asset>>(`/assets?search=${encodeURIComponent(term)}&page=${page}&test=${test}`) })
  const edges = useQuery({ queryKey: ['edges', selected?.id], queryFn: () => api<{ items: Edge[]; truncated: boolean }>(`/assets/${selected!.id}/relationships`), enabled: !!selected })
  return <><header className="page-heading"><div><h1>Envanter</h1><p>Kaynağı ve kanıt durumu korunmuş varlıklar.</p></div></header>
    <div className="toolbar"><form className="search" onSubmit={e => { e.preventDefault(); setTerm(search); setPage(1) }}><Search size={18} /><input aria-label="Envanterde ara" placeholder="Ad, kaynak veya lokasyon ara" value={search} onChange={e => setSearch(e.target.value)} /><Button variant="outline">Ara</Button></form><label className="inline-label"><input type="checkbox" checked={test} onChange={e => { setTest(e.target.checked); setPage(1); setSelected(undefined) }} /> Test verilerini göster</label></div>
    {test && <div className="notice">Test ortamı — bu kayıtlar canlı altyapı verisi değildir.</div>}
    {q.isPending ? <Loading /> : q.error ? <ErrorState error={q.error} retry={() => void q.refetch()} /> : <Panel title="Varlıklar"><div className="table-wrap"><table><thead><tr><th>Varlık / Kaynak kimliği</th><th>İşletim sistemi</th><th>Model / Lokasyon</th><th>Kanıt</th><th>Son gözlem</th></tr></thead><tbody>{q.data.items.map(a => <tr key={a.id}><td><button className="text-button" onClick={() => setSelected(a)}>{a.name}</button><small>{a.source} / {a.externalId}</small></td><td>{a.os || 'Bilinmiyor'}</td><td>{a.model || '—'}<small>{a.location || '—'}</small></td><td><Badge danger={a.evidence !== 'Verified' || now - Date.parse(a.observedAt) > 7 * 86400000}>{now - Date.parse(a.observedAt) > 7 * 86400000 ? 'Stale' : a.evidence}</Badge>{a.critical && <small>Kritik varlık</small>}</td><td>{date(a.observedAt)}</td></tr>)}</tbody></table></div>{q.data.total === 0 && <Empty title="Envanter kaydı yok">Entegrasyonlar ekranından doğrulanmış JSON verisini içe aktarın.</Empty>}<Pager page={page} total={q.data.total} setPage={setPage} /></Panel>}
    {selected && <Panel title={`${selected.name} · Bağımlılık kanıtları`} actions={<Button variant="ghost" onClick={() => setSelected(undefined)}>Kapat</Button>}><p className="muted">Kaynak varlık → bağımlı olduğu hedef. Etki analizi bu yönün tersine ilerler.</p>{edges.isPending ? <Loading /> : edges.error ? <ErrorState error={edges.error} /> : edges.data?.items.length === 0 ? <Empty title="İlişki kanıtı yok">Bu durum, varlığın bağımlılığı olmadığı anlamına gelmez.</Empty> : <ul className="evidence-list">{edges.data?.items.map(e => <li key={e.id}><strong>{e.type} · {e.state}</strong><p>{e.sourceId} → {e.targetId}</p><p>{e.evidence}</p></li>)}</ul>}{edges.data?.truncated && <div className="notice">İlk 200 ilişki gösteriliyor.</div>}</Panel>}
  </>
}

