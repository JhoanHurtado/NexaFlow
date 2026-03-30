import type { AgendaDTO } from '../../api/book-admin.api';
import styles from './ReservationsPage.module.scss';

interface Props { agenda: AgendaDTO; }

const STATS = [
  { key: 'totalReservations', label: 'Total',      cls: '' },
  { key: 'pending',           label: 'Pendientes', cls: 'pending' },
  { key: 'confirmed',         label: 'Confirmadas',cls: 'confirmed' },
  { key: 'arrived',           label: 'En local',   cls: 'arrived' },
  { key: 'completed',         label: 'Completadas',cls: 'completed' },
  { key: 'cancelled',         label: 'Canceladas', cls: 'cancelled' },
] as const;

export const AgendaStats = ({ agenda }: Props) => (
  <div className={styles.statsRow}>
    {STATS.map(s => (
      <div key={s.label} className={`${styles.statCard} ${s.cls ? styles[s.cls] : ''}`}>
        <strong>{agenda[s.key]}</strong>
        <span>{s.label}</span>
      </div>
    ))}
  </div>
);
