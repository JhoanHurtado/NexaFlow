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
    createProduct, createCustomer, createSale, customerName,
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

  const handleCreateSale = async () => {
    const ok = await createSale(cart, selectedCustomer);
    if (ok) { setCart([]); setSelectedCustomer(''); }
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
      {tab === 'products' && (
        <StockTab products={products} loading={loadingProducts} onCreateProduct={createProduct} />
      )}
      {tab === 'customers' && (
        <CustomersTab customers={customers} loading={loadingCustomers} onCreateCustomer={createCustomer} />
      )}
    </div>
  );
};
