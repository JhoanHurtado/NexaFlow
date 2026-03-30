import { useState, useCallback, useEffect } from 'react';
import { insightApi, mlApi } from '../../api/insight.api';
import type {
  AverageTicketDTO, CancellationRateDTO, DailySummaryDTO,
  ForecastDTO, AnomalyDTO, MLInsightDTO,
  TopProductDTO, LowStockProductDTO,
} from '../../api/insight.api';
import { useTenant } from '../../hooks/useTenant';
import {
  BarChart3, TrendingUp, AlertTriangle, Sparkles, RefreshCw,
  Package, ArrowUpRight, ArrowDownRight, Info,
} from 'lucide-react';
import styles from './AnalyticsPage.module.scss';

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
  const [insightLoading, setInsightLoading] = useState(false);
  const [insightError,   setInsightError]   = useState('');

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

  const handleInsight = async () => {
    setInsightLoading(true); setInsightError('');
    try { setMlInsight(await mlApi.getInsight(tenantId)); }
    catch (e: unknown) { setInsightError(e instanceof Error ? e.message : 'Error'); }
    finally { setInsightLoading(false); }
  };

  const safeAnomalies = anomalies ?? [];
  const anomalyList   = safeAnomalies.filter(a => a.is_anomaly);
  const maxRevenue    = Math.max(...dailySummary.map(d => d.totalRevenue), 1);
  const avgRevenue    = dailySummary.length
    ? dailySummary.reduce((s, d) => s + d.totalRevenue, 0) / dailySummary.length
    : 0;
  const peakDay       = dailySummary.reduce((a, b) => b.totalRevenue > a.totalRevenue ? b : a, dailySummary[0]);
  const daysAboveAvg  = dailySummary.filter(d => d.totalRevenue > avgRevenue).length;

  return (
    <div className={styles.page}>
      {/* Header */}
      <div className={styles.header}>
        <h1><BarChart3 size={20} /> Analytics</h1>
        <div className={styles.dateRange}>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} />
          <span>→</span>
          <input type="date" value={to} onChange={e => setTo(e.target.value)} />
          <button className={styles.btnLoad} onClick={loadAll}><RefreshCw size={13} /> Cargar</button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className={styles.kpiRow}>
        <KpiCard
          label="Ticket Promedio"
          value={avgTicket ? `$${avgTicket.average.toFixed(2)}` : '—'}
          sub={avgTicket ? `${avgTicket.saleCount} ventas · $${avgTicket.totalRevenue.toFixed(0)} total` : 'Cargando...'}
          loading={loading.ticket} error={errors.ticket}
          trend="+12.5%" trendUp
        />
        <KpiCard
          label="Tasa de Cancelación"
          value={cancelRate ? `${cancelRate.ratePercent.toFixed(1)}%` : '—'}
          sub={cancelRate ? `${cancelRate.cancelledReservations} de ${cancelRate.totalReservations} reservas` : 'Cargando...'}
          loading={loading.cancel} error={errors.cancel}
          danger={cancelRate != null && cancelRate.ratePercent > 20}
        />
        <KpiCard
          label="Anomalías Detectadas"
          value={String(anomalyList.length)}
          sub="En los últimos 30 días"
          loading={loading.anomaly} error={errors.anomaly}
          warn={anomalyList.length > 0}
          warnLabel="Requiere revisión"
        />
        <KpiCard
          label="Stock Bajo"
          value={String(lowStock.length)}
          sub={`${lowStock.filter(p => p.isDepleted).length} agotados`}
          loading={loading.stock} error={errors.stock}
          danger={lowStock.some(p => p.isDepleted)}
          warnLabel="Crítico"
        />
      </div>

      {/* Daily Revenue Chart */}
      <section className={styles.chartCard}>
        <div className={styles.chartHeader}>
          <div>
            <h3><BarChart3 size={18} className={styles.iconBlue} /> Ingresos diarios</h3>
            {dailySummary.length > 0 && (
              <p className={styles.chartSub}>
                Flujo de caja diario · media <strong>${avgRevenue.toFixed(2)}</strong>
              </p>
            )}
          </div>
          <div className={styles.chartLegend}>
            <span className={styles.legendDot} style={{ background: '#3b82f6' }} /> <span>Ventas</span>
            <span className={styles.legendDot} style={{ background: '#e2e8f0' }} /> <span>Promedio</span>
          </div>
        </div>

        {loading.daily ? <p className={styles.loading}>Cargando...</p>
          : errors.daily ? <p className={styles.errorMsg}>{errors.daily}</p>
          : dailySummary.length === 0 ? <p className={styles.empty}>Sin datos para el rango seleccionado.</p>
          : (
            <>
              <div className={styles.barChartWrap}>
                {/* Y-axis */}
                <div className={styles.yAxis}>
                  {[1, 0.75, 0.5, 0.25, 0].map(f => (
                    <span key={f}>${(maxRevenue * f).toFixed(0)}</span>
                  ))}
                </div>
                {/* Bars */}
                <div className={styles.barsArea}>
                  {/* Grid lines */}
                  <div className={styles.gridLines}>
                    {[0,1,2,3,4].map(i => <div key={i} className={styles.gridLine} />)}
                  </div>
                  {/* Average line */}
                  <div
                    className={styles.avgLine}
                    style={{ bottom: `calc(${(avgRevenue / maxRevenue) * 100}% + 28px)` }}
                  >
                    <span className={styles.avgLabel}>PROM ${avgRevenue.toFixed(0)}</span>
                  </div>
                  {/* Bar columns */}
                  {dailySummary.map((d, i) => {
                    const pct = (d.totalRevenue / maxRevenue) * 100;
                    const aboveAvg = d.totalRevenue > avgRevenue;
                    return (
                      <div key={i} className={styles.barCol}>
                        <div className={styles.barTooltip}>
                          <strong>{d.date.slice(5)}</strong>
                          <span>${d.totalRevenue.toFixed(2)}</span>
                        </div>
                        <div
                          className={`${styles.bar} ${aboveAvg ? styles.barHigh : styles.barLow}`}
                          style={{ height: `${pct}%` }}
                        />
                        {(i % 5 === 0 || i === dailySummary.length - 1) && (
                          <span className={styles.barLabel}>{d.date.slice(5)}</span>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
              {/* Chart footer stats */}
              <div className={styles.chartFooter}>
                <div className={styles.footerStat}>
                  <div className={styles.footerIcon} style={{ background: '#f0fdf4', color: '#16a34a' }}>
                    <TrendingUp size={15} />
                  </div>
                  <div>
                    <p className={styles.footerLabel}>Pico máximo</p>
                    <p className={styles.footerValue}>${peakDay?.totalRevenue.toFixed(2) ?? '—'}</p>
                  </div>
                </div>
                <div className={styles.footerStat}>
                  <div className={styles.footerIcon} style={{ background: '#eff6ff', color: '#2563eb' }}>
                    <Info size={15} />
                  </div>
                  <div>
                    <p className={styles.footerLabel}>Días sobre media</p>
                    <p className={styles.footerValue}>{daysAboveAvg} días</p>
                  </div>
                </div>
              </div>
            </>
          )}
      </section>

      {/* Top Products + Low Stock */}
      <div className={styles.twoCol}>
        <div className={styles.chartCard}>
          <div className={styles.chartHeader}>
            <h3><TrendingUp size={18} className={styles.iconGreen} /> Top productos</h3>
          </div>
          {loading.top ? <p className={styles.loading}>Cargando...</p>
            : errors.top ? <p className={styles.errorMsg}>{errors.top}</p>
            : topProducts.length === 0 ? <p className={styles.empty}>Sin ventas en el período.</p>
            : (
              <div className={styles.listRows}>
                {topProducts.map((p, i) => (
                  <div key={p.productId} className={styles.listRow}>
                    <div className={styles.listRank}>{p.totalUnits}</div>
                    <span className={styles.listName}>{p.productName}</span>
                    <span className={styles.listValue}>${p.totalRevenue.toFixed(2)}</span>
                  </div>
                ))}
              </div>
            )}
        </div>

        <div className={styles.chartCard}>
          <div className={styles.chartHeader}>
            <h3><AlertTriangle size={18} className={styles.iconAmber} /> Alertas de Stock</h3>
          </div>
          {loading.stock ? <p className={styles.loading}>Cargando...</p>
            : errors.stock ? <p className={styles.errorMsg}>{errors.stock}</p>
            : lowStock.length === 0 ? <p className={styles.empty}>Todos los productos tienen stock suficiente.</p>
            : (
              <div className={styles.listRows}>
                {lowStock.map(p => (
                  <div key={p.productId} className={styles.listRow}>
                    <span className={styles.listName}>{p.productName}</span>
                    <span className={`${styles.stockBadge} ${p.isDepleted ? styles.stockOut : p.currentStock <= 2 ? styles.stockCritical : styles.stockLow}`}>
                      {p.isDepleted ? 'Agotado' : `${p.currentStock} uds`}
                    </span>
                  </div>
                ))}
              </div>
            )}
        </div>
      </div>

      {/* Forecast */}
      <section className={styles.chartCard}>
        <div className={styles.chartHeader}>
          <div>
            <h3><TrendingUp size={18} className={styles.iconBlue} /> Predicción próximos 7 días</h3>
            <p className={styles.chartSub}>Proyección generada por motor de IA basado en históricos</p>
          </div>
          <span className={styles.badge}>ML</span>
        </div>
        {loading.forecast ? <p className={styles.loading}>Cargando...</p>
          : errors.forecast ? <p className={styles.errorMsg}>
              {errors.forecast.includes('insuficientes') || errors.forecast.includes('mínimo')
                ? 'Se necesitan al menos 2 días de ventas para generar predicciones.'
                : errors.forecast}
            </p>
          : forecast.length === 0 ? <p className={styles.empty}>Sin predicción disponible.</p>
          : (
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr><th>Fecha</th><th>Predicción</th><th>Rango de Probabilidad</th></tr>
                </thead>
                <tbody>
                  {forecast.map((f, i) => (
                    <tr key={f.date} className={i === 0 ? styles.rowToday : ''}>
                      <td>
                        {f.date}
                        {i === 0 && <span className={styles.todayBadge}>HOY</span>}
                      </td>
                      <td className={styles.tdBold}>${f.predicted.toFixed(2)}</td>
                      <td className={styles.tdMuted}>${f.lower.toFixed(0)} — ${f.upper.toFixed(0)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </section>

      {/* Anomalies */}
      {anomalyList.length > 0 && (
        <section className={styles.chartCard}>
          <div className={styles.chartHeader}>
            <h3><AlertTriangle size={18} className={styles.iconRed} /> Días anómalos detectados</h3>
          </div>
          <div className={styles.anomalyGrid}>
            {anomalyList.map(a => (
              <div key={a.date} className={styles.anomalyItem}>
                <span className={styles.anomalyDate}>{a.date}</span>
                <span className={styles.anomalyRevenue}>${a.revenue.toFixed(2)}</span>
                <span className={styles.anomalyZ}>Z: {a.zscore.toFixed(2)}</span>
                <span className={styles.anomalyBadge}>Anómalo</span>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* AI Insight */}
      <section className={styles.insightSection}>
        <div className={styles.insightGlow} />
        <div className={styles.insightContent}>
          <div className={styles.insightIcon}><Sparkles size={28} /></div>
          <div className={styles.insightBody}>
            <h3>Insight generado por IA</h3>
            {insightLoading ? <p className={styles.insightText}>Generando insight...</p>
              : insightError ? <p className={styles.insightError}>{insightError}</p>
              : mlInsight ? (
                <>
                  <p className={styles.insightText}>{mlInsight.insight}</p>
                  {mlInsight.context && Object.keys(mlInsight.context).length > 0 && (
                    <div className={styles.insightChips}>
                      {Object.entries(mlInsight.context).map(([k, v]) => (
                        <span key={k} className={styles.chip}>
                          <strong>{k.replace(/_/g, ' ')}:</strong>{' '}
                          {typeof v === 'number' ? v.toFixed(2) : v}
                        </span>
                      ))}
                    </div>
                  )}
                </>
              ) : <p className={styles.insightPlaceholder}>Genera un análisis inteligente de tu negocio con un clic.</p>}
          </div>
          <button className={styles.insightBtn} onClick={handleInsight} disabled={insightLoading}>
            {insightLoading ? '...' : 'Regenerar Insight'}
          </button>
        </div>
      </section>
    </div>
  );
};

// ─── KPI Card ─────────────────────────────────────────────────────────────────
interface KpiProps {
  label: string; value: string; sub: string;
  loading?: boolean; error?: string;
  trend?: string; trendUp?: boolean;
  warn?: boolean; danger?: boolean; warnLabel?: string;
}

const KpiCard = ({ label, value, sub, loading, error, trend, trendUp, warn, danger, warnLabel }: KpiProps) => (
  <div className={`${styles.kpiCard} ${danger ? styles.kpiCardDanger : warn ? styles.kpiCardWarn : ''}`}>
    <p className={styles.kpiLabel}>{label}</p>
    {loading ? <span className={styles.kpiLoading}>…</span>
      : error ? <span className={styles.kpiError}>{error}</span>
      : (
        <div className={styles.kpiValueRow}>
          <h2 className={`${styles.kpiValue} ${danger ? styles.textDanger : warn ? styles.textWarn : ''}`}>{value}</h2>
          {trend && !warn && !danger && (
            <span className={`${styles.trendBadge} ${trendUp ? styles.trendUp : styles.trendDown}`}>
              {trendUp ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}{trend}
            </span>
          )}
          {(warn || danger) && warnLabel && (
            <span className={`${styles.trendBadge} ${danger ? styles.trendDanger : styles.trendWarn}`}>{warnLabel}</span>
          )}
        </div>
      )}
    <p className={styles.kpiSub}>{sub}</p>
  </div>
);
