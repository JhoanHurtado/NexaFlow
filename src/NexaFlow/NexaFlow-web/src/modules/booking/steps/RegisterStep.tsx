import { useState } from 'react';
import { User } from 'lucide-react';
import styles from '../BookingPage.module.scss';

interface Props {
  loading: boolean;
  error: string;
  onRegister: (form: { name: string; phone: string; email: string }) => void;
  onClearError: () => void;
}

export const RegisterStep = ({ loading, error, onRegister, onClearError }: Props) => {
  const [form, setForm] = useState({ name: '', phone: '', email: '' });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    onClearError();
  };

  return (
    <form onSubmit={e => { e.preventDefault(); onRegister(form); }} className={styles.form}>
      <h2><User size={18} /> Tus datos</h2>
      <div className={styles.field}>
        <label>Nombre completo *</label>
        <input name="name" value={form.name} onChange={handleChange} placeholder="Tu nombre" required />
      </div>
      <div className={styles.formRow}>
        <div className={styles.field}>
          <label>Teléfono</label>
          <input name="phone" value={form.phone} onChange={handleChange} placeholder="+1 555 0000" />
        </div>
        <div className={styles.field}>
          <label>Correo electrónico</label>
          <input type="email" name="email" value={form.email} onChange={handleChange} placeholder="tu@correo.com" />
        </div>
      </div>
      {error && <p className={styles.errorMsg}>{error}</p>}
      <button type="submit" className={styles.btnPrimary} disabled={loading}>
        {loading ? 'Registrando...' : 'Continuar'}
      </button>
    </form>
  );
};
