import { useState, useEffect } from 'react';
import { Settings, Save, RefreshCw, CheckCircle2, DollarSign, Clock, Sparkles, Link2, Copy, ExternalLink } from 'lucide-react';
import { posApi } from '../../api/pos.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './SettingsPage.module.scss';

const CURRENCIES = [
  { value: 'COP', label: 'COP - Peso Colombiano' },
  { value: 'USD', label: 'USD - Dólar Estadounidense' },
  { value: 'EUR', label: 'EUR - Euro' },
  { value: 'MXN', label: 'MXN - Peso Mexicano' },
  { value: 'ARS', label: 'ARS - Peso Argentino' },
  { value: 'PEN', label: 'PEN - Sol Peruano' },
  { value: 'CLP', label: 'CLP - Peso Chileno' },
  { value: 'BRL', label: 'BRL - Real Brasileño' },
];
const SLOT_OPTIONS = [15, 20, 30, 45, 60, 90, 120];

export const SettingsPage = () => {
  const { tenantId } = useTenant();

  const [taxRate,   setTaxRate]   = useState('19');
  const [currency,  setCurrency]  = useState('COP');
  const [slotMin,   setSlotMin]   = useState('60');
  const [openTime,  setOpenTime]  = useState('08:00');
  const [closeTime, setCloseTime] = useState('20:00');
  const [updatedAt, setUpdatedAt] = useState<string | undefined>();

  const [loading, setLoading] = useState(true);
  const [saving,  setSaving]  = useState(false);
  const [success, setSuccess] = useState('');
  const [error,   setError]   = useState('');

  useEffect(() => {
    if (!tenantId) return;
    posApi.getConfig(tenantId)
      .then(c => {
        setTaxRate(String(c.taxRate));
        setCurrency(c.currency);
        setSlotMin(String(c.slotDurationMinutes));
        setOpenTime(c.openTime);
        setCloseTime(c.closeTime);
        setUpdatedAt(c.updatedAt);
      })
      .catch(() => setError('No se pudo cargar la configuración.'))
      .finally(() => setLoading(false));
  }, [tenantId]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const rate = parseFloat(taxRate);
    if (isNaN(rate) || rate < 0 || rate > 100) { setError('La tasa de IVA debe ser entre 0 y 100.'); return; }
    if (openTime >= closeTime) { setError('La hora de apertura debe ser anterior a la de cierre.'); return; }
    setSaving(true); setError(''); setSuccess('');
    try {
      const updated = await posApi.updateConfig(tenantId, {
        taxRate: rate, currency,
        slotDurationMinutes: parseInt(slotMin),
        openTime, closeTime,
      });
      setUpdatedAt(updated.updatedAt);
      setSuccess('Configuración guardada correctamente.');
      setTimeout(() => setSuccess(''), 4000);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al guardar la configuración.');
    } finally { setSaving(false); }
  };

  const subtotal  = 100;
  const taxAmount = Math.round(subtotal * parseFloat(taxRate || '0') / 100 * 100) / 100;
  const total     = subtotal + taxAmount;

  const slotsCount = (() => {
    try {
      const [oh, om] = openTime.split(':').map(Number);
      const [ch, cm] = closeTime.split(':').map(Number);
      const mins = (ch * 60 + cm) - (oh * 60 + om);
      return Math.max(0, Math.floor(mins / parseInt(slotMin || '60')));
    } catch { return 0; }
  })();

  // ── URLs públicas del portal ──────────────────────────────────────────────
  const baseUrl    = window.location.origin;
  const bookingUrl = `${baseUrl}/book/${tenantId}`;
  const menuUrl    = `${baseUrl}/book/menu/${tenantId}`;
  const [copiedBooking, setCopiedBooking] = useState(false);
  const [copiedMenu,    setCopiedMenu]    = useState(false);

  const copyUrl = (url: string, which: 'booking' | 'menu') => {
    navigator.clipboard.writeText(url);
    if (which === 'booking') { setCopiedBooking(true); setTimeout(() => setCopiedBooking(false), 2000); }
    else                     { setCopiedMenu(true);    setTimeout(() => setCopiedMenu(false),    2000); }
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <div className={styles.pageHeaderIcon}><Settings size={20} /></div>
        <div>
          <h1>Configuración del Negocio</h1>
          <p>
            Parámetros fiscales y operativos de tu tenant.
            {updatedAt && <span className={styles.lastUpdate}> · Actualizado: {new Date(updatedAt).toLocaleString('es-ES')}</span>}
          </p>
        </div>
      </div>

      {loading ? (
        <div className={styles.loadingState}><RefreshCw size={18} className={styles.spin} /> Cargando configuración...</div>
      ) : (
        <form onSubmit={handleSave} className={styles.form}>
          {/* Fila 1: Fiscal + Horario */}
          <div className={styles.grid}>
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <DollarSign size={18} className={styles.cardIcon} />
                <h2>Configuración Fiscal</h2>
              </div>
              <p className={styles.cardDesc}>Tasa de IVA y moneda aplicados a todas las ventas.</p>
              <div className={styles.fields}>
                <div className={styles.field}>
                  <label>Tasa de IVA (%)</label>
                  <div className={styles.inputWrap}>
                    <input type="number" min="0" max="100" step="0.01"
                      value={taxRate} onChange={e => setTaxRate(e.target.value)}
                      className={styles.input} required />
                    <span className={styles.suffix}>%</span>
                  </div>
                  <p className={styles.hint}>Ej: 19 (Colombia), 16 (México), 21 (España), 0 (sin IVA)</p>
                </div>
                <div className={styles.field}>
                  <label>Moneda</label>
                  <select value={currency} onChange={e => setCurrency(e.target.value)} className={styles.select}>
                    {CURRENCIES.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
                  </select>
                  <p className={styles.hint}>Moneda en la que se expresan precios y totales.</p>
                </div>
              </div>
            </div>

            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <Clock size={18} className={styles.cardIcon} />
                <h2>Horario de Funcionamiento</h2>
              </div>
              <p className={styles.cardDesc}>Define cuándo tu negocio acepta reservas.</p>
              <div className={styles.fields}>
                <div className={styles.field}>
                  <label>Hora de apertura</label>
                  <input type="time" value={openTime} onChange={e => setOpenTime(e.target.value)} className={styles.input} required />
                </div>
                <div className={styles.field}>
                  <label>Hora de cierre</label>
                  <input type="time" value={closeTime} onChange={e => setCloseTime(e.target.value)} className={styles.input} required />
                </div>
                <div className={`${styles.field} ${styles.fieldFull}`}>
                  <label>Duración del slot (minutos)</label>
                  <select value={slotMin} onChange={e => setSlotMin(e.target.value)} className={styles.select}>
                    {SLOT_OPTIONS.map(m => <option key={m} value={m}>{m} min</option>)}
                  </select>
                  <p className={styles.hint}>
                    Con {slotMin} min y horario {openTime}–{closeTime} hay{' '}
                    <strong>{slotsCount}</strong> slots disponibles por día.
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Fila 2: Enlaces + Preview */}
          <div className={styles.gridBottom}>
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <Link2 size={18} className={styles.cardIcon} />
                <h2>Enlaces Públicos</h2>
              </div>
              <p className={styles.cardDesc}>Comparte estas URLs con tus clientes para que puedan reservar o ver el menú.</p>
              <div className={styles.linksList}>
                {[
                  { label: 'Portal de Reservas', url: bookingUrl, copied: copiedBooking, which: 'booking' as const },
                  { label: 'Menú Digital',       url: menuUrl,    copied: copiedMenu,    which: 'menu'    as const },
                ].map(({ label, url, copied, which }) => (
                  <div key={which} className={styles.linkRow}>
                    <div className={styles.linkInfo}>
                      <span className={styles.linkLabel}>{label}</span>
                      <span className={styles.linkUrl}>{url}</span>
                    </div>
                    <div className={styles.linkActions}>
                      <button type="button" className={styles.linkBtn} onClick={() => copyUrl(url, which)} title="Copiar">
                        {copied ? <CheckCircle2 size={15} /> : <Copy size={15} />}
                        {copied ? 'Copiado' : 'Copiar'}
                      </button>
                      <a href={url} target="_blank" rel="noopener noreferrer" className={styles.linkBtn} title="Abrir">
                        <ExternalLink size={15} /> Abrir
                      </a>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className={styles.preview}>
              <div className={styles.previewHeader}>
                <span className={styles.previewLabel}>Vista Previa — Venta de $100.00</span>
                <Sparkles size={14} className={styles.previewIcon} />
              </div>
              <div className={styles.previewRows}>
                <div className={styles.previewRow}>
                  <span>Subtotal</span>
                  <span>$100.00 {currency}</span>
                </div>
                <div className={styles.previewRow}>
                  <span>IVA ({taxRate || 0}%)</span>
                  <span>${taxAmount.toFixed(2)} {currency}</span>
                </div>
                <div className={styles.previewTotal}>
                  <span>Total</span>
                  <strong>${total.toFixed(2)} <span className={styles.previewCurrency}>{currency}</span></strong>
                </div>
              </div>
            </div>
          </div>

          {error   && <p className={styles.errorMsg}>{error}</p>}
          {success && <p className={styles.successMsg}><CheckCircle2 size={14} /> {success}</p>}

          <div className={styles.actions}>
            <button type="submit" className={styles.btnSave} disabled={saving}>
              {saving
                ? <><RefreshCw size={14} className={styles.spin} /> Guardando...</>
                : <><Save size={14} /> Guardar cambios</>}
            </button>
          </div>
        </form>
      )}
    </div>
  );
};
