import { useState, useEffect, useCallback } from 'react';
import { posApi } from '../../api/pos.api';
import type { ProductDTO, PosCustomerDTO, SaleDTO } from '../../api/pos.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './PosPage.module.scss';
import { ShoppingCart, Package, Users, Plus, Trash2, RefreshCw } from 'lucide-react';

type Tab = 'sale' | 'products' | 'customers' | 'history';
interface CartItem { productId: string; name: string; price: number; quantity: number; }

export const PosPage = () => {
  const { tenantId } = useTenant();
  const [tab, setTab] = useState<Tab>('sale');

  const [products,  setProducts]  = useState<ProductDTO[]>([]);
  const [customers, setCustomers] = useState<PosCustomerDTO[]>([]);
  const [sales,     setSales]     = useState<SaleDTO[]>([]);
  const [loading,   setLoading]   = useState(false);
  const [error,     setError]     = useState('');
  const [success,   setSuccess]   = useState('');
  const [selectedSale, setSelectedSale] = useState<SaleDTO | null>(null);

  const [cart,             setCart]             = useState<CartItem[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState('');
  const [productForm,      setProductForm]      = useState({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  const [customerForm,     setCustomerForm]     = useState({ name: '', phone: '', email: '' });

  const load = useCallback(async () => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      const [p, c, s] = await Promise.all([
        posApi.listProducts(tenantId),
        posApi.listCustomers(tenantId),
        posApi.listSales(tenantId),
      ]);
      setProducts(p);
      setCustomers(c);
      setSales(s);
      // Clear cart items whose product no longer exists
      setCart(prev => prev.filter(item => p.some(prod => prod.id === item.productId)));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar datos');
    } finally { setLoading(false); }
  }, [tenantId]);

  useEffect(() => { load(); }, [load]);

  const addToCart = (p: ProductDTO) => {
    setCart(prev => {
      const existing = prev.find(i => i.productId === p.id);
      if (existing) return prev.map(i => i.productId === p.id ? { ...i, quantity: i.quantity + 1 } : i);
      return [...prev, { productId: p.id, name: p.name, price: p.price, quantity: 1 }];
    });
  };

  const removeFromCart = (productId: string) => setCart(prev => prev.filter(i => i.productId !== productId));
  const updateQty = (productId: string, qty: number) => {
    if (qty < 1) return removeFromCart(productId);
    setCart(prev => prev.map(i => i.productId === productId ? { ...i, quantity: qty } : i));
  };
  const cartTotal = cart.reduce((sum, i) => sum + i.price * i.quantity, 0);

  const handleCreateSale = async () => {
    if (!cart.length) return;
    // Validate all cart items still exist in current products
    const invalidItems = cart.filter(item => !products.some(p => p.id === item.productId));
    if (invalidItems.length > 0) {
      setError(`Productos no disponibles: ${invalidItems.map(i => i.name).join(', ')}. Recarga la página.`);
      return;
    }
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createSale(tenantId, {
        customerId: selectedCustomer || undefined,
        items: cart.map(i => ({ productId: i.productId, quantity: i.quantity })),
      });
      setSuccess('Venta registrada exitosamente');
      setCart([]); setSelectedCustomer('');
      await load();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear venta');
    } finally { setLoading(false); }
  };

  const handleCreateProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createProduct(tenantId, {
        name: productForm.name,
        price: parseFloat(productForm.price),
        initialStock: parseInt(productForm.initialStock),
        lowStockThreshold: parseInt(productForm.lowStockThreshold),
      });
      setSuccess('Producto creado');
      setProductForm({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
      await load();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear producto');
    } finally { setLoading(false); }
  };

  const handleCreateCustomer = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setError(''); setSuccess('');
    try {
      await posApi.createCustomer(tenantId, customerForm);
      setSuccess('Cliente creado');
      setCustomerForm({ name: '', phone: '', email: '' });
      await load();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear cliente');
    } finally { setLoading(false); }
  };

  const TABS: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'sale',      label: 'Nueva venta', icon: <ShoppingCart size={15} /> },
    { id: 'products',  label: 'Productos',   icon: <Package size={15} /> },
    { id: 'customers', label: 'Clientes',    icon: <Users size={15} /> },
    { id: 'history',   label: 'Historial',   icon: <RefreshCw size={15} /> },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1><ShoppingCart size={20} /> Punto de Venta</h1>
        <button className={styles.btnRefresh} onClick={load} disabled={loading}><RefreshCw size={14} /></button>
      </div>

      <div className={styles.tabs}>
        {TABS.map(t => (
          <button key={t.id} className={tab === t.id ? styles.tabActive : styles.tab}
            onClick={() => { setTab(t.id); setError(''); setSuccess(''); }}>
            {t.icon}{t.label}
          </button>
        ))}
      </div>

      {error   && <p className={styles.error}>{error}</p>}
      {success && <p className={styles.successMsg}>{success}</p>}
      {loading && <p className={styles.loading}>Cargando...</p>}

      {/* NUEVA VENTA */}
      {tab === 'sale' && (
        <div className={styles.saleLayout}>
          <div className={styles.productGrid}>
            <h3>Productos ({products.length})</h3>
            {products.length === 0 && !loading && <p className={styles.empty}>No hay productos disponibles.</p>}
            {products.map(p => (
              <button key={p.id} className={styles.productCard} onClick={() => addToCart(p)}>
                <span className={styles.productName}>{p.name}</span>
                <span className={styles.productPrice}>${p.price.toFixed(2)}</span>
                {p.stock > 0 && <span className={styles.productStock}>Stock: {p.stock}</span>}
                <span className={styles.addIcon}><Plus size={14} /></span>
              </button>
            ))}
          </div>

          <div className={styles.cartPanel}>
            <h3>Carrito</h3>
            <div className={styles.customerSelect}>
              <label>Cliente (opcional)</label>
              <select value={selectedCustomer} onChange={e => setSelectedCustomer(e.target.value)}>
                <option value="">Sin cliente</option>
                {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>

            {cart.length === 0
              ? <p className={styles.emptyCart}>Agrega productos al carrito</p>
              : (
                <>
                  <div className={styles.cartItems}>
                    {cart.map(item => (
                      <div key={item.productId} className={styles.cartItem}>
                        <span className={styles.cartName}>{item.name}</span>
                        <div className={styles.qtyControl}>
                          <button onClick={() => updateQty(item.productId, item.quantity - 1)}>−</button>
                          <span>{item.quantity}</span>
                          <button onClick={() => updateQty(item.productId, item.quantity + 1)}>+</button>
                        </div>
                        <span className={styles.cartSubtotal}>${(item.price * item.quantity).toFixed(2)}</span>
                        <button className={styles.removeBtn} onClick={() => removeFromCart(item.productId)}><Trash2 size={13} /></button>
                      </div>
                    ))}
                  </div>
                  <div className={styles.cartTotal}>
                    <span>Total</span><strong>${cartTotal.toFixed(2)}</strong>
                  </div>
                  <button className={styles.btnSell} onClick={handleCreateSale} disabled={loading}>
                    {loading ? 'Procesando...' : 'Cobrar'}
                  </button>
                </>
              )}
          </div>
        </div>
      )}

      {/* PRODUCTOS */}
      {tab === 'products' && (
        <div className={styles.section}>
          <form onSubmit={handleCreateProduct} className={styles.inlineForm}>
            <input placeholder="Nombre del producto" value={productForm.name}
              onChange={e => setProductForm(p => ({ ...p, name: e.target.value }))} required />
            <input type="number" placeholder="Precio" value={productForm.price}
              onChange={e => setProductForm(p => ({ ...p, price: e.target.value }))} required min="0" step="0.01" />
            <input type="number" placeholder="Stock inicial" value={productForm.initialStock}
              onChange={e => setProductForm(p => ({ ...p, initialStock: e.target.value }))} min="0" />
            <button type="submit" disabled={loading}><Plus size={14} /> Agregar</button>
          </form>
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead><tr><th>Nombre</th><th>Precio</th><th>Stock</th><th>Estado</th></tr></thead>
              <tbody>
                {products.map(p => (
                  <tr key={p.id}>
                    <td>{p.name}</td>
                    <td>${p.price.toFixed(2)}</td>
                    <td>{p.stock}</td>
                    <td>{p.active ? 'Activo' : 'Inactivo'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* CLIENTES */}
      {tab === 'customers' && (
        <div className={styles.section}>
          <form onSubmit={handleCreateCustomer} className={styles.inlineForm}>
            <input placeholder="Nombre" value={customerForm.name}
              onChange={e => setCustomerForm(p => ({ ...p, name: e.target.value }))} required />
            <input placeholder="Teléfono" value={customerForm.phone}
              onChange={e => setCustomerForm(p => ({ ...p, phone: e.target.value }))} />
            <input type="email" placeholder="Email" value={customerForm.email}
              onChange={e => setCustomerForm(p => ({ ...p, email: e.target.value }))} />
            <button type="submit" disabled={loading}><Plus size={14} /> Agregar</button>
          </form>
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead><tr><th>Nombre</th><th>Teléfono</th><th>Email</th></tr></thead>
              <tbody>
                {customers.map(c => (
                  <tr key={c.id}><td>{c.name}</td><td>{c.phone ?? '—'}</td><td>{c.email ?? '—'}</td></tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* HISTORIAL */}
      {tab === 'history' && (
        <div className={styles.section}>
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead><tr><th>Fecha</th><th>Cliente</th><th>Items</th><th>Total</th><th>Detalle</th></tr></thead>
              <tbody>
                {sales.length === 0 && <tr><td colSpan={5} className={styles.empty}>No hay ventas registradas.</td></tr>}
                {sales.map(s => (
                  <tr key={s.id}>
                    <td>{new Date(s.createdAt).toLocaleDateString()}</td>
                    <td>{customers.find(c => c.id === s.customerId)?.name ?? '—'}</td>
                    <td>{s.items?.length ?? 0} producto(s)</td>
                    <td><strong>${s.total.toFixed(2)}</strong></td>
                    <td>
                      <button className={styles.btnDetail} onClick={() => setSelectedSale(s)}>
                        Ver
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* SALE DETAIL MODAL */}
      {selectedSale && (
        <div className={styles.modalOverlay} onClick={() => setSelectedSale(null)}>
          <div className={styles.modal} onClick={e => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3>Detalle de Venta</h3>
              <button className={styles.modalClose} onClick={() => setSelectedSale(null)}>✕</button>
            </div>
            <div className={styles.modalBody}>
              <div className={styles.saleMetaRow}>
                <span><strong>Fecha:</strong> {new Date(selectedSale.createdAt).toLocaleString()}</span>
                <span><strong>Cliente:</strong> {customers.find(c => c.id === selectedSale.customerId)?.name ?? 'Sin cliente'}</span>
              </div>
              <table className={styles.table}>
                <thead><tr><th>Producto</th><th>Cant.</th><th>Precio unit.</th><th>Subtotal</th></tr></thead>
                <tbody>
                  {(selectedSale.items ?? []).map((item, i) => (
                    <tr key={i}>
                      <td>{item.productName || products.find(p => p.id === item.productId)?.name || item.productId}</td>
                      <td>{item.quantity}</td>
                      <td>${item.unitPrice.toFixed(2)}</td>
                      <td>${(item.quantity * item.unitPrice).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={3}><strong>Total</strong></td>
                    <td><strong>${selectedSale.total.toFixed(2)}</strong></td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
