export class ApiError extends Error { status: number; constructor(status: number, message: string) { super(message); this.status = status } }
let token: string | undefined
export async function csrf() {
  const r = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin' })
  if (!r.ok) throw new ApiError(r.status, 'Oturum güvenlik belirteci alınamadı.')
  token = (await r.json()).token
}
export async function api<T>(path: string, method = 'GET', body?: unknown, key?: string): Promise<T> {
  if (method !== 'GET' && !token) await csrf()
  const r = await fetch(`/api/v1${path}`, { method, credentials: 'same-origin', headers: { ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}), ...(method !== 'GET' ? { 'X-CSRF-TOKEN': token! } : {}), ...(key ? { 'Idempotency-Key': key } : {}) }, body: body === undefined ? undefined : JSON.stringify(body) })
  if (!r.ok) { const error = await r.json().catch(() => ({})); throw new ApiError(r.status, error.detail || error.title || `İstek başarısız (${r.status})`) }
  if (r.status === 204) return undefined as T
  return r.json()
}
export function resetCsrf() { token = undefined }
export const date = (value: string) => new Date(value).toLocaleString('tr-TR')
