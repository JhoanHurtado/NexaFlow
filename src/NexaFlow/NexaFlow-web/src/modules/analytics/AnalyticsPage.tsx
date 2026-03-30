import { useState, useCallback, useEffect } from 'react';
import { insightApi, mlApi } from '../../api/insight.api';
import type {
  AverageTicketDTO, CancellationRateDTO, DailySummaryDTO,
  ForecastDTO, AnomalyDTO, MLInsightDTO,
  TopProductDTO, LowStockProductDTO,
} from '../../api/insight.api';
import { useTenant } from '../../hooks/useTenant';
import styles from './AnalyticsPage.module.scss';
import { BarChart3, TrendingUp, AlertTriangle, Sparkles, RefreshCw, Package, ShoppingCart } from 'lucide-react';

const fmt = (d: Date) => d.toISOString().split('T')[0];
const thirtyDaysAgo = () => fmt(new Date(Date.now() - 30 * 86400000));
const today = () => fmt(new Date());

export const AnalyticsPage = () => {
  const { tenantId } = useTenant();

  const [from, setFrom] = useState(thirtyDaysAgo());
  const [to,   setTo]   = useState(today());

  const [avgTicket,    setAvgTicket]    = useState<AverageTicketDTO | null>(null);
  const [cancelRate,   setCancelRate]   = useState<CancellationRateDTO | null>(null);
  const [dailySummary, setDailySummary] = useState<DailySummaryDTO[]>([]);
  const [topProducts,  setTopProducts]  = useState<TopProductDTO[]>([]);
  const [lowStock,     setLowStock]     = useState<LowStockProductDTO[]>([]);
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
    run('ticket',   () => insightApi.getAverageTicket(tenantId, from, to),    setAvgTicket);
    run('cancel',   () => insightApi.getCancellationRate(tenantId, from, to), setCancelRate);
    run('daily',    () => insightApi.getDailySummary(tenantId, from, to),     setDailySummary);
    run('top',      () => insightApi.getTopProducts(tenantId, from, to, 5),   setTopProducts);
    run('stock',    () => insightApi.getLowStock(tenantId),                   setLowStock);
    run('forecast', () => mlApi.getForecast(tenantId, 7),                     r => setForecast(r.predictions));
    run('anomaly',  () => mlApi.getAnomalies(tenantId, 30),                   r => setAnomalies(r.anomalies));
  }, [tenantId, from, to, run]);

  useEffect(() => { loadAll(); }, [loadAll]);

  const maxRevenue = Math.max(...dailySummary.map(d => d.totalRevenue), 1);
  const anomalyList = (anomalies ?? []).filter(a => a.is_anomaly);

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
          <span className={styles.kpiSub}>
            {avgTicket ? `${avgTicket.saleCount} ventas · $${avgTicket.totalRevenue.toFixed(0)} total` : 'Haz clic en Cargar'}
          </span>
        </div>

        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Tasa de cancelación</span>
          {loading.cancel ? <span className={styles.kpiLoading}>…</span>
            : errors.cancel ? <span className={styles.kpiError}>{errors.cancel}</span>
            : <strong className={`${styles.kpiValue} ${cancelRate && cancelRate.ratePercent > 20 ? styles.kpiDanger : ''}`}>
                {cancelRate ? `${cancelRate.ratePercent.toFixed(1)}%` : '—'}
              </strong>}
          <span className={styles.kpiSub}>
            {cancelRate ? `${cancelRate.cancelledReservations} de ${cancelRate.totalReservations} reservas` : 'Haz clic en Cargar'}
          </span>
        </div>

        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Anomalías detectadas</span>
          {loading.anomaly ? <span className={styles.kpiLoading}>…</span>
            : errors.anomaly ? <span className={styles.kpiError}>{errors.anomaly}</span>
            : <strong className={`${styles.kpiValue} ${anomalyList.length > 0 ? styles.kpiWarn : ''}`}>
                {anomalyList.length}
              </strong>}
          <span className={styles.kpiSub}>últimos 30 días</span>
        </div>

        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Stock bajo</span>
          {loading.stock ? <span className={styles.kpiLoading}>…</span>
            : errors.stock ? <span className={styles.kpiError}>{errors.stock}</span>
            : <strong className={`${styles.kpiValue} ${lowStock.length > 0 ? styles.kpiWarn : ''}`}>
                {lowStock.length}
              </strong>}
          <span className={styles.kpiSub}>{lowStock.filter(p => p.isDepleted).length} agotados</span>
        </div>
      </div>

      {/* DAILY CHART */}
      <div className={styles.chartCard}>
        <div className={styles.chartHeader}><h3><BarChart3 size={16} /> Ingresos diarios</h3></div>
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

      {/* TOP PRODUCTS + LOW STOCK */}
      <div className={styles.twoCol}>
        <div className={styles.chartCard}>
          <div className={styles.chartHeader}><h3><ShoppingCart size={16} /> Top productos</h3></div>
          {loading.top ? <p className={styles.loading}>Cargando...</p>
            : errors.top ? <p className={styles.errorMsg}>{errors.top}</p>
            : topProducts.length === 0 ? <p className={styles.empty}>Sin ventas en el período.</p>
            : (
              <div className={styles.topList}>
                {topProducts.map((p, i) => (
                  <div key={p.productId} className={styles.topItem}>
                    <span className={styles.topRank}>#{i + 1}</span>
                    <span className={styles.topName}>{p.productName}</span>
                    <span className={styles.topUnits}>{p.totalUnits} uds</span>
                    <span className={styles.topRevenue}>${p.totalRevenue.toFixed(2)}</span>
                  </div>
                ))}
              </div>
            )}
        </div>

        <div className={styles.chartCard}>
          <div className={styles.chartHeader}><h3><Package size={16} /> Stock bajo</h3></div>
          {loading.stock ? <p className={styles.loading}>Cargando...</p>
            : errors.stock ? <p className={styles.errorMsg}>{errors.stock}</p>
            : lowStock.length === 0 ? <p className={styles.empty}>Todos los productos tienen stock suficiente.</p>
            : (
              <div className={styles.topList}>
                {lowStock.map(p => (
                  <div key={p.productId} className={styles.topItem}>
                    <span className={`${styles.topRank} ${p.isDepleted ? styles.kpiDanger : styles.kpiWarn}`}>
                      {p.isDepleted ? '✕' : '!'}
                    </span>
                    <span className={styles.topName}>{p.productName}</span>
                    <span className={styles.topUnits}>{p.currentStock} / {p.lowStockThreshold}</span>
                    <span className={p.isDepleted ? styles.kpiDanger : styles.kpiWarn}>
                      {p.isDepleted ? 'Agotado' : 'Bajo'}
                    </span>
                  </div>
                ))}
              </div>
            )}
        </div>
      </div>

      {/* FORECAST */}
      <div className={styles.chartCard}>
        <div className={styles.chartHeader}><h3><TrendingUp size={16} /> Predicción próximos 7 días</h3></div>
        {loading.forecast ? <p className={styles.loading}>Cargando...</p>
          : errors.forecast ? <p className={styles.errorMsg}>
              {errors.forecast.includes('insuficientes') || errors.forecast.includes('mínimo')
                ? 'Se necesitan al menos 2 días de ventas para generar predicciones.'
                : errors.forecast}
            </p>
          : forecast.length === 0 ? <p className={styles.empty}>Sin predicción disponible. Se necesitan datos históricos.</p>
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
      <div className={styles.chartCard}>
        <div className={styles.chartHeader}><h3><AlertTriangle size={16} /> Días anómalos</h3></div>
        {loading.anomaly ? <p className={styles.loading}>Cargando...</p>
          : errors.anomaly ? <p className={styles.errorMsg}>{errors.anomaly}</p>
          : anomalyList.length === 0 ? <p className={styles.empty}>No se detectaron anomalías en los últimos 30 días.</p>
          : (
            <div className={styles.anomalyList}>
              {anomalyList.map(a => (
                <div key={a.date} className={styles.anomalyItem}>
                  <span className={styles.anomalyDate}>{a.date}</span>
                  <span className={styles.anomalyRevenue}>${a.revenue.toFixed(2)}</span>
                  <span className={styles.anomalyZ}>Z: {a.zscore.toFixed(2)}</span>
                  <span className={styles.anomalyBadge}>Anómalo</span>
                </div>
              ))}
            </div>
          )}
      </div>

      {/* ML INSIGHT */}
      <div className={styles.insightCard}>
        <div className={styles.chartHeader}>
          <h3><Sparkles size={16} /> Insight generado por IA</h3>
          <button className={styles.btnSmall}
            onClick={() => run('insight', () => mlApi.getInsight(tenantId), setMlInsight)}
            disabled={loading.insight}>
            {loading.insight ? '…' : <RefreshCw size={13} />}
          </button>
        </div>
        {loading.insight ? <p className={styles.loading}>Generando insight...</p>
          : errors.insight ? <p className={styles.errorMsg}>{errors.insight}</p>
          : mlInsight ? (
            <>
              <p className={styles.insightText}>{mlInsight.insight}</p>
              {mlInsight.context && Object.keys(mlInsight.context).length > 0 && (
                <div className={styles.insightContext}>
                  {Object.entries(mlInsight.context).map(([k, v]) => (
                    <span key={k} className={styles.contextChip}>
                      <strong>{k.replace(/_/g, ' ')}:</strong>{' '}
                      {typeof v === 'number' ? v.toFixed(2) : v}
                    </span>
                  ))}
                </div>
              )}
            </>
          ) : <p className={styles.empty}>Haz clic en el botón para generar un insight con IA.</p>}
      </div>
    </div>
  );
};
