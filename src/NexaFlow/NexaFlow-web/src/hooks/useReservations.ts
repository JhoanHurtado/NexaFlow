import { useState, useCallback } from 'react';
import { bookAdminApi } from '../api/book-admin.api';
import type { ReservationDTO, AgendaDTO, PaginatedResult, BookCustomerDTO } from '../api/book-admin.api';

const today = () => new Date().toISOString().split('T')[0];

const EMPTY_PAGE = <T,>(): PaginatedResult<T> => ({
  items: [], currentPage: 1, pageSize: 20,
  totalCount: 0, totalPages: 1, hasNext: false, hasPrev: false,
});

export const useReservations = (tenantId: string) => {
  const [agenda, setAgenda]                   = useState<AgendaDTO | null>(null);
  const [reservationsPage, setReservationsPage] = useState<PaginatedResult<ReservationDTO>>(EMPTY_PAGE());
  const [customersPage, setCustomersPage]       = useState<PaginatedResult<BookCustomerDTO>>(EMPTY_PAGE());
  const [agendaDate, setAgendaDate]             = useState(today());
  const [statusFilter, setStatusFilter]         = useState('all');
  const [loading, setLoading]                   = useState(false);
  const [actionLoading, setActionLoading]       = useState<string | null>(null);
  const [error, setError]                       = useState('');

  const loadAgenda = useCallback(async (date = agendaDate) => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      setAgenda(await bookAdminApi.getAgenda(tenantId, date));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar agenda');
    } finally { setLoading(false); }
  }, [tenantId, agendaDate]);

  const loadList = useCallback(async (filter = statusFilter, page = 1, pageSize = 50) => {
    if (!tenantId) return;
    setLoading(true); setError('');
    try {
      const result = await bookAdminApi.listReservations(tenantId, page, pageSize, filter === 'all' ? undefined : filter);
      setReservationsPage(result);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar reservas');
    } finally { setLoading(false); }
  }, [tenantId, statusFilter]);

  const loadCustomers = useCallback(async (page = 1, pageSize = 100) => {
    if (!tenantId) return;
    try {
      const result = await bookAdminApi.listCustomers(tenantId, page, pageSize);
      setCustomersPage(result);
    } catch { /* silent */ }
  }, [tenantId]);

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
    agenda,
    reservations: reservationsPage.items,
    reservationsPage,
    customersPage,
    agendaDate, setAgendaDate,
    statusFilter, setStatusFilter,
    loading, actionLoading, error,
    loadAgenda, loadList, loadCustomers, doAction,
  };
};
