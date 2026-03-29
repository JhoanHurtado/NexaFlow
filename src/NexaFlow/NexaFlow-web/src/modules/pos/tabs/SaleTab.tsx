import { ShoppingCart, Search, PlusCircle, MinusCircle, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { Pagination } from '../../../components/Pagination';
import type { ProductDTO, PosCustomerDTO } from '../../../api/pos.api';
import type { CartItem } from '../../../hooks/usePosData';
import styles from '../PosPage.module.scss';

const PAGE_SIZE = 20;

interface Props {
  products: ProductDTO[];
  customers: PosCustomerDTO[];
  cart: CartItem[];
  loading: boolean;
  onAddToCart: (p: ProductDTO) => void;
  onRemoveFromCart: (id: string) => void;
  onUpdateQty: (id: string, delta: number) => void;
  onClearCart: () => void;
  onCreateSale: (customerId?: string) => void;
}

export const SaleTab = ({
  products, customers, cart, loading,
  onAddToCart, onRemoveFromCart, onUpdateQty, onClearCart, onCreateSale,
}: Props) => {
  const [search, setSearch] = useState('');
  const [selectedCustomer, setSelectedCustomer] = useState('');
  const [page, setPage] = useState(1);

  const filtered = products.filter(p => p.name.toLowerCase().includes(search.toLowerCase()));
  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const cartTotal = cart.reduce((sum, i) => sum + i.price * i.quantity, 0);

  return (
    <div className={styles.saleLayout}>
      <div className={styles.catalogCol}>
        <div className={styles.searchBar}>
          <Search size={16} className={styles.searchIcon} />
          <input
            type="text" placeholder="Buscar producto por nombre..."
            value={search} onChange={e => { setSearch(e.target.value); setPage(1); }}
          />
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
                <span className={styles.productPrice}>${p.price.toFixed(2)}</span>
                <span className={p.stock <= 5 ? styles.stockLow : styles.productStock}>Quedan: {p.stock}</span>
              </div>
              <div className={styles.addIconOverlay}><PlusCircle size={20} /></div>
            </button>
          ))}
        </div>
        <Pagination page={page} totalPages={totalPages} info={`${filtered.length} productos`} onChange={setPage} />
      </div>

      <div className={styles.cartPanel}>
        <div className={styles.cartHeader}>
          <h3><ShoppingCart size={16} /> Venta Actual</h3>
          <button className={styles.clearBtn} onClick={onClearCart}>Limpiar</button>
        </div>
        <div className={styles.customerSelect}>
          <label>Cliente (opcional)</label>
          <select value={selectedCustomer} onChange={e => setSelectedCustomer(e.target.value)}>
            <option value="">Sin cliente</option>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className={styles.cartItems}>
          {cart.length === 0 ? (
            <div className={styles.emptyCart}>
              <ShoppingCart size={44} strokeWidth={1} />
              <p>Tu carrito está vacío.<br />Selecciona productos para comenzar.</p>
            </div>
          ) : cart.map(item => (
            <div key={item.productId} className={styles.cartItem}>
              <div className={styles.cartItemInfo}>
                <span className={styles.cartName}>{item.name}</span>
                <span className={styles.cartUnitPrice}>${item.price.toFixed(2)} c/u</span>
              </div>
              <div className={styles.qtyControl}>
                <button onClick={() => onUpdateQty(item.productId, -1)}><MinusCircle size={14} /></button>
                <span>{item.quantity}</span>
                <button onClick={() => onUpdateQty(item.productId, 1)}><PlusCircle size={14} /></button>
              </div>
              <span className={styles.cartSubtotal}>${(item.price * item.quantity).toFixed(2)}</span>
              <button className={styles.removeBtn} onClick={() => onRemoveFromCart(item.productId)}><Trash2 size={13} /></button>
            </div>
          ))}
        </div>
        <div className={styles.cartFooter}>
          <div className={styles.cartTotals}>
            <div className={styles.totalRow}><span>Subtotal</span><span>${cartTotal.toFixed(2)}</span></div>
            <div className={styles.totalRow}><span>Impuestos (0%)</span><span>$0.00</span></div>
            <div className={styles.totalRowMain}><span>Total Cobrar</span><strong>${cartTotal.toFixed(2)}</strong></div>
          </div>
          <button className={styles.btnSell} onClick={() => onCreateSale(selectedCustomer || undefined)} disabled={cart.length === 0 || loading}>
            {loading ? 'Procesando...' : 'Finalizar Venta'}
          </button>
        </div>
      </div>
    </div>
  );
};
