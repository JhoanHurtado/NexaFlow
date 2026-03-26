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
  token: string;
  tenantId: string;
  role: string;
}

const BASE = API_URLS.auth;

export const authApi = {
  register: (body: RegisterTenantPayload) =>
    request<{ tenantId: string }>(BASE, '/auth/register', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  login: (body: LoginPayload) =>
    request<LoginResponse>(BASE, '/auth/login', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
};
