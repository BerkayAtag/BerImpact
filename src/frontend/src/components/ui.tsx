import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { ChevronLeft, ChevronRight, RefreshCw } from 'lucide-react'

const variants = cva('button', { variants: { variant: { default: 'primary', outline: 'button-outline', ghost: 'ghost' } }, defaultVariants: { variant: 'default' } })
export function Button({ className, variant, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & VariantProps<typeof variants>) { return <button className={twMerge(clsx(variants({ variant }), className))} {...props} /> }
export function Panel({ title, children, actions }: { title: string; children: ReactNode; actions?: ReactNode }) { return <section className="panel"><div className="panel-heading"><h2>{title}</h2>{actions}</div>{children}</section> }
export function Empty({ title, children }: { title: string; children?: ReactNode }) { return <div className="empty"><h3>{title}</h3>{children && <div className="muted">{children}</div>}</div> }
export function ErrorState({ error, retry }: { error: Error; retry?: () => void }) { return <div role="alert" className="notice danger"><p>{error.message}</p>{retry && <Button variant="outline" onClick={retry}><RefreshCw size={16} /> Tekrar dene</Button>}</div> }
export function Loading() { return <div role="status" aria-label="Yükleniyor" className="skeleton"><span /><span /><span /></div> }
export function Badge({ children, danger = false }: { children: ReactNode; danger?: boolean }) { return <span className={`badge ${danger ? 'warning' : ''}`}>{children}</span> }
export function Pager({ page, total, size = 25, setPage }: { page: number; total: number; size?: number; setPage: (n: number) => void }) { return <div className="pager"><span>{total} kayıt · Sayfa {page}</span><Button variant="ghost" aria-label="Önceki sayfa" disabled={page <= 1} onClick={() => setPage(page - 1)}><ChevronLeft size={18} /></Button><Button variant="ghost" aria-label="Sonraki sayfa" disabled={page * size >= total} onClick={() => setPage(page + 1)}><ChevronRight size={18} /></Button></div> }
