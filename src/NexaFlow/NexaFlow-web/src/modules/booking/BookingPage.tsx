import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { bookApi } from '../../api/book.api';
import type { AvailabilitySlot, ReservationItem } from '../../api/book.api';
import styles from './BookingPage.module.scss';
import { Calendar, Clock, CheckCircle, XCircle, User } from 'lucide-react';

type Step = 'register' | 'availability' | 'confirm' | 'my-reservations';

export const BookingPage = () => {
  const { tenantId } = useParams<{ tenantId: string }>();
  const tid = tenantId ?? '';

  const [step, setStep] = useState<Step>('register');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Customer
  const [customerForm, setCustomerForm] = useState({ name: '', phone: '', email: '' });
  const [customerId, setCustomerId] = useState('');

  // Availability
  const [selectedDate, setSelectedDate] = useState('');
  const [slots, setSlots] = useState<AvailabilitySlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState('');

  // Reservation
  const [notes, setNotes] = useState('');
  const [reservationId, setReservationId] = useState('');
  const [reservations, setReservations] = useState<ReservationItem[]>([]);

  const handleCustomerChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setCustomerForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setError('');
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const res = await bookApi.registerCustomer(tid, customerForm);
      setCustomerId(res.id);
      setStep('availability');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al registrar');
    } finally {
      setLoading(false);
    }
  };

  const handleLoadSlots = async () => {
    if (!selectedDate) return;
    setLoading(true);
    setError('');
    try {
      const res = await bookApi.getAvailability(tid, selectedDate);
      setSlots(res.data ?? []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cargar disponibilidad');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateReservation = async () => {
    if (!selectedSlot || !selectedDate) return;
    setLoading(true);
    setError('');
    try {
      const res = await bookApi.createReservation(tid, {
        customerId,
        reservationDate: selectedDate,
        timeSlot: selectedSlot,
        notes: notes || undefined,
      });
      setReservationId(res.id);
      setStep('confirm');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al crear reserva');
    } finally {
      setLoading(false);
    }
  };

  const handleLoadReservations = async () => {
    if (!customerId) return;
    setLoading(true);
    setError('');
    try {
      const res = await bookApi.getCustomerReservations(tid, customerId);
      setReservations(res.data ?? []);
      setStep('my-reservations');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cargar reservas');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = async (id: string) => {
    setLoading(true);
    setError('');
    try {
      await bookApi.cancelReservation(tid, id, customerForm.name || 'Cliente');
      setReservations(prev => prev.map(r => r.id === id ? { ...r, status: 'cancelled' } : r));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al cancelar');
    } finally {
      setLoading(false);
    }
  };

  const statusLabel: Record<string, string> = {
    pending: 'Pendiente',
    confirmed: 'Confirmada',
    cancelled: 'Cancelada',
    completed: 'Completada',
    arrived: 'En local',
  };

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.header}>
          <div className={styles.logo}>
            <span className={styles.logoIcon}>N</span>
            <span>NexaFlow Booking</span>
          </div>
          <p>Reserva tu cita de forma rápida y sencilla</p>
        </div>

        {/* STEP: REGISTER */}
        {step === 'register' && (
          <form onSubmit={handleRegister} className={styles.form}>
            <h2><User size={18} /> Tus datos</h2>
            <div className={styles.field}>
              <label>Nombre completo *</label>
              <input name="name" value={customerForm.name} onChange={handleCustomerChange} placeholder="Tu nombre" required />
            </div>
            <div className={styles.formRow}>
              <div className={styles.field}>
                <label>Teléfono</label>
                <input name="phone" value={customerForm.phone} onChange={handleCustomerChange} placeholder="+1 555 0000" />
              </div>
              <div className={styles.field}>
                <label>Correo electrónico</label>
                <input type="email" name="email" value={customerForm.email} onChange={handleCustomerChange} placeholder="tu@correo.com" />
              </div>
            </div>
            {error && <p className={styles.errorMsg}>{error}</p>}
            <button type="submit" className={styles.btnPrimary} disabled={loading}>
              {loading ? 'Registrando...' : 'Continuar'}
            </button>
          </form>
        )}

        {/* STEP: AVAILABILITY */}
        {step === 'availability' && (
          <div className={styles.form}>
            <h2><Calendar size={18} /> Elige fecha y hora</h2>
            <div className={styles.dateRow}>
              <div className={styles.field}>
                <label>Fecha</label>
                <input
                  type="date"
                  value={selectedDate}
                  min={new Date().toISOString().split('T')[0]}
                  onChange={e => { setSelectedDate(e.target.value); setSlots([]); setSelectedSlot(''); }}
                />
              </div>
              <button className={styles.btnOutline} onClick={handleLoadSlots} disabled={!selectedDate || loading}>
                {loading ? 'Cargando...' : 'Ver disponibilidad'}
              </button>
            </div>

            {slots.length > 0 && (
              <div className={styles.slotsGrid}>
                {slots.map(s => (
                  <button
                    key={s.timeSlot}
                    className={`${styles.slot} ${!s.available ? styles.slotUnavailable : ''} ${selectedSlot === s.timeSlot ? styles.slotSelected : ''}`}
                    disabled={!s.available}
                    onClick={() => setSelectedSlot(s.timeSlot)}
                  >
                    <Clock size={13} />{s.timeSlot}
                  </button>
                ))}
              </div>
            )}

            {selectedSlot && (
              <>
                <div className={styles.field}>
                  <label>Notas (opcional)</label>
                  <input value={notes} onChange={e => setNotes(e.target.value)} placeholder="Ej: Alergia a la penicilina" />
                </div>
                {error && <p className={styles.errorMsg}>{error}</p>}
                <button className={styles.btnPrimary} onClick={handleCreateReservation} disabled={loading}>
                  {loading ? 'Reservando...' : `Reservar ${selectedDate} a las ${selectedSlot}`}
                </button>
              </>
            )}

            <button className={styles.btnGhost} onClick={handleLoadReservations} disabled={loading}>
              Ver mis reservas anteriores
            </button>
          </div>
        )}

        {/* STEP: CONFIRM */}
        {step === 'confirm' && (
          <div className={styles.confirmBox}>
            <CheckCircle size={48} className={styles.confirmIcon} />
            <h2>¡Reserva creada!</h2>
            <p>Tu reserva para el <strong>{selectedDate}</strong> a las <strong>{selectedSlot}</strong> fue registrada exitosamente.</p>
            <p className={styles.reservationId}>ID: {reservationId}</p>
            <div className={styles.confirmActions}>
              <button className={styles.btnPrimary} onClick={handleLoadReservations}>Ver mis reservas</button>
              <button className={styles.btnOutline} onClick={() => { setStep('availability'); setSelectedSlot(''); setReservationId(''); }}>
                Nueva reserva
              </button>
            </div>
          </div>
        )}

        {/* STEP: MY RESERVATIONS */}
        {step === 'my-reservations' && (
          <div className={styles.form}>
            <h2>Mis reservas</h2>
            {reservations.length === 0 ? (
              <p className={styles.emptyMsg}>No tienes reservas registradas.</p>
            ) : (
              <div className={styles.reservationList}>
                {reservations.map(r => (
                  <div key={r.id} className={`${styles.reservationItem} ${styles[`status_${r.status}`]}`}>
                    <div className={styles.reservationInfo}>
                      <span className={styles.reservationDate}><Calendar size={13} />{r.reservationDate}</span>
                      <span className={styles.reservationTime}><Clock size={13} />{r.timeSlot}</span>
                      <span className={`${styles.statusBadge} ${styles[r.status]}`}>{statusLabel[r.status] ?? r.status}</span>
                    </div>
                    {(r.status === 'pending' || r.status === 'confirmed') && (
                      <button
                        className={styles.btnCancel}
                        onClick={() => handleCancel(r.id)}
                        disabled={loading}
                      >
                        <XCircle size={14} /> Cancelar
                      </button>
                    )}
                  </div>
                ))}
              </div>
            )}
            {error && <p className={styles.errorMsg}>{error}</p>}
            <button className={styles.btnGhost} onClick={() => setStep('availability')}>
              ← Volver
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
