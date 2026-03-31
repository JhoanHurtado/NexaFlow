import { useState } from 'react';
import { Clock, FileText, Search, X, ChevronLeft, ChevronRight, CreditCard, Download, Printer } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import { printTicket, downloadPDF } from '../receipt';
import type { SaleDTO, PosCustomerDTO, PaginatedResult } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

interface Props {
  salesPage: PaginatedResult<SaleDTO>;
  customers: PosCustomerDTO[];
  loading: boolean;
  customerName: (id?: string) => string;
  onPageChange: (page: number) => void;
}

const STATUS_LABEL: Record<string, string> = { pending: 'Pendiente', completed: 'Completada', cancelled: 'Cancelada' };
const STATUS_COLOR: Record<string, string> = { pending: '#f59e0b', completed: '#16a34a', cancelled: '#ef4444' };

export const HistoryTab = ({ salesPage, customers, loading, customerName, onPageChange }: Props) => {
  const [search, setSearch]           = useState('');
  const [filterCustomer, setFilterCustomer] = useState('');
  const [selectedSale, setSelectedSale] = useState<SaleDTO | null>(null);

  const sales = salesPage.items;

  // Filtro local sobre la página actual
  const filtered = sales.filter(s => {
    const matchSearch = !search ||
      s.id.toLowerCase().includes(search.toLowerCase()) ||
      customerName(s.customerId).toLowerCase().includes(search.toLowerCase());
    return matchSearch && (!filterCustomer || s.customerId === filterCustomer);
  });

  const hasFilters = search || filterCustomer;

  return (
    <div className={styles.historyLayout}>
      <div className={styles.historyTable}>
        <div className={styles.historyTableHeader}>
          <h3><Clock size={16} /> Historial de Facturación</h3>
          <span className={styles.salesCount}>
            {salesPage.totalCount} facturas · pág {salesPage.currentPage}/{salesPage.totalPages}
          </span>
        </div>

        <div className={styles.historyFilters}>
          <div className={styles.historySearch}>
            <Search size={13} />
            <input type="text" placeholder="Buscar por ID o cliente..." value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <select className={styles.historyFilterSelect} value={filterCustomer} onChange={e => setFilterCustomer(e.target.value)}>
            <option value="">Todos los clientes</option>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          {hasFilters && (
            <button className={styles.clearFiltersBtn} onClick={() => { setSearch(''); setFilterCustomer(''); }}>
              <X size={12} /> Limpiar
            </button>
          )}
        </div>

        <div className={styles.tableScroll}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>No. Documento</th><th>Fecha y Hora</th>
                <th className={selectedSale ? styles.hiddenMd : ''}>Cliente</th>
                <th>Estado</th><th>Total</th><th className={styles.textRight}>Detalle</th>
              </tr>
            </thead>
            <tbody>
              {loading && <tr><td colSpan={6} className={styles.empty}>Cargando...</td></tr>}
              {!loading && filtered.length === 0 && (
                <tr><td colSpan={6} className={styles.empty}>
                  {sales.length === 0 ? 'No hay ventas registradas.' : 'Sin resultados para los filtros aplicados.'}
                </td></tr>
              )}
              {filtered.map(s => (
                <tr key={s.id} className={selectedSale?.id === s.id ? styles.rowActive : styles.rowHover} onClick={() => setSelectedSale(s)}>
                  <td>
                    <div className={styles.docCell}>
                      <div className={selectedSale?.id === s.id ? styles.docIconActive : styles.docIcon}><FileText size={13} /></div>
                      <span className={styles.docId}>{s.id.slice(0, 8)}…</span>
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
                      <div className={styles.customerAvatar}>{customerName(s.customerId).charAt(0)}</div>
                      <span>{customerName(s.customerId)}</span>
                    </div>
                  </td>
                  <td>
                    <span style={{ fontSize: '0.7rem', fontWeight: 800, color: STATUS_COLOR[s.status] ?? '#64748b',
                      background: `${STATUS_COLOR[s.status] ?? '#64748b'}18`, padding: '0.2rem 0.5rem', borderRadius: '999px' }}>
                      {STATUS_LABEL[s.status] ?? s.status}
                    </span>
                  </td>
                  <td><span className={styles.totalAmount}>{formatValue(s.total)}</span></td>
                  <td className={styles.textRight}>
                    <div className={selectedSale?.id === s.id ? styles.chevronActive : styles.chevron}><ChevronRight size={15} /></div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Paginación del backend */}
        <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid #f1f5f9', background: '#fff' }}>
          <Pagination
            page={salesPage.currentPage}
            totalPages={salesPage.totalPages}
            info={`${salesPage.totalCount} facturas`}
            onChange={p => { onPageChange(p); setSelectedSale(null); }}
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
                <p>Venta {STATUS_LABEL[selectedSale.status] ?? selectedSale.status}</p>
              </div>
              <div className={styles.receiptMeta}>
                <div className={styles.metaRow}><span>Emisión</span><span>{new Date(selectedSale.createdAt).toLocaleString('es-ES')}</span></div>
                <div className={styles.metaRow}><span>Titular</span><span>{customerName(selectedSale.customerId)}</span></div>
                <div className={styles.metaRow}><span>Método</span><div className={styles.metaPayment}><CreditCard size={12} /><span>Electrónico</span></div></div>
              </div>
              <p className={styles.itemsLabel}>Artículos</p>
              <div className={styles.receiptItems}>
                {(selectedSale.items ?? []).map((item, i) => (
                  <div key={i} className={styles.receiptItem}>
                    <div>
                      <p className={styles.receiptItemName}>{item.productName}</p>
                      <p className={styles.receiptItemSub}>{formatValue(item.unitPrice)} × {item.quantity}</p>
                    </div>
                    <p className={styles.receiptItemTotal}>{formatValue(item.quantity * item.unitPrice)}</p>
                  </div>
                ))}
              </div>
              <div className={styles.receiptTotals}>
                <div className={styles.receiptTotalRow}><span>Subtotal</span><span>{formatValue(selectedSale.subtotal ?? selectedSale.total)}</span></div>
                <div className={styles.receiptTotalRow}><span>IVA ({selectedSale.taxRate ?? 0}%)</span><span>{formatValue(selectedSale.taxAmount ?? 0)}</span></div>
                <div className={styles.receiptTotalMain}><span>Total</span><strong>{formatValue(selectedSale.total)}</strong></div>
              </div>
              <div className={styles.receiptStamp}>
                <div className={styles.barcodeLines}>{[...Array(20)].map((_, i) => <div key={i} />)}</div>
                <p>Firma Digital Autorizada</p>
              </div>
            </div>
            <div className={styles.slideOverFooter}>
              <button className={styles.btnTicket} onClick={() => printTicket(selectedSale, customerName(selectedSale.customerId))}>
                <Printer size={14} /> Ticket
              </button>
              <button className={styles.btnPdf} onClick={() => downloadPDF(selectedSale, customerName(selectedSale.customerId))}>
                <Download size={14} /> PDF
              </button>
            </div>
          </>
        )}
      </div>
      {selectedSale && <div className={styles.slideOverBackdrop} onClick={() => setSelectedSale(null)} />}
    </div>
  );
};
