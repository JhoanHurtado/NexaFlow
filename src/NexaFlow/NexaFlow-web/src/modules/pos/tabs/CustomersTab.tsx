import { useState } from 'react';
import { Plus, Pencil } from 'lucide-react';
import { Pagination } from '../../../components/Pagination';
import type { PosCustomerDTO } from '../../../api/pos.api';
import styles from '../PosPage.module.scss';
import { formatValue } from '../../../utils/formatters';

interface Props {
  customers: PosCustomerDTO[];
  loading: boolean;
  onCreateCustomer: (form: { name: string; phone: string; email: string }) => Promise<boolean>;
  onUpdateCustomer: (id: string, body: { name: string; phone?: string; email?: string }) => Promise<boolean>;
}

export const CustomersTab = ({ customers, loading, onCreateCustomer, onUpdateCustomer }: Props) => {
  const [form, setForm]       = useState({ name: '', phone: '', email: '' });
  const [page, setPage]       = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [editing, setEditing] = useState<PosCustomerDTO | null>(null);
  const [editForm, setEditForm] = useState({ name: '', phone: '', email: '' });

  const totalPages = Math.max(1, Math.ceil(customers.length / pageSize));
  const paged      = customers.slice((page - 1) * pageSize, page * pageSize);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const ok = await onCreateCustomer(form);
    if (ok) setForm({ name: '', phone: '', email: '' });
  };

  const openEdit = (c: PosCustomerDTO) => {
    setEditing(c);
    setEditForm({ name: c.name, phone: c.phone ?? '', email: c.email ?? '' });
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editing) return;
    const ok = await onUpdateCustomer(editing.id, { name: editForm.name, phone: editForm.phone || undefined, email: editForm.email || undefined });
    if (ok) setEditing(null);
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

      <div className={styles.tableWrap}>
        <div className={styles.tableScroll}>
          <table className={styles.customersTable}>
            <thead><tr><th>Cliente</th><th>Teléfono</th><th>Email</th><th></th></tr></thead>
            <tbody>
              {paged.length === 0 && (
                <tr><td colSpan={4} className={styles.empty}>No hay clientes registrados.</td></tr>
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
                  <td>
                    <button onClick={() => openEdit(c)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#94a3b8' }}>
                      <Pencil size={14} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className={styles.historyPagination}>
          <Pagination page={page} totalPages={totalPages} pageSize={pageSize}
            onPageSizeChange={s => { setPageSize(s); setPage(1); }}
            info={`${customers.length} clientes`} onChange={p => setPage(p)} />
        </div>
      </div>

      {editing && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div style={{ background: '#1e293b', borderRadius: '12px', padding: '2rem', maxWidth: '420px', width: '90%' }}>
            <h3 style={{ color: '#f1f5f9', marginBottom: '1.2rem' }}>Editar Cliente</h3>
            <form onSubmit={handleUpdate} style={{ display: 'flex', flexDirection: 'column', gap: '0.8rem' }}>
              <label style={{ color: '#94a3b8', fontSize: '0.85rem' }}>Nombre
                <input value={editForm.name} onChange={e => setEditForm(f => ({ ...f, name: e.target.value }))} required
                  style={{ display: 'block', width: '100%', marginTop: '0.3rem', padding: '0.5rem', borderRadius: '6px', border: '1px solid #334155', background: '#0f172a', color: '#f1f5f9' }} />
              </label>
              <label style={{ color: '#94a3b8', fontSize: '0.85rem' }}>Teléfono
                <input value={editForm.phone} onChange={e => setEditForm(f => ({ ...f, phone: e.target.value }))}
                  style={{ display: 'block', width: '100%', marginTop: '0.3rem', padding: '0.5rem', borderRadius: '6px', border: '1px solid #334155', background: '#0f172a', color: '#f1f5f9' }} />
              </label>
              <label style={{ color: '#94a3b8', fontSize: '0.85rem' }}>Email
                <input type="email" value={editForm.email} onChange={e => setEditForm(f => ({ ...f, email: e.target.value }))}
                  style={{ display: 'block', width: '100%', marginTop: '0.3rem', padding: '0.5rem', borderRadius: '6px', border: '1px solid #334155', background: '#0f172a', color: '#f1f5f9' }} />
              </label>
              <div style={{ display: 'flex', gap: '0.8rem', justifyContent: 'flex-end', marginTop: '0.5rem' }}>
                <button type="button" onClick={() => setEditing(null)}
                  style={{ padding: '0.5rem 1rem', borderRadius: '6px', border: '1px solid #475569', background: 'transparent', color: '#94a3b8', cursor: 'pointer' }}>
                  Cancelar
                </button>
                <button type="submit"
                  style={{ padding: '0.5rem 1rem', borderRadius: '6px', border: 'none', background: '#3b82f6', color: '#fff', cursor: 'pointer', fontWeight: 600 }}>
                  Guardar
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
