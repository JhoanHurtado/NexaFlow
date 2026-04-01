import { useState, useCallback } from 'react';
import { posApi } from '../api/pos.api';
import type { ProductDTO, PosCustomerDTO, SaleDTO, PaginatedResult, TenantConfigDTO } from '../api/pos.api';

export interface CartItem { productId: string; name: string; price: number; quantity: number; }

const EMPTY_PAGE: PaginatedResult<SaleDTO> = {
  items: [], currentPage: 1, pageSize: 20, totalCount: 0, totalPages: 1, hasNext: false, hasPrev: false,
};

export const usePosData = (tenantId: string) => {
  const [products, setProducts]   = useState<ProductDTO[]>([]);
  const [customers, setCustomers] = useState<PosCustomerDTO[]>([]);
  const [salesPage, setSalesPage] = useState<PaginatedResult<SaleDTO>>(EMPTY_PAGE);
  const [config, setConfig]       = useState<TenantConfigDTO>({ taxRate: 19, currency: 'COP', slotDurationMinutes: 60, openTime: '08:00', closeTime: '20:00' });

  const [loadingProducts,  setLoadingProducts]  = useState(false);
  const [loadingCustomers, setLoadingCustomers] = useState(false);
  const [loadingSales,     setLoadingSales]     = useState(false);
  const [error,   setError]   = useState('');
  const [success, setSuccess] = useState('');

  // Carga productos (tab venta/stock)
  const loadProducts = useCallback(async () => {
    if (!tenantId) return;
    setLoadingProducts(true);
    try { setProducts(await posApi.listProducts(tenantId)); }
    catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al cargar productos'); }
    finally { setLoadingProducts(false); }
  }, [tenantId]);

  // Carga clientes (tab clientes)
  const loadCustomers = useCallback(async () => {
    if (!tenantId) return;
    setLoadingCustomers(true);
    try { setCustomers(await posApi.listCustomers(tenantId)); }
    catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al cargar clientes'); }
    finally { setLoadingCustomers(false); }
  }, [tenantId]);

  // Carga ventas paginadas (tab historial)
  const loadSales = useCallback(async (page = 1, pageSize = 20) => {
    if (!tenantId) return;
    setLoadingSales(true);
    try { setSalesPage(await posApi.listSales(tenantId, page, pageSize)); }
    catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al cargar ventas'); }
    finally { setLoadingSales(false); }
  }, [tenantId]);

  // Carga config (IVA) — se llama una vez al montar
  const loadConfig = useCallback(async () => {
    if (!tenantId) return;
    try { setConfig(await posApi.getConfig(tenantId)); }
    catch { /* usa default 19% */ }
  }, [tenantId]);

  // Carga inicial prioritaria: productos primero, luego el resto en paralelo
  const loadAll = useCallback(async () => {
    if (!tenantId) return;
    setError('');
    // Productos primero (tab activo por defecto)
    await loadProducts();
    // El resto en paralelo sin bloquear
    Promise.all([loadCustomers(), loadSales(), loadConfig()]).catch(() => {});
  }, [tenantId, loadProducts, loadCustomers, loadSales, loadConfig]);

  const createProduct = useCallback(async (form: { name: string; price: string; initialStock: string; lowStockThreshold: string }) => {
    setError(''); setSuccess('');
    try {
      await posApi.createProduct(tenantId, { name: form.name, price: parseFloat(form.price), initialStock: parseInt(form.initialStock), lowStockThreshold: parseInt(form.lowStockThreshold) });
      setSuccess('Producto creado');
      await loadProducts();
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al crear producto'); return false; }
  }, [tenantId, loadProducts]);

  const createCustomer = useCallback(async (form: { name: string; phone: string; email: string }) => {
    setError(''); setSuccess('');
    try {
      await posApi.createCustomer(tenantId, form);
      setSuccess('Cliente creado');
      await loadCustomers();
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al crear cliente'); return false; }
  }, [tenantId, loadCustomers]);

  const createSale = useCallback(async (cart: CartItem[], customerId?: string) => {
    setError(''); setSuccess('');
    if (!customerId) { setError('Debe seleccionar un cliente para registrar la venta'); return false; }
    try {
      await posApi.createSale(tenantId, { customerId, items: cart.map(i => ({ productId: i.productId, quantity: i.quantity })) });
      setSuccess('Venta registrada exitosamente');
      await Promise.all([loadProducts(), loadSales()]);
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al crear venta'); return false; }
  }, [tenantId, loadProducts, loadSales]);

  const customerName = useCallback((id?: string) =>
    customers.find(c => c.id === id)?.name ?? 'Cliente General', [customers]);

  return {
    products, customers, salesPage, config,
    loadingProducts, loadingCustomers, loadingSales,
    loading: loadingProducts || loadingCustomers || loadingSales,
    error, success, setSuccess, setError,
    loadAll, loadProducts, loadCustomers, loadSales, loadConfig,
    createProduct, createCustomer, createSale, customerName,
  };
};
