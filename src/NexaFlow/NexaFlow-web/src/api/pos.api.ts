import request, { API_URLS } from './config';

const BASE = import.meta.env.VITE_POS_API_URL ?? API_URLS.pos;
const h = (tenantId: string) => ({ 'x-tenant-id': tenantId });

export interface ProductDTO {
  id: string;
  name: string;
  price: number;
  stock: number;
  lowStockThreshold: number;
  active: boolean;
}

export interface PosCustomerDTO {
  id: string;
  name: string;
  phone?: string;
  email?: string;
}

export interface SaleItemDTO {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface SaleDTO {
  id: string;
  tenantId: string;
  customerId?: string;
  total: number;
  createdAt: string;
  items: SaleItemDTO[];
}

export interface CreateProductPayload {
  name: string;
  price: number;
  initialStock?: number;
  lowStockThreshold?: number;
}

export interface CreatePosCustomerPayload {
  name: string;
  phone?: string;
  email?: string;
}

export interface CreateSalePayload {
  customerId?: string;
  reservationId?: string;
  items: { productId: string; quantity: number }[];
}

// Normalize a raw product from the API (handles both PascalCase and camelCase)
function normalizeProduct(p: Record<string, unknown>): ProductDTO {
  return {
    id:                (p.Id ?? p.id ?? '') as string,
    name:              (p.Name ?? p.name ?? '') as string,
    price:             (p.Price ?? p.price ?? 0) as number,
    stock:             (p.Stock ?? p.stock ?? 0) as number,
    lowStockThreshold: (p.LowStockThreshold ?? p.lowStockThreshold ?? 5) as number,
    active:            (p.Active ?? p.active ?? true) as boolean,
  };
}

function normalizeCustomer(c: Record<string, unknown>): PosCustomerDTO {
  return {
    id:    (c.Id ?? c.id ?? '') as string,
    name:  (c.Name ?? c.name ?? '') as string,
    phone: (c.Phone ?? c.phone) as string | undefined,
    email: (c.Email ?? c.email) as string | undefined,
  };
}

function normalizeSale(s: Record<string, unknown>): SaleDTO {
  return {
    id:         (s.Id ?? s.id ?? '') as string,
    tenantId:   (s.TenantId ?? s.tenantId ?? '') as string,
    customerId: (s.CustomerId ?? s.customerId) as string | undefined,
    total:      (s.Total ?? s.total ?? 0) as number,
    createdAt:  (s.CreatedAt ?? s.createdAt ?? '') as string,
    items:      ((s.Items ?? s.items ?? []) as Record<string, unknown>[]).map(i => ({
      productId:   (i.ProductId ?? i.productId ?? '') as string,
      productName: (i.ProductName ?? i.productName ?? '') as string,
      quantity:    (i.Quantity ?? i.quantity ?? 0) as number,
      unitPrice:   (i.UnitPrice ?? i.unitPrice ?? 0) as number,
    })),
  };
}

function extractList<T>(res: unknown, normalize: (item: Record<string, unknown>) => T): T[] {
  const r = res as Record<string, unknown>;
  const raw = (r.Data ?? r.data ?? r) as unknown;
  if (Array.isArray(raw)) return raw.map(item => normalize(item as Record<string, unknown>));
  return [];
}

export const posApi = {
  listProducts: async (tenantId: string, page = 1, pageSize = 20): Promise<ProductDTO[]> => {
    const res = await request<unknown>(BASE, `/products?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeProduct);
  },

  getProducts: async (tenantId: string, page = 1, pageSize = 20): Promise<ProductDTO[]> => {
    const res = await request<unknown>(BASE, `/products?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeProduct);
  },

  createProduct: (tenantId: string, body: CreateProductPayload) =>
    request<{ id: string }>(BASE, '/products', {
      method: 'POST', headers: h(tenantId),
      body: JSON.stringify({
        Name: body.name,
        Price: body.price,
        InitialStock: body.initialStock ?? 0,
        LowStockThreshold: body.lowStockThreshold ?? 5,
      }),
    }),

  listCustomers: async (tenantId: string, page = 1, pageSize = 20): Promise<PosCustomerDTO[]> => {
    const res = await request<unknown>(BASE, `/customers?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeCustomer);
  },

  createCustomer: (tenantId: string, body: CreatePosCustomerPayload) =>
    request<{ id: string }>(BASE, '/customers', {
      method: 'POST', headers: h(tenantId),
      body: JSON.stringify({ Name: body.name, Phone: body.phone, Email: body.email }),
    }),

  listSales: async (tenantId: string, page = 1, pageSize = 20): Promise<SaleDTO[]> => {
    const res = await request<unknown>(BASE, `/sales?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) });
    return extractList(res, normalizeSale);
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
      body: JSON.stringify({
        CustomerId: body.customerId ?? null,
        ReservationId: body.reservationId ?? null,
        Items: body.items.map(i => ({ ProductId: i.productId, Quantity: i.quantity })),
      }),
    }),
};
