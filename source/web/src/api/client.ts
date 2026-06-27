const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

async function request<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  })

  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error(body.error ?? `HTTP ${res.status}`)
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
}

export const apiClient = {
  post<T>(path: string, body: unknown): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body: JSON.stringify(body),
    })
  },

  postForm<T>(path: string, formData: FormData): Promise<T> {
    return request<T>(path, {
      method: 'POST',
      body: formData,
      headers: {},
    })
  },

  get<T>(path: string): Promise<T> {
    return request<T>(path, { method: 'GET' })
  },

  patch<T>(path: string, body: unknown): Promise<T> {
    return request<T>(path, {
      method: 'PATCH',
      body: JSON.stringify(body),
    })
  },

  put<T>(path: string, body: unknown): Promise<T> {
    return request<T>(path, {
      method: 'PUT',
      body: JSON.stringify(body),
    })
  },

  del<T>(path: string): Promise<T> {
    return request<T>(path, { method: 'DELETE' })
  },
}
