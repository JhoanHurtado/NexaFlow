import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import type { PosCustomerDTO } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

const PAGE_SIZE_OPTIONS = [10, 20, 50];

interface Props {
  customers: PosCustomerDTO[];
  loading: boolean;
  onCreateCustomer: (form: { name: string; phone: string; email: string }) => Promise<boolean>;
}

export const CustomersTab = ({ customers, loading, onCreateCustomer }: Props) => {
  const [form, setForm]       = useState({ name: '', phone: '', email: '' });
  const [page, setPage]       = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const totalPages = Math.max(1, Math.ceil(customers.length / pageSize));
  const paged      = customers.slice((page - 1) * pageSize, page * pageSize);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const ok = await onCreateCustomer(form);
    if (ok) setForm({ name: '', phone: '', email: '' });
  };

  return (
    <div className={styles.inventorySection}>
      <div className={styles.inventoryHeader}>
        <div><h2>Clientes</h2><p>Gestión de clientes registrados</p></div>
      </div>
      <form onSubmit={handleSubmit} className={styles.inlineForm}>
        <input placeholder="Nombre" value={form.name}
          onChange={e => setForm(p => ({ ...p, name: e.target.value }))} required />
        <input placeholder="Teléfono" value={form.phone}
          onChange={e => setForm(p => ({ ...p, phone: e.target.value }))} />
        <input type="email" placeholder="Email" value={form.email}
          onChange={e => setForm(p => ({ ...p, email: e.target.value }))} />
        <button type="submit" disabled={loading}><Plus size={14} /> Agregar</button>
      </form>

      {/* Tabla con scroll fijo */}
      <div className={styles.tableWrap}>
        <div className={styles.tableScroll}>
          <table className={styles.customersTable}>
            <thead><tr><th>Cliente</th><th>Teléfono</th><th>Email</th></tr></thead>
            <tbody>
              {paged.length === 0 && (
                <tr><td colSpan={3} className={styles.empty}>No hay clientes registrados.</td></tr>
              )}
              {paged.map(c => (
                <tr key={c.id}>
                  <td>
                    <div className={styles.custName}>
                      <div className={styles.custAvatar}>{c.name.charAt(0).toUpperCase()}</div>
                      <span className={styles.custNameText}>{c.name}</span>
                    </div>
                  </td>
                  <td><span className={styles.custMuted}>{formatValue(c.phone, 'phone') ?? '—'}</span></td>
                  <td><span className={styles.custMuted}>{c.email ?? '—'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Paginación siempre visible */}
        <div className={styles.historyPagination}>
          <Pagination
            page={page}
            totalPages={totalPages}
            pageSize={pageSize}
            onPageSizeChange={s => { setPageSize(s); setPage(1); }}
            info={`${customers.length} clientes`}
            onChange={p => setPage(p)}
          />
        </div>
      </div>
    </div>
  );
};
