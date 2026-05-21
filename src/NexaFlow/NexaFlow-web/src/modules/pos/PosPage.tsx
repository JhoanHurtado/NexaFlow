import { useState, useEffect, useCallback } from 'react';
import { ShoppingCart, Clock, Package, Users, RefreshCw } from 'lucide-react';
import { usePosData } from '../../hooks/usePosData';
import { useTenant } from '../../hooks/useTenant';
import type { ProductDTO } from '../../api/pos.api';
import type { CartItem } from '../../hooks/usePosData';
import { SaleTab }      from './tabs/SaleTab';
import { HistoryTab }   from './tabs/HistoryTab';
import { StockTab }     from './tabs/StockTab';
import { CustomersTab } from './tabs/CustomersTab';
import styles from './PosPage.module.scss';

type Tab = 'sale' | 'history' | 'products' | 'customers';

const TABS: { id: Tab; label: string; icon: React.ReactNode }[] = [
  { id: 'sale',      label: 'Nueva Venta', icon: <ShoppingCart size={14} /> },
  { id: 'history',   label: 'Historial',   icon: <Clock size={14} /> },
  { id: 'products',  label: 'Stock',       icon: <Package size={14} /> },
  { id: 'customers', label: 'Clientes',    icon: <Users size={14} /> },
];

export const PosPage = () => {
  const { tenantId } = useTenant();
  const [tab, setTab] = useState<Tab>('sale');
  const [cart, setCart] = useState<CartItem[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState('');

  const {
    products, customers, salesPage, config,
    loadingProducts, loadingCustomers, loadingSales,
    error, success, setSuccess, setError,
    loadAll, loadProducts, loadCustomers, loadSales,
    createProduct, createCustomer, createSale, updateProduct, updateCustomer, customerName,
  } = usePosData(tenantId);

  // Carga inicial: productos primero, resto en paralelo
  useEffect(() => { loadAll(); }, [loadAll]);

  // Carga lazy por tab
  useEffect(() => {
    if (tab === 'history')   loadSales(1);
    if (tab === 'customers') loadCustomers();
    if (tab === 'products')  loadProducts();
  }, [tab]); // eslint-disable-line react-hooks/exhaustive-deps

  const addToCart = useCallback((p: ProductDTO) => {
    if (!selectedCustomer) { setError('Selecciona un cliente antes de agregar productos.'); return; }
    setCart(prev => {
      const existing = prev.find(i => i.productId === p.id);
      const currentQty = existing?.quantity ?? 0;
      if (currentQty >= p.stock || p.stock <= 0) { setError(`Sin stock suficiente para "${p.name}".`); return prev; }
      setError('');
      if (existing) return prev.map(i => i.productId === p.id ? { ...i, quantity: i.quantity + 1 } : i);
      return [...prev, { productId: p.id, name: p.name, price: p.price, quantity: 1 }];
    });
  }, [selectedCustomer, setError]);

  const removeFromCart = useCallback((id: string) => setCart(prev => prev.filter(i => i.productId !== id)), []);

  const updateQty = useCallback((id: string, delta: number) => {
    setCart(prev => prev.map(i => i.productId === id ? { ...i, quantity: i.quantity + delta } : i).filter(i => i.quantity > 0));
  }, []);

  const [salePopup, setSalePopup] = useState<{ cart: CartItem[]; customerId: string } | null>(null);

  const handleCreateSale = async () => {
    if (cart.length === 0 || !selectedCustomer) return;
    setSalePopup({ cart: [...cart], customerId: selectedCustomer });
  };

  const confirmSale = async (status: 'pending' | 'completed') => {
    if (!salePopup) return;
    const ok = await createSale(salePopup.cart, salePopup.customerId, status);
    if (ok) { setCart([]); setSelectedCustomer(''); }
    setSalePopup(null);
  };

  const handleTabChange = (t: Tab) => { setTab(t); setSuccess(''); setError(''); };

  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.headerBrand}>
          <div className={styles.brandIcon}><ShoppingCart size={26} strokeWidth={2.5} /></div>
          <div><h1>NexaFlow POS</h1><p>Punto de Venta Inteligente</p></div>
        </div>
        <div className={styles.headerRight}>
          <div className={styles.tabs}>
            {TABS.map(t => (
              <button key={t.id} className={tab === t.id ? styles.tabActive : styles.tab} onClick={() => handleTabChange(t.id)}>
                {t.icon}<span>{t.label}</span>
              </button>
            ))}
          </div>
          <button className={styles.btnRefresh} onClick={loadAll} disabled={loadingProducts}><RefreshCw size={14} /></button>
        </div>
      </header>

      {error   && <p className={styles.error}>{error}</p>}
      {success && <p className={styles.successMsg}>{success}</p>}

      {tab === 'sale' && (
        <SaleTab
          products={products} customers={customers} cart={cart}
          loading={loadingProducts} taxRate={config.taxRate}
          selectedCustomer={selectedCustomer} onCustomerChange={setSelectedCustomer}
          onAddToCart={addToCart} onRemoveFromCart={removeFromCart}
          onUpdateQty={updateQty} onClearCart={() => setCart([])}
          onCreateSale={handleCreateSale}
        />
      )}
      {tab === 'history' && (
        <HistoryTab salesPage={salesPage} customers={customers} loading={loadingSales} tenantId={tenantId} customerName={customerName} onPageChange={loadSales} onRefresh={() => loadSales(salesPage.currentPage, salesPage.pageSize)} />
      )}
      {tab === 'products'  && (
        <StockTab products={products} loading={loadingProducts} onCreateProduct={createProduct} onUpdateProduct={updateProduct} />
      )}
      {tab === 'customers' && (
        <CustomersTab customers={customers} loading={loadingCustomers} onCreateCustomer={createCustomer} onUpdateCustomer={updateCustomer} />
      )}

      {salePopup && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div style={{ background: '#1e293b', borderRadius: '12px', padding: '2rem', maxWidth: '400px', width: '90%', textAlign: 'center' }}>
            <h3 style={{ marginBottom: '0.5rem', color: '#f1f5f9' }}>¿Cómo deseas registrar la venta?</h3>
            <p style={{ color: '#94a3b8', marginBottom: '1.5rem', fontSize: '0.9rem' }}>
              Puedes finalizarla como completada o dejarla pendiente de pago.
            </p>
            <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
              <button onClick={() => confirmSale('pending')}
                style={{ padding: '0.6rem 1.2rem', borderRadius: '8px', border: '1px solid #475569', background: 'transparent', color: '#94a3b8', cursor: 'pointer' }}>
                Pendiente de pago
              </button>
              <button onClick={() => confirmSale('completed')}
                style={{ padding: '0.6rem 1.2rem', borderRadius: '8px', border: 'none', background: '#22c55e', color: '#fff', cursor: 'pointer', fontWeight: 600 }}>
                Completar ahora
              </button>
            </div>
            <button onClick={() => setSalePopup(null)}
              style={{ marginTop: '1rem', background: 'none', border: 'none', color: '#64748b', cursor: 'pointer', fontSize: '0.85rem' }}>
              Cancelar
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
