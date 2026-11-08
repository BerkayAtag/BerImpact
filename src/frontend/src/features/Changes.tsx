import { useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { api, date } from '../lib/api'
import type { Assessment, Change, Job, Page, User } from '../lib/types'
import { allowed } from '../lib/types'
import { Badge, Button, Empty, ErrorState, Loading, Pager, Panel } from '../components/ui'
import { ChangeEditor } from './ChangeEditor'
import { AssessmentView } from './AssessmentView'

export function Changes({ user, path, navigate }: { user: User; path: string; navigate: (path: string) => void }) {
  const id = path.split('/')[1]; const canWrite = allowed(user, 'PlatformAdministrator', 'ChangeManager')
  if (id === 'new') return canWrite ? <ChangeEditor done={id => navigate(`changes/${id}`)} cancel={() => navigate('changes')} /> : <p>Değişiklik oluşturma yetkiniz yok.</p>
  if (id) return <ChangeDetail key={id} id={id} user={user} />
  return <ChangeList canWrite={canWrite} navigate={navigate} />
}
function ChangeList({ canWrite, navigate }: { canWrite: boolean; navigate: (path: string) => void }) {
  const [page, setPage] = useState(1); const q = useQuery({ queryKey: ['changes', page], queryFn: () => api<Page<Change>>(`/changes?page=${page}`) })
  return <><header className="page-heading"><div><h1>Değişiklikler</h1><p>Taslak, analiz ve inceleme kayıtları.</p></div>{canWrite && <Button onClick={() => navigate('changes/new')}>Yeni değişiklik</Button>}</header>{q.isPending ? <Loading /> : q.error ? <ErrorState error={q.error} retry={() => void q.refetch()} /> : <Panel title="Değişiklik merkezi"><div className="table-wrap"><table><thead><tr><th>Başlık</th><th>Tür</th><th>Durum</th><th>Sürüm</th><th>Güncelleme</th></tr></thead><tbody>{q.data.items.map(c => <tr key={c.id}><td><button className="text-button" onClick={() => navigate(`changes/${c.id}`)}>{c.title}</button>{JSON.parse(c.targetJson).isTest && <small>Test kapsamı</small>}</td><td>{c.type}</td><td><Badge>{c.state}</Badge></td><td>{c.version}</td><td>{date(c.updatedAt)}</td></tr>)}</tbody></table></div>{q.data.total === 0 && <Empty title="Henüz değişiklik yok">İlk değişiklik taslağınızı oluşturun.</Empty>}<Pager page={page} total={q.data.total} setPage={setPage} /></Panel>}</>
}
function ChangeDetail({ id, user }: { id: string; user: User }) {
  const client = useQueryClient(); const [edit, setEdit] = useState(false); const [jobId, setJobId] = useState<string>(); const [error, setError] = useState<Error>(); const [busy, setBusy] = useState(false); const [assessmentId, setAssessmentId] = useState(''); const [reason, setReason] = useState('')
  const q = useQuery({ queryKey: ['change', id], queryFn: () => api<Change>(`/changes/${id}`) })
  const job = useQuery({ queryKey: ['job', jobId], queryFn: () => api<Job>(`/jobs/${jobId}`), enabled: !!jobId, refetchInterval: query => ['Queued', 'Running'].includes(query.state.data?.status || 'Queued') ? 1000 : false })
  const assessments = useQuery({ queryKey: ['assessments', id, job.data?.status], queryFn: () => api<Assessment[]>(`/changes/${id}/assessments`) })
  const decisions = useQuery({ queryKey: ['decisions', id], queryFn: () => api<{ id: string; approved: boolean; reason: string; changeVersion: number }[]>(`/changes/${id}/decisions`) })
  async function act(path: string, body: unknown) { try { setBusy(true); setError(undefined); const result = await api<Job>(path, 'POST', body, crypto.randomUUID()); if (path.endsWith('/assessments')) { setJobId(result.id); setAssessmentId('') } await client.invalidateQueries() } catch (e) { setError(e as Error) } finally { setBusy(false) } }
  if (q.isPending) return <Loading />
  if (q.error) return <ErrorState error={q.error} />
  const c = q.data; const canWrite = allowed(user, 'PlatformAdministrator', 'ChangeManager'); const canAnalyze = allowed(user, 'PlatformAdministrator', 'ChangeManager', 'RiskAnalyst'); const currentAssessment = assessmentId || job.data?.assessmentId || assessments.data?.[0]?.id
  if (edit) return <ChangeEditor existing={c} done={() => { setEdit(false); setJobId(undefined); setAssessmentId('') }} cancel={() => setEdit(false)} />
  return <><header className="page-heading"><div><h1>{c.title}</h1><p>{c.type} · Sürüm {c.version} · {date(c.updatedAt)}</p></div><Badge>{c.state}</Badge></header>{JSON.parse(c.targetJson).isTest && <div className="notice">Test kapsamı — canlı altyapı verisi değildir.</div>}<div className="toolbar">{canWrite && <Button variant="outline" onClick={() => setEdit(true)}>Taslağı düzenle</Button>}{canAnalyze && <Button disabled={busy || ['Queued', 'Running'].includes(job.data?.status || '')} onClick={() => void act(`/changes/${id}/assessments`, { scenarios: [] })}>Etki analizi çalıştır</Button>}{canWrite && c.state === 'Draft' && <Button variant="outline" disabled={busy} onClick={() => void act(`/changes/${id}/submit`, { version: c.version })}>İncelemeye gönder</Button>}</div>{error && <ErrorState error={error} />}{job.data && <div role="status" className="notice">Analiz işi: {job.data.status}{job.data.error && ` · ${job.data.error}`}</div>}{job.error && <ErrorState error={job.error} />}
    <Panel title="Değişiklik kapsamı"><p>{c.description || 'Açıklama eklenmedi.'}</p><div className="two-columns"><div><h3>Mevcut</h3><pre>{JSON.stringify(JSON.parse(c.beforeJson), null, 2)}</pre></div><div><h3>Hedef</h3><pre>{JSON.stringify(JSON.parse(c.afterJson), null, 2)}</pre></div></div><details><summary>Hedef filtreleri ve geri dönüş</summary><pre>{JSON.stringify(JSON.parse(c.targetJson), null, 2)}</pre><p><strong>Rollback:</strong> {c.rollbackPlan || 'Belirtilmedi'}</p><p><strong>Başarı kriterleri:</strong> {c.successCriteria || 'Belirtilmedi'}</p></details></Panel>
    {assessments.error && <ErrorState error={assessments.error} />}{assessments.data && assessments.data.length > 0 && <label>Analiz geçmişi<select value={currentAssessment} onChange={e => setAssessmentId(e.target.value)}>{assessments.data.map(a => <option key={a.id} value={a.id}>{date(a.createdAt)} · v{a.changeVersion}</option>)}</select></label>}{currentAssessment && assessments.data?.find(a => a.id === currentAssessment)?.changeVersion !== c.version && <div className="notice">Bu analiz önceki bir değişiklik sürümüne aittir. Güncel taslak için yeniden analiz çalıştırın.</div>}{c.state === 'Rejected' && <div className="notice">Yeniden inceleme için taslağı düzenleyerek yeni sürüm oluşturun.</div>}{currentAssessment && <AssessmentView key={currentAssessment} assessmentId={currentAssessment} />}
    {allowed(user, 'ChangeApprover') && ['Submitted', 'UnderReview'].includes(c.state) && currentAssessment && <Panel title="İnceleme kararı"><p className="muted">Değişiklik sahibi kendi kaydını onaylayamaz. Eksik kanıtlar onayı engeller.</p><label>Karar gerekçesi<textarea value={reason} onChange={e => setReason(e.target.value)} /></label><div className="actions"><Button disabled={busy || !reason.trim()} onClick={() => void act(`/changes/${id}/decision`, { assessmentId: currentAssessment, version: c.version, approved: true, reason })}>Onayla</Button><Button variant="outline" disabled={busy || !reason.trim()} onClick={() => void act(`/changes/${id}/decision`, { assessmentId: currentAssessment, version: c.version, approved: false, reason })}>Reddet</Button></div></Panel>}
    {decisions.error && <ErrorState error={decisions.error} />}{!!decisions.data?.length && <Panel title="Karar geçmişi">{decisions.data.map(d => <p key={d.id}>v{d.changeVersion} · {d.approved ? 'Onaylandı' : 'Reddedildi'} · {d.reason}</p>)}</Panel>}
  </>
}

