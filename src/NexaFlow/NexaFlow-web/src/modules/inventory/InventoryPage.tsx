import { useState, useEffect, useMemo } from 'react';
import styles from './InventoryPage.module.scss';
import { Plus, Package, AlertTriangle, RefreshCcw, Search, Pencil } from 'lucide-react';
import { posApi, type ProductDTO } from '../../api/pos.api';
import { useTenant } from '../../hooks/useTenant';
import { formatValue } from '../../utils/formatters';
import { Pagination } from '../../components/Pagination';

const PAGE_SIZE = 20;

export const InventoryPage = () => {
  const { tenantId } = useTenant();
  const [products,   setProducts]   = useState<ProductDTO[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [showModal,  setShowModal]  = useState(false);
  const [search,     setSearch]     = useState('');
  const [page,       setPage]       = useState(1);
  const [pageSize,   setPageSize]   = useState(PAGE_SIZE);
  const [form,       setForm]       = useState({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  const [editing,    setEditing]    = useState<ProductDTO | null>(null);
  const [editForm,   setEditForm]   = useState({ name: '', price: '', stock: '', lowStockThreshold: '', active: true });

  const fetchProducts = async () => {
    if (!tenantId) return;
    setLoading(true);
    try {
      const items = await posApi.listProducts(tenantId, 1, 100);
      setProducts(items);
    } catch (error) {
      console.error('Error al cargar inventario:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchProducts(); }, [tenantId]);

  const filtered = useMemo(() =>
    search.trim()
      ? products.filter(p => p.name.toLowerCase().includes(search.toLowerCase()))
      : products,
    [products, search]
  );

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const paged      = filtered.slice((page - 1) * pageSize, page * pageSize);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await posApi.createProduct(tenantId, {
        name: form.name,
        price: parseFloat(form.price) || 0,
        initialStock: parseInt(form.initialStock) || 0,
        lowStockThreshold: parseInt(form.lowStockThreshold) || 5,
      });
      setShowModal(false);
      setForm({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
      fetchProducts();
    } catch {
      alert('Error al crear el producto. Verifique los datos.');
    }
  };

  const openEdit = (p: ProductDTO) => {
    setEditing(p);
    setEditForm({ name: p.name, price: String(p.price), stock: String(p.stock), lowStockThreshold: String(p.lowStockThreshold), active: p.active });
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editing) return;
    try {
      await posApi.updateProduct(tenantId, editing.id, {
        name: editForm.name,
        price: parseFloat(editForm.price),
        stock: parseInt(editForm.stock),
        lowStockThreshold: parseInt(editForm.lowStockThreshold),
        active: editForm.active,
      });
      setEditing(null);
      await fetchProducts();
    } catch {
      alert('Error al actualizar el producto.');
    }
  };

  const lowStockCount = products.filter(p => p.stock <= p.lowStockThreshold).length;

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div className={styles.titleSection}>
          <h1>Gestión de Inventario</h1>
          <p>Monitorea y actualiza el stock de tus productos.</p>
        </div>
        <button className={styles.addBtn} onClick={() => setShowModal(true)}>
          <Plus size={18} /> Nuevo Producto
        </button>
      </header>

      <div className={styles.statsRow}>
        <div className={styles.statCard}>
          <span className={styles.statLabel}>Total Productos</span>
          <strong className={styles.statValue}>{products.length}</strong>
        </div>
        <div className={styles.statCard}>
          <span className={styles.statLabel}>Stock Bajo</span>
          <strong className={`${styles.statValue} ${lowStockCount > 0 ? styles.warning : ''}`}>
            {lowStockCount}
          </strong>
        </div>
        <div className={styles.statCard}>
          <span className={styles.statLabel}>Activos</span>
          <strong className={styles.statValue}>{products.filter(p => p.active).length}</strong>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.searchBar}>
          <Search size={18} />
          <input
            type="text"
            placeholder="Buscar producto..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <button className={styles.refreshBtn} onClick={fetchProducts} title="Refrescar">
          <RefreshCcw size={18} className={loading ? styles.spin : ''} />
        </button>
      </div>

      {loading ? (
        <div className={styles.loadingState}>
          <RefreshCcw className={styles.spin} />
          <span>Cargando productos...</span>
        </div>
      ) : (
        <div className={styles.tableWrapper}>
          <div className={styles.tableScroll}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>Estado</th>
                  <th>Precio</th>
                  <th>Stock</th>
                  <th>Mínimo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {paged.length > 0 ? (
                  paged.map(p => (
                    <tr key={p.id}>
                      <td>
                        <div className={styles.productCell}>
                          <Package size={16} />
                          <span>{p.name}</span>
                        </div>
                      </td>
                      <td>
                        <span className={`${styles.badge} ${p.active ? styles.active : styles.inactive}`}>
                          {p.active ? 'Activo' : 'Inactivo'}
                        </span>
                      </td>
                      <td>{formatValue(p.price, 'currency')}</td>
                      <td>
                        <div className={`${styles.stockValue} ${p.stock <= p.lowStockThreshold ? styles.danger : ''}`}>
                          {p.stock}
                          {p.stock <= p.lowStockThreshold && <AlertTriangle size={14} />}
                        </div>
                      </td>
                      <td>{p.lowStockThreshold}</td>
                      <td>
                        <button onClick={() => openEdit(p)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#94a3b8' }}>
                          <Pencil size={14} />
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} className={styles.empty}>
                      {search ? `No se encontraron productos con "${search}".` : 'No hay productos registrados.'}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <div className={styles.tablePagination}>
            <Pagination
              page={page}
              totalPages={totalPages}
              pageSize={pageSize}
              onPageSizeChange={s => { setPageSize(s); setPage(1); }}
              info={`${filtered.length} productos`}
              onChange={p => setPage(p)}
            />
          </div>
        </div>
      )}

      {showModal && (
        <div className={styles.modalOverlay}>
          <div className={styles.modal}>
            <h2>Registrar Producto</h2>
            <form onSubmit={handleCreate} className={styles.form}>
              <div className={styles.field}>
                <label>Nombre del Producto</label>
                <input required placeholder="Ej: Café Americano"
                  value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
              </div>
              <div className={styles.formRow}>
                <div className={styles.field}>
                  <label>Precio Unitario</label>
                  <input type="number" step="0.01" min="0" required
                    placeholder="Ej: 15000"
                    value={form.price}
                    onChange={e => setForm({ ...form, price: e.target.value })} />
                </div>
                <div className={styles.field}>
                  <label>Stock Inicial</label>
                  <input type="number" min="0" required
                    value={form.initialStock}
                    onChange={e => setForm({ ...form, initialStock: e.target.value })} />
                </div>
              </div>
              <div className={styles.field}>
                <label>Alerta Stock Bajo (Mínimo)</label>
                <input type="number" min="0" required
                  value={form.lowStockThreshold}
                  onChange={e => setForm({ ...form, lowStockThreshold: e.target.value })} />
              </div>
              <div className={styles.modalActions}>
                <button type="button" className={styles.btnSecondary} onClick={() => setShowModal(false)}>Cancelar</button>
                <button type="submit" className={styles.btnPrimary}>Crear Producto</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {editing && (
        <div className={styles.modalOverlay}>
          <div className={styles.modal}>
            <h2>Editar Producto</h2>
            <form onSubmit={handleUpdate} className={styles.form}>
              <div className={styles.field}>
                <label>Nombre del Producto</label>
                <input required value={editForm.name} onChange={e => setEditForm({ ...editForm, name: e.target.value })} />
              </div>
              <div className={styles.formRow}>
                <div className={styles.field}>
                  <label>Precio Unitario</label>
                  <input type="number" step="0.01" min="0" required value={editForm.price}
                    onChange={e => setEditForm({ ...editForm, price: e.target.value })} />
                </div>
                <div className={styles.field}>
                  <label>Stock</label>
                  <input type="number" min="0" required value={editForm.stock}
                    onChange={e => setEditForm({ ...editForm, stock: e.target.value })} />
                </div>
              </div>
              <div className={styles.field}>
                <label>Alerta Stock Bajo (Mínimo)</label>
                <input type="number" min="1" required value={editForm.lowStockThreshold}
                  onChange={e => setEditForm({ ...editForm, lowStockThreshold: e.target.value })} />
              </div>
              <div className={styles.field}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                  <input type="checkbox" checked={editForm.active}
                    onChange={e => setEditForm({ ...editForm, active: e.target.checked })} />
                  Producto activo (visible en punto de venta)
                </label>
              </div>
              <div className={styles.modalActions}>
                <button type="button" className={styles.btnSecondary} onClick={() => setEditing(null)}>Cancelar</button>
                <button type="submit" className={styles.btnPrimary}>Guardar Cambios</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
