import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authApi } from '../../api/auth.api';
import { Eye, EyeOff, Lock, Mail, Hash, ArrowRight, ShieldCheck, Globe, Zap } from 'lucide-react';
import styles from './LoginPage.module.scss';

export const LoginPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState({ tenantId: '', email: '', password: '' });
  const [showPassword, setShowPassword] = useState(false);
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
      const token    = res.accessToken || res.AccessToken;
      const tenantId = res.tenantId    || res.TenantId;
      const role     = res.role        || res.Role;
      if (!token) throw new Error('No se recibió un token válido.');
      const payload = JSON.parse(window.atob(token.split('.')[1]));
      localStorage.setItem('token',    token);
      localStorage.setItem('tenantId', tenantId || '');
      localStorage.setItem('role',     role     || '');
      localStorage.setItem('userName', payload.name || payload.unique_name || '');
      navigate('/app');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Credenciales inválidas');
    } finally { setLoading(false); }
  };

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        {/* Left panel */}
        <div className={styles.left}>
          <div className={styles.leftGlow1} /><div className={styles.leftGlow2} />
          <div className={styles.leftTop}>
            <div className={styles.brand}><span className={styles.brandIcon}>N</span><span className={styles.brandName}>NexaFlow</span></div>
            <h1 className={styles.leftTitle}>Impulsa tu negocio con <span className={styles.leftAccent}>Inteligencia Real.</span></h1>
            <p className={styles.leftSub}>Accede a tu panel de control centralizado y gestiona cada aspecto de tu flujo comercial.</p>
          </div>
          <div className={styles.leftBottom}>
            <div className={styles.featureRow}>
              <div className={styles.featureIcon}><ShieldCheck size={18} /></div>
              <div>
                <p className={styles.featureTitle}>Seguridad de Grado Bancario</p>
                <p className={styles.featureSub}>Tus datos están encriptados de punto a punto.</p>
              </div>
            </div>
            <div className={styles.leftFooter}>
              <span>v4.0 Enterprise</span>
              <div className={styles.leftFooterIcons}><Globe size={14} /><Zap size={14} /></div>
            </div>
          </div>
        </div>

        {/* Right panel — form */}
        <div className={styles.right}>
          <div className={styles.formHeader}>
            <h2>Bienvenido de nuevo</h2>
            <p>Ingresa tus credenciales para continuar</p>
          </div>

          <form onSubmit={handleSubmit} className={styles.form}>
            {/* Tenant ID */}
            <div className={styles.field}>
              <label>ID de Proyecto / Tenant</label>
              <div className={styles.inputWrap}>
                <Hash size={17} className={styles.inputIcon} />
                <input name="tenantId" value={form.tenantId} onChange={handleChange}
                  placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" required className={styles.input} />
                {form.tenantId.length === 36 && <span className={styles.verifiedBadge}>VERIFICADO</span>}
              </div>
            </div>

            {/* Email */}
            <div className={styles.field}>
              <label>Correo Electrónico</label>
              <div className={styles.inputWrap}>
                <Mail size={17} className={styles.inputIcon} />
                <input type="email" name="email" value={form.email} onChange={handleChange}
                  placeholder="ejemplo@correo.com" required className={styles.input} />
              </div>
            </div>

            {/* Password */}
            <div className={styles.field}>
              <div className={styles.fieldLabelRow}>
                <label>Contraseña</label>
                <button type="button" className={styles.forgotBtn}>¿Olvidaste tu contraseña?</button>
              </div>
              <div className={styles.inputWrap}>
                <Lock size={17} className={styles.inputIcon} />
                <input type={showPassword ? 'text' : 'password'} name="password"
                  value={form.password} onChange={handleChange}
                  placeholder="••••••••" required className={styles.input} />
                <button type="button" className={styles.eyeBtn} onClick={() => setShowPassword(p => !p)}>
                  {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
                </button>
              </div>
            </div>

            {error && <p className={styles.errorMsg}>{error}</p>}

            <button type="submit" className={styles.submitBtn} disabled={loading}>
              {loading
                ? <span className={styles.spinner} />
                : <><span>Ingresar al Panel</span><ArrowRight size={17} className={styles.arrowIcon} /></>}
            </button>
          </form>

          <p className={styles.registerLink}>
            ¿No tienes cuenta?{' '}
            <Link to="/" className={styles.registerLinkBtn}>Registra tu negocio</Link>
          </p>
        </div>
      </div>
    </div>
  );
};
