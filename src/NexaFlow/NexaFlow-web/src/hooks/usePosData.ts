import { useState, useCallback, useMemo } from 'react';
import { posApi } from '../api/pos.api';
import type { ProductDTO, PosCustomerDTO, SaleDTO } from '../api/pos.api';

export interface CartItem { productId: string; name: string; price: number; quantity: number; }

export const usePosData = (tenantId: string) => {
  const [products, setProducts]   = useState<ProductDTO[]>([]);
  const [customers, setCustomers] = useState<PosCustomerDTO[]>([]);
  const [sales, setSales]         = useState<SaleDTO[]>([]);
  const [loading, setLoading]     = useState(false);
  const [error, setError]         = useState('');
  const [success, setSuccess]     = useState('');

  const load = useCallback(async () => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      const [p, c, s] = await Promise.all([
        posApi.listProducts(tenantId),
        posApi.listCustomers(tenantId),
        posApi.listSales(tenantId),
      ]);
      setProducts(p); setCustomers(c); setSales(s);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar datos');
    } finally { setLoading(false); }
  }, [tenantId]);

  const createProduct = useCallback(async (form: { name: string; price: string; initialStock: string; lowStockThreshold: string }) => {
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createProduct(tenantId, {
        name: form.name,
        price: parseFloat(form.price),
        initialStock: parseInt(form.initialStock),
        lowStockThreshold: parseInt(form.lowStockThreshold),
      });
      setSuccess('Producto creado');
      await load();
      return true;
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear producto');
      return false;
    } finally { setLoading(false); }
  }, [tenantId, load]);

  const createCustomer = useCallback(async (form: { name: string; phone: string; email: string }) => {
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createCustomer(tenantId, form);
      setSuccess('Cliente creado');
      await load();
      return true;
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear cliente');
      return false;
    } finally { setLoading(false); }
  }, [tenantId, load]);

  const createSale = useCallback(async (cart: CartItem[], customerId?: string) => {
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createSale(tenantId, {
        customerId: customerId || undefined,
        items: cart.map(i => ({ productId: i.productId, quantity: i.quantity })),
      });
      setSuccess('Venta registrada exitosamente');
      await load();
      return true;
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear venta');
      return false;
    } finally { setLoading(false); }
  }, [tenantId, load]);

  const customerName = useCallback((id?: string) =>
    customers.find(c => c.id === id)?.name ?? 'Cliente General', [customers]);

  const filteredSales = useCallback((search: string, customerId: string, from: string, to: string) =>
    sales.filter(s => {
      const matchSearch = !search || s.id.toLowerCase().includes(search.toLowerCase()) ||
        customerName(s.customerId).toLowerCase().includes(search.toLowerCase());
      const matchCustomer = !customerId || s.customerId === customerId;
      const sDate = s.createdAt.split('T')[0];
      return matchSearch && matchCustomer && (!from || sDate >= from) && (!to || sDate <= to);
    }), [sales, customerName]);

  const filteredProducts = useCallback((search: string) =>
    !search ? products : products.filter(p => p.name.toLowerCase().includes(search.toLowerCase())),
    [products]);

  const stats = useMemo(() => ({
    total: products.length,
    lowStock: products.filter(p => p.stock <= p.lowStockThreshold).length,
    active: products.filter(p => p.active).length,
  }), [products]);

  return {
    products, customers, sales,
    loading, error, success, setSuccess,
    load, createProduct, createCustomer, createSale,
    customerName, filteredSales, filteredProducts, stats,
  };
};
