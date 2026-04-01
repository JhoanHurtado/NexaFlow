import { useState } from 'react';
import { Plus, Package } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import type { ProductDTO } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

const PAGE_SIZE = 20;

interface Props {
  products: ProductDTO[];
  loading: boolean;
  onCreateProduct: (form: { name: string; price: string; initialStock: string; lowStockThreshold: string }) => Promise<boolean>;
}

export const StockTab = ({ products, loading, onCreateProduct }: Props) => {
  const [form, setForm] = useState({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  const [page, setPage] = useState(1);
  const paged = products.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.max(1, Math.ceil(products.length / PAGE_SIZE));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const ok = await onCreateProduct(form);
    if (ok) setForm({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  };

  return (
    <div className={styles.inventorySection}>
      <div className={styles.inventoryHeader}>
        <div><h2>Gestión de Inventario</h2><p>Control total de existencias en almacén</p></div>
      </div>
      <form onSubmit={handleSubmit} className={styles.inlineForm}>
        <input placeholder="Nombre del producto" value={form.name}
          onChange={e => setForm(p => ({ ...p, name: e.target.value }))} required />
        <input type="number" placeholder="Precio" value={form.price}
          onChange={e => setForm(p => ({ ...p, price: e.target.value }))} required min="0" step="0.01" />
        <input type="number" placeholder="Stock inicial" value={form.initialStock}
          onChange={e => setForm(p => ({ ...p, initialStock: e.target.value }))} min="0" />
        <button type="submit" disabled={loading}><Plus size={14} /> Añadir Producto</button>
      </form>
      <div className={styles.inventoryGrid}>
        {paged.map(p => (
          <div key={p.id} className={styles.inventoryCard}>
            <div className={styles.inventoryCardTop}>
              <div className={p.stock <= 5 ? styles.invIconDanger : styles.invIcon}><Package size={18} /></div>
              <span className={styles.invCategory}>General</span>
            </div>
            <h4>{p.name}</h4>
            <p className={styles.invPrice}>{formatValue(p.price)}</p>
            <div className={styles.invStock}>
              <div className={styles.invStockRow}>
                <span>Disp.</span>
                <span className={p.stock <= 5 ? styles.stockLow : ''}>{p.stock} UND</span>
              </div>
              <div className={styles.stockBar}>
                <div
                  className={p.stock <= 5 ? styles.stockBarFillDanger : styles.stockBarFill}
                  style={{ width: `${Math.min((p.stock / 100) * 100, 100)}%` }}
                />
              </div>
            </div>
          </div>
        ))}
      </div>
      <Pagination page={page} totalPages={totalPages} info={`${products.length} productos`} onChange={setPage} />
    </div>
  );
};
