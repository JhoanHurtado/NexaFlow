import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../../api/auth.api';
import type { PlanDTO } from '../../api/auth.api';
import {
  Calendar, BarChart3, CheckCircle2, ArrowRight,
  Sparkles, Zap, Rocket, Crown, Layout, ChevronRight,
} from 'lucide-react';
import styles from './LandingPage.module.scss';
import { formatValue } from '../../utils/formatters';

const PLAN_ICONS: Record<string, React.ReactNode> = {
  starter:    <Rocket size={24} className={styles.iconBlue} />,
  business:   <Zap    size={24} className={styles.iconWhite} />,
  enterprise: <Crown  size={24} className={styles.iconAmber} />,
};
const PLAN_FEATURES: Record<string, string[]> = {
  starter:    ['Hasta 100 reservas/mes', 'Menú Digital QR', 'Soporte vía Email'],
  business:   ['Reservas Ilimitadas', 'Gestión de Inventario', 'Análisis de IA básico', 'Soporte 24/7'],
  enterprise: ['Múltiples Sedes', 'API de Integración', 'IA Predictiva Avanzada', 'Account Manager'],
};

function getPlanIcon(plan: PlanDTO): React.ReactNode {
  return PLAN_ICONS[plan.id.toLowerCase()] ?? <Rocket size={24} className={styles.iconBlue} />;
}
function getPlanFeatures(plan: PlanDTO): string[] {
  return PLAN_FEATURES[plan.id.toLowerCase()] ?? [`Hasta ${plan.maxUsers} usuarios`];
}

export const LandingPage = () => {
  const navigate = useNavigate();
  const [view, setView] = useState<'landing' | 'register'>('landing');
  const [plans, setPlans] = useState<PlanDTO[]>([]);
  const [form, setForm] = useState({ businessName: '', ownerName: '', ownerEmail: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => { authApi.getPlans().then(setPlans); }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
    setError('');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setError('');
    try {
      const res = await authApi.register(form);
      setSuccess(`¡Negocio registrado! Tu ID de tenant es: ${res.tenantId}. Redirigiendo al login...`);
      //setTimeout(() => navigate('/login'), 600000);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Error al registrar');
    } finally { setLoading(false); }
  };

  return (
    <div className={styles.page}>
      <nav className={styles.nav}>
        <div className={styles.navLogo} onClick={() => setView('landing')}>
          <span className={styles.logoIcon}>N</span>
          <span className={styles.logoText}>NexaFlow</span>
        </div>
        <div className={styles.navLinks}>
          {view === 'landing' && (
            <>
              <a href="#features" className={styles.navLink}>Características</a>
              <a href="#pricing"  className={styles.navLink}>Planes</a>
              <button className={styles.navLinkBlue} onClick={() => setView('register')}>Registrar mi Negocio</button>
            </>
          )}
          <button className={styles.navCta} onClick={() => navigate('/login')}>Iniciar sesión</button>
        </div>
      </nav>

      {view === 'landing' && (
        <>
          <section className={styles.hero}>
            <div className={styles.heroInner}>
              <div className={styles.heroBadge}><Sparkles size={13} /> Ahora con Menú Digital Automático</div>
              <h1 className={styles.heroTitle}>
                La infraestructura operativa de <span className={styles.heroAccent}>tu restaurante.</span>
              </h1>
              <p className={styles.heroSub}>
                Gestiona reservas, menús digitales y ventas desde una única plataforma inteligente.
              </p>
              <div className={styles.heroCta}>
                <button className={styles.btnDark} onClick={() => setView('register')}>Comenzar Gratis <ArrowRight size={17} /></button>
                <button className={styles.btnOutline} onClick={() => navigate('/login')}>Ver Demo en Vivo</button>
              </div>
            </div>
            <div className={styles.heroCard}>
              <div className={styles.heroCardHeader}>
                <p className={styles.heroCardLabel}>Actividad Hoy</p>
                <Sparkles size={18} className={styles.iconBlue} />
              </div>
              <div className={styles.heroStats}>
                <div className={styles.heroStat}><strong>24</strong><span>Reservas</span></div>
                <div className={`${styles.heroStat} ${styles.heroStatBlue}`}><strong>$4,820</strong><span>Ventas Mes</span></div>
              </div>
              <div className={styles.heroProgress}>
                <div className={styles.heroProgressBar}><div className={styles.heroProgressFill} /></div>
                <p className={styles.heroProgressLabel}>94% de eficiencia operativa alcanzada</p>
              </div>
            </div>
          </section>

          <section className={styles.features} id="features">
            <div className={styles.sectionHeader}>
              <h2>Todo lo que necesitas</h2>
              <p>Herramientas potentes, interfaz sencilla.</p>
            </div>
            <div className={styles.featureGrid}>
              {[
                { icon: <Calendar  size={26} className={styles.iconBlue} />,  title: 'NexaBook',    desc: 'Sistema de reservas con disponibilidad real y recordatorios automáticos.' },
                { icon: <Zap       size={26} className={styles.iconAmber} />, title: 'NexaPOS',     desc: 'Punto de venta ágil con gestión de stock sincronizado en tiempo real.' },
                { icon: <BarChart3 size={26} className={styles.iconGreen} />, title: 'NexaInsight', desc: 'Analítica avanzada con predicción de demanda mediante IA.' },
              ].map(f => (
                <div key={f.title} className={styles.featureCard}>
                  <div className={styles.featureIconWrap}>{f.icon}</div>
                  <h3>{f.title}</h3>
                  <p>{f.desc}</p>
                  <span className={styles.featureLink}>Saber más <ChevronRight size={13} /></span>
                </div>
              ))}
            </div>
          </section>

          <section className={styles.pricing} id="pricing">
            <div className={styles.sectionHeader}>
              <h2>Planes que crecen contigo</h2>
              <p>Desde pequeños cafés hasta cadenas de restaurantes.</p>
            </div>
            <div className={styles.planGrid}>
              {plans.length === 0 ? (
                <p style={{ gridColumn: '1/-1', textAlign: 'center', color: '#94a3b8', fontSize: '0.88rem' }}>Cargando planes...</p>
              ) : plans.map(plan => {
                const featured = plan.id.toLowerCase() === 'business';
                const features = getPlanFeatures(plan);
                return (
                  <div key={plan.id} className={`${styles.planCard} ${featured ? styles.planFeatured : ''}`}>
                    {featured && <span className={styles.planBadge}>Recomendado</span>}
                    <div className={`${styles.planIconWrap} ${featured ? styles.planIconBlue : styles.planIconGray}`}>
                      {getPlanIcon(plan)}
                    </div>
                    <h3 className={styles.planName}>{plan.name}</h3>
                    <div className={styles.planPrice}>
                      <strong>{formatValue(plan.price)}</strong><span>/mes</span>
                    </div>
                    <ul className={styles.planFeatures}>
                      {features.map((f: string) => (
                        <li key={f}>
                          <CheckCircle2 size={15} className={featured ? styles.iconBlueLight : styles.iconBlue} />
                          {f}
                        </li>
                      ))}
                      <li>
                        <CheckCircle2 size={15} className={featured ? styles.iconBlueLight : styles.iconBlue} />
                        Hasta {plan.maxUsers} usuarios
                      </li>
                    </ul>
                    <button className={featured ? styles.planCtaWhite : styles.planCtaDark} onClick={() => setView('register')}>
                      Elegir Plan {plan.name}
                    </button>
                  </div>
                );
              })}
            </div>
          </section>

          <footer className={styles.footer}>
            <p>© 2026 NexaFlow · Todos los derechos reservados</p>
          </footer>
        </>
      )}

      {view === 'register' && (
        <section className={styles.registerSection}>
          <div className={styles.registerCard}>
            <div className={styles.registerHeader}>
              <div className={styles.registerIconWrap}><Layout size={28} /></div>
              <h2>Registra tu Establecimiento</h2>
              <p>Comienza tu prueba gratuita de 14 días.</p>
            </div>
            {success ? (
              <div className={styles.successMsg}><CheckCircle2 size={18} /><span>{success}</span></div>
            ) : (
              <form onSubmit={handleSubmit} className={styles.form}>
                <div className={styles.formRow}>
                  <div className={styles.field}>
                    <label>Nombre del Negocio</label>
                    <input name="businessName" value={form.businessName} onChange={handleChange} placeholder="Ej: La Toscana Pizzería" required />
                  </div>
                  <div className={styles.field}>
                    <label>Tu nombre</label>
                    <input name="ownerName" value={form.ownerName} onChange={handleChange} placeholder="Nombre completo" required />
                  </div>
                </div>
                <div className={styles.field}>
                  <label>Correo Corporativo</label>
                  <input type="email" name="ownerEmail" value={form.ownerEmail} onChange={handleChange} placeholder="admin@tunegocio.com" required />
                </div>
                <div className={styles.field}>
                  <label>Contraseña</label>
                  <input type="password" name="password" value={form.password} onChange={handleChange} placeholder="Mínimo 8 caracteres" required minLength={8} />
                </div>
                <div className={styles.urlPreview}>
                  <p className={styles.urlLabel}>Tu URL de Reservas personalizada</p>
                  <p className={styles.urlValue}>
                    nexaflow.app/book/<span className={styles.urlSlug}>{form.businessName.toLowerCase().replace(/\s+/g, '-') || 'tu-negocio'}</span>
                  </p>
                </div>
                {error && <p className={styles.errorMsg}>{error}</p>}
                <button type="submit" className={styles.submitBtn} disabled={loading}>
                  {loading ? 'Registrando...' : 'Crear mi cuenta'}
                </button>
                <button type="button" className={styles.backLink} onClick={() => setView('landing')}>
                  ← Volver al inicio
                </button>
              </form>
            )}
          </div>
        </section>
      )}
    </div>
  );
};
