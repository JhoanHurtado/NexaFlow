import { CheckCircle, Clock, Calendar, UserCheck, XCircle } from 'lucide-react';
import { StatusBadge } from '../../components/StatusBadge';
import type { ReservationDTO } from '../../api/book-admin.api';
import styles from './ReservationsPage.module.scss';

interface Props {
  reservation: ReservationDTO;
  showDate?: boolean;
  actionLoading: string | null;
  onConfirm: (id: string) => void;
  onArrived: (id: string) => void;
  onComplete: (id: string) => void;
  onCancel: (id: string) => void;
}

export const ReservationRow = ({
  reservation: r, showDate = false, actionLoading,
  onConfirm, onArrived, onComplete, onCancel,
}: Props) => (
  <div className={`${styles.row} ${styles[r.status] ?? ''}`}>
    <div className={styles.rowMain}>
      <span className={styles.customerName}>{r.customerName}</span>
      {showDate && <span className={styles.rowDate}><Calendar size={12} />{r.reservationDate}</span>}
      <span className={styles.rowTime}><Clock size={12} />{r.timeSlot}</span>
      <StatusBadge status={r.status} />
      {r.notes && <span className={styles.notes}>{r.notes}</span>}
    </div>
    <div className={styles.actions}>
      {r.status === 'pending' && (
        <button className={styles.btnConfirm} disabled={!!actionLoading} onClick={() => onConfirm(r.id)}>
          <CheckCircle size={13} /> Confirmar
        </button>
      )}
      {r.status === 'confirmed' && (
        <button className={styles.btnArrived} disabled={!!actionLoading} onClick={() => onArrived(r.id)}>
          <UserCheck size={13} /> Llegó
        </button>
      )}
      {r.status === 'arrived' && (
        <button className={styles.btnComplete} disabled={!!actionLoading} onClick={() => onComplete(r.id)}>
          <CheckCircle size={13} /> Completar
        </button>
      )}
      {(r.status === 'pending' || r.status === 'confirmed') && (
        <button className={styles.btnCancel} disabled={!!actionLoading} onClick={() => onCancel(r.id)}>
          <XCircle size={13} /> Cancelar
        </button>
      )}
    </div>
  </div>
);
