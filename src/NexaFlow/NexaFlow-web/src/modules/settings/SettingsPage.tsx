import { useState, useEffect } from 'react';
import { Settings, Save, RefreshCw, CheckCircle2, AlertTriangle, Clock, DollarSign, Calendar } from 'lucide-react';
import { posApi } from '../../api/pos.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './SettingsPage.module.scss';
import { formatValue } from '../../utils/formatters';

const CURRENCIES = ['COP', 'USD', 'EUR', 'MXN', 'ARS', 'PEN', 'CLP', 'BRL'];
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
    if (isNaN(rate) || rate < 0 || rate > 100) { setError('La tasa de IVA debe ser un número entre 0 y 100.'); return; }
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
  console.log(subtotal);
  
  const taxAmount = Math.round(subtotal * parseFloat(taxRate || '0') / 100 * 100) / 100;
  console.log(taxAmount);
  
  const total     = subtotal + taxAmount;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div className={styles.headerIcon}><Settings size={22} /></div>
        <div>
          <h1>Configuración del Negocio</h1>
          <p>Ajusta los parámetros fiscales y operativos de tu tenant.
            {updatedAt && <span className={styles.lastUpdate}> · Última actualización: {new Date(updatedAt).toLocaleString('es-ES')}</span>}
          </p>
        </div>
      </div>

      {loading ? (
        <div className={styles.loadingState}><RefreshCw size={20} className={styles.spin} /> Cargando configuración...</div>
      ) : (
        <form onSubmit={handleSave} className={styles.form}>

          {/* Fiscal */}
          <div className={styles.section}>
            <div className={styles.sectionTitleRow}>
              <DollarSign size={16} className={styles.sectionIcon} />
              <h2 className={styles.sectionTitle}>Configuración Fiscal</h2>
            </div>
            <p className={styles.sectionDesc}>Tasa de IVA y moneda aplicados a todas las ventas.</p>
            <div className={styles.fields}>
              <div className={styles.field}>
                <label>Tasa de IVA (%)</label>
                <div className={styles.inputWrap}>
                  <input type="number" min="0" max="100" step="0.01"
                    value={taxRate} onChange={e => setTaxRate(e.target.value)}
                    className={styles.input} required />
                  <span className={styles.inputSuffix}>%</span>
                </div>
                <p className={styles.fieldHint}>Ej: 19 (Colombia), 16 (México), 21 (España), 0 (sin IVA)</p>
              </div>
              <div className={styles.field}>
                <label>Moneda</label>
                <select value={currency} onChange={e => setCurrency(e.target.value)} className={styles.select}>
                  {CURRENCIES.map(c => <option key={c} value={c}>{c}</option>)}
                </select>
                <p className={styles.fieldHint}>Moneda en la que se expresan precios y totales.</p>
              </div>
            </div>
          </div>

          {/* Horario */}
          <div className={styles.section}>
            <div className={styles.sectionTitleRow}>
              <Clock size={16} className={styles.sectionIcon} />
              <h2 className={styles.sectionTitle}>Horario de Funcionamiento</h2>
            </div>
            <p className={styles.sectionDesc}>Define el horario en que tu negocio acepta reservas.</p>
            <div className={styles.fields}>
              <div className={styles.field}>
                <label>Hora de apertura</label>
                <input type="time" value={openTime} onChange={e => setOpenTime(e.target.value)} className={styles.input} required />
                <p className={styles.fieldHint}>Primera hora disponible para reservas.</p>
              </div>
              <div className={styles.field}>
                <label>Hora de cierre</label>
                <input type="time" value={closeTime} onChange={e => setCloseTime(e.target.value)} className={styles.input} required />
                <p className={styles.fieldHint}>Última hora disponible para reservas.</p>
              </div>
            </div>
          </div>

          {/* Reservas */}
          <div className={styles.section}>
            <div className={styles.sectionTitleRow}>
              <Calendar size={16} className={styles.sectionIcon} />
              <h2 className={styles.sectionTitle}>Configuración de Reservas</h2>
            </div>
            <p className={styles.sectionDesc}>Duración de cada slot de tiempo para las citas.</p>
            <div className={styles.fields}>
              <div className={styles.field}>
                <label>Duración del slot (minutos)</label>
                <select value={slotMin} onChange={e => setSlotMin(e.target.value)} className={styles.select}>
                  {SLOT_OPTIONS.map(m => <option key={m} value={m}>{m} min</option>)}
                </select>
                <p className={styles.fieldHint}>
                  Con {slotMin} min y horario {openTime}–{closeTime} hay{' '}
                  <strong>{Math.floor((parseInt(closeTime.split(':')[0]) * 60 + parseInt(closeTime.split(':')[1]) - parseInt(openTime.split(':')[0]) * 60 - parseInt(openTime.split(':')[1])) / parseInt(slotMin))}</strong> slots disponibles por día.
                </p>
              </div>
            </div>
          </div>

          {/* Preview fiscal */}
          <div className={styles.preview}>
            <p className={styles.previewLabel}>Vista previa — venta de $100.00</p>
            <div className={styles.previewRows}>
              <div className={styles.previewRow}><span>Subtotal</span><span>$100</span></div>
              <div className={styles.previewRow}><span>IVA ({taxRate || 0}%)</span><span>{taxAmount}</span></div>
              <div className={`${styles.previewRow} ${styles.previewTotal}`}>
                <span>Total</span>
                <strong>{formatValue(total)}</strong>
              </div>
            </div>
          </div>

          {error   && <div className={styles.errorMsg}><AlertTriangle size={15} /> {error}</div>}
          {success && <div className={styles.successMsg}><CheckCircle2 size={15} /> {success}</div>}

          <div className={styles.actions}>
            <button type="submit" className={styles.btnSave} disabled={saving}>
              {saving
                ? <><RefreshCw size={15} className={styles.spin} /> Guardando...</>
                : <><Save size={15} /> Guardar cambios</>}
            </button>
          </div>
        </form>
      )}
    </div>
  );
};
