import { useState, useCallback } from 'react';
import { bookAdminApi } from '../api/book-admin.api';
import type { ReservationDTO, AgendaDTO } from '../api/book-admin.api';

const today = () => new Date().toISOString().split('T')[0];

export const useReservations = (tenantId: string) => {
  const [agenda, setAgenda]           = useState<AgendaDTO | null>(null);
  const [reservations, setReservations] = useState<ReservationDTO[]>([]);
  const [agendaDate, setAgendaDate]   = useState(today());
  const [statusFilter, setStatusFilter] = useState('all');
  const [loading, setLoading]         = useState(false);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [error, setError]             = useState('');

  const loadAgenda = useCallback(async (date = agendaDate) => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      setAgenda(await bookAdminApi.getAgenda(tenantId, date));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar agenda');
    } finally { setLoading(false); }
  }, [tenantId, agendaDate]);

  const loadList = useCallback(async (filter = statusFilter) => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      setReservations(await bookAdminApi.listReservations(tenantId, 1, 50, filter === 'all' ? undefined : filter));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar reservas');
    } finally { setLoading(false); }
  }, [tenantId, statusFilter]);

  const doAction = useCallback(async (action: () => Promise<unknown>, id: string, onDone: () => void) => {
    setActionLoading(id); setError('');
    try {
      await action();
      onDone();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error en la acción');
    } finally { setActionLoading(null); }
  }, []);

  return {
    agenda, reservations,
    agendaDate, setAgendaDate,
    statusFilter, setStatusFilter,
    loading, actionLoading, error,
    loadAgenda, loadList, doAction,
  };
};
