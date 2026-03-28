import { useState, useEffect } from 'react';
import styles from './InventoryPage.module.scss';
import { Plus, Package, AlertTriangle, RefreshCcw, Search } from 'lucide-react';
import { posApi, type ProductDTO } from '../../api/pos.api';

export const InventoryPage = () => {
  const [products, setProducts] = useState<ProductDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [form, setForm] = useState({ name: '', price: 0, initialStock: 0, lowStockThreshold: 5 });

  const tenantId = localStorage.getItem('tenantId') || '';

  const fetchProducts = async () => {
    setLoading(true);
    try {
      const res = await posApi.getProducts(tenantId);
      // Soporte para 'data' o 'Data' (PascalCase del backend)
      const items = res.data || res.Data || [];
      setProducts(items);
    } catch (error) {
      console.error('Error al cargar inventario:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await posApi.createProduct(tenantId, form);
      setShowModal(false);
      setForm({ name: '', price: 0, initialStock: 0, lowStockThreshold: 5 });
      fetchProducts();
    } catch (error) {
      alert('Error al crear el producto. Verifique los datos.');
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div className={styles.titleSection}>
          <h1>Gestión de Inventario</h1>
          <p>Monitorea y actualiza el stock de tus productos de óptica y odontología.</p>
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
          <strong className={`${styles.statValue} ${styles.warning}`}>
            {products.filter(p => {
              const stock = p.stock ?? p.Stock ?? 0;
              const threshold = p.lowStockThreshold ?? p.LowStockThreshold ?? 0;
              return stock <= threshold;
            }).length}
          </strong>
        </div>
      </div>

      <div className={styles.filters}>
        <div className={styles.searchBar}>
          <Search size={18} />
          <input type="text" placeholder="Buscar producto..." disabled />
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
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Estado</th>
                <th>Precio</th>
                <th>Stock</th>
                <th>Mínimo</th>
              </tr>
            </thead>
            <tbody>
              {products.length > 0 ? (
                products.map(p => {
                  const stock = p.stock ?? p.Stock ?? 0;
                  const threshold = p.lowStockThreshold ?? p.LowStockThreshold ?? 0;
                  const price = p.price ?? p.Price ?? 0;
                  const status = (p.status ?? p.Status ?? '').toLowerCase();
                  const name = p.name ?? p.Name ?? 'Sin nombre';

                  return (
                    <tr key={p.id ?? p.Id}>
                      <td>
                        <div className={styles.productCell}>
                          <Package size={16} />
                          <span>{name}</span>
                        </div>
                      </td>
                      <td>
                        <span className={`${styles.badge} ${styles[status]}`}>
                          {status === 'active' ? 'Activo' : 'Inactivo'}
                        </span>
                      </td>
                      <td>${price.toLocaleString()}</td>
                      <td>
                        <div className={`${styles.stockValue} ${stock <= threshold ? styles.danger : ''}`}>
                          {stock}
                          {stock <= threshold && <AlertTriangle size={14} />}
                        </div>
                      </td>
                      <td>{threshold}</td>
                    </tr>
                  );
                })
              ) : (
                <tr>
                  <td colSpan={5} className={styles.empty}>No hay productos registrados.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {showModal && (
        <div className={styles.modalOverlay}>
          <div className={styles.modal}>
            <h2>Registrar Producto</h2>
            <form onSubmit={handleCreate} className={styles.form}>
              <div className={styles.field}>
                <label>Nombre del Producto</label>
                <input 
                  required 
                  placeholder="Ej: Montura Titanium X" 
                  value={form.name} 
                  onChange={e => setForm({...form, name: e.target.value})} 
                />
              </div>
              
              <div className={styles.formRow}>
                <div className={styles.field}>
                  <label>Precio Unitario</label>
                  <input 
                    type="number" 
                    step="0.01" 
                    required 
                    value={form.price} 
                    onChange={e => setForm({...form, price: parseFloat(e.target.value)})} 
                  />
                </div>
                <div className={styles.field}>
                  <label>Stock Inicial</label>
                  <input 
                    type="number" 
                    required 
                    value={form.initialStock} 
                    onChange={e => setForm({...form, initialStock: parseInt(e.target.value)})} 
                  />
                </div>
              </div>

              <div className={styles.field}>
                <label>Alerta Stock Bajo (Mínimo)</label>
                <input 
                  type="number" 
                  required 
                  value={form.lowStockThreshold} 
                  onChange={e => setForm({...form, lowStockThreshold: parseInt(e.target.value)})} 
                />
              </div>

              <div className={styles.modalActions}>
                <button type="button" className={styles.btnSecondary} onClick={() => setShowModal(false)}>
                  Cancelar
                </button>
                <button type="submit" className={styles.btnPrimary}>
                  Crear Producto
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};