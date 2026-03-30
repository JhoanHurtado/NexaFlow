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

  const {
    products, customers, sales,
    loading, error, success, setSuccess,
    load, createProduct, createCustomer, createSale,
    customerName,
  } = usePosData(tenantId);

  useEffect(() => { load(); }, [load]);

  const addToCart = useCallback((p: ProductDTO) => {
    setCart(prev => {
      const existing = prev.find(i => i.productId === p.id);
      if (existing) return prev.map(i => i.productId === p.id ? { ...i, quantity: i.quantity + 1 } : i);
      return [...prev, { productId: p.id, name: p.name, price: p.price, quantity: 1 }];
    });
  }, []);

  const removeFromCart = useCallback((id: string) => setCart(prev => prev.filter(i => i.productId !== id)), []);

  const updateQty = useCallback((id: string, delta: number) => {
    setCart(prev => prev.map(i => i.productId === id ? { ...i, quantity: i.quantity + delta } : i).filter(i => i.quantity > 0));
  }, []);

  const handleCreateSale = async (customerId?: string) => {
    const ok = await createSale(cart, customerId);
    if (ok) setCart([]);
  };

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
              <button key={t.id}
                className={tab === t.id ? styles.tabActive : styles.tab}
                onClick={() => { setTab(t.id); setSuccess(''); }}>
                {t.icon}<span>{t.label}</span>
              </button>
            ))}
          </div>
          <button className={styles.btnRefresh} onClick={load} disabled={loading}><RefreshCw size={14} /></button>
        </div>
      </header>

      {error   && <p className={styles.error}>{error}</p>}
      {success && <p className={styles.successMsg}>{success}</p>}

      {tab === 'sale' && (
        <SaleTab
          products={products} customers={customers} cart={cart} loading={loading}
          onAddToCart={addToCart} onRemoveFromCart={removeFromCart}
          onUpdateQty={updateQty} onClearCart={() => setCart([])}
          onCreateSale={handleCreateSale}
        />
      )}
      {tab === 'history' && (
        <HistoryTab sales={sales} customers={customers} customerName={customerName} />
      )}
      {tab === 'products' && (
        <StockTab products={products} loading={loading} onCreateProduct={createProduct} />
      )}
      {tab === 'customers' && (
        <CustomersTab customers={customers} loading={loading} onCreateCustomer={createCustomer} />
      )}
    </div>
  );
};
