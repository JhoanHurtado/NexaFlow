import { useState, useEffect, useCallback, useMemo } from 'react';
import { posApi } from '../../api/pos.api';
import type { ProductDTO, PosCustomerDTO, SaleDTO } from '../../api/pos.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './PosPage.module.scss';
import {
  ShoppingCart, Package, Users, Plus, Trash2, RefreshCw,
  ChevronRight, ChevronLeft, Search, CreditCard, FileText,
  Clock, Download, Printer, X, PlusCircle, MinusCircle,
} from 'lucide-react';

// ─── PDF / Ticket helpers ────────────────────────────────────────────────────
function buildReceiptHTML(sale: SaleDTO, customerName: string): string {
  const items = (sale.items ?? []).map(i =>
    `<tr>
      <td>${i.productName}</td>
      <td style="text-align:center">${i.quantity}</td>
      <td style="text-align:right">$${i.unitPrice.toFixed(2)}</td>
      <td style="text-align:right">$${(i.quantity * i.unitPrice).toFixed(2)}</td>
    </tr>`
  ).join('');
  return `<!DOCTYPE html><html><head><meta charset="utf-8"/>
  <title>Comprobante ${sale.id}</title>
  <style>
    body{font-family:sans-serif;font-size:13px;color:#0f172a;margin:0;padding:24px}
    h2{text-align:center;font-size:16px;margin:0 0 4px}
    p.sub{text-align:center;color:#64748b;font-size:11px;margin:0 0 16px}
    .meta{background:#f8fafc;border-radius:8px;padding:10px 14px;margin-bottom:16px;font-size:12px}
    .meta div{display:flex;justify-content:space-between;padding:3px 0}
    .meta span:first-child{color:#94a3b8;text-transform:uppercase;font-size:10px;font-weight:700}
    table{width:100%;border-collapse:collapse;margin-bottom:16px}
    thead tr{border-bottom:2px solid #e2e8f0}
    th{text-align:left;padding:6px 4px;font-size:10px;text-transform:uppercase;color:#94a3b8;font-weight:700}
    td{padding:7px 4px;border-bottom:1px solid #f1f5f9}
    .totals{border-top:2px solid #e2e8f0;padding-top:10px}
    .totals div{display:flex;justify-content:space-between;font-size:12px;padding:2px 0;color:#64748b}
    .totals .grand{font-size:18px;font-weight:900;color:#0052cc;margin-top:6px}
    .stamp{text-align:center;margin-top:20px;padding:12px;border:2px dashed #e2e8f0;border-radius:8px;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:.15em}
  </style></head><body>
  <h2>NexaFlow POS</h2>
  <p class="sub">Comprobante de Venta</p>
  <div class="meta">
    <div><span>No. Documento</span><span>${sale.id}</span></div>
    <div><span>Emisión</span><span>${new Date(sale.createdAt).toLocaleString('es-ES')}</span></div>
    <div><span>Titular</span><span>${customerName}</span></div>
    <div><span>Método</span><span>Electrónico</span></div>
  </div>
  <table>
    <thead><tr><th>Producto</th><th style="text-align:center">Cant.</th><th style="text-align:right">P.Unit</th><th style="text-align:right">Total</th></tr></thead>
    <tbody>${items}</tbody>
  </table>
  <div class="totals">
    <div><span>Subtotal</span><span>$${sale.total.toFixed(2)}</span></div>
    <div><span>IVA (0%)</span><span>$0.00</span></div>
    <div class="grand"><span>Total de Venta</span><span>$${sale.total.toFixed(2)}</span></div>
  </div>
  <div class="stamp">Firma Digital Autorizada</div>
  </body></html>`;
}

function printTicket(sale: SaleDTO, customerName: string) {
  const w = window.open('', '_blank', 'width=400,height=600');
  if (!w) return;
  w.document.write(buildReceiptHTML(sale, customerName));
  w.document.close();
  w.focus();
  w.print();
}

async function downloadPDF(sale: SaleDTO, customerName: string) {
  const html = buildReceiptHTML(sale, customerName);
  const blob = new Blob([html], { type: 'text/html' });
  const url = URL.createObjectURL(blob);
  const iframe = document.createElement('iframe');
  iframe.style.cssText = 'position:fixed;top:-9999px;left:-9999px;width:800px;height:1000px';
  document.body.appendChild(iframe);
  iframe.src = url;
  iframe.onload = () => {
    iframe.contentWindow?.focus();
    iframe.contentWindow?.print();
    setTimeout(() => { document.body.removeChild(iframe); URL.revokeObjectURL(url); }, 2000);
  };
}

type Tab = 'sale' | 'products' | 'customers' | 'history';
interface CartItem { productId: string; name: string; price: number; quantity: number; }

// ─── Pagination component ────────────────────────────────────────────────────
const Pagination = ({
  page, total, onChange, info,
}: { page: number; total: number; onChange: (p: number) => void; info: string }) => {
  if (total <= 1) return null;
  const pages = Array.from({ length: total }, (_, i) => i + 1);
  const visible = pages.filter(p => p === 1 || p === total || Math.abs(p - page) <= 1);
  return (
    <div className={styles.pagination}>
      <span className={styles.paginationInfo}>{info}</span>
      <div className={styles.paginationControls}>
        <button className={styles.pageBtn} disabled={page === 1} onClick={() => onChange(page - 1)}>
          <ChevronLeft size={13} />
        </button>
        {visible.map((p, i, arr) => (
          <>
            {i > 0 && arr[i - 1] !== p - 1 && <span key={`dots-${p}`} className={styles.paginationInfo}>…</span>}
            <button
              key={p}
              className={`${styles.pageBtn} ${p === page ? styles.pageBtnActive : ''}`}
              onClick={() => onChange(p)}
            >{p}</button>
          </>
        ))}
        <button className={styles.pageBtn} disabled={page === total} onClick={() => onChange(page + 1)}>
          <ChevronRight size={13} />
        </button>
      </div>
    </div>
  );
};

export const PosPage = () => {
  const { tenantId } = useTenant();
  const [tab, setTab] = useState<Tab>('sale');

  const [products, setProducts] = useState<ProductDTO[]>([]);
  const [customers, setCustomers] = useState<PosCustomerDTO[]>([]);
  const [sales, setSales] = useState<SaleDTO[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectedSale, setSelectedSale] = useState<SaleDTO | null>(null);

  const [cart, setCart] = useState<CartItem[]>([]);
  const [selectedCustomer, setSelectedCustomer] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [historySearch, setHistorySearch] = useState('');
  const [historyCustomer, setHistoryCustomer] = useState('');
  const [historyDateFrom, setHistoryDateFrom] = useState('');
  const [historyDateTo, setHistoryDateTo] = useState('');
  const [productForm, setProductForm] = useState({ name: '', price: '', initialStock: '0', lowStockThreshold: '5' });
  const [customerForm, setCustomerForm] = useState({ name: '', phone: '', email: '' });

  // Pagination
  const [productPage, setProductPage] = useState(1);
  const [historyPage, setHistoryPage] = useState(1);
  const [customerPage, setCustomerPage] = useState(1);
  const [stockPage, setStockPage] = useState(1);
  const PAGE_SIZE = 20;

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
  const updateQty = (productId: string, delta: number) => {
    setCart(prev => prev.map(i => {
      if (i.productId === productId) {
        const newQty = i.quantity + delta;
        return newQty > 0 ? { ...i, quantity: newQty } : i;
      }
      return i;
    }).filter(i => i.quantity > 0));
  };
  const cartTotal = cart.reduce((sum, i) => sum + i.price * i.quantity, 0);
  const clearCart = () => setCart([]);

  // Filtered sales for history
  const filteredSales = useMemo(() => {
    return sales.filter(s => {
      const matchSearch = historySearch === '' ||
        s.id.toLowerCase().includes(historySearch.toLowerCase()) ||
        (customers.find(c => c.id === s.customerId)?.name ?? '').toLowerCase().includes(historySearch.toLowerCase());
      const matchCustomer = historyCustomer === '' || s.customerId === historyCustomer;
      const sDate = s.createdAt.split('T')[0];
      const matchFrom = historyDateFrom === '' || sDate >= historyDateFrom;
      const matchTo = historyDateTo === '' || sDate <= historyDateTo;
      return matchSearch && matchCustomer && matchFrom && matchTo;
    });
  }, [sales, historySearch, historyCustomer, historyDateFrom, historyDateTo, customers]);

  // Paginated slices
  const filteredProducts = products.filter(p => p.name.toLowerCase().includes(searchQuery.toLowerCase()));
  const pagedProducts = filteredProducts.slice((productPage - 1) * PAGE_SIZE, productPage * PAGE_SIZE);
  const pagedHistory = filteredSales.slice((historyPage - 1) * PAGE_SIZE, historyPage * PAGE_SIZE);
  const pagedCustomers = customers.slice((customerPage - 1) * PAGE_SIZE, customerPage * PAGE_SIZE);
  const pagedStock = products.slice((stockPage - 1) * PAGE_SIZE, stockPage * PAGE_SIZE);
  const totalProductPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
  const totalHistoryPages = Math.max(1, Math.ceil(filteredSales.length / PAGE_SIZE));
  const totalCustomerPages = Math.max(1, Math.ceil(customers.length / PAGE_SIZE));
  const totalStockPages = Math.max(1, Math.ceil(products.length / PAGE_SIZE));

  const handleCreateSale = async () => {
    if (!cart.length) return;
    const invalidItems = cart.filter(item => !products.some(p => p.id === item.productId));
    if (invalidItems.length > 0) {
      setError(`Productos no disponibles: ${invalidItems.map(i => i.name).join(', ')}`);
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
    { id: 'sale', label: 'Nueva Venta', icon: <ShoppingCart size={14} /> },
    { id: 'history', label: 'Historial', icon: <Clock size={14} /> },
    { id: 'products', label: 'Stock', icon: <Package size={14} /> },
    { id: 'customers', label: 'Clientes', icon: <Users size={14} /> },
  ];

  return (
    <div className={styles.page}>
      {/* Header */}
      <header className={styles.header}>
        <div className={styles.headerBrand}>
          <div className={styles.brandIcon}><ShoppingCart size={26} strokeWidth={2.5} /></div>
          <div>
            <h1>NexaFlow POS</h1>
            <p>Punto de Venta Inteligente</p>
          </div>
        </div>
        <div className={styles.headerRight}>
          <div className={styles.tabs}>
            {TABS.map(t => (
              <button key={t.id}
                className={tab === t.id ? styles.tabActive : styles.tab}
                onClick={() => { setTab(t.id); setError(''); setSuccess(''); setSelectedSale(null); }}>
                {t.icon}<span>{t.label}</span>
              </button>
            ))}
          </div>
          <button className={styles.btnRefresh} onClick={load} disabled={loading}><RefreshCw size={14} /></button>
        </div>
      </header>

      {error && <p className={styles.error}>{error}</p>}
      {success && <p className={styles.successMsg}>{success}</p>}

      {/* NUEVA VENTA */}
      {tab === 'sale' && (
        <div className={styles.saleLayout}>
          {/* Catálogo */}
          <div className={styles.catalogCol}>
            <div className={styles.searchBar}>
              <Search size={16} className={styles.searchIcon} />
              <input
                type="text"
                placeholder="Buscar producto por nombre..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
              />
            </div>
            <div className={styles.productGrid}>
              {loading && <p className={styles.empty}>Cargando...</p>}
              {!loading && filteredProducts.length === 0 && <p className={styles.empty}>No hay productos disponibles.</p>}
              {pagedProducts.map(p => (
                <button key={p.id} className={styles.productCard} onClick={() => addToCart(p)}>
                  <div className={styles.productCardTop}>
                    <span className={styles.productCategory}>{(p as ProductDTO & { category?: string }).category ?? 'General'}</span>
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
            <Pagination
              page={productPage} total={totalProductPages}
              onChange={p => setProductPage(p)}
              info={`${filteredProducts.length} productos`}
            />
          </div>

          {/* Carrito */}
          <div className={styles.cartPanel}>
            <div className={styles.cartHeader}>
              <h3><ShoppingCart size={16} /> Venta Actual</h3>
              <button className={styles.clearBtn} onClick={clearCart}>Limpiar</button>
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
                    <button onClick={() => updateQty(item.productId, -1)}><MinusCircle size={14} /></button>
                    <span>{item.quantity}</span>
                    <button onClick={() => updateQty(item.productId, 1)}><PlusCircle size={14} /></button>
                  </div>
                  <span className={styles.cartSubtotal}>${(item.price * item.quantity).toFixed(2)}</span>
                  <button className={styles.removeBtn} onClick={() => removeFromCart(item.productId)}><Trash2 size={13} /></button>
                </div>
              ))}
            </div>

            <div className={styles.cartFooter}>
              <div className={styles.cartTotals}>
                <div className={styles.totalRow}><span>Subtotal</span><span>${cartTotal.toFixed(2)}</span></div>
                <div className={styles.totalRow}><span>Impuestos (0%)</span><span>$0.00</span></div>
                <div className={styles.totalRowMain}>
                  <span>Total Cobrar</span>
                  <strong>${cartTotal.toFixed(2)}</strong>
                </div>
              </div>
              <button
                className={styles.btnSell}
                onClick={handleCreateSale}
                disabled={cart.length === 0 || loading}
              >
                {loading ? 'Procesando...' : 'Finalizar Venta'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* HISTORIAL */}
      {tab === 'history' && (
        <div className={styles.historyLayout}>
          <div className={styles.historyTable}>
            <div className={styles.historyTableHeader}>
              <h3><Clock size={16} /> Historial de Facturación</h3>
              <span className={styles.salesCount}>{filteredSales.length} / {sales.length} Facturas</span>
            </div>

            {/* Filtros */}
            <div className={styles.historyFilters}>
              <div className={styles.historySearch}>
                <Search size={13} />
                <input
                  type="text"
                  placeholder="Buscar por ID o cliente..."
                  value={historySearch}
                  onChange={e => setHistorySearch(e.target.value)}
                />
              </div>
              <select
                className={styles.historyFilterSelect}
                value={historyCustomer}
                onChange={e => setHistoryCustomer(e.target.value)}
              >
                <option value="">Todos los clientes</option>
                {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              <div className={styles.historyDateRange}>
                <input type="date" value={historyDateFrom} onChange={e => setHistoryDateFrom(e.target.value)} title="Desde" />
                <span>—</span>
                <input type="date" value={historyDateTo} onChange={e => setHistoryDateTo(e.target.value)} title="Hasta" />
              </div>
              {(historySearch || historyCustomer || historyDateFrom || historyDateTo) && (
                <button className={styles.clearFiltersBtn} onClick={() => { setHistorySearch(''); setHistoryCustomer(''); setHistoryDateFrom(''); setHistoryDateTo(''); }}>
                  <X size={12} /> Limpiar
                </button>
              )}
            </div>

            <div className={styles.tableScroll}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>No. Documento</th>
                    <th>Fecha y Hora</th>
                    <th className={selectedSale ? styles.hiddenMd : ''}>Cliente</th>
                    <th>Monto Total</th>
                    <th className={styles.textRight}>Detalle</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSales.length === 0 && (
                    <tr><td colSpan={5} className={styles.empty}>
                      {sales.length === 0 ? 'No hay ventas registradas.' : 'Sin resultados para los filtros aplicados.'}
                    </td></tr>
                  )}
                  {pagedHistory.map(s => (
                    <tr key={s.id}
                      className={selectedSale?.id === s.id ? styles.rowActive : styles.rowHover}
                      onClick={() => setSelectedSale(s)}>
                      <td>
                        <div className={styles.docCell}>
                          <div className={selectedSale?.id === s.id ? styles.docIconActive : styles.docIcon}>
                            <FileText size={13} />
                          </div>
                          <span className={styles.docId}>{s.id}</span>
                        </div>
                      </td>
                      <td>
                        <div className={styles.dateCell}>
                          <span>{new Date(s.createdAt).toLocaleDateString('es-ES', { day: '2-digit', month: 'short' })}</span>
                          <span className={styles.timeText}>{new Date(s.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                        </div>
                      </td>
                      <td className={selectedSale ? styles.hiddenMd : ''}>
                        <div className={styles.customerCell}>
                          <div className={styles.customerAvatar}>
                            {(customers.find(c => c.id === s.customerId)?.name || 'C').charAt(0)}
                          </div>
                          <span>{customers.find(c => c.id === s.customerId)?.name || 'Cliente Ocasional'}</span>
                        </div>
                      </td>
                      <td><span className={styles.totalAmount}>${s.total.toFixed(2)}</span></td>
                      <td className={styles.textRight}>
                        <div className={selectedSale?.id === s.id ? styles.chevronActive : styles.chevron}>
                          <ChevronRight size={15} />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid #f1f5f9', background: '#fff' }}>
              <Pagination
                page={historyPage} total={totalHistoryPages}
                onChange={p => setHistoryPage(p)}
                info={`${filteredSales.length} / ${sales.length} facturas`}
              />
            </div>
          </div>

          {/* Slide-over detalle */}
          <div className={`${styles.slideOver} ${selectedSale ? styles.slideOverOpen : ''}`}>
            {selectedSale && (
              <>
                <div className={styles.slideOverHeader}>
                  <div className={styles.slideOverHeaderLeft}>
                    <button className={styles.backBtn} onClick={() => setSelectedSale(null)}><ChevronLeft size={18} /></button>
                    <h2>Comprobante</h2>
                  </div>
                  <button className={styles.closeBtn} onClick={() => setSelectedSale(null)}><X size={16} /></button>
                </div>

                <div className={styles.slideOverBody}>
                  <div className={styles.receiptHero}>
                    <div className={styles.receiptIcon}><FileText size={28} /></div>
                    <h3>{selectedSale.id}</h3>
                    <p>Venta Finalizada con Éxito</p>
                  </div>

                  <div className={styles.receiptMeta}>
                    <div className={styles.metaRow}>
                      <span>Emisión</span>
                      <span>{new Date(selectedSale.createdAt).toLocaleString('es-ES')}</span>
                    </div>
                    <div className={styles.metaRow}>
                      <span>Titular</span>
                      <span>{customers.find(c => c.id === selectedSale.customerId)?.name || 'Cliente General'}</span>
                    </div>
                    <div className={styles.metaRow}>
                      <span>Método</span>
                      <div className={styles.metaPayment}><CreditCard size={12} /><span>Electrónico</span></div>
                    </div>
                  </div>

                  <p className={styles.itemsLabel}>Artículos Comprados</p>
                  <div className={styles.receiptItems}>
                    {(selectedSale.items ?? []).map((item, i) => (
                      <div key={i} className={styles.receiptItem}>
                        <div>
                          <p className={styles.receiptItemName}>{item.productName || products.find(p => p.id === item.productId)?.name || item.productId}</p>
                          <p className={styles.receiptItemSub}>${item.unitPrice.toFixed(2)} × {item.quantity}</p>
                        </div>
                        <p className={styles.receiptItemTotal}>${(item.quantity * item.unitPrice).toFixed(2)}</p>
                      </div>
                    ))}
                  </div>

                  <div className={styles.receiptTotals}>
                    <div className={styles.receiptTotalRow}><span>Subtotal</span><span>${selectedSale.total.toFixed(2)}</span></div>
                    <div className={styles.receiptTotalRow}><span>IVA (0%)</span><span>$0.00</span></div>
                    <div className={styles.receiptTotalMain}>
                      <span>Total de Venta</span>
                      <strong>${selectedSale.total.toFixed(2)}</strong>
                    </div>
                  </div>

                  <div className={styles.receiptStamp}>
                    <div className={styles.barcodeLines}>
                      {[...Array(20)].map((_, i) => <div key={i} />)}
                    </div>
                    <p>Firma Digital Autorizada</p>
                  </div>
                </div>

                <div className={styles.slideOverFooter}>
                  <button className={styles.btnTicket} onClick={() => printTicket(selectedSale, customers.find(c => c.id === selectedSale.customerId)?.name || 'Cliente General')}>
                    <Printer size={14} /> Ticket
                  </button>
                  <button className={styles.btnPdf} onClick={() => downloadPDF(selectedSale, customers.find(c => c.id === selectedSale.customerId)?.name || 'Cliente General')}>
                    <Download size={14} /> PDF
                  </button>
                </div>
              </>
            )}
          </div>
          {selectedSale && <div className={styles.slideOverBackdrop} onClick={() => setSelectedSale(null)} />}
        </div>
      )}

      {/* PRODUCTOS */}
      {tab === 'products' && (
        <div className={styles.inventorySection}>
          <div className={styles.inventoryHeader}>
            <div>
              <h2>Gestión de Inventario</h2>
              <p>Control total de existencias en almacén</p>
            </div>
          </div>
          <form onSubmit={handleCreateProduct} className={styles.inlineForm}>
            <input placeholder="Nombre del producto" value={productForm.name}
              onChange={e => setProductForm(p => ({ ...p, name: e.target.value }))} required />
            <input type="number" placeholder="Precio" value={productForm.price}
              onChange={e => setProductForm(p => ({ ...p, price: e.target.value }))} required min="0" step="0.01" />
            <input type="number" placeholder="Stock inicial" value={productForm.initialStock}
              onChange={e => setProductForm(p => ({ ...p, initialStock: e.target.value }))} min="0" />
            <button type="submit" disabled={loading}><Plus size={14} /> Añadir Producto</button>
          </form>
          <div className={styles.inventoryGrid}>
            {pagedStock.map(p => (
              <div key={p.id} className={styles.inventoryCard}>
                <div className={styles.inventoryCardTop}>
                  <div className={p.stock <= 5 ? styles.invIconDanger : styles.invIcon}><Package size={18} /></div>
                  <span className={styles.invCategory}>{(p as ProductDTO & { category?: string }).category ?? 'General'}</span>
                </div>
                <h4>{p.name}</h4>
                <p className={styles.invPrice}>${p.price.toFixed(2)}</p>
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
        </div>
      )}

      {/* CLIENTES */}
      {tab === 'customers' && (
        <div className={styles.inventorySection}>
          <div className={styles.inventoryHeader}>
            <div>
              <h2>Clientes</h2>
              <p>Gestión de clientes registrados</p>
            </div>
          </div>
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
            <table className={styles.customersTable}>
              <thead>
                <tr>
                  <th>Cliente</th>
                  <th>Teléfono</th>
                  <th>Email</th>
                </tr>
              </thead>
              <tbody>
                {customers.length === 0 && (
                  <tr><td colSpan={3} className={styles.empty}>No hay clientes registrados.</td></tr>
                )}
                {customers.map(c => (
                  <tr key={c.id}>
                    <td>
                      <div className={styles.custName}>
                        <div className={styles.custAvatar}>{c.name.charAt(0).toUpperCase()}</div>
                        <span className={styles.custNameText}>{c.name}</span>
                      </div>
                    </td>
                    <td><span className={styles.custMuted}>{c.phone ?? '—'}</span></td>
                    <td><span className={styles.custMuted}>{c.email ?? '—'}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
