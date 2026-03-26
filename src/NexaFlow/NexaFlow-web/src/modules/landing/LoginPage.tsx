import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authApi } from '../../api/auth.api';
import styles from './LoginPage.module.scss';

export const LoginPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState({ tenantId: '', email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.login(form);
      localStorage.setItem('token', res.token);
      localStorage.setItem('tenantId', res.tenantId);
      localStorage.setItem('role', res.role);
      navigate('/app');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Credenciales inválidas');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.logo}>
          <span className={styles.logoIcon}>N</span>
          <span>NexaFlow</span>
        </div>
        <h1>Iniciar sesión</h1>
        <p>Accede al panel de tu negocio</p>

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.field}>
            <label>ID de Tenant</label>
            <input name="tenantId" value={form.tenantId} onChange={handleChange} placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" required />
          </div>
          <div className={styles.field}>
            <label>Correo electrónico</label>
            <input type="email" name="email" value={form.email} onChange={handleChange} placeholder="correo@negocio.com" required />
          </div>
          <div className={styles.field}>
            <label>Contraseña</label>
            <input type="password" name="password" value={form.password} onChange={handleChange} placeholder="••••••••" required />
          </div>
          {error && <p className={styles.errorMsg}>{error}</p>}
          <button type="submit" className={styles.submitBtn} disabled={loading}>
            {loading ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>

        <p className={styles.footer}>
          ¿No tienes cuenta? <Link to="/">Registra tu negocio</Link>
        </p>
      </div>
    </div>
  );
};
