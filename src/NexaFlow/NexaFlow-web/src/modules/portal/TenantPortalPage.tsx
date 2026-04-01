import { useState, useEffect, useMemo } from 'react';
import { useParams, useLocation, Link } from 'react-router-dom';
import {
  Calendar, Clock, User, Phone, Mail, CheckCircle2,
  ShieldCheck, Utensils, ArrowRight, Share2, Info,
  MapPin, ChevronRight, Star, Search, Flame, X, Package,
} from 'lucide-react';
import { bookApi } from '../../api/book.api';
import { posApi } from '../../api/pos.api';
import { authApi } from '../../api/auth.api';
import type { ProductDTO } from '../../api/pos.api';
import styles from './TenantPortalPage.module.scss';

const DAYS_AHEAD = 14;

// ── Datos quemados para enriquecer los productos del API ──────────────────────
const FALLBACK_IMAGES = [
  'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1590080873974-9a3dcac60a17?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1628840042765-356cda07504e?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1525351484163-7529414344d8?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1585238342024-78d387f4a707?auto=format&fit=crop&q=80&w=800',
];
const FALLBACK_TAGS   = ['Popular', 'Más Vendido', 'Nuevo', null, null, null];
const FALLBACK_CATS   = ['Platos', 'Postres', 'Bebidas', 'Desayunos', 'Entradas'];
const FALLBACK_DESCS  = [
  'Preparado con ingredientes frescos seleccionados cada día.',
  'Receta tradicional de la casa con toque especial del chef.',
  'Ingredientes de temporada, sabor auténtico garantizado.',
  'Elaborado artesanalmente con productos locales de calidad.',
];
const MENU_CATEGORIES = ['Todos', 'Platos', 'Postres', 'Bebidas', 'Desayunos', 'Entradas'];

function enrichProduct(p: ProductDTO, index: number) {
  return {
    ...p,
    image:    FALLBACK_IMAGES[index % FALLBACK_IMAGES.length],
    tag:      FALLBACK_TAGS[index % FALLBACK_TAGS.length],
    category: FALLBACK_CATS[index % FALLBACK_CATS.length],
    desc:     FALLBACK_DESCS[index % FALLBACK_DESCS.length],
  };
}
type EnrichedProduct = ReturnType<typeof enrichProduct>;

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

function generateTimeSlots(openTime: string, closeTime: string, slotMinutes: number, takenSlots: Set<string>) {
  const [oh, om] = openTime.split(':').map(Number);
  const [ch, cm] = closeTime.split(':').map(Number);
  const startMin = oh * 60 + om;
  const endMin   = ch * 60 + cm;
  const slots: { time: string; available: boolean }[] = [];
  for (let m = startMin; m < endMin; m += slotMinutes) {
    const h   = Math.floor(m / 60).toString().padStart(2, '0');
    const min = (m % 60).toString().padStart(2, '0');
    const time = `${h}:${min}`;
    slots.push({ time, available: !takenSlots.has(time) });
  }
  return slots;
}

type Step = 1 | 2 | 3;

export const TenantPortalPage = () => {
  const { tenantId = '' } = useParams<{ tenantId: string }>();
  const location = useLocation();
  const isMenuOnly = location.pathname.includes('/book/menu/');

  const [tenantName, setTenantName] = useState('');
  const [openTime,   setOpenTime]   = useState('08:00');
  const [closeTime,  setCloseTime]  = useState('20:00');
  const [slotMin,    setSlotMin]    = useState(60);
  const [menu,       setMenu]       = useState<ProductDTO[]>([]);
  const [menuLoading, setMenuLoading] = useState(true);

  const [calDays]                   = useState(buildCalendarDays);
  const [selectedDay, setSelectedDay] = useState<Date | null>(null);
  const [takenSlots, setTakenSlots]   = useState<Set<string>>(new Set());
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState('');
  const [customDuration, setCustomDuration] = useState('');

  const [step, setStep]             = useState<Step>(1);
  const [form, setForm]             = useState({ name: '', email: '', phone: '', notes: '' });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]           = useState('');
  const [reservationId, setReservationId] = useState('');
  const [copied, setCopied]         = useState(false);

  // ── Estado menú ──────────────────────────────────────────────────────────
  const [menuSearch,   setMenuSearch]   = useState('');
  const [menuCategory, setMenuCategory] = useState('Todos');
  const [detailItem,   setDetailItem]   = useState<EnrichedProduct | null>(null);

  useEffect(() => {
    if (!tenantId) return;
    authApi.getTenantInfo(tenantId).then(t => { if (t) setTenantName(t.name); });
    posApi.getConfig(tenantId).then(c => {
      setOpenTime(c.openTime); setCloseTime(c.closeTime); setSlotMin(c.slotDurationMinutes);
    });
    posApi.getMenu(tenantId).then(setMenu).catch(() => setMenu([]))
      .finally(() => setMenuLoading(false));
  }, [tenantId]);

  const handleDaySelect = async (day: Date) => {
    setSelectedDay(day); setSelectedSlot(''); setTakenSlots(new Set()); setSlotsLoading(true);
    try {
      const available = await bookApi.getAvailability(tenantId, fmtDate(day));
      setTakenSlots(new Set(available.filter(s => !s.available).map(s => s.timeSlot.slice(0, 5))));
    } catch { setTakenSlots(new Set()); }
    finally { setSlotsLoading(false); }
  };

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
      const dur = customDuration ? parseInt(customDuration) : slotMin;
      const res = await bookApi.createReservation(tenantId, {
        customerId: customer.id,
        reservationDate: fmtDate(selectedDay),
        timeSlot: selectedSlot,
        notes: (form.notes ? form.notes + ' ' : '') + (dur && dur !== slotMin ? `[Duración: ${dur} min]` : '') || undefined,
      });
      setReservationId(res.id);
      setStep(3);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear la reserva');
    } finally { setSubmitting(false); }
  };

  const copyLink = () => {
    navigator.clipboard.writeText(window.location.href);
    setCopied(true); setTimeout(() => setCopied(false), 2000);
  };

  const displayName = tenantName || 'Portal de Reservas';

  // ── VISTA: SOLO MENÚ ──────────────────────────────────────────────────────
  if (isMenuOnly) {
    const enriched = menu.map(enrichProduct);
    const filtered = enriched.filter(p =>
      (menuCategory === 'Todos' || p.category === menuCategory) &&
      (!menuSearch || p.name.toLowerCase().includes(menuSearch.toLowerCase()))
    );

    return (
      <div className={styles.menuPage}>
        {/* Header */}
        <header className={styles.menuHeader}>
          <div className={styles.menuHeaderInner}>
            <div className={styles.menuHeaderBrand}>
              <div className={styles.menuHeaderLogo}>{displayName.charAt(0)}</div>
              <div>
                <h1 className={styles.menuHeaderTitle}>{displayName}</h1>
                <p className={styles.menuHeaderSub}>Menú Digital</p>
              </div>
            </div>
            <Link to={`/book/${tenantId}`} className={styles.menuHeaderBtn}>
              <Calendar size={14} /> Reservar Mesa
            </Link>
          </div>
        </header>

        <main className={styles.menuMain}>
          {/* Business card */}
          <div className={styles.menuHeroCard}>
            <div className={styles.menuHeroGlow} />
            <div className={styles.menuHeroBody}>
              <div className={styles.menuHeroLeft}>
                <div className={styles.menuHeroBadges}>
                  <span className={styles.menuOpenBadge}>Abierto</span>
                  <div className={styles.menuStars}>
                    {[...Array(5)].map((_, i) => <Star key={i} size={12} fill="currentColor" />)}
                  </div>
                </div>
                <h2 className={styles.menuHeroTitle}>Experiencia Gastronómica</h2>
                <div className={styles.menuHeroMeta}>
                  <span><MapPin size={13} /> {displayName}</span>
                  <span><Clock size={13} /> {openTime} – {closeTime}</span>
                </div>
              </div>
              <div className={styles.menuHeroStats}>
                <div className={styles.menuHeroStat}><strong>4.9</strong><span>Rating</span></div>
                <div className={styles.menuHeroStat}><strong>+2k</strong><span>Visitas</span></div>
              </div>
            </div>
          </div>

          {/* Search + filters */}
          <div className={styles.menuControls}>
            <div className={styles.menuSearchWrap}>
              <Search size={18} className={styles.menuSearchIcon} />
              <input
                type="text"
                placeholder="Busca tu plato favorito..."
                value={menuSearch}
                onChange={e => setMenuSearch(e.target.value)}
                className={styles.menuSearchInput}
              />
            </div>
            <div className={styles.menuCats}>
              {MENU_CATEGORIES.map(cat => (
                <button
                  key={cat}
                  onClick={() => setMenuCategory(cat)}
                  className={`${styles.menuCatBtn} ${menuCategory === cat ? styles.menuCatBtnActive : ''}`}
                >{cat}</button>
              ))}
            </div>
          </div>

          {/* Grid */}
          {menuLoading ? (
            <p className={styles.loading}>Cargando menú...</p>
          ) : filtered.length === 0 ? (
            <p className={styles.empty}>
              {menu.length === 0 ? 'Este negocio aún no tiene menú publicado.' : 'Sin resultados para tu búsqueda.'}
            </p>
          ) : (
            <div className={styles.menuCardGrid}>
              {filtered.map((item, i) => (
                <div key={item.id} className={styles.menuProductCard}>
                  <div className={styles.menuProductImg}>
                    <img src={item.image} alt={item.name} />
                    <div className={styles.menuProductImgOverlay} />
                    {item.tag && (
                      <div className={styles.menuProductTag}>
                        <Flame size={11} className={styles.menuTagFlame} />
                        <span>{item.tag}</span>
                      </div>
                    )}
                    <span className={styles.menuProductCat}>{item.category}</span>
                  </div>
                  <div className={styles.menuProductBody}>
                    <div className={styles.menuProductTop}>
                      <h3 className={styles.menuProductName}>{item.name}</h3>
                      <span className={styles.menuProductPrice}>
                        ${item.price.toLocaleString('es-CO')}
                      </span>
                    </div>
                    <p className={styles.menuProductDesc}>{item.desc}</p>
                    <button
                      className={styles.menuProductBtn}
                      onClick={() => setDetailItem(item)}
                    >
                      Detalle del plato <ChevronRight size={14} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Info alérgenos */}
          <div className={styles.menuAllergyCard}>
            <div className={styles.menuAllergyIcon}><Info size={28} /></div>
            <div className={styles.menuAllergyText}>
              <h4>Información Alimentaria</h4>
              <p>Nuestros platos se preparan con ingredientes frescos cada día. Si tienes alguna restricción dietética, por favor hazlo saber a nuestro equipo al momento de reservar.</p>
            </div>
            <Link to={`/book/${tenantId}`} className={styles.menuAllergyBtn}>
              Hacer Reserva
            </Link>
          </div>

          <footer className={styles.menuFooter}>
            <div className={styles.menuFooterLogo}>N</div>
            <p>Tecnología por NexaFlow Cloud</p>
          </footer>
        </main>

        {/* Mobile sticky */}
        <div className={styles.menuMobileSticky}>
          <Link to={`/book/${tenantId}`} className={styles.menuMobileStickyBtn}>
            <Calendar size={18} /> Reservar ahora
          </Link>
        </div>

        {/* Modal detalle */}
        {detailItem && (
          <div className={styles.modalBackdrop} onClick={() => setDetailItem(null)}>
            <div className={styles.modalCard} onClick={e => e.stopPropagation()}>
              <button className={styles.modalClose} onClick={() => setDetailItem(null)}>
                <X size={18} />
              </button>
              <div className={styles.modalImg}>
                <img src={detailItem.image} alt={detailItem.name} />
              </div>
              <div className={styles.modalBody}>
                <div className={styles.modalMeta}>
                  <span className={styles.modalCat}>{detailItem.category}</span>
                  {detailItem.tag && (
                    <span className={styles.modalTag}><Flame size={11} /> {detailItem.tag}</span>
                  )}
                </div>
                <h2 className={styles.modalTitle}>{detailItem.name}</h2>
                <p className={styles.modalDesc}>{detailItem.desc}</p>
                <div className={styles.modalDetails}>
                  <div className={styles.modalDetailRow}>
                    <span>Precio</span>
                    <strong>${detailItem.price.toLocaleString('es-CO')}</strong>
                  </div>
                  <div className={styles.modalDetailRow}>
                    <span>Stock disponible</span>
                    <strong className={detailItem.stock <= detailItem.lowStockThreshold ? styles.modalStockLow : ''}>
                      {detailItem.stock > 0 ? `${detailItem.stock} unidades` : 'Agotado'}
                    </strong>
                  </div>
                  <div className={styles.modalDetailRow}>
                    <span>Disponibilidad</span>
                    <strong className={detailItem.active ? styles.modalActive : styles.modalInactive}>
                      {detailItem.active ? 'Disponible' : 'No disponible'}
                    </strong>
                  </div>
                </div>
                <Link to={`/book/${tenantId}`} className={styles.modalReserveBtn}>
                  <Calendar size={16} /> Reservar una mesa
                </Link>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  }

  // ── VISTA: RESERVA ────────────────────────────────────────────────────────
  return (
    <div className={styles.page}>
      {/* Hero header oscuro */}
      <div className={styles.heroHeader}>
        <div className={styles.heroBg} />
        <div className={styles.heroContent}>
          <div className={styles.heroLeft}>
            <div className={styles.businessLogo}>{displayName.charAt(0)}</div>
            <div>
              <div className={styles.heroBadgeRow}>
                <span className={styles.verifiedBadge}>Verificado</span>
                <div className={styles.stars}><Star size={11} fill="currentColor" /><Star size={11} fill="currentColor" /><Star size={11} fill="currentColor" /></div>
              </div>
              <h1 className={styles.heroTitle}>{displayName}</h1>
              <p className={styles.heroSub}><MapPin size={13} /> {openTime} – {closeTime} · Citas cada {slotMin} min</p>
            </div>
          </div>
          <div className={styles.heroActions}>
            <button className={styles.heroBtnIcon} onClick={copyLink} title="Compartir">
              <Share2 size={18} />
              {copied && <span className={styles.copiedToast}>¡Copiado!</span>}
            </button>
            <Link to={`/book/menu/${tenantId}`} className={styles.heroBtnSecondary}>
              Ver Menú <ArrowRight size={14} />
            </Link>
          </div>
        </div>
      </div>

      {/* Grid principal */}
      <main className={styles.main}>
        {/* Columna izquierda — info */}
        <div className={styles.infoCol}>
          <div className={styles.infoCard}>
            <h3 className={styles.infoCardTitle}><Info size={16} className={styles.infoIcon} /> Información</h3>
            <div className={styles.infoItems}>
              <div className={styles.infoItem}>
                <div className={styles.infoItemIcon}><Clock size={15} /></div>
                <div>
                  <p className={styles.infoItemLabel}>Horario de atención</p>
                  <p className={styles.infoItemValue}>{openTime} – {closeTime}</p>
                </div>
              </div>
              <div className={styles.infoItem}>
                <div className={styles.infoItemIcon}><Calendar size={15} /></div>
                <div>
                  <p className={styles.infoItemLabel}>Duración por cita</p>
                  <p className={styles.infoItemValue}>{slotMin} minutos</p>
                </div>
              </div>
              <div className={styles.infoItem}>
                <div className={styles.infoItemIcon}><ShieldCheck size={15} /></div>
                <div>
                  <p className={styles.infoItemLabel}>Política</p>
                  <p className={styles.infoItemValue}>Sin registro requerido</p>
                </div>
              </div>
            </div>
            {selectedDay && selectedSlot && (
              <div className={styles.selectedSummary}>
                <p className={styles.selectedSummaryLabel}>Tu selección</p>
                <p className={styles.selectedSummaryDate}>{fmtLong(selectedDay)}</p>
                <p className={styles.selectedSummaryTime}><Clock size={13} /> {selectedSlot}</p>
              </div>
            )}
          </div>
        </div>

        {/* Columna derecha — stepper */}
        <div className={styles.bookingCol}>
          <div className={styles.bookingCard}>
            {/* Stepper */}
            <div className={styles.stepper}>
              {([
                { n: 1, label: 'Fecha y Hora' },
                { n: 2, label: 'Tus Datos' },
                { n: 3, label: 'Confirmar' },
              ] as { n: Step; label: string }[]).map(s => (
                <div key={s.n} className={`${styles.stepItem} ${step === s.n ? styles.stepActive : ''} ${step > s.n ? styles.stepDone : ''}`}>
                  <div className={styles.stepNum}>
                    {step > s.n ? <CheckCircle2 size={12} /> : s.n}
                  </div>
                  <span className={styles.stepLabel}>{s.label}</span>
                </div>
              ))}
            </div>

            <div className={styles.stepContent}>
              {/* STEP 1 — Fecha y hora */}
              {step === 1 && (
                <div className={styles.stepBody}>
                  <h2 className={styles.stepTitle}>¿Cuándo quieres venir?</h2>
                  <p className={styles.stepSub}>Selecciona el día y la hora que prefieras.</p>

                  <p className={styles.fieldLabel}>Selecciona el día</p>
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
                      <p className={styles.fieldLabel} style={{ marginTop: '1.25rem' }}>
                        Horario disponible — {fmtLong(selectedDay)}
                      </p>
                      {slotsLoading ? (
                        <p className={styles.loading}>Cargando disponibilidad...</p>
                      ) : timeSlots.length === 0 ? (
                        <p className={styles.empty}>No hay horarios configurados.</p>
                      ) : (
                        <div className={styles.slotsGrid}>
                          {timeSlots.map(s => (
                            <button key={s.time} disabled={!s.available}
                              className={`${styles.slot} ${!s.available ? styles.slotTaken : ''} ${selectedSlot === s.time ? styles.slotActive : ''}`}
                              onClick={() => setSelectedSlot(s.time)}>
                              <Clock size={11} /> {s.time}
                            </button>
                          ))}
                        </div>
                      )}

                      {/* Duración personalizada */}
                      {selectedSlot && (
                        <div className={styles.durationSection}>
                          <p className={styles.fieldLabel}>Duración de tu cita (opcional)</p>
                          <div className={styles.durationOptions}>
                            {[slotMin, slotMin * 2, slotMin * 3].map(m => (
                              <button key={m}
                                className={`${styles.durationBtn} ${customDuration === String(m) ? styles.durationBtnActive : ''}`}
                                onClick={() => setCustomDuration(customDuration === String(m) ? '' : String(m))}>
                                {m} min
                              </button>
                            ))}
                            <div className={styles.durationCustomWrap}>
                              <input type="number" min="1" max="480" placeholder="Personalizada"
                                value={customDuration} onChange={e => setCustomDuration(e.target.value)}
                                className={styles.durationInput} />
                              <span className={styles.durationSuffix}>min</span>
                            </div>
                          </div>
                          <p className={styles.durationHint}>
                            Duración base: {slotMin} min. Deja vacío para usar la duración estándar.
                          </p>
                        </div>
                      )}
                    </>
                  )}

                  <button className={styles.btnPrimary} style={{ marginTop: '1.5rem' }}
                    disabled={!selectedDay || !selectedSlot}
                    onClick={() => setStep(2)}>
                    Continuar con mis datos <ChevronRight size={16} />
                  </button>
                </div>
              )}

              {/* STEP 2 — Datos */}
              {step === 2 && (
                <div className={styles.stepBody}>
                  <div className={styles.step2Header}>
                    <div>
                      <h2 className={styles.stepTitle}>Completa tus datos</h2>
                      <p className={styles.stepSub}>Casi terminamos. Necesitamos saber quién eres.</p>
                    </div>
                    {selectedSlot && (
                      <div className={styles.selectedBadge}>
                        <div className={styles.selectedBadgeDot} />
                        <span>{selectedSlot} · {selectedDay ? fmtLong(selectedDay).split(',')[0] : ''}</span>
                      </div>
                    )}
                  </div>

                  <form onSubmit={handleSubmit} className={styles.formFields}>
                    <div className={styles.formGrid}>
                      <div className={styles.inputGroup}>
                        <label>Nombre completo</label>
                        <div className={styles.inputWrap}>
                          <User size={16} className={styles.inputIcon} />
                          <input type="text" placeholder="Juan Pérez" required
                            value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                            className={styles.input} />
                        </div>
                      </div>
                      <div className={styles.inputGroup}>
                        <label>Correo electrónico</label>
                        <div className={styles.inputWrap}>
                          <Mail size={16} className={styles.inputIcon} />
                          <input type="email" placeholder="juan@ejemplo.com" required
                            value={form.email} onChange={e => setForm(p => ({ ...p, email: e.target.value }))}
                            className={styles.input} />
                        </div>
                      </div>
                      <div className={styles.inputGroup}>
                        <label>Teléfono / WhatsApp</label>
                        <div className={styles.inputWrap}>
                          <Phone size={16} className={styles.inputIcon} />
                          <input type="tel" placeholder="+57 300..."
                            value={form.phone} onChange={e => setForm(p => ({ ...p, phone: e.target.value }))}
                            className={styles.input} />
                        </div>
                      </div>
                      <div className={styles.inputGroup}>
                        <label>Notas especiales (opcional)</label>
                        <textarea placeholder="Alergias, preferencia de mesa, etc."
                          value={form.notes} onChange={e => setForm(p => ({ ...p, notes: e.target.value }))}
                          className={styles.textarea} rows={3} />
                      </div>
                    </div>

                    <div className={styles.infoBox}>
                      <ShieldCheck size={18} className={styles.infoBoxIcon} />
                      <p>Al continuar, tu reserva quedará pre-confirmada. Si ya tienes cuenta, se asociará automáticamente. <strong>No requiere registro.</strong></p>
                    </div>

                    {error && <p className={styles.errorMsg}>{error}</p>}

                    <div className={styles.formActions}>
                      <button type="button" className={styles.btnBack} onClick={() => setStep(1)}>
                        ← Atrás
                      </button>
                      <button type="submit" className={styles.btnPrimaryFlex} disabled={submitting}>
                        {submitting ? 'Confirmando...' : <><span>Finalizar Reserva</span><CheckCircle2 size={18} /></>}
                      </button>
                    </div>
                  </form>
                </div>
              )}

              {/* STEP 3 — Confirmación */}
              {step === 3 && (
                <div className={styles.confirmSection}>
                  <div className={styles.confirmIcon}><CheckCircle2 size={48} /></div>
                  <h2 className={styles.confirmTitle}>¡Todo listo, {form.name.split(' ')[0]}!</h2>
                  <p className={styles.confirmSub}>
                    Tu reserva en <strong>{displayName}</strong> ha sido confirmada con éxito.
                  </p>
                  <div className={styles.confirmDetails}>
                    <div className={styles.confirmRow}><span>Fecha</span><strong>{selectedDay ? fmtLong(selectedDay) : ''}</strong></div>
                    <div className={styles.confirmRow}><span>Hora</span><strong>{selectedSlot}</strong></div>
                    {customDuration && <div className={styles.confirmRow}><span>Duración</span><strong>{customDuration} min</strong></div>}
                    <div className={styles.confirmRow}><span>ID</span><strong className={styles.confirmId}>#{reservationId.slice(0, 8).toUpperCase()}</strong></div>
                  </div>
                  <div className={styles.confirmActions}>
                    <button className={styles.btnDark} onClick={() => {
                      setStep(1); setSelectedDay(null); setSelectedSlot(''); setCustomDuration('');
                      setForm({ name: '', email: '', phone: '', notes: '' });
                    }}>
                      Volver al inicio
                    </button>
                    <Link to={`/book/menu/${tenantId}`} className={styles.btnOutline}>
                      <Utensils size={14} /> Ver menú
                    </Link>
                  </div>
                  <p className={styles.poweredBy}>Potenciado por <span>NexaFlow</span></p>
                </div>
              )}
            </div>
          </div>

          <div className={styles.portalFooter}>
            <p>Potenciado por <span className={styles.footerBrand}>NexaFlow</span></p>
            <div className={styles.footerLinks}>
              <a href="#">Términos</a>
              <a href="#">Soporte</a>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
