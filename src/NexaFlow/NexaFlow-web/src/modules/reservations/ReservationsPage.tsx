import { useState, useEffect } from 'react';
import { Calendar, RefreshCw, ChevronDown } from 'lucide-react';
import { bookAdminApi } from '../../api/book-admin.api';
import { useReservations } from '../../hooks/useReservations';
import { useTenant } from '../../hooks/useTenant';
import { EmptyState } from '../../components/EmptyState';
import { Pagination } from '../../components/Pagination';
import { AgendaStats } from './AgendaStats';
import { ReservationRow } from './ReservationRow';
import styles from './ReservationsPage.module.scss';

type Tab = 'agenda' | 'list';

const STATUS_FILTERS = ['all', 'pending', 'confirmed', 'arrived', 'completed', 'cancelled'];
const STATUS_LABELS: Record<string, string> = {
  all: 'Todos los estados', pending: 'Pendiente', confirmed: 'Confirmada',
  arrived: 'En local', completed: 'Completada', cancelled: 'Cancelada',
};

export const ReservationsPage = () => {
  const { tenantId } = useTenant();
  const [tab, setTab] = useState<Tab>('agenda');
  const {
    agenda, reservations, reservationsPage,
    agendaDate, setAgendaDate,
    statusFilter, setStatusFilter,
    loading, actionLoading, error,
    loadAgenda, loadList, doAction,
  } = useReservations(tenantId);

  useEffect(() => {
    if (tab === 'agenda') loadAgenda();
    else loadList();
  }, [tab]); // eslint-disable-line react-hooks/exhaustive-deps

  const reload = () => tab === 'agenda' ? loadAgenda() : loadList();

  const rowActions = () => ({
    onConfirm:  (i: string) => doAction(() => bookAdminApi.confirmReservation(tenantId, i), i, reload),
    onArrived:  (i: string) => doAction(() => bookAdminApi.markArrived(tenantId, i), i, reload),
    onComplete: (i: string) => doAction(() => bookAdminApi.completeReservation(tenantId, i), i, reload),
    onCancel:   (i: string) => doAction(() => bookAdminApi.cancelReservation(tenantId, i, 'admin'), i, reload),
  });

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1><Calendar size={20} /> Reservas</h1>
        <div className={styles.tabs}>
          <button className={tab === 'agenda' ? styles.tabActive : styles.tab} onClick={() => setTab('agenda')}>
            Agenda del día
          </button>
          <button className={tab === 'list' ? styles.tabActive : styles.tab} onClick={() => setTab('list')}>
            Todas las reservas
          </button>
        </div>
      </div>

      {error && <p className={styles.error}>{error}</p>}

      {tab === 'agenda' && (
        <div className={styles.section}>
          <div className={styles.toolbar}>
            <input
              type="date" value={agendaDate}
              onChange={e => { setAgendaDate(e.target.value); loadAgenda(e.target.value); }}
              className={styles.dateInput}
            />
            <button className={styles.btnRefresh} onClick={() => loadAgenda()} disabled={loading}>
              <RefreshCw size={14} />
            </button>
          </div>
          {agenda && <AgendaStats agenda={agenda} />}
          {loading ? (
            <p className={styles.loading}>Cargando...</p>
          ) : !agenda?.reservations?.length ? (
            <EmptyState message="No hay reservas para este día." />
          ) : (
            <div className={styles.list}>
              {agenda.reservations.map(r => (
                <ReservationRow key={r.id} reservation={r} actionLoading={actionLoading} {...rowActions()} />
              ))}
            </div>
          )}
        </div>
      )}

      {tab === 'list' && (
        <div className={styles.section}>
          <div className={styles.toolbar}>
            <div className={styles.filterSelect}>
              <select value={statusFilter} onChange={e => { setStatusFilter(e.target.value); loadList(e.target.value); }}>
                {STATUS_FILTERS.map(s => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
              </select>
              <ChevronDown size={14} />
            </div>
            <button className={styles.btnRefresh} onClick={() => loadList()} disabled={loading}>
              <RefreshCw size={14} />
            </button>
          </div>
          {loading ? (
            <p className={styles.loading}>Cargando...</p>
          ) : !reservations.length ? (
            <EmptyState message="No hay reservas con este filtro." />
          ) : (
            <div className={styles.list}>
              {reservations.map(r => (
                <ReservationRow key={r.id} reservation={r} showDate actionLoading={actionLoading} {...rowActions()} />
              ))}
            </div>
          )}
          <Pagination
            page={reservationsPage.currentPage}
            totalPages={reservationsPage.totalPages}
            hasNext={reservationsPage.hasNext}
            hasPrev={reservationsPage.hasPrev}
            pageSize={reservationsPage.pageSize}
            onPageSizeChange={size => loadList(statusFilter, 1, size)}
            info={`${reservationsPage.totalCount} reservas`}
            onChange={p => loadList(statusFilter, p)}
          />
        </div>
      )}
    </div>
  );
};
