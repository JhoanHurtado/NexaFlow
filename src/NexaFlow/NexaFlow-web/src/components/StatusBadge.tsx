import styles from './StatusBadge.module.scss';

const LABELS: Record<string, string> = {
  pending:   'Pendiente',
  confirmed: 'Confirmada',
  cancelled: 'Cancelada',
  completed: 'Completada',
  arrived:   'En local',
  active:    'Activo',
  inactive:  'Inactivo',
};

interface Props {
  status: string;
  className?: string;
}

export const StatusBadge = ({ status, className = '' }: Props) => (
  <span className={`${styles.badge} ${styles[status] ?? ''} ${className}`}>
    {LABELS[status] ?? status}
  </span>
);
