import { ChevronLeft, ChevronRight } from 'lucide-react';
import styles from './Pagination.module.scss';

interface Props {
  page: number;
  totalPages: number;
  info?: string;
  hasNext?: boolean;
  hasPrev?: boolean;
  onChange: (page: number) => void;
}

export const Pagination = ({ page, totalPages, info, hasNext, hasPrev, onChange }: Props) => {
  if (totalPages <= 1 && !hasNext && !hasPrev) return null;

  const canPrev = hasPrev != null ? hasPrev : page > 1;
  const canNext = hasNext != null ? hasNext : page < totalPages;

  const pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  const visible = pages.filter(p => p === 1 || p === totalPages || Math.abs(p - page) <= 1);

  return (
    <div className={styles.pagination}>
      {info && <span className={styles.info}>{info}</span>}
      <div className={styles.controls}>
        <button className={styles.btn} disabled={!canPrev} onClick={() => onChange(page - 1)}>
          <ChevronLeft size={13} />
        </button>
        {visible.map((p, i, arr) => (
          <>
            {i > 0 && arr[i - 1] !== p - 1 && (
              <span key={`dots-${p}`} className={styles.dots}>…</span>
            )}
            <button
              key={p}
              className={`${styles.btn} ${p === page ? styles.active : ''}`}
              onClick={() => onChange(p)}
            >{p}</button>
          </>
        ))}
        <button className={styles.btn} disabled={!canNext} onClick={() => onChange(page + 1)}>
          <ChevronRight size={13} />
        </button>
      </div>
    </div>
  );
};
