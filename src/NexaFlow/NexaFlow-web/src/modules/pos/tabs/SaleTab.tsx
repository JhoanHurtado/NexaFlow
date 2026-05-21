import { ShoppingCart, Search, PlusCircle, MinusCircle, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { Pagination } from '../../../components/Pagination';
import type { ProductDTO, PosCustomerDTO } from '../../../api/pos.api';
import type { CartItem } from '../../../hooks/usePosData';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

const PAGE_SIZE = 20;

interface Props {
  products: ProductDTO[];
  customers: PosCustomerDTO[];
  cart: CartItem[];
  loading: boolean;
  taxRate: number;
  selectedCustomer: string;
  onCustomerChange: (id: string) => void;
  onAddToCart: (p: ProductDTO) => void;
  onRemoveFromCart: (id: string) => void;
  onUpdateQty: (id: string, delta: number) => void;
  onClearCart: () => void;
  onCreateSale: () => void;
}

export const SaleTab = ({
  products, customers, cart, loading, taxRate,
  selectedCustomer, onCustomerChange,
  onAddToCart, onRemoveFromCart, onUpdateQty, onClearCart, onCreateSale,
}: Props) => {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const filtered = products.filter(p => p.active && p.name.toLowerCase().includes(search.toLowerCase()));
  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));

  const subtotal  = cart.reduce((s, i) => s + i.price * i.quantity, 0);
  const taxAmount = Math.round(subtotal * taxRate / 100 * 100) / 100;
  const total     = subtotal + taxAmount;

  return (
    <div className={styles.saleLayout}>
      {/* Catalog */}
      <div className={styles.catalogCol}>
        <div className={styles.searchBar}>
          <Search size={16} className={styles.searchIcon} />
          <input type="text" placeholder="Buscar producto..." value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }} />
        </div>
        <div className={styles.productGrid}>
          {loading && <p className={styles.empty}>Cargando...</p>}
          {!loading && filtered.length === 0 && <p className={styles.empty}>No hay productos disponibles.</p>}
          {paged.map(p => (
            <button key={p.id} className={styles.productCard} onClick={() => onAddToCart(p)}>
              <div className={styles.productCardTop}>
                <span className={styles.productCategory}>General</span>
                {p.stock <= 5 && <span className={styles.lowStock}>¡Stock Bajo!</span>}
              </div>
              <span className={styles.productName}>{p.name}</span>
              <div className={styles.productCardBottom}>
                <span className={styles.productPrice}>{formatValue(p.price)}</span>
                <span className={p.stock <= 5 ? styles.stockLow : styles.productStock}>Quedan: {p.stock}</span>
              </div>
              <div className={styles.addIconOverlay}><PlusCircle size={20} /></div>
            </button>
          ))}
        </div>
        <Pagination page={page} totalPages={totalPages} info={`${filtered.length} productos`} onChange={setPage} />
      </div>

      {/* Cart */}
      <div className={styles.cartPanel}>
        <div className={styles.cartHeader}>
          <h3><ShoppingCart size={16} /> Venta Actual</h3>
          <button className={styles.clearBtn} onClick={onClearCart}>Limpiar</button>
        </div>

        {/* Cliente — obligatorio antes de agregar productos */}
        <div className={styles.customerSelect}>
          <label>Cliente <span style={{ color: '#ef4444' }}>*</span></label>
          <select value={selectedCustomer} onChange={e => onCustomerChange(e.target.value)}>
            <option value="">— Selecciona un cliente —</option>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          {!selectedCustomer && <p style={{ fontSize: '0.7rem', color: '#94a3b8', margin: '0.25rem 0 0' }}>Selecciona un cliente para agregar productos</p>}
        </div>

        <div className={styles.cartItems}>
          {cart.length === 0 ? (
            <div className={styles.emptyCart}>
              <ShoppingCart size={44} strokeWidth={1} />
              <p>Tu carrito está vacío.<br />Selecciona un cliente y luego productos.</p>
            </div>
          ) : cart.map(item => (
            <div key={item.productId} className={styles.cartItem}>
              <div className={styles.cartItemInfo}>
                <span className={styles.cartName}>{item.name}</span>
                <span className={styles.cartUnitPrice}>{formatValue(item.price)} c/u</span>
              </div>
              <div className={styles.qtyControl}>
                <button onClick={() => onUpdateQty(item.productId, -1)}><MinusCircle size={14} /></button>
                <span>{item.quantity}</span>
                <button onClick={() => onUpdateQty(item.productId, 1)}><PlusCircle size={14} /></button>
              </div>
              <span className={styles.cartSubtotal}>{formatValue(item.price * item.quantity)}</span>
              <button className={styles.removeBtn} onClick={() => onRemoveFromCart(item.productId)}><Trash2 size={13} /></button>
            </div>
          ))}
        </div>

        <div className={styles.cartFooter}>
          <div className={styles.cartTotals}>
            <div className={styles.totalRow}><span>Subtotal</span><span>{formatValue(subtotal)}</span></div>
            <div className={styles.totalRow}><span>IVA ({taxRate}%)</span><span>{formatValue(taxAmount)}</span></div>
            <div className={styles.totalRowMain}><span>Total</span><strong>{formatValue(total)}</strong></div>
          </div>
          <button className={styles.btnSell} onClick={onCreateSale}
            disabled={cart.length === 0 || !selectedCustomer || loading}>
            {loading ? 'Procesando...' : 'Finalizar Venta'}
          </button>
        </div>
      </div>
    </div>
  );
};
