import request, { API_URLS } from './config';

const BASE = import.meta.env.VITE_POS_API_URL ?? API_URLS.pos;
const h = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface ProductDTO {
  id: string; name: string; price: number;
  stock: number; lowStockThreshold: number; active: boolean;
}
export interface PosCustomerDTO {
  id: string; name: string; phone?: string; email?: string;
}
export interface SaleItemDTO {
  productId: string; productName: string; quantity: number; unitPrice: number; subtotal: number;
}
export interface SaleDTO {
  id: string; tenantId: string; customerId?: string; reservationId?: string;
  subtotal: number; taxRate: number; taxAmount: number; total: number;
  status: string; createdAt: string; items: SaleItemDTO[];
}
export interface PaginatedResult<T> {
  items: T[]; currentPage: number; pageSize: number;
  totalCount: number; totalPages: number; hasNext: boolean; hasPrev: boolean;
}
export interface TenantConfigDTO {
  taxRate: number; currency: string;
  slotDurationMinutes: number; openTime: string; closeTime: string;
  updatedAt?: string;
}
export interface CreateProductPayload { name: string; price: number; initialStock?: number; lowStockThreshold?: number; }
export interface CreatePosCustomerPayload { name: string; phone?: string; email?: string; }
export interface CreateSalePayload { customerId?: string; reservationId?: string; items: { productId: string; quantity: number }[]; }

function normalizeProduct(p: Record<string, unknown>): ProductDTO {
  return {
    id: (p.Id ?? p.id ?? '') as string, name: (p.Name ?? p.name ?? '') as string,
    price: (p.Price ?? p.price ?? 0) as number, stock: (p.Stock ?? p.stock ?? 0) as number,
    lowStockThreshold: (p.LowStockThreshold ?? p.lowStockThreshold ?? 5) as number,
    active: (p.Active ?? p.active ?? true) as boolean,
  };
}
function normalizeCustomer(c: Record<string, unknown>): PosCustomerDTO {
  return {
    id: (c.Id ?? c.id ?? '') as string, name: (c.Name ?? c.name ?? '') as string,
    phone: (c.Phone ?? c.phone) as string | undefined, email: (c.Email ?? c.email) as string | undefined,
  };
}
function normalizeSale(s: Record<string, unknown>): SaleDTO {
  return {
    id: (s.Id ?? s.id ?? '') as string, tenantId: (s.TenantId ?? s.tenantId ?? '') as string,
    customerId: (s.CustomerId ?? s.customerId) as string | undefined,
    reservationId: (s.ReservationId ?? s.reservationId) as string | undefined,
    subtotal: (s.Subtotal ?? s.subtotal ?? s.Total ?? s.total ?? 0) as number,
    taxRate: (s.TaxRate ?? s.taxRate ?? 0) as number,
    taxAmount: (s.TaxAmount ?? s.taxAmount ?? 0) as number,
    total: (s.Total ?? s.total ?? 0) as number,
    status: ((s.Status ?? s.status ?? 'completed') as string).toLowerCase(),
    createdAt: (s.CreatedAt ?? s.createdAt ?? '') as string,
    items: ((s.Items ?? s.items ?? []) as Record<string, unknown>[]).map(i => ({
      productId: (i.ProductId ?? i.productId ?? '') as string,
      productName: (i.ProductName ?? i.productName ?? '') as string,
      quantity: (i.Quantity ?? i.quantity ?? 0) as number,
      unitPrice: (i.UnitPrice ?? i.unitPrice ?? 0) as number,
      subtotal: (i.Subtotal ?? i.subtotal ?? 0) as number,
    })),
  };
}

function extractList<T>(res: unknown, normalize: (item: Record<string, unknown>) => T): T[] {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as unknown;
  if (Array.isArray(raw)) return raw.map(item => normalize(item as Record<string, unknown>));
  return [];
}

function extractPaginated<T>(res: unknown, normalize: (item: Record<string, unknown>) => T): PaginatedResult<T> {
  const r = res as Record<string, unknown>;
  const data = (r.Data ?? r.data ?? r) as unknown;
  const pagination = (r.Pagination ?? r.pagination) as Record<string, unknown> | undefined;
  const items = Array.isArray(data) ? data.map(item => normalize(item as Record<string, unknown>)) : [];
  const totalCount  = (pagination?.TotalCount  ?? pagination?.totalCount  ?? items.length) as number;
  const pageSize    = (pagination?.PageSize    ?? pagination?.pageSize    ?? 20) as number;
  const currentPage = (pagination?.CurrentPage ?? pagination?.currentPage ?? 1) as number;
  const totalPages  = Math.max(1, Math.ceil(totalCount / pageSize));
  // Prefer backend-provided flags; fall back to local calculation
  const hasNext = pagination?.HasNext  != null ? Boolean(pagination.HasNext)
                : pagination?.hasNext  != null ? Boolean(pagination.hasNext)
                : currentPage < totalPages;
  const hasPrev = pagination?.HasPrev  != null ? Boolean(pagination.HasPrev)
                : pagination?.hasPrev  != null ? Boolean(pagination.hasPrev)
                : currentPage > 1;
  return { items, currentPage, pageSize, totalCount, totalPages, hasNext, hasPrev };
}

export const posApi = {
  listProducts: async (tenantId: string, page = 1, pageSize = 100): Promise<ProductDTO[]> => {
    const res = await request<unknown>(BASE, `/products?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeProduct);
  },
  getMenu: async (tenantId: string): Promise<ProductDTO[]> => {
    const res = await request<unknown>(BASE, `/products?page=1&pageSize=100`, { headers: h(tenantId) });
    return extractList(res, normalizeProduct);
  },
  createProduct: (tenantId: string, body: CreateProductPayload) =>
    request<{ id: string }>(BASE, '/products', {
      method: 'POST', headers: h(tenantId),
      body: JSON.stringify({ Name: body.name, Price: body.price, InitialStock: body.initialStock ?? 0, LowStockThreshold: body.lowStockThreshold ?? 5 }),
    }),
  listCustomers: async (tenantId: string, page = 1, pageSize = 100): Promise<PosCustomerDTO[]> => {
    const res = await request<unknown>(BASE, `/customers?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeCustomer);
  },
  createCustomer: (tenantId: string, body: CreatePosCustomerPayload) =>
    request<{ id: string }>(BASE, '/customers', {
      method: 'POST', headers: h(tenantId),
      body: JSON.stringify({ Name: body.name, Phone: body.phone, Email: body.email }),
    }),
  listSales: async (tenantId: string, page = 1, pageSize = 20): Promise<PaginatedResult<SaleDTO>> => {
    const res = await request<unknown>(BASE, `/sales?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractPaginated(res, normalizeSale);
  },
  getSale: async (tenantId: string, id: string): Promise<SaleDTO | null> => {
    const res = await request<unknown>(BASE, `/sales/${id}`, { headers: h(tenantId) });
    const r = res as Record<string, unknown>;
    const raw = (r.Data ?? r.data ?? r) as Record<string, unknown>;
    return normalizeSale(raw);
  },
  createSale: (tenantId: string, body: CreateSalePayload) =>
    request<{ id: string }>(BASE, '/sales', {
      method: 'POST', headers: h(tenantId),
      body: JSON.stringify({ CustomerId: body.customerId ?? null, ReservationId: body.reservationId ?? null, Items: body.items.map(i => ({ ProductId: i.productId, Quantity: i.quantity })) }),
    }),
  updateSaleStatus: (tenantId: string, saleId: string, status: 'pending' | 'completed' | 'cancelled') =>
    request<void>(BASE, `/sales/${saleId}/status`, {
      method: 'PATCH', headers: h(tenantId),
      body: JSON.stringify({ status }),
    }),
  getConfig: async (tenantId: string): Promise<TenantConfigDTO> => {
    try {
      const res = await request<unknown>(BASE, '/config', { headers: h(tenantId) });
      const r = res as Record<string, unknown>;
      const d = (r.Data ?? r.data ?? r) as Record<string, unknown>;
      return {
        taxRate:              (d.TaxRate              ?? d.taxRate              ?? 19)       as number,
        currency:             (d.Currency             ?? d.currency             ?? 'COP')    as string,
        slotDurationMinutes:  (d.SlotDurationMinutes  ?? d.slotDurationMinutes  ?? 60)       as number,
        openTime:             (d.OpenTime             ?? d.openTime             ?? '08:00')  as string,
        closeTime:            (d.CloseTime            ?? d.closeTime            ?? '20:00')  as string,
        updatedAt:            (d.UpdatedAt            ?? d.updatedAt)                        as string | undefined,
      };
    } catch { return { taxRate: 19, currency: 'COP', slotDurationMinutes: 60, openTime: '08:00', closeTime: '20:00' }; }
  },

  updateConfig: async (tenantId: string, body: Omit<TenantConfigDTO, 'updatedAt'>): Promise<TenantConfigDTO> => {
    const res = await request<unknown>(BASE, '/config', {
      method: 'PUT', headers: h(tenantId),
      body: JSON.stringify({
        TaxRate:             body.taxRate,
        Currency:            body.currency,
        SlotDurationMinutes: body.slotDurationMinutes,
        OpenTime:            body.openTime,
        CloseTime:           body.closeTime,
      }),
    });
    const r = res as Record<string, unknown>;
    const d = (r.Data ?? r.data ?? r) as Record<string, unknown>;
    return {
      taxRate:             (d.TaxRate             ?? d.taxRate             ?? body.taxRate)             as number,
      currency:            (d.Currency            ?? d.currency            ?? body.currency)            as string,
      slotDurationMinutes: (d.SlotDurationMinutes ?? d.slotDurationMinutes ?? body.slotDurationMinutes) as number,
      openTime:            (d.OpenTime            ?? d.openTime            ?? body.openTime)            as string,
      closeTime:           (d.CloseTime           ?? d.closeTime           ?? body.closeTime)           as string,
      updatedAt:           (d.UpdatedAt           ?? d.updatedAt)                                       as string | undefined,
    };
  },
};
