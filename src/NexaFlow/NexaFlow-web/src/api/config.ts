export const API_URLS = {
  auth: import.meta.env.VITE_AUTH_API_URL ?? 'http://localhost:5001',
  book: import.meta.env.VITE_BOOK_API_URL ?? 'http://localhost:5002',
};

async function request<T>(base: string, path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${base}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  const data = await res.json();
  if (!res.ok) throw new Error(data?.message ?? data ?? 'Error en la solicitud');
  return data as T;
}

export default request;
