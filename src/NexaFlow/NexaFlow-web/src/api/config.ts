export const API_URLS = {
  auth:    import.meta.env.VITE_AUTH_API_URL    ?? 'http://localhost:5002',
  book:    import.meta.env.VITE_BOOK_API_URL    ?? 'http://localhost:5001',
  pos:     import.meta.env.VITE_POS_API_URL     ?? 'http://localhost:5050',
  insight: import.meta.env.VITE_INSIGHT_API_URL ?? 'http://localhost:5053',
  ml:      import.meta.env.VITE_ML_API_URL      ?? 'http://localhost:5054',
};

/** Normalizes a response that may have PascalCase or camelCase wrapper keys */
export function unwrap<T>(res: unknown): T {
  if (res && typeof res === 'object') {
    const r = res as Record<string, unknown>;
    // ApiResponse<T> wrapper from .NET backend
    if ('Data' in r) return r['Data'] as T;
    if ('data' in r) return r['data'] as T;
  }
  return res as T;
}

/** Normalizes a single object's keys from PascalCase to camelCase (shallow) */
export function normalize<T extends object>(obj: T): T {
  if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return obj;
  return Object.fromEntries(
    Object.entries(obj).map(([k, v]) => [k.charAt(0).toLowerCase() + k.slice(1), v])
  ) as T;
}

async function request<T>(base: string, path: string, options?: RequestInit): Promise<T> {
  const { headers: extraHeaders, ...restOptions } = options ?? {};
  const res = await fetch(`${base}${path}`, {
    ...restOptions,
    headers: { 'Content-Type': 'application/json', ...extraHeaders },
  });
  const data = await res.json();
  if (!res.ok) {
    const msg = data?.Message ?? data?.message ?? data?.detail ?? data ?? 'Error en la solicitud';
    throw new Error(typeof msg === 'string' ? msg : JSON.stringify(msg));
  }
  return data as T;
}

export default request;
