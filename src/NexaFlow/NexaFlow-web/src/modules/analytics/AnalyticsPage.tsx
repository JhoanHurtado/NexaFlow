import { useState, useCallback } from 'react';
import { insightApi, mlApi } from '../../api/insight.api';
import type { DailySummaryDTO, ForecastDTO, AnomalyDTO, MLInsightDTO } from '../../api/insight.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './AnalyticsPage.module.scss';
import { BarChart3, TrendingUp, AlertTriangle, Sparkles, RefreshCw } from 'lucide-react';

const fmt = (d: Date) => d.toISOString().split('T')[0];
const thirtyDaysAgo = () => fmt(new Date(Date.now() - 30 * 86400000));
const today = () => fmt(new Date());

export const AnalyticsPage = () => {
  const { tenantId } = useTenant();

  const [from, setFrom] = useState(thirtyDaysAgo());
  const [to, setTo]     = useState(today());

  const [avgTicket,    setAvgTicket]    = useState<{ average: number; totalRevenue: number; saleCount: number } | null>(null);
  const [cancelRate,   setCancelRate]   = useState<{ ratePercent: number; totalReservations: number; cancelledReservations: number } | null>(null);
  const [dailySummary, setDailySummary] = useState<DailySummaryDTO[]>([]);
  const [forecast,     setForecast]     = useState<ForecastDTO[]>([]);
  const [anomalies,    setAnomalies]    = useState<AnomalyDTO[]>([]);
  const [mlInsight,    setMlInsight]    = useState<MLInsightDTO | null>(null);

  const [loading, setLoading] = useState<Record<string, boolean>>({});
  const [errors,  setErrors]  = useState<Record<string, string>>({});

  const run = useCallback(async <T,>(key: string, fn: () => Promise<T>, setter: (v: T) => void) => {
    setLoading(p => ({ ...p, [key]: true }));
    setErrors(p => ({ ...p, [key]: '' }));
    try { setter(await fn()); }
    catch (e: unknown) { setErrors(p => ({ ...p, [key]: e instanceof Error ? e.message : 'Error' })); }
    finally { setLoading(p => ({ ...p, [key]: false })); }
  }, []);

  const loadAll = useCallback(() => {
    if (!tenantId) return;
    run('ticket',   () => insightApi.getAverageTicket(tenantId, from, to),   r => setAvgTicket((r as { data: { average: number; totalRevenue: number; saleCount: number } }).data ?? (r as unknown as { average: number; totalRevenue: number; saleCount: number })));
    run('cancel',   () => insightApi.getCancellationRate(tenantId, from, to), r => setCancelRate((r as { data: { ratePercent: number; totalReservations: number; cancelledReservations: number } }).data ?? (r as unknown as { ratePercent: number; totalReservations: number; cancelledReservations: number })));
    run('daily',    () => insightApi.getDailySummary(tenantId, from, to),     r => setDailySummary((r as { data: DailySummaryDTO[] }).data ?? []));
    run('forecast', () => mlApi.getForecast(tenantId, 7),                     r => setForecast(r.predictions ?? []));
    run('anomaly',  () => mlApi.getAnomalies(tenantId, 30),                   r => setAnomalies(r.anomalies ?? []));
    run('insight',  () => mlApi.getInsight(tenantId),                         r => setMlInsight(r));
  }, [tenantId, from, to, run]);

  const maxRevenue = Math.max(...dailySummary.map(d => d.totalRevenue), 1);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1><BarChart3 size={20} /> Analytics</h1>
        <div className={styles.dateRange}>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} />
          <span>→</span>
          <input type="date" value={to} onChange={e => setTo(e.target.value)} />
          <button className={styles.btnLoad} onClick={loadAll}><RefreshCw size={14} /> Cargar</button>
        </div>
      </div>

      {/* KPI CARDS */}
      <div className={styles.kpiRow}>
        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Ticket promedio</span>
          {loading.ticket ? <span className={styles.kpiLoading}>…</span>
            : errors.ticket ? <span className={styles.kpiError}>{errors.ticket}</span>
            : <strong className={styles.kpiValue}>${avgTicket?.average.toFixed(2) ?? '—'}</strong>}
          <span className={styles.kpiSub}>{avgTicket ? `${avgTicket.saleCount} ventas · $${avgTicket.totalRevenue.toFixed(0)} total` : 'Sin datos'}</span>
        </div>

        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Tasa de cancelación</span>
          {loading.cancel ? <span className={styles.kpiLoading}>…</span>
            : errors.cancel ? <span className={styles.kpiError}>{errors.cancel}</span>
            : <strong className={`${styles.kpiValue} ${cancelRate && cancelRate.ratePercent > 20 ? styles.kpiDanger : ''}`}>
                {cancelRate?.ratePercent.toFixed(1) ?? '—'}%
              </strong>}
          <span className={styles.kpiSub}>{cancelRate ? `${cancelRate.cancelledReservations} de ${cancelRate.totalReservations} reservas` : 'Sin datos'}</span>
        </div>

        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Anomalías detectadas</span>
          {loading.anomaly ? <span className={styles.kpiLoading}>…</span>
            : errors.anomaly ? <span className={styles.kpiError}>{errors.anomaly}</span>
            : <strong className={`${styles.kpiValue} ${anomalies.filter(a => a.is_anomaly).length > 0 ? styles.kpiWarn : ''}`}>
                {anomalies.filter(a => a.is_anomaly).length}
              </strong>}
          <span className={styles.kpiSub}>últimos 30 días</span>
        </div>
      </div>

      {/* DAILY CHART */}
      <div className={styles.chartCard}>
        <div className={styles.chartHeader}>
          <h3><BarChart3 size={16} /> Ingresos diarios</h3>
        </div>
        {loading.daily ? <p className={styles.loading}>Cargando...</p>
          : errors.daily ? <p className={styles.errorMsg}>{errors.daily}</p>
          : dailySummary.length === 0 ? <p className={styles.empty}>Sin datos para el rango seleccionado.</p>
          : (
            <div className={styles.barChart}>
              {dailySummary.map(d => (
                <div key={d.date} className={styles.barCol}>
                  <span className={styles.barValue}>${d.totalRevenue.toFixed(0)}</span>
                  <div className={styles.bar} style={{ height: `${(d.totalRevenue / maxRevenue) * 100}%` }} />
                  <span className={styles.barLabel}>{d.date.slice(5)}</span>
                </div>
              ))}
            </div>
          )}
      </div>

      {/* FORECAST */}
      <div className={styles.chartCard}>
        <div className={styles.chartHeader}>
          <h3><TrendingUp size={16} /> Predicción próximos 7 días</h3>
        </div>
        {loading.forecast ? <p className={styles.loading}>Cargando...</p>
          : errors.forecast ? <p className={styles.errorMsg}>{errors.forecast}</p>
          : forecast.length === 0 ? <p className={styles.empty}>Sin predicción disponible.</p>
          : (
            <div className={styles.forecastTable}>
              <table>
                <thead><tr><th>Fecha</th><th>Predicción</th><th>Rango</th></tr></thead>
                <tbody>
                  {forecast.map(f => (
                    <tr key={f.date}>
                      <td>{f.date}</td>
                      <td><strong>${f.predicted.toFixed(2)}</strong></td>
                      <td className={styles.range}>${f.lower.toFixed(0)} – ${f.upper.toFixed(0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </div>

      {/* ANOMALIES */}
      {(anomalies.length > 0 || loading.anomaly) && (
        <div className={styles.chartCard}>
          <div className={styles.chartHeader}>
            <h3><AlertTriangle size={16} /> Días anómalos</h3>
          </div>
          {loading.anomaly ? <p className={styles.loading}>Cargando...</p>
            : (
              <div className={styles.anomalyList}>
                {anomalies.filter(a => a.is_anomaly).map(a => (
                  <div key={a.date} className={styles.anomalyItem}>
                    <span className={styles.anomalyDate}>{a.date}</span>
                    <span className={styles.anomalyRevenue}>${a.revenue.toFixed(2)}</span>
                    <span className={styles.anomalyZ}>Z: {a.zscore.toFixed(2)}</span>
                    <span className={styles.anomalyBadge}>Anómalo</span>
                  </div>
                ))}
                {anomalies.filter(a => a.is_anomaly).length === 0 && <p className={styles.empty}>No se detectaron anomalías.</p>}
              </div>
            )}
        </div>
      )}

      {/* ML INSIGHT */}
      <div className={styles.insightCard}>
        <div className={styles.chartHeader}>
          <h3><Sparkles size={16} /> Insight generado por IA</h3>
          <button className={styles.btnSmall} onClick={() => run('insight', () => mlApi.getInsight(tenantId), r => setMlInsight(r))} disabled={loading.insight}>
            {loading.insight ? '…' : <RefreshCw size={13} />}
          </button>
        </div>
        {errors.insight ? <p className={styles.errorMsg}>{errors.insight}</p>
          : mlInsight ? (
            <>
              <p className={styles.insightText}>{mlInsight.insight}</p>
              <div className={styles.insightContext}>
                {Object.entries(mlInsight.context).map(([k, v]) => (
                  <span key={k} className={styles.contextChip}>
                    <strong>{k.replace(/_/g, ' ')}:</strong> {typeof v === 'number' ? v.toFixed(2) : v}
                  </span>
                ))}
              </div>
            </>
          ) : <p className={styles.empty}>Haz clic en "Cargar" para generar un insight con IA.</p>}
      </div>
    </div>
  );
};
