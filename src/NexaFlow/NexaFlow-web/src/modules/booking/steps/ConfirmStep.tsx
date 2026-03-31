import { CheckCircle } from 'lucide-react';
import styles from '../BookingPage.module.scss';

interface Props {
  reservationId: string;
  selectedDate: string;
  selectedSlot: string;
  onViewReservations: () => void;
  onNewReservation: () => void;
}

export const ConfirmStep = ({ reservationId, selectedDate, selectedSlot, onViewReservations, onNewReservation }: Props) => (
  <div className={styles.confirmBox}>
    <CheckCircle size={48} className={styles.confirmIcon} />
    <h2>¡Reserva creada!</h2>
    <p>Tu reserva para el <strong>{selectedDate}</strong> a las <strong>{selectedSlot}</strong> fue registrada exitosamente.</p>
    <p className={styles.reservationId}>ID: {reservationId}</p>
    <div className={styles.confirmActions}>
      <button className={styles.btnPrimary} onClick={onViewReservations}>Ver mis reservas</button>
      <button className={styles.btnOutline} onClick={onNewReservation}>Nueva reserva</button>
    </div>
  </div>
);
