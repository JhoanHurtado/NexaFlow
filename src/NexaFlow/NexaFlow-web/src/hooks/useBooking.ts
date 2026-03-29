import { useState } from 'react';
import { bookApi } from '../api/book.api';
import type { AvailabilitySlot, ReservationItem } from '../api/book.api';

export type BookingStep = 'register' | 'availability' | 'confirm' | 'my-reservations';

export const useBooking = (tenantId: string) => {
  const [step, setStep]           = useState<BookingStep>('register');
  const [loading, setLoading]     = useState(false);
  const [error, setError]         = useState('');

  const [customerId, setCustomerId]   = useState('');
  const [customerName, setCustomerName] = useState('');

  const [selectedDate, setSelectedDate] = useState('');
  const [slots, setSlots]               = useState<AvailabilitySlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState('');
  const [notes, setNotes]               = useState('');

  const [reservationId, setReservationId] = useState('');
  const [reservations, setReservations]   = useState<ReservationItem[]>([]);

  const clearError = () => setError('');

  const register = async (form: { name: string; phone: string; email: string }) => {
    setLoading(true); clearError();
    try {
      const res = await bookApi.registerCustomer(tenantId, form);
      setCustomerId(res.id);
      setCustomerName(form.name);
      setStep('availability');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al registrar');
    } finally { setLoading(false); }
  };

  const loadSlots = async (date: string) => {
    if (!date) return;
    setLoading(true); clearError();
    try {
      setSlots(await bookApi.getAvailability(tenantId, date));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar disponibilidad');
    } finally { setLoading(false); }
  };

  const createReservation = async () => {
    if (!selectedSlot || !selectedDate) return;
    setLoading(true); clearError();
    try {
      const res = await bookApi.createReservation(tenantId, {
        customerId, reservationDate: selectedDate, timeSlot: selectedSlot,
        notes: notes || undefined,
      });
      setReservationId(res.id);
      setStep('confirm');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear reserva');
    } finally { setLoading(false); }
  };

  const loadReservations = async () => {
    if (!customerId) { setError('Primero debes registrarte para ver tus reservas.'); return; }
    setLoading(true); clearError();
    try {
      setReservations(await bookApi.getCustomerReservations(tenantId, customerId));
      setStep('my-reservations');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cargar reservas');
    } finally { setLoading(false); }
  };

  const cancelReservation = async (id: string) => {
    setLoading(true); clearError();
    try {
      await bookApi.cancelReservation(tenantId, id, customerName || 'Cliente');
      setReservations(prev => prev.map(r => r.id === id ? { ...r, status: 'cancelled' } : r));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al cancelar');
    } finally { setLoading(false); }
  };

  const resetAvailability = () => { setSelectedSlot(''); setReservationId(''); setStep('availability'); };

  return {
    step, loading, error, clearError,
    customerId,
    selectedDate, setSelectedDate,
    slots, setSlots, setSelectedSlot,
    selectedSlot, notes, setNotes,
    reservationId, reservations,
    register, loadSlots, createReservation, loadReservations, cancelReservation,
    resetAvailability,
  };
};
