import request from './config';

const BASE = import.meta.env.VITE_POS_API_URL ?? 'http://localhost:5050';
const h = (tenantId: string) => ({ 'x-tenant-id': tenantId });

// ── Products (Inventory) ──────────────────────────────────────────────
export interface ProductDTO {
  id?: string;
  Id?: string;
  name?: string;
  Name?: string;
  price?: number;
  Price?: number;
  stock?: number;
  Stock?: number;
  lowStockThreshold?: number;
  LowStockThreshold?: number;
  status?: string;
  Status?: string;
}
export interface CreateProductPayload { name: string; price: number; initialStock?: number; lowStockThreshold?: number; }

// ── Customers ─────────────────────────────────────────────
export interface PosCustomerDTO {
  id?: string;
  Id?: string;
  name?: string;
  Name?: string;
  phone?: string;
  Phone?: string;
  email?: string;
  Email?: string;
}
export interface CreatePosCustomerPayload { name: string; phone?: string; email?: string; }

// ── Sales ─────────────────────────────────────────────────
export interface SaleItemDTO { productId: string; productName: string; quantity: number; unitPrice: number; subtotal: number; }
export interface SaleDTO { id: string; tenantId: string; customerId?: string; total: number; createdAt: string; items: SaleItemDTO[]; }
export interface CreateSalePayload { customerId?: string; reservationId?: string; items: { productId: string; quantity: number }[]; }

export interface ApiResponse<T> {
  data?: T;
  Data?: T;
}

export const posApi = {
  // Products
  listProducts: (tenantId: string, page = 1, pageSize = 20) =>
    request<ApiResponse<ProductDTO[]>>(BASE, `/products?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) }),

  getProducts: (tenantId: string, page = 1, pageSize = 20) =>
    request<ApiResponse<ProductDTO[]>>(BASE, `/products?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) }),

  createProduct: (tenantId: string, body: CreateProductPayload) =>
    request<{ id: string }>(BASE, '/products', { method: 'POST', headers: h(tenantId), body: JSON.stringify(body) }),

  // Customers
  listCustomers: (tenantId: string, page = 1, pageSize = 20) =>
    request<ApiResponse<PosCustomerDTO[]>>(BASE, `/customers?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) }),

  createCustomer: (tenantId: string, body: CreatePosCustomerPayload) =>
    request<{ id: string }>(BASE, '/customers', { method: 'POST', headers: h(tenantId), body: JSON.stringify(body) }),

  // Sales
  listSales: (tenantId: string, page = 1, pageSize = 20) =>
    request<{ data: SaleDTO[] }>(BASE, `/sales?page=${page}&pageSize=${pageSize}`, { headers: h(tenantId) }),

  getSale: (tenantId: string, id: string) =>
    request<{ data: SaleDTO }>(BASE, `/sales/${id}`, { headers: h(tenantId) }),

  createSale: (tenantId: string, body: CreateSalePayload) =>
    request<{ id: string }>(BASE, '/sales', { method: 'POST', headers: h(tenantId), body: JSON.stringify(body) }),
};
