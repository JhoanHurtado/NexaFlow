import { useState } from 'react';
import { Plus, Package, Pencil } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import type { ProductDTO } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

const PAGE_SIZE = 20;

interface Props {
  products: ProductDTO[];
  loading: boolean;
  onCreateProduct: (form: { name: string; price: string; initialStock: string; lowStockThreshold: string }) => Promise<boolean>;
  onUpdateProduct: (id: string, body: { name?: string; price?: number; stock?: number; lowStockThreshold?: number; active?: boolean }) => Promise<boolean>;
}

// ── Modal aislado en su propio componente ─────────────────────────────────────
// Cada campo tiene su propio useState independiente del padre.
// Esto evita que re-renders del padre (por loadProducts) reseteen los valores
// que el usuario está editando.
interface EditModalProps {
  product: ProductDTO;
  onSave: (id: string, body: { name?: string; price?: number; stock?: number; lowStockThreshold?: number; active?: boolean }) => Promise<boolean>;
  onClose: () => void;
}

const EditModal = ({ product, onSave, onClose }: EditModalProps) => {
  const [name,              setName]              = useState(product.name);
  const [price,             setPrice]             = useState(String(product.price));
  const [stock,             setStock]             = useState(String(product.stock));
  const [lowStockThreshold, setLowStockThreshold] = useState(String(product.lowStockThreshold));
  const [active,            setActive]            = useState(product.active);
  const [saving,            setSaving]            = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    const ok = await onSave(product.id, {
      name,
      price:             parseFloat(price)           || 0,
      stock:             parseInt(stock, 10)         || 0,
      lowStockThreshold: parseInt(lowStockThreshold, 10) || 1,
      active,
    });
    setSaving(false);
    if (ok) onClose();
  };

  const field: React.CSSProperties = {
    display: 'block', width: '100%', marginTop: '0.3rem', padding: '0.5rem',
    borderRadius: '6px', border: '1px solid #334155', background: '#0f172a',
    color: '#f1f5f9', boxSizing: 'border-box',
  };
  const lbl: React.CSSProperties = { color: '#94a3b8', fontSize: '0.85rem' };

  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.55)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
      <div style={{ background: '#1e293b', borderRadius: '12px', padding: '2rem', maxWidth: '420px', width: '90%' }}>
        <h3 style={{ color: '#f1f5f9', marginBottom: '1.2rem' }}>Editar Producto</h3>
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '0.8rem' }}>

          <label style={lbl}>Nombre
            <input value={name} onChange={e => setName(e.target.value)} required style={field} />
          </label>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.8rem' }}>
            <label style={lbl}>Precio
              <input type="number" step="0.01" min="0" value={price}
                onChange={e => setPrice(e.target.value)} required style={field} />
            </label>
            <label style={lbl}>Stock actual
              <input type="number" min="0" value={stock}
                onChange={e => setStock(e.target.value)} required style={field} />
            </label>
          </div>

          <label style={lbl}>Alerta stock bajo (mínimo)
            <input type="number" min="1" value={lowStockThreshold}
              onChange={e => setLowStockThreshold(e.target.value)} required style={field} />
          </label>

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', ...lbl, cursor: 'pointer' }}>
            <input type="checkbox" checked={active} onChange={e => setActive(e.target.checked)} />
            Producto activo (visible en punto de venta)
          </label>

          <div style={{ display: 'flex', gap: '0.8rem', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
            <button type="button" onClick={onClose}
              style={{ padding: '0.5rem 1rem', borderRadius: '6px', border: '1px solid #475569', background: 'transparent', color: '#94a3b8', cursor: 'pointer' }}>
              Cancelar
            </button>
            <button type="submit" disabled={saving}
              style={{ padding: '0.5rem 1rem', borderRadius: '6px', border: 'none', background: '#3b82f6', color: '#fff', cursor: 'pointer', fontWeight: 600, opacity: saving ? 0.7 : 1 }}>
              {saving ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

// ── StockTab ──────────────────────────────────────────────────────────────────
export const StockTab = ({ products, loading, onCreateProduct, onUpdateProduct }: Props) => {
  const [form,    setForm]    = useState({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  const [page,    setPage]    = useState(1);
  const [editing, setEditing] = useState<ProductDTO | null>(null);

  const paged      = products.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
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

      {/* Formulario de creación */}
      <form onSubmit={handleSubmit} className={styles.inlineForm}>
        <input placeholder="Nombre del producto" value={form.name}
          onChange={e => setForm(p => ({ ...p, name: e.target.value }))} required />
        <input type="number" placeholder="Precio" value={form.price}
          onChange={e => setForm(p => ({ ...p, price: e.target.value }))} required min="0" step="0.01" />
        <input type="number" placeholder="Stock inicial" value={form.initialStock}
          onChange={e => setForm(p => ({ ...p, initialStock: e.target.value }))} min="0" />
        <button type="submit" disabled={loading}><Plus size={14} /> Añadir Producto</button>
      </form>

      {/* Grid de productos */}
      <div className={styles.inventoryGridWrap}>
        <div className={styles.inventoryGrid}>
          {paged.map(p => (
            <div key={p.id} className={styles.inventoryCard} style={{ opacity: p.active ? 1 : 0.5 }}>
              <div className={styles.inventoryCardTop}>
                <div className={p.stock <= p.lowStockThreshold ? styles.invIconDanger : styles.invIcon}>
                  <Package size={18} />
                </div>
                <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'center' }}>
                  <span className={styles.invCategory}>{p.active ? 'Activo' : 'Inactivo'}</span>
                  <button
                    onClick={() => setEditing(p)}
                    title="Editar producto"
                    style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#94a3b8', padding: '2px', display: 'flex' }}
                  >
                    <Pencil size={13} />
                  </button>
                </div>
              </div>
              <h4>{p.name}</h4>
              <p className={styles.invPrice}>{formatValue(p.price)}</p>
              <div className={styles.invStock}>
                <div className={styles.invStockRow}>
                  <span>Disp.</span>
                  <span className={p.stock <= p.lowStockThreshold ? styles.stockLow : ''}>
                    {p.stock} UND
                  </span>
                </div>
                <div className={styles.stockBar}>
                  <div
                    className={p.stock <= p.lowStockThreshold ? styles.stockBarFillDanger : styles.stockBarFill}
                    style={{ width: `${Math.min((p.stock / 100) * 100, 100)}%` }}
                  />
                </div>
                <div style={{ fontSize: '0.65rem', color: '#94a3b8', marginTop: '0.2rem' }}>
                  Mín: {p.lowStockThreshold} UND
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <Pagination page={page} totalPages={totalPages} info={`${products.length} productos`} onChange={setPage} />

      {/* Modal de edición — componente aislado con estado propio */}
      {editing && (
        <EditModal
          key={editing.id}
          product={editing}
          onSave={onUpdateProduct}
          onClose={() => setEditing(null)}
        />
      )}
    </div>
  );
};
