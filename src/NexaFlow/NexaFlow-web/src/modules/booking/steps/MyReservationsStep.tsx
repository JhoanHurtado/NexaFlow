import { Calendar, Clock, XCircle } from 'lucide-react';
import { StatusBadge } from '../../../components/StatusBadge';
import { EmptyState } from '../../../components/EmptyState';
import type { ReservationItem } from '../../../api/book.api';
import styles from '../BookingPage.module.scss';

interface Props {
  reservations: ReservationItem[];
  loading: boolean;
  error: string;
  onCancel: (id: string) => void;
  onBack: () => void;
}

export const MyReservationsStep = ({ reservations, loading, error, onCancel, onBack }: Props) => (
  <div className={styles.form}>
    <h2>Mis reservas</h2>
    {!reservations.length ? (
      <EmptyState message="No tienes reservas registradas." />
    ) : (
      <div className={styles.reservationList}>
        {reservations.map(r => (
          <div key={r.id} className={styles.reservationItem}>
            <div className={styles.reservationInfo}>
              <span className={styles.reservationDate}><Calendar size={13} />{r.reservationDate}</span>
              <span className={styles.reservationTime}><Clock size={13} />{r.timeSlot}</span>
              <StatusBadge status={r.status} className={styles.statusBadge} />
            </div>
            {(r.status === 'pending' || r.status === 'confirmed') && (
              <button className={styles.btnCancel} onClick={() => onCancel(r.id)} disabled={loading}>
                <XCircle size={14} /> Cancelar
              </button>
            )}
          </div>
        ))}
      </div>
    )}
    {error && <p className={styles.errorMsg}>{error}</p>}
    <button className={styles.btnGhost} onClick={onBack}>← Volver</button>
  </div>
);
