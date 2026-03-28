import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../../api/auth.api';
import styles from './LandingPage.module.scss';
import { Calendar, ShoppingCart, BarChart3, CheckCircle, ArrowRight } from 'lucide-react';

const PLANS = [
  { id: 'starter', name: 'Starter', price: '$29', period: '/mes', features: ['Hasta 100 reservas/mes', 'POS básico', 'Reportes simples'] },
  { id: 'pro', name: 'Pro', price: '$79', period: '/mes', features: ['Reservas ilimitadas', 'POS completo', 'Analytics con IA', 'Soporte prioritario'], highlight: true },
  { id: 'enterprise', name: 'Enterprise', price: 'Custom', period: '', features: ['Multi-sucursal', 'API dedicada', 'SLA garantizado', 'Onboarding personalizado'] },
];

export const LandingPage = () => {
  const navigate = useNavigate();
  const [selectedPlan, setSelectedPlan] = useState('pro');
  const [form, setForm] = useState({ businessName: '', ownerName: '', ownerEmail: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const res = await authApi.register(form);
      setSuccess(`¡Negocio registrado! Tu ID de tenant es: ${res.TenantId}. Redirigiendo al login...`);
      setTimeout(() => navigate('/login'), 3000);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al registrar');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.page}>
      {/* NAV */}
      <nav className={styles.nav}>
        <div className={styles.navLogo}>
          <span className={styles.logoIcon}>N</span>
          <span>NexaFlow</span>
        </div>
        <div className={styles.navActions}>
          <button className={styles.btnOutline} onClick={() => navigate('/login')}>Iniciar sesión</button>
          <a href="#register" className={styles.btnPrimary}>Comenzar gratis</a>
        </div>
      </nav>

      {/* HERO */}
      <section className={styles.hero}>
        <div className={styles.heroContent}>
          <span className={styles.badge}>Plataforma todo-en-uno</span>
          <h1>Gestiona tu negocio con inteligencia</h1>
          <p>Reservas, punto de venta, inventario y analytics con IA — todo integrado en una sola plataforma.</p>
          <div className={styles.heroCta}>
            <a href="#register" className={styles.btnPrimary}>Empieza hoy <ArrowRight size={16} /></a>
            <a href="#features" className={styles.btnGhost}>Ver características</a>
          </div>
        </div>
        <div className={styles.heroVisual}>
          <div className={styles.mockCard}>
            <div className={styles.mockStat}><span>Reservas hoy</span><strong>24</strong></div>
            <div className={styles.mockStat}><span>Ventas del mes</span><strong>$4,820</strong></div>
            <div className={styles.mockStat}><span>Clientes activos</span><strong>138</strong></div>
          </div>
        </div>
      </section>

      {/* FEATURES */}
      <section className={styles.features} id="features">
        <h2>Todo lo que necesitas</h2>
        <div className={styles.featureGrid}>
          <div className={styles.featureCard}>
            <Calendar size={28} className={styles.featureIcon} />
            <h3>NexaBook</h3>
            <p>Sistema de reservas con disponibilidad en tiempo real, confirmaciones automáticas y gestión de agenda.</p>
          </div>
          <div className={styles.featureCard}>
            <ShoppingCart size={28} className={styles.featureIcon} />
            <h3>NexaPOS</h3>
            <p>Punto de venta con control de inventario, gestión de productos y historial de ventas completo.</p>
          </div>
          <div className={styles.featureCard}>
            <BarChart3 size={28} className={styles.featureIcon} />
            <h3>NexaInsight + IA</h3>
            <p>Analytics avanzados con predicción de ventas, detección de anomalías e insights generados por IA.</p>
          </div>
        </div>
      </section>

      {/* PLANES */}
      <section className={styles.pricing} id="pricing">
        <h2>Planes simples y transparentes</h2>
        <div className={styles.planGrid}>
          {PLANS.map(plan => (
            <div
              key={plan.id}
              className={`${styles.planCard} ${plan.highlight ? styles.planHighlight : ''} ${selectedPlan === plan.id ? styles.planSelected : ''}`}
              onClick={() => setSelectedPlan(plan.id)}
            >
              {plan.highlight && <span className={styles.planBadge}>Más popular</span>}
              <h3>{plan.name}</h3>
              <div className={styles.planPrice}>
                <strong>{plan.price}</strong><span>{plan.period}</span>
              </div>
              <ul>
                {plan.features.map(f => (
                  <li key={f}><CheckCircle size={14} />{f}</li>
                ))}
              </ul>
              <a href="#register" className={styles.planCta}>Seleccionar</a>
            </div>
          ))}
        </div>
      </section>

      {/* REGISTRO */}
      <section className={styles.registerSection} id="register">
        <div className={styles.registerCard}>
          <h2>Registra tu negocio</h2>
          <p>Crea tu cuenta y empieza a usar NexaFlow en minutos.</p>

          {success ? (
            <div className={styles.successMsg}>
              <CheckCircle size={20} />
              <span>{success}</span>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className={styles.form}>
              <div className={styles.formRow}>
                <div className={styles.field}>
                  <label>Nombre del negocio</label>
                  <input name="businessName" value={form.businessName} onChange={handleChange} placeholder="Ej: Clínica Dental Norte" required />
                </div>
                <div className={styles.field}>
                  <label>Tu nombre</label>
                  <input name="ownerName" value={form.ownerName} onChange={handleChange} placeholder="Nombre completo" required />
                </div>
              </div>
              <div className={styles.field}>
                <label>Correo electrónico</label>
                <input type="email" name="ownerEmail" value={form.ownerEmail} onChange={handleChange} placeholder="correo@negocio.com" required />
              </div>
              <div className={styles.field}>
                <label>Contraseña</label>
                <input type="password" name="password" value={form.password} onChange={handleChange} placeholder="Mínimo 8 caracteres" required minLength={8} />
              </div>
              {error && <p className={styles.errorMsg}>{error}</p>}
              <button type="submit" className={styles.submitBtn} disabled={loading}>
                {loading ? 'Registrando...' : `Crear cuenta — Plan ${PLANS.find(p => p.id === selectedPlan)?.name}`}
              </button>
            </form>
          )}
        </div>
      </section>

      <footer className={styles.footer}>
        <p>© 2025 NexaFlow · Todos los derechos reservados</p>
      </footer>
    </div>
  );
};
