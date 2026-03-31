import { useState, useEffect, useMemo } from 'react';
import { useParams, useLocation, Link } from 'react-router-dom';
import {
  Calendar, Clock, User, Phone, Mail, CheckCircle2,
  ShieldCheck, Utensils, ArrowRight, Copy, Check, Sparkles, ExternalLink,
} from 'lucide-react';
import { bookApi } from '../../api/book.api';
import { posApi } from '../../api/pos.api';
import { authApi } from '../../api/auth.api';
import type { ProductDTO } from '../../api/pos.api';
import styles from './TenantPortalPage.module.scss';

const DAYS_AHEAD = 14;

function buildCalendarDays() {
  const base = new Date();
  return Array.from({ length: DAYS_AHEAD }, (_, i) => {
    const d = new Date(base); d.setDate(base.getDate() + i); return d;
  });
}

const fmtDate  = (d: Date) => d.toISOString().split('T')[0];
const fmtDay   = (d: Date) => d.toLocaleDateString('es-ES', { weekday: 'short' });
const fmtNum   = (d: Date) => d.getDate();
const fmtMonth = (d: Date) => d.toLocaleDateString('es-ES', { month: 'short' });
const fmtLong  = (d: Date) => d.toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' });

/** Genera slots de tiempo entre openTime y closeTime con paso slotMinutes */
function generateTimeSlots(openTime: string, closeTime: string, slotMinutes: number, takenSlots: Set<string>): { time: string; available: boolean }[] {
  const [oh, om] = openTime.split(':').map(Number);
  const [ch, cm] = closeTime.split(':').map(Number);
  const startMin = oh * 60 + om;
  const endMin   = ch * 60 + cm;
  const slots: { time: string; available: boolean }[] = [];
  for (let m = startMin; m < endMin; m += slotMinutes) {
    const h = Math.floor(m / 60).toString().padStart(2, '0');
    const min = (m % 60).toString().padStart(2, '0');
    const time = `${h}:${min}`;
    slots.push({ time, available: !takenSlots.has(time) });
  }
  return slots;
}

type BookingPhase = 'pick' | 'form' | 'done';

export const TenantPortalPage = () => {
  const { tenantId = '' } = useParams<{ tenantId: string }>();
  const location = useLocation();
  const isMenuOnly = location.pathname.includes('/book/menu/');

  // Tenant info
  const [tenantName, setTenantName] = useState('');

  // Config (horario + slot)
  const [openTime,  setOpenTime]  = useState('08:00');
  const [closeTime, setCloseTime] = useState('20:00');
  const [slotMin,   setSlotMin]   = useState(60);

  // Menu
  const [menu, setMenu]           = useState<ProductDTO[]>([]);
  const [menuLoading, setMenuLoading] = useState(true);

  // Calendar
  const [calDays]                 = useState(buildCalendarDays);
  const [selectedDay, setSelectedDay] = useState<Date | null>(null);
  const [takenSlots, setTakenSlots]   = useState<Set<string>>(new Set());
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState('');

  // Booking
  const [phase, setPhase]         = useState<BookingPhase>('pick');
  const [form, setForm]           = useState({ name: '', email: '', phone: '', notes: '' });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]         = useState('');
  const [reservationId, setReservationId] = useState('');
  const [copied, setCopied]       = useState(false);

  useEffect(() => {
    if (!tenantId) return;
    // Carga en paralelo: info del tenant, config y menú
    authApi.getTenantInfo(tenantId).then(t => { if (t) setTenantName(t.name); });
    posApi.getConfig(tenantId).then(c => {
      setOpenTime(c.openTime); setCloseTime(c.closeTime); setSlotMin(c.slotDurationMinutes);
    });
    posApi.getMenu(tenantId).then(setMenu).catch(() => setMenu([]))
      .finally(() => setMenuLoading(false));
  }, [tenantId]);

  // Cuando se selecciona un día, carga los slots ocupados del backend
  const handleDaySelect = async (day: Date) => {
    setSelectedDay(day); setSelectedSlot(''); setTakenSlots(new Set()); setSlotsLoading(true);
    try {
      const available = await bookApi.getAvailability(tenantId, fmtDate(day));
      // Los slots que NO están disponibles son los ocupados
      const taken = new Set(available.filter(s => !s.available).map(s => s.timeSlot.slice(0, 5)));
      setTakenSlots(taken);
    } catch { setTakenSlots(new Set()); }
    finally { setSlotsLoading(false); }
  };

  // Genera todos los slots del horario del tenant, marcando los ocupados
  const timeSlots = useMemo(() =>
    selectedDay ? generateTimeSlots(openTime, closeTime, slotMin, takenSlots) : [],
    [selectedDay, openTime, closeTime, slotMin, takenSlots]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDay || !selectedSlot) return;
    setSubmitting(true); setError('');
    try {
      const customer = await bookApi.findOrCreateCustomer(tenantId, {
        name: form.name, email: form.email, phone: form.phone,
      });
      const res = await bookApi.createReservation(tenantId, {
        customerId: customer.id,
        reservationDate: fmtDate(selectedDay),
        timeSlot: selectedSlot,
        notes: form.notes || undefined,
      });
      setReservationId(res.id);
      setPhase('done');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear la reserva');
    } finally { setSubmitting(false); }
  };

  const copyLink = () => {
    navigator.clipboard.writeText(window.location.href);
    setCopied(true); setTimeout(() => setCopied(false), 2000);
  };

  const progressPct = phase === 'pick'
    ? (selectedSlot ? '66%' : selectedDay ? '33%' : '5%')
    : phase === 'form' ? '85%' : '100%';

  const displayName = tenantName || 'Portal de Reservas';

  // ── VISTA: SOLO MENÚ ──────────────────────────────────────────────────────
  if (isMenuOnly) {
    return (
      <div className={styles.page}>
        <nav className={styles.nav}>
          <div className={styles.navLogo}>
            <div className={styles.logoIcon}>N</div>
            <span className={styles.logoText}>{displayName || 'NexaFlow'}</span>
          </div>
          <Link to={`/book/${tenantId}`} className={styles.copyBtn}>
            <Calendar size={13} /> Hacer una reserva
          </Link>
        </nav>
        <div className={styles.hero}>
          <div className={styles.heroAvatar}>{displayName.charAt(0).toUpperCase()}</div>
          <div>
            <h1 className={styles.heroTitle}>{displayName}</h1>
            <p className={styles.heroSub}>Menú Digital</p>
            <span className={styles.heroBadge}><Sparkles size={10} /> Powered by NexaFlow</span>
          </div>
        </div>
        <div className={styles.menuOnlyWrap}>
          <div className={styles.menuHeader}>
            <h2><Utensils size={17} /> Nuestro Menú</h2>
          </div>
          {menuLoading ? <p className={styles.loading}>Cargando menú...</p>
            : menu.length === 0 ? <p className={styles.menuEmpty}>Este negocio aún no tiene menú publicado.</p>
            : (
              <div className={styles.menuGrid}>
                {menu.map(p => (
                  <div key={p.id} className={styles.menuCard}>
                    <div className={styles.menuCardIcon}>{p.name.charAt(0).toUpperCase()}</div>
                    <div className={styles.menuCardInfo}>
                      <p className={styles.menuItemName}>{p.name}</p>
                      <p className={styles.menuItemPrice}>${p.price.toFixed(2)}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
        </div>
      </div>
    );
  }

  // ── VISTA: RESERVA ────────────────────────────────────────────────────────
  return (
    <div className={styles.page}>
      <nav className={styles.nav}>
        <div className={styles.navLogo}>
          <div className={styles.logoIcon}>N</div>
          <span className={styles.logoText}>{displayName || 'NexaFlow'}</span>
        </div>
        <div className={styles.navActions}>
          <Link to={`/book/menu/${tenantId}`} className={styles.menuLink}>
            <Utensils size={13} /> Ver menú <ExternalLink size={11} />
          </Link>
          <button className={styles.copyBtn} onClick={copyLink}>
            {copied ? <><Check size={13} /> Copiado</> : <><Copy size={13} /> Compartir</>}
          </button>
        </div>
      </nav>

      <div className={styles.hero}>
        <div className={styles.heroAvatar}>{displayName.charAt(0).toUpperCase()}</div>
        <div>
          <h1 className={styles.heroTitle}>{displayName}</h1>
          <p className={styles.heroSub}>
            Horario: {openTime} – {closeTime} · Citas cada {slotMin} min
          </p>
          <span className={styles.heroBadge}><Sparkles size={10} /> Reserva segura vía NexaFlow</span>
        </div>
      </div>

      <div className={styles.main}>
        {/* LEFT — info del negocio */}
        <div className={styles.menuCol}>
          <div className={styles.businessCard}>
            <h2 className={styles.businessName}>{displayName}</h2>
            <div className={styles.businessMeta}>
              <div className={styles.metaItem}><Clock size={14} /><span>Horario: {openTime} – {closeTime}</span></div>
              <div className={styles.metaItem}><Calendar size={14} /><span>Citas cada {slotMin} minutos</span></div>
            </div>
            <Link to={`/book/menu/${tenantId}`} className={styles.menuLinkCard}>
              <Utensils size={15} /> Ver menú completo <ArrowRight size={13} />
            </Link>
          </div>
        </div>

        {/* RIGHT — widget de reserva */}
        <div className={styles.bookingCol}>
          <div className={styles.bookingCard}>
            <div className={styles.progressWrap}>
              <div className={styles.progressTrack}>
                <div className={styles.progressFill} style={{ width: progressPct }} />
              </div>
              <div className={styles.progressSteps}>
                <span className={`${styles.progressStep} ${phase === 'pick' ? styles.progressStepActive : ''}`}>Fecha y hora</span>
                <span className={`${styles.progressStep} ${phase === 'form' ? styles.progressStepActive : ''}`}>Tus datos</span>
                <span className={`${styles.progressStep} ${phase === 'done' ? styles.progressStepActive : ''}`}>Confirmado</span>
              </div>
            </div>

            {/* PICK */}
            {phase === 'pick' && (
              <>
                <p className={styles.bookingTitle}>Reserva tu cita</p>
                <p className={styles.bookingSub}>Elige el día y la hora que prefieras</p>

                <p className={styles.calLabel}>1. Selecciona el día</p>
                <div className={styles.calStrip}>
                  {calDays.map(d => (
                    <button key={fmtDate(d)}
                      className={`${styles.calDay} ${selectedDay && fmtDate(selectedDay) === fmtDate(d) ? styles.calDayActive : ''}`}
                      onClick={() => handleDaySelect(d)}>
                      <span className={styles.calDayName}>{fmtDay(d)}</span>
                      <span className={styles.calDayNum}>{fmtNum(d)}</span>
                      <span className={styles.calDayMonth}>{fmtMonth(d)}</span>
                    </button>
                  ))}
                </div>

                {selectedDay && (
                  <>
                    <div className={styles.divider} />
                    <p className={styles.slotsLabel}>
                      2. Elige tu horario — {fmtLong(selectedDay)}
                    </p>
                    {slotsLoading ? (
                      <p className={styles.loading}>Cargando disponibilidad...</p>
                    ) : timeSlots.length === 0 ? (
                      <p className={styles.empty}>No hay horarios configurados para este negocio.</p>
                    ) : (
                      <div className={styles.slotsGrid}>
                        {timeSlots.map(s => (
                          <button key={s.time}
                            disabled={!s.available}
                            className={`${styles.slot} ${!s.available ? styles.slotTaken : ''} ${selectedSlot === s.time ? styles.slotActive : ''}`}
                            onClick={() => setSelectedSlot(s.time)}>
                            <Clock size={11} /> {s.time}
                          </button>
                        ))}
                      </div>
                    )}
                  </>
                )}

                <button className={styles.btnPrimary} style={{ marginTop: '1rem' }}
                  disabled={!selectedDay || !selectedSlot}
                  onClick={() => setPhase('form')}>
                  Continuar <ArrowRight size={15} />
                </button>
              </>
            )}

            {/* FORM */}
            {phase === 'form' && (
              <>
                <div className={styles.summaryPill}>
                  <span className={styles.summaryItem}><Calendar size={13} /> {selectedDay ? fmtLong(selectedDay) : ''}</span>
                  <span className={styles.summaryItem}><Clock size={13} /> {selectedSlot}</span>
                </div>
                <p className={styles.bookingTitle}>Tus datos</p>
                <p className={styles.bookingSub}>Solo necesitamos esto para confirmar tu lugar.</p>
                <form onSubmit={handleSubmit} className={styles.formSection}>
                  <div className={styles.inputGroup}>
                    <User size={15} className={styles.inputIcon} />
                    <input className={styles.input} type="text" placeholder="Nombre completo"
                      value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} required />
                  </div>
                  <div className={styles.inputGroup}>
                    <Mail size={15} className={styles.inputIcon} />
                    <input className={styles.input} type="email" placeholder="Correo electrónico"
                      value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))} required />
                  </div>
                  <div className={styles.inputGroup}>
                    <Phone size={15} className={styles.inputIcon} />
                    <input className={styles.input} type="tel" placeholder="WhatsApp / Teléfono"
                      value={form.phone} onChange={e => setForm(p => ({ ...p, phone: e.target.value }))} />
                  </div>
                  <textarea className={styles.textarea} placeholder="Notas opcionales"
                    value={form.notes} onChange={e => setForm(p => ({ ...p, notes: e.target.value }))} rows={2} />
                  <div className={styles.infoBox}>
                    <ShieldCheck size={16} className={styles.infoIcon} />
                    <p>Si ya tienes cuenta, tu reserva se asociará automáticamente. <strong>No requiere registro.</strong></p>
                  </div>
                  {error && <p className={styles.errorMsg}>{error}</p>}
                  <button type="submit" className={styles.btnPrimary} disabled={submitting}>
                    {submitting ? 'Confirmando...' : 'Finalizar Reserva'}
                  </button>
                  <button type="button" className={styles.backBtn}
                    style={{ marginTop: '0.75rem', width: '100%', textAlign: 'center' }}
                    onClick={() => setPhase('pick')}>
                    ← Cambiar fecha u hora
                  </button>
                </form>
              </>
            )}

            {/* DONE */}
            {phase === 'done' && (
              <div className={styles.confirmSection}>
                <div className={styles.confirmIconWrap}><CheckCircle2 size={36} /></div>
                <h2 className={styles.confirmTitle}>¡Reserva Confirmada!</h2>
                <p className={styles.confirmSub}>Hemos registrado tu cita en {displayName}.</p>
                <div className={styles.confirmDetails}>
                  <div className={styles.confirmRow}><Calendar size={14} /><span>{selectedDay ? fmtLong(selectedDay) : ''}</span></div>
                  <div className={styles.confirmRow}><Clock size={14} /><span>{selectedSlot}</span></div>
                  <div className={styles.confirmRow}><User size={14} /><span>{form.name}</span></div>
                  <div className={styles.confirmRow}><Mail size={14} /><span>{form.email}</span></div>
                </div>
                <p className={styles.confirmId}>ID: {reservationId}</p>
                <button className={styles.btnOutline} onClick={() => {
                  setPhase('pick'); setSelectedDay(null); setSelectedSlot('');
                  setForm({ name: '', email: '', phone: '', notes: '' });
                }}>
                  Hacer otra reserva
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
