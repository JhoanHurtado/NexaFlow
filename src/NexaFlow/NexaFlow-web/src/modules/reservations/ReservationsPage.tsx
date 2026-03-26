import { useState, useEffect, useCallback } from 'react';
import { bookAdminApi } from '../../api/book-admin.api';
import type { ReservationDTO, AgendaDTO } from '../../api/book-admin.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './ReservationsPage.module.scss';
import { Calendar, CheckCircle, Clock, UserCheck, XCircle, RefreshCw, ChevronDown } from 'lucide-react';

const today = () => new Date().toISOString().split('T')[0];

const STATUS_LABELS: Record<string, string> = {
  pending: 'Pendiente', confirmed: 'Confirmada', arrived: 'En local',
  completed: 'Completada', cancelled: 'Cancelada',
};

const STATUS_FILTERS = ['all', 'pending', 'confirmed', 'arrived', 'completed', 'cancelled'];

export const ReservationsPage = () => {
  const { tenantId } = useTenant();
  const [tab, setTab] = useState<'agenda' | 'list'>('agenda');

  // Agenda
  const [agendaDate, setAgendaDate] = useState(today());
  const [agenda, setAgenda] = useState<AgendaDTO | null>(null);

  // List
  const [reservations, setReservations] = useState<ReservationDTO[]>([]);
  const [statusFilter, setStatusFilter] = useState('all');

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const loadAgenda = useCallback(async () => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      const res = await bookAdminApi.getAgenda(tenantId, agendaDate);
      setAgenda(res.data ?? res as unknown as AgendaDTO);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar agenda');
    } finally { setLoading(false); }
  }, [tenantId, agendaDate]);

  const loadList = useCallback(async () => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      const res = await bookAdminApi.listReservations(tenantId, 1, 50, statusFilter === 'all' ? undefined : statusFilter);
      setReservations(res.data ?? []);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar reservas');
    } finally { setLoading(false); }
  }, [tenantId, statusFilter]);

  useEffect(() => { if (tab === 'agenda') loadAgenda(); else loadList(); }, [tab, loadAgenda, loadList]);

  const doAction = async (action: () => Promise<unknown>, id: string) => {
    setActionLoading(id);
    try {
      await action();
      if (tab === 'agenda') await loadAgenda(); else await loadList();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error en la acción');
    } finally { setActionLoading(null); }
  };

  const renderActions = (r: ReservationDTO) => (
    <div className={styles.actions}>
      {r.status === 'pending' && (
        <button className={styles.btnConfirm} disabled={!!actionLoading}
          onClick={() => doAction(() => bookAdminApi.confirmReservation(tenantId, r.id), r.id)}>
          <CheckCircle size={13} /> Confirmar
        </button>
      )}
      {r.status === 'confirmed' && (
        <button className={styles.btnArrived} disabled={!!actionLoading}
          onClick={() => doAction(() => bookAdminApi.markArrived(tenantId, r.id), r.id)}>
          <UserCheck size={13} /> Llegó
        </button>
      )}
      {r.status === 'arrived' && (
        <button className={styles.btnComplete} disabled={!!actionLoading}
          onClick={() => doAction(() => bookAdminApi.completeReservation(tenantId, r.id), r.id)}>
          <CheckCircle size={13} /> Completar
        </button>
      )}
      {(r.status === 'pending' || r.status === 'confirmed') && (
        <button className={styles.btnCancel} disabled={!!actionLoading}
          onClick={() => doAction(() => bookAdminApi.cancelReservation(tenantId, r.id, 'admin'), r.id)}>
          <XCircle size={13} /> Cancelar
        </button>
      )}
    </div>
  );

  const renderRow = (r: ReservationDTO) => (
    <div key={r.id} className={`${styles.row} ${styles[r.status]}`}>
      <div className={styles.rowMain}>
        <span className={styles.customerName}>{r.customerName}</span>
        <span className={styles.rowTime}><Clock size={12} />{r.timeSlot}</span>
        <span className={`${styles.badge} ${styles[r.status]}`}>{STATUS_LABELS[r.status]}</span>
        {r.notes && <span className={styles.notes}>{r.notes}</span>}
      </div>
      {renderActions(r)}
    </div>
  );

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1><Calendar size={20} /> Reservas</h1>
        <div className={styles.tabs}>
          <button className={tab === 'agenda' ? styles.tabActive : styles.tab} onClick={() => setTab('agenda')}>Agenda del día</button>
          <button className={tab === 'list' ? styles.tabActive : styles.tab} onClick={() => setTab('list')}>Todas las reservas</button>
        </div>
      </div>

      {error && <p className={styles.error}>{error}</p>}

      {/* AGENDA */}
      {tab === 'agenda' && (
        <div className={styles.section}>
          <div className={styles.toolbar}>
            <input type="date" value={agendaDate} onChange={e => setAgendaDate(e.target.value)} className={styles.dateInput} />
            <button className={styles.btnRefresh} onClick={loadAgenda} disabled={loading}><RefreshCw size={14} /></button>
          </div>

          {agenda && (
            <div className={styles.statsRow}>
              {[
                { label: 'Total', val: agenda.totalReservations, cls: '' },
                { label: 'Pendientes', val: agenda.pending, cls: 'pending' },
                { label: 'Confirmadas', val: agenda.confirmed, cls: 'confirmed' },
                { label: 'En local', val: agenda.arrived, cls: 'arrived' },
                { label: 'Completadas', val: agenda.completed, cls: 'completed' },
                { label: 'Canceladas', val: agenda.cancelled, cls: 'cancelled' },
              ].map(s => (
                <div key={s.label} className={`${styles.statCard} ${s.cls ? styles[s.cls] : ''}`}>
                  <strong>{s.val}</strong><span>{s.label}</span>
                </div>
              ))}
            </div>
          )}

          {loading ? <p className={styles.loading}>Cargando...</p> : (
            <div className={styles.list}>
              {!agenda?.reservations?.length
                ? <p className={styles.empty}>No hay reservas para este día.</p>
                : agenda.reservations.map(renderRow)}
            </div>
          )}
        </div>
      )}

      {/* LIST */}
      {tab === 'list' && (
        <div className={styles.section}>
          <div className={styles.toolbar}>
            <div className={styles.filterSelect}>
              <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
                {STATUS_FILTERS.map(s => (
                  <option key={s} value={s}>{s === 'all' ? 'Todos los estados' : STATUS_LABELS[s]}</option>
                ))}
              </select>
              <ChevronDown size={14} />
            </div>
            <button className={styles.btnRefresh} onClick={loadList} disabled={loading}><RefreshCw size={14} /></button>
          </div>

          {loading ? <p className={styles.loading}>Cargando...</p> : (
            <div className={styles.list}>
              {!reservations.length
                ? <p className={styles.empty}>No hay reservas con este filtro.</p>
                : reservations.map(r => (
                  <div key={r.id} className={`${styles.row} ${styles[r.status]}`}>
                    <div className={styles.rowMain}>
                      <span className={styles.customerName}>{r.customerName}</span>
                      <span className={styles.rowDate}><Calendar size={12} />{r.reservationDate}</span>
                      <span className={styles.rowTime}><Clock size={12} />{r.timeSlot}</span>
                      <span className={`${styles.badge} ${styles[r.status]}`}>{STATUS_LABELS[r.status]}</span>
                      {r.notes && <span className={styles.notes}>{r.notes}</span>}
                    </div>
                    {renderActions(r)}
                  </div>
                ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
