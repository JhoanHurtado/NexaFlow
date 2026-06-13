/**
 * TenantPortalPage
 *
 * Página pública del portal de un tenant. Accesible sin autenticación.
 * Soporta dos rutas:
 *   - /book/:tenantId        → Vista de reservas (stepper 3 pasos)
 *   - /book/menu/:tenantId   → Vista de menú digital con filtros y modal de detalle
 *
 * Flujo de datos:
 *   1. Al montar: obtiene nombre del tenant (authApi), config de horarios (posApi) y menú (posApi).
 *   2. Vista menú: enriquece productos con datos quemados (imagen, tag, categoría, descripción).
 *   3. Vista reservas:
 *      - Step 1: calendario mensual navegable → selección de día → carga slots disponibles (bookApi).
 *      - Step 2: formulario de datos del cliente.
 *      - Step 3: confirmación con ID de reserva.
 */

import { useState, useEffect, useMemo } from 'react';
import { useParams, useLocation, Link } from 'react-router-dom';
import {
  Calendar, Clock, User, Phone, Mail, CheckCircle2,
  ShieldCheck, Utensils, Share2, Info,
  MapPin, ChevronRight, ChevronLeft, Star, Search, Flame, X,
} from 'lucide-react';
import { bookApi } from '../../api/book.api';
import { posApi } from '../../api/pos.api';
import { authApi } from '../../api/auth.api';
import type { ProductDTO } from '../../api/pos.api';
import styles from './TenantPortalPage.module.scss';

// ─── Datos quemados para enriquecer productos (campos no disponibles en el API) ─
const FALLBACK_IMAGES = [
  'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1628840042765-356cda07504e?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1525351484163-7529414344d8?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1511690656952-34342bb7c2f2?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&q=80&w=800',
  'https://images.unsplash.com/photo-1585238342024-78d387f4a707?auto=format&fit=crop&q=80&w=800',
];
const FALLBACK_TAGS  = ['Popular', 'Más Vendido', 'Nuevo', null, null, null];
const FALLBACK_CATS  = ['Platos', 'Postres', 'Bebidas', 'Desayunos', 'Entradas'];
const FALLBACK_DESCS = [
  'Preparado con ingredientes frescos seleccionados cada día.',
  'Receta tradicional de la casa con toque especial del chef.',
  'Ingredientes de temporada, sabor auténtico garantizado.',
  'Elaborado artesanalmente con productos locales de calidad.',
];
const MENU_CATEGORIES = ['Todos', 'Platos', 'Postres', 'Bebidas', 'Desayunos', 'Entradas'];

/** Combina un ProductDTO del API con datos visuales quemados por índice */
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

// ─── Helpers de fecha ─────────────────────────────────────────────────────────

/** Formatea una Date como "YYYY-MM-DD" para enviar al API */
const fmtDate = (d: Date) => d.toISOString().split('T')[0];

/** Formatea una Date como texto largo en español: "lunes, 5 de mayo" */
const fmtLong = (d: Date) => d.toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' });

/**
 * Construye las celdas del calendario para un mes dado.
 * Devuelve null para las celdas vacías de alineación (lunes como primer columna).
 * Ejemplo: si el mes empieza en miércoles, las dos primeras celdas son null.
 */
function buildMonthCells(year: number, month: number): (Date | null)[] {
  const firstDay    = new Date(year, month, 1);
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const offset      = (firstDay.getDay() + 6) % 7; // 0=Lun … 6=Dom
  const cells: (Date | null)[] = Array(offset).fill(null);
  for (let d = 1; d <= daysInMonth; d++) cells.push(new Date(year, month, d));
  return cells;
}

const MONTH_NAMES = [
  'Enero','Febrero','Marzo','Abril','Mayo','Junio',
  'Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre',
];

/** Etiqueta corta del día de la semana para mostrar dentro de cada celda */
const DAY_LABELS = ['Lun','Mar','Mié','Jue','Vie','Sáb','Dom'];

/** Fecha de hoy a medianoche (para comparar sin hora) */
const today = new Date(); today.setHours(0, 0, 0, 0);

// ─── Generador de slots de tiempo ─────────────────────────────────────────────

/**
 * Genera todos los slots de tiempo entre openTime y closeTime con paso slotMinutes.
 * Marca como no disponibles los que están en takenSlots (obtenidos del API de disponibilidad).
 */
function generateTimeSlots(
  openTime: string, closeTime: string,
  slotMinutes: number, takenSlots: Set<string>,
): { time: string; available: boolean }[] {
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

// ─── Componente principal ─────────────────────────────────────────────────────
export const TenantPortalPage = () => {
  const { tenantId = '' } = useParams<{ tenantId: string }>();
  const location  = useLocation();

  /** true cuando la ruta es /reservar/menu/:tenantId */
  const isMenuOnly = location.pathname.includes('/reservar/menu/');

  // ── Datos del tenant (cargados desde el API al montar) ──────────────────
  const [tenantName,   setTenantName]   = useState('');
  const [openTime,     setOpenTime]     = useState('08:00');  // desde posApi.getConfig
  const [closeTime,    setCloseTime]    = useState('20:00');  // desde posApi.getConfig
  const [slotMin,      setSlotMin]      = useState(60);       // desde posApi.getConfig
  const [menu,         setMenu]         = useState<ProductDTO[]>([]); // desde posApi.getMenu
  const [menuLoading,  setMenuLoading]  = useState(true);

  // ── Estado del calendario mensual ───────────────────────────────────────
  const [calYear,  setCalYear]  = useState(() => today.getFullYear());
  const [calMonth, setCalMonth] = useState(() => today.getMonth());

  // ── Estado de selección de fecha/hora ───────────────────────────────────
  const [selectedDay,   setSelectedDay]   = useState<Date | null>(null);
  const [takenSlots,    setTakenSlots]    = useState<Set<string>>(new Set()); // desde bookApi.getAvailability
  const [slotsLoading,  setSlotsLoading]  = useState(false);
  const [selectedSlot,  setSelectedSlot]  = useState('');
  const [customDuration, setCustomDuration] = useState('');

  // ── Estado del stepper de reserva ───────────────────────────────────────
  const [step,          setStep]          = useState<Step>(1);
  const [form,          setForm]          = useState({ name: '', email: '', phone: '', notes: '' });
  const [submitting,    setSubmitting]    = useState(false);
  const [error,         setError]         = useState('');
  const [reservationId, setReservationId] = useState(''); // ID devuelto por bookApi.createReservation
  const [copied,        setCopied]        = useState(false);

  // ── Estado del menú digital ─────────────────────────────────────────────
  const [menuSearch,   setMenuSearch]   = useState('');
  const [menuCategory, setMenuCategory] = useState('Todos');
  const [detailItem,   setDetailItem]   = useState<EnrichedProduct | null>(null);

  // ── Carga inicial: nombre del tenant, config de horarios y menú ─────────
  useEffect(() => {
    if (!tenantId) return;
    // Nombre del negocio → authApi (NexaAuth_Billing /tenants/:id)
    authApi.getTenantInfo(tenantId).then(t => { if (t) setTenantName(t.name); });
    // Horarios y duración de slot → posApi (NexaPOS /config)
    posApi.getConfig(tenantId).then(c => {
      setOpenTime(c.openTime);
      setCloseTime(c.closeTime);
      setSlotMin(c.slotDurationMinutes);
    });
    // Productos del menú → posApi (NexaPOS /products)
    posApi.getMenu(tenantId)
      .then(setMenu)
      .catch(() => setMenu([]))
      .finally(() => setMenuLoading(false));
  }, [tenantId]);

  // ── Celdas del calendario: recalcula al cambiar mes/año ─────────────────
  const calCells = useMemo(() => buildMonthCells(calYear, calMonth), [calYear, calMonth]);

  // ── Navegación del calendario ────────────────────────────────────────────
  const prevMonth = () => {
    if (calMonth === 0) { setCalMonth(11); setCalYear(y => y - 1); }
    else setCalMonth(m => m - 1);
  };
  const nextMonth = () => {
    if (calMonth === 11) { setCalMonth(0); setCalYear(y => y + 1); }
    else setCalMonth(m => m + 1);
  };
  /** No se puede retroceder al mes anterior al mes actual */
  const canGoPrev = calYear > today.getFullYear() || calMonth > today.getMonth();

  /**
   * Al seleccionar un día:
   * 1. Actualiza el día seleccionado y limpia el slot.
   * 2. Consulta bookApi.getAvailability para obtener los slots ocupados.
   */
  const handleDaySelect = async (day: Date) => {
    setSelectedDay(day);
    setSelectedSlot('');
    setTakenSlots(new Set());
    setSlotsLoading(true);
    try {
      const available = await bookApi.getAvailability(tenantId, fmtDate(day));
      setTakenSlots(new Set(available.filter(s => !s.available).map(s => s.timeSlot.slice(0, 5))));
    } catch {
      setTakenSlots(new Set());
    } finally {
      setSlotsLoading(false);
    }
  };

  /** Slots del día seleccionado: recalcula cuando cambia el día, horarios o slots ocupados */
  const timeSlots = useMemo(() =>
    selectedDay ? generateTimeSlots(openTime, closeTime, slotMin, takenSlots) : [],
    [selectedDay, openTime, closeTime, slotMin, takenSlots],
  );

  /**
   * Envío del formulario (Step 2 → Step 3):
   * 1. findOrCreateCustomer → obtiene o crea el cliente en NexaBook.
   * 2. createReservation → crea la reserva y guarda el ID para mostrar en confirmación.
   */
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
        customerId:      customer.id,
        reservationDate: fmtDate(selectedDay),
        timeSlot:        selectedSlot,
        notes: (form.notes ? form.notes + ' ' : '') +
               (dur && dur !== slotMin ? `[Duración: ${dur} min]` : '') || undefined,
      });
      setReservationId(res.id);
      setStep(3);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al crear la reserva');
    } finally {
      setSubmitting(false);
    }
  };

  /** Copia la URL actual al portapapeles y muestra toast temporal */
  const copyLink = () => {
    navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const displayName = tenantName || 'Portal de Reservas';

  // ── VISTA: SOLO MENÚ (/book/menu/:tenantId) ───────────────────────────────
  if (isMenuOnly) {
    // Enriquece cada producto con datos visuales quemados
    const enriched = menu.map(enrichProduct);
    // Filtra por categoría y búsqueda de texto
    const filtered = enriched.filter(p =>
      (menuCategory === 'Todos' || p.category === menuCategory) &&
      (!menuSearch || p.name.toLowerCase().includes(menuSearch.toLowerCase()))
    );

    return (
      <div className={styles.menuPage}>
        {/* Header sticky con logo del negocio y botón de reserva */}
        <header className={styles.menuHeader}>
          <div className={styles.menuHeaderInner}>
            <div className={styles.menuHeaderBrand}>
              <div className={styles.menuHeaderLogo}>{displayName.charAt(0)}</div>
              <div>
                <h1 className={styles.menuHeaderTitle}>{displayName}</h1>
                <p className={styles.menuHeaderSub}>Menú Digital</p>
              </div>
            </div>
            <Link to={`/reservar/${tenantId}`} className={styles.menuHeaderBtn}>
              <Calendar size={14} /> Reservar Mesa
            </Link>
          </div>
        </header>

        <main className={styles.menuMain}>
          {/* Hero card oscuro con stats del negocio */}
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

          {/* Barra de búsqueda + filtros de categoría */}
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
                <button key={cat} onClick={() => setMenuCategory(cat)}
                  className={`${styles.menuCatBtn} ${menuCategory === cat ? styles.menuCatBtnActive : ''}`}>
                  {cat}
                </button>
              ))}
            </div>
          </div>

          {/* Grid de productos */}
          {menuLoading ? (
            <p className={styles.loading}>Cargando menú...</p>
          ) : filtered.length === 0 ? (
            <p className={styles.empty}>
              {menu.length === 0 ? 'Este negocio aún no tiene menú publicado.' : 'Sin resultados para tu búsqueda.'}
            </p>
          ) : (
            <div className={styles.menuCardGrid}>
              {filtered.map((item) => (
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
                      <span className={styles.menuProductPrice}>${item.price.toLocaleString('es-CO')}</span>
                    </div>
                    <p className={styles.menuProductDesc}>{item.desc}</p>
                    <button className={styles.menuProductBtn} onClick={() => setDetailItem(item)}>
                      Detalle del plato <ChevronRight size={14} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Sección de información alimentaria */}
          <div className={styles.menuAllergyCard}>
            <div className={styles.menuAllergyIcon}><Info size={28} /></div>
            <div className={styles.menuAllergyText}>
              <h4>Información Alimentaria</h4>
              <p>Nuestros platos se preparan con ingredientes frescos cada día. Si tienes alguna restricción dietética, por favor hazlo saber a nuestro equipo al momento de reservar.</p>
            </div>
            <Link to={`/reservar/${tenantId}`} className={styles.menuAllergyBtn}>Hacer Reserva</Link>
          </div>

          <footer className={styles.menuFooter}>
            <div className={styles.menuFooterLogo}>N</div>
            <p>Tecnología por NexaFlow Cloud</p>
          </footer>
        </main>

        {/* Botón sticky en mobile para ir a reservar */}
        <div className={styles.menuMobileSticky}>
          <Link to={`/reservar/${tenantId}`} className={styles.menuMobileStickyBtn}>
            <Calendar size={18} /> Reservar ahora
          </Link>
        </div>

        {/* Modal de detalle del plato */}
        {detailItem && (
          <div className={styles.modalBackdrop} onClick={() => setDetailItem(null)}>
            <div className={styles.modalCard} onClick={e => e.stopPropagation()}>
              <button className={styles.modalClose} onClick={() => setDetailItem(null)}><X size={18} /></button>
              <div className={styles.modalImg}>
                <img src={detailItem.image} alt={detailItem.name} />
              </div>
              <div className={styles.modalBody}>
                <div className={styles.modalMeta}>
                  <span className={styles.modalCat}>{detailItem.category}</span>
                  {detailItem.tag && <span className={styles.modalTag}><Flame size={11} /> {detailItem.tag}</span>}
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
                <Link to={`/reservar/${tenantId}`} className={styles.modalReserveBtn}>
                  <Calendar size={16} /> Reservar una mesa
                </Link>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  }

  // ── VISTA: RESERVAS (/book/:tenantId) ─────────────────────────────────────
  return (
    <div className={styles.bookPage}>
      {/* Header sticky: logo + botón "Ver Menú" + compartir */}
      <header className={styles.menuHeader}>
        <div className={styles.menuHeaderInner}>
          <div className={styles.menuHeaderBrand}>
            <div className={styles.menuHeaderLogo}>{displayName.charAt(0)}</div>
            <div>
              <h1 className={styles.menuHeaderTitle}>{displayName}</h1>
              <p className={styles.menuHeaderSub}>Portal de Reservas</p>
            </div>
          </div>
          <div className={styles.bookHeaderRight}>
            <button className={styles.bookShareBtn} onClick={copyLink} title="Compartir">
              <Share2 size={16} />
              {copied && <span className={styles.copiedToast}>¡Copiado!</span>}
            </button>
            <Link to={`/reservar/menu/${tenantId}`} className={styles.menuHeaderBtn}>
              <Utensils size={14} /> Ver Menú
            </Link>
          </div>
        </div>
      </header>

      <main className={styles.bookMain}>
        {/* Hero card oscuro con horario y stats */}
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
              <h2 className={styles.menuHeroTitle}>Reserva tu mesa</h2>
              <div className={styles.menuHeroMeta}>
                <span><MapPin size={13} /> {displayName}</span>
                <span><Clock size={13} /> {openTime} – {closeTime}</span>
                <span><Calendar size={13} /> Citas cada {slotMin} min</span>
              </div>
            </div>
            <div className={styles.menuHeroStats}>
              <div className={styles.menuHeroStat}><strong>4.9</strong><span>Rating</span></div>
              <div className={styles.menuHeroStat}><strong>{slotMin}m</strong><span>Por cita</span></div>
            </div>
          </div>
        </div>

        {/* Layout dos columnas: info lateral + stepper */}
        <div className={styles.bookGrid}>

          {/* Columna izquierda: info del negocio + resumen de selección */}
          <div className={styles.bookInfoCol}>
            <div className={styles.bookInfoCard}>
              <h3 className={styles.bookInfoTitle}><Info size={16} /> Información</h3>
              <div className={styles.bookInfoItems}>
                <div className={styles.bookInfoItem}>
                  <div className={styles.bookInfoIcon}><Clock size={15} /></div>
                  <div>
                    <p className={styles.bookInfoLabel}>Horario</p>
                    <p className={styles.bookInfoValue}>{openTime} – {closeTime}</p>
                  </div>
                </div>
                <div className={styles.bookInfoItem}>
                  <div className={styles.bookInfoIcon}><Calendar size={15} /></div>
                  <div>
                    <p className={styles.bookInfoLabel}>Duración por cita</p>
                    <p className={styles.bookInfoValue}>{slotMin} minutos</p>
                  </div>
                </div>
                <div className={styles.bookInfoItem}>
                  <div className={styles.bookInfoIcon}><ShieldCheck size={15} /></div>
                  <div>
                    <p className={styles.bookInfoLabel}>Política</p>
                    <p className={styles.bookInfoValue}>Sin registro requerido</p>
                  </div>
                </div>
              </div>
              {/* Resumen de selección: aparece cuando hay día y slot elegidos */}
              {selectedDay && selectedSlot && (
                <div className={styles.bookSelectionSummary}>
                  <p className={styles.bookSelectionLabel}>Tu selección</p>
                  <p className={styles.bookSelectionDate}>{fmtLong(selectedDay)}</p>
                  <p className={styles.bookSelectionTime}><Clock size={13} /> {selectedSlot}</p>
                </div>
              )}
            </div>
          </div>

          {/* Columna derecha: stepper con los 3 pasos */}
          <div className={styles.bookStepperCol}>
            <div className={styles.bookStepperCard}>

              {/* Indicador de progreso (stepper) */}
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

                {/* ── STEP 1: Selección de fecha y hora ── */}
                {step === 1 && (
                  <div className={styles.stepBody}>
                    <h2 className={styles.stepTitle}>¿Cuándo quieres venir?</h2>
                    <p className={styles.stepSub}>Selecciona el día y la hora que prefieras.</p>

                    <p className={styles.fieldLabel}>Selecciona el día</p>

                    {/* Navegación del mes */}
                    <div className={styles.calNav}>
                      <button className={styles.calNavBtn} onClick={prevMonth} disabled={!canGoPrev}>
                        <ChevronLeft size={15} />
                      </button>
                      <span className={styles.calNavTitle}>{MONTH_NAMES[calMonth]} {calYear}</span>
                      <button className={styles.calNavBtn} onClick={nextMonth}>
                        <ChevronRight size={15} />
                      </button>
                    </div>

                    {/* Encabezados de días de la semana */}
                    <div className={styles.calHeader}>
                      {['Lun','Mar','Mié','Jue','Vie','Sáb','Dom'].map(d => (
                        <span key={d} className={styles.calHeaderDay}>{d}</span>
                      ))}
                    </div>

                    {/* Grid del calendario mensual */}
                    <div className={styles.calStrip}>
                      {calCells.map((d, i) => {
                        if (!d) return <div key={`e-${i}`} />;
                        const isPast     = d < today;
                        const isSelected = selectedDay ? fmtDate(selectedDay) === fmtDate(d) : false;
                        // Índice del día de la semana (0=Lun … 6=Dom)
                        const dayIdx     = (d.getDay() + 6) % 7;
                        const isWeekend  = dayIdx === 5 || dayIdx === 6; // Sáb o Dom
                        const isFriday   = dayIdx === 4;
                        return (
                          <button
                            key={fmtDate(d)}
                            disabled={isPast}
                            className={[
                              styles.calDay,
                              isSelected  ? styles.calDayActive  : '',
                              isPast      ? styles.calDayPast    : '',
                              isWeekend   ? styles.calDayWeekend : '',
                              isFriday    ? styles.calDayFriday  : '',
                            ].join(' ')}
                            onClick={() => handleDaySelect(d)}
                          >
                            <span className={styles.calDayNum}>{d.getDate()}</span>
                            {/* Etiqueta del día: Vie / Sáb / Dom o abreviatura normal */}
                            <span className={styles.calDayLabel}>{DAY_LABELS[dayIdx]}</span>
                          </button>
                        );
                      })}
                    </div>

                    {/* Slots de hora: aparecen al seleccionar un día */}
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

                        {/* Duración personalizada: aparece al seleccionar un slot */}
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
                            <p className={styles.durationHint}>Duración base: {slotMin} min.</p>
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

                {/* ── STEP 2: Datos del cliente ── */}
                {step === 2 && (
                  <div className={styles.stepBody}>
                    <div className={styles.step2Header}>
                      <div>
                        <h2 className={styles.stepTitle}>Completa tus datos</h2>
                        <p className={styles.stepSub}>Casi terminamos. Necesitamos saber quién eres.</p>
                      </div>
                      {/* Badge con el slot seleccionado */}
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
                        <p>Al continuar, tu reserva quedará pre-confirmada. <strong>No requiere registro.</strong></p>
                      </div>
                      {error && <p className={styles.errorMsg}>{error}</p>}
                      <div className={styles.formActions}>
                        <button type="button" className={styles.btnBack} onClick={() => setStep(1)}>← Atrás</button>
                        <button type="submit" className={styles.btnPrimaryFlex} disabled={submitting}>
                          {submitting ? 'Confirmando...' : <><span>Finalizar Reserva</span><CheckCircle2 size={18} /></>}
                        </button>
                      </div>
                    </form>
                  </div>
                )}

                {/* ── STEP 3: Confirmación ── */}
                {step === 3 && (
                  <div className={styles.confirmSection}>
                    <div className={styles.confirmIcon}><CheckCircle2 size={48} /></div>
                    <h2 className={styles.confirmTitle}>¡Todo listo, {form.name.split(' ')[0]}!</h2>
                    <p className={styles.confirmSub}>Tu reserva en <strong>{displayName}</strong> ha sido confirmada.</p>
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
                      }}>Volver al inicio</button>
                      <Link to={`/reservar/menu/${tenantId}`} className={styles.btnOutline}>
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
              <div className={styles.footerLinks}><a href="#">Términos</a><a href="#">Soporte</a></div>
            </div>
          </div>
        </div>
      </main>

      {/* Botón sticky en mobile para ver el menú */}
      <div className={styles.menuMobileSticky}>
        <Link to={`/reservar/menu/${tenantId}`} className={styles.menuMobileStickyBtn}>
          <Utensils size={18} /> Ver el menú
        </Link>
      </div>
    </div>
  );
};
