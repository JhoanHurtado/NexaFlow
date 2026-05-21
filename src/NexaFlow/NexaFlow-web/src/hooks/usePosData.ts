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
  const [salesPageSize, setSalesPageSize] = useState(20);
  const loadSales = useCallback(async (page = 1, pageSize?: number) => {
    if (!tenantId) return;
    const size = pageSize ?? salesPageSize;
    if (pageSize != null) setSalesPageSize(pageSize);
    setLoadingSales(true);
    try { setSalesPage(await posApi.listSales(tenantId, page, size)); }
    catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al cargar ventas'); }
    finally { setLoadingSales(false); }
  }, [tenantId, salesPageSize]);

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

  const createSale = useCallback(async (cart: CartItem[], customerId?: string, status: 'pending' | 'completed' = 'pending') => {
    setError(''); setSuccess('');
    if (!customerId) { setError('Debe seleccionar un cliente para registrar la venta'); return false; }
    try {
      const saleId = await posApi.createSale(tenantId, { customerId, items: cart.map(i => ({ productId: i.productId, quantity: i.quantity })) });
      if (status === 'completed') {
        await posApi.updateSaleStatus(tenantId, (saleId as unknown as { id?: string } | string) instanceof Object ? (saleId as unknown as { id: string }).id : String(saleId), 'completed');
      }
      setSuccess(status === 'completed' ? 'Venta completada exitosamente' : 'Venta registrada como pendiente');
      await Promise.all([loadProducts(), loadSales()]);
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al crear venta'); return false; }
  }, [tenantId, loadProducts, loadSales]);

  const updateProduct = useCallback(async (id: string, body: { name?: string; price?: number; stock?: number; lowStockThreshold?: number; active?: boolean }) => {
    setError(''); setSuccess('');
    try {
      await posApi.updateProduct(tenantId, id, body);
      setSuccess('Producto actualizado');
      await loadProducts();
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al actualizar producto'); return false; }
  }, [tenantId, loadProducts]);

  const updateCustomer = useCallback(async (id: string, body: { name: string; phone?: string; email?: string }) => {
    setError(''); setSuccess('');
    try {
      await posApi.updateCustomer(tenantId, id, body);
      setSuccess('Cliente actualizado');
      await loadCustomers();
      return true;
    } catch (e: unknown) { setError(e instanceof Error ? e.message : 'Error al actualizar cliente'); return false; }
  }, [tenantId, loadCustomers]);

  const customerName = useCallback((id?: string) =>
    customers.find(c => c.id === id)?.name ?? 'Cliente General', [customers]);

  return {
    products, customers, salesPage, config,
    loadingProducts, loadingCustomers, loadingSales,
    loading: loadingProducts || loadingCustomers || loadingSales,
    error, success, setSuccess, setError,
    loadAll, loadProducts, loadCustomers, loadSales, loadConfig,
    createProduct, createCustomer, createSale, updateProduct, updateCustomer, customerName,
  };
};
