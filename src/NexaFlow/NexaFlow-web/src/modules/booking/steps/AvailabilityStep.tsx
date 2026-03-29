import { Clock, Calendar } from 'lucide-react';
import type { AvailabilitySlot } from '../../../api/book.api';
import styles from '../BookingPage.module.scss';

interface Props {
  loading: boolean;
  error: string;
  selectedDate: string;
  slots: AvailabilitySlot[];
  selectedSlot: string;
  notes: string;
  onDateChange: (date: string) => void;
  onLoadSlots: (date: string) => void;
  onSelectSlot: (slot: string) => void;
  onNotesChange: (notes: string) => void;
  onReserve: () => void;
  onViewReservations: () => void;
}

export const AvailabilityStep = ({
  loading, error, selectedDate, slots, selectedSlot, notes,
  onDateChange, onLoadSlots, onSelectSlot, onNotesChange, onReserve, onViewReservations,
}: Props) => (
  <div className={styles.form}>
    <h2><Calendar size={18} /> Elige fecha y hora</h2>
    <div className={styles.dateRow}>
      <div className={styles.field}>
        <label>Fecha</label>
        <input
          type="date" value={selectedDate}
          min={new Date().toISOString().split('T')[0]}
          onChange={e => { onDateChange(e.target.value); }}
        />
      </div>
      <button
        className={styles.btnOutline}
        onClick={() => onLoadSlots(selectedDate)}
        disabled={!selectedDate || loading}
      >
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
            onClick={() => onSelectSlot(s.timeSlot)}
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
          <input value={notes} onChange={e => onNotesChange(e.target.value)} placeholder="Ej: Alergia a la penicilina" />
        </div>
        {error && <p className={styles.errorMsg}>{error}</p>}
        <button className={styles.btnPrimary} onClick={onReserve} disabled={loading}>
          {loading ? 'Reservando...' : `Reservar ${selectedDate} a las ${selectedSlot}`}
        </button>
      </>
    )}

    <button className={styles.btnGhost} onClick={onViewReservations} disabled={loading}>
      Ver mis reservas anteriores
    </button>
  </div>
);
