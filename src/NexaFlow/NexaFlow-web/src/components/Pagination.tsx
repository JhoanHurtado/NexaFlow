import { ChevronLeft, ChevronRight } from 'lucide-react';
import styles from './Pagination.module.scss';

interface Props {
  page: number;
  totalPages: number;
  info?: string;
  onChange: (page: number) => void;
}

export const Pagination = ({ page, totalPages, info, onChange }: Props) => {
  if (totalPages <= 1) return null;

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  const visible = pages.filter(p => p === 1 || p === totalPages || Math.abs(p - page) <= 1);

  return (
    <div className={styles.pagination}>
      {info && <span className={styles.info}>{info}</span>}
      <div className={styles.controls}>
        <button className={styles.btn} disabled={page === 1} onClick={() => onChange(page - 1)}>
          <ChevronLeft size={13} />
        </button>
        {visible.map((p, i, arr) => (
          <span key={p}>
            {i > 0 && arr[i - 1] !== p - 1 && <span className={styles.dots}>…</span>}
            <button
              className={`${styles.btn} ${p === page ? styles.active : ''}`}
              onClick={() => onChange(p)}
            >{p}</button>
          </span>
        ))}
        <button className={styles.btn} disabled={page === totalPages} onClick={() => onChange(page + 1)}>
          <ChevronRight size={13} />
        </button>
      </div>
    </div>
  );
};
