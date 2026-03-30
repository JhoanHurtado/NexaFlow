import styles from './EmptyState.module.scss';

interface Props {
  message: string;
  icon?: React.ReactNode;
}

export const EmptyState = ({ message, icon }: Props) => (
  <div className={styles.empty}>
    {icon && <div className={styles.icon}>{icon}</div>}
    <p>{message}</p>
  </div>
);
