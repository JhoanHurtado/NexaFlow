import { useState, useMemo } from 'react';
import { Clock, FileText, Search, X, ChevronLeft, ChevronRight, CreditCard, Download, Printer } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import { printTicket, downloadPDF } from '../receipt';
import type { SaleDTO, PosCustomerDTO } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';

const PAGE_SIZE = 20;

interface Props {
  sales: SaleDTO[];
  customers: PosCustomerDTO[];
  customerName: (id?: string) => string;
}

export const HistoryTab = ({ sales, customers, customerName }: Props) => {
  const [search, setSearch]           = useState('');
  const [filterCustomer, setFilterCustomer] = useState('');
  const [dateFrom, setDateFrom]       = useState('');
  const [dateTo, setDateTo]           = useState('');
  const [page, setPage]               = useState(1);
  const [selectedSale, setSelectedSale] = useState<SaleDTO | null>(null);

  const filtered = useMemo(() => sales.filter(s => {
    const matchSearch = !search ||
      s.id.toLowerCase().includes(search.toLowerCase()) ||
      customerName(s.customerId).toLowerCase().includes(search.toLowerCase());
    const matchCustomer = !filterCustomer || s.customerId === filterCustomer;
    const sDate = s.createdAt.split('T')[0];
    return matchSearch && matchCustomer && (!dateFrom || sDate >= dateFrom) && (!dateTo || sDate <= dateTo);
  }), [sales, search, filterCustomer, dateFrom, dateTo, customerName]);

  const paged = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const hasFilters = search || filterCustomer || dateFrom || dateTo;

  return (
    <div className={styles.historyLayout}>
      <div className={styles.historyTable}>
        <div className={styles.historyTableHeader}>
          <h3><Clock size={16} /> Historial de Facturación</h3>
          <span className={styles.salesCount}>{filtered.length} / {sales.length} Facturas</span>
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
          <div className={styles.historyDateRange}>
            <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} title="Desde" />
            <span>—</span>
            <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} title="Hasta" />
          </div>
          {hasFilters && (
            <button className={styles.clearFiltersBtn} onClick={() => { setSearch(''); setFilterCustomer(''); setDateFrom(''); setDateTo(''); }}>
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
                <th>Monto Total</th><th className={styles.textRight}>Detalle</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 && (
                <tr><td colSpan={5} className={styles.empty}>
                  {sales.length === 0 ? 'No hay ventas registradas.' : 'Sin resultados para los filtros aplicados.'}
                </td></tr>
              )}
              {paged.map(s => (
                <tr key={s.id}
                  className={selectedSale?.id === s.id ? styles.rowActive : styles.rowHover}
                  onClick={() => setSelectedSale(s)}>
                  <td>
                    <div className={styles.docCell}>
                      <div className={selectedSale?.id === s.id ? styles.docIconActive : styles.docIcon}><FileText size={13} /></div>
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
                      <div className={styles.customerAvatar}>{customerName(s.customerId).charAt(0)}</div>
                      <span>{customerName(s.customerId)}</span>
                    </div>
                  </td>
                  <td><span className={styles.totalAmount}>${s.total.toFixed(2)}</span></td>
                  <td className={styles.textRight}>
                    <div className={selectedSale?.id === s.id ? styles.chevronActive : styles.chevron}><ChevronRight size={15} /></div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div style={{ padding: '0.75rem 1.5rem', borderTop: '1px solid #f1f5f9', background: '#fff' }}>
          <Pagination page={page} totalPages={totalPages} info={`${filtered.length} / ${sales.length} facturas`} onChange={setPage} />
        </div>
      </div>

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
                <div className={styles.metaRow}><span>Emisión</span><span>{new Date(selectedSale.createdAt).toLocaleString('es-ES')}</span></div>
                <div className={styles.metaRow}><span>Titular</span><span>{customerName(selectedSale.customerId)}</span></div>
                <div className={styles.metaRow}><span>Método</span><div className={styles.metaPayment}><CreditCard size={12} /><span>Electrónico</span></div></div>
              </div>
              <p className={styles.itemsLabel}>Artículos Comprados</p>
              <div className={styles.receiptItems}>
                {(selectedSale.items ?? []).map((item, i) => (
                  <div key={i} className={styles.receiptItem}>
                    <div>
                      <p className={styles.receiptItemName}>{item.productName}</p>
                      <p className={styles.receiptItemSub}>${item.unitPrice.toFixed(2)} × {item.quantity}</p>
                    </div>
                    <p className={styles.receiptItemTotal}>${(item.quantity * item.unitPrice).toFixed(2)}</p>
                  </div>
                ))}
              </div>
              <div className={styles.receiptTotals}>
                <div className={styles.receiptTotalRow}><span>Subtotal</span><span>${selectedSale.total.toFixed(2)}</span></div>
                <div className={styles.receiptTotalRow}><span>IVA (0%)</span><span>$0.00</span></div>
                <div className={styles.receiptTotalMain}><span>Total de Venta</span><strong>${selectedSale.total.toFixed(2)}</strong></div>
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
