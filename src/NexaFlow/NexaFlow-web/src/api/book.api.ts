import request, { API_URLS } from './config';

const BASE = API_URLS.book;

const headers = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface RegisterCustomerPayload {
  name: string;
  phone?: string;
  email?: string;
}

export interface CreateReservationPayload {
  customerId: string;
  reservationDate: string; // yyyy-MM-dd
  timeSlot: string;        // HH:mm
  notes?: string;
}

export interface AvailabilitySlot {
  timeSlot: string;
  available: boolean;
}

export const bookApi = {
  registerCustomer: (tenantId: string, body: RegisterCustomerPayload) =>
    request<{ id: string }>(BASE, '/customers', {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify({ ...body, selfRegistered: true }),
    }),

  getAvailability: (tenantId: string, date: string) =>
    request<{ data: AvailabilitySlot[] }>(BASE, `/reservations/availability?date=${date}`, {
      headers: headers(tenantId),
    }),

  createReservation: (tenantId: string, body: CreateReservationPayload) =>
    request<{ id: string }>(BASE, '/reservations', {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify(body),
    }),

  cancelReservation: (tenantId: string, reservationId: string, cancelledBy: string) =>
    request<{ id: string }>(BASE, `/reservations/${reservationId}/cancel`, {
      method: 'POST',
      headers: headers(tenantId),
      body: JSON.stringify({ cancelledBy }),
    }),

  getCustomerReservations: (tenantId: string, customerId: string) =>
    request<{ data: ReservationItem[] }>(BASE, `/customers/${customerId}/reservations`, {
      headers: headers(tenantId),
    }),
};

export interface ReservationItem {
  id: string;
  reservationDate: string;
  timeSlot: string;
  status: string;
  notes?: string;
}
