import request, { API_URLS } from './config';

export interface RegisterTenantPayload {
  businessName: string;
  ownerName: string;
  ownerEmail: string;
  password: string;
}

export interface LoginPayload {
  tenantId: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken?: string; AccessToken?: string;
  tenantId?: string;    TenantId?: string;
  role?: string;        Role?: string;
}

export interface PlanDTO {
  id: string; name: string; price: number;
  maxUsers: number; stripePriceId?: string;
}

const BASE = API_URLS.auth;

export const authApi = {
  register: (body: RegisterTenantPayload) =>
    request<{ tenantId: string }>(BASE, '/auth/register', { method: 'POST', body: JSON.stringify(body) }),

  login: (body: LoginPayload) =>
    request<LoginResponse>(BASE, '/auth/login', { method: 'POST', body: JSON.stringify(body) }),

  getPlans: async (): Promise<PlanDTO[]> => {
    try {
      const res = await request<unknown>(BASE, '/plans');
      const r = res as Record<string, unknown>;
      const raw = (r.Data ?? r.data ?? r) as unknown;
      if (Array.isArray(raw)) return raw.map((p: Record<string, unknown>) => ({
        id:          (p.Id          ?? p.id          ?? '') as string,
        name:        (p.Name        ?? p.name        ?? '') as string,
        price:       (p.Price       ?? p.price       ?? 0)  as number,
        maxUsers:    (p.MaxUsers    ?? p.maxUsers    ?? 0)  as number,
        stripePriceId: (p.StripePriceId ?? p.stripePriceId) as string | undefined,
      }));
      return [];
    } catch { return []; }
  },

  getTenantInfo: async (tenantId: string): Promise<{ id: string; name: string } | null> => {
    try {
      const res = await request<unknown>(BASE, `/tenants/${tenantId}`);
      const r = res as Record<string, unknown>;
      const d = (r.Data ?? r.data ?? r) as Record<string, unknown>;
      return { id: (d.Id ?? d.id ?? '') as string, name: (d.Name ?? d.name ?? '') as string };
    } catch { return null; }
  },
};
