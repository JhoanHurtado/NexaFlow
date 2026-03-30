import { useState, useCallback, useEffect } from 'react';
import { insightApi, mlApi } from '../../api/insight.api';
import type {
  AverageTicketDTO, CancellationRateDTO, DailySummaryDTO,
  ForecastDTO, AnomalyDTO, MLInsightDTO,
  TopProductDTO, LowStockProductDTO,
} from '../../api/insight.api';
import { useTenant } from '../../hooks/useTenant';
import {
  TrendingUp, AlertTriangle, Sparkles, RefreshCw,
  ArrowUpRight, ArrowDownRight, Info, Flame, Zap,
  Calendar, CheckCircle2, Filter,
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

  const [hoveredBar,  setHoveredBar]  = useState<DailySummaryDTO | null>(null);
  const [selectedBar, setSelectedBar] = useState<DailySummaryDTO | null>(null);

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
    ? dailySummary.reduce((s, d) => s + d.totalRevenue, 0) / dailySummary.length : 0;

  return (
    <div className={styles.page}>

      {/* Status bar */}
      <div className={styles.statusBar}>
        <div className={styles.statusLeft}>
          <span className={styles.liveIndicator}>
            <span className={styles.liveDot} /> Sistema Live
          </span>
          <span className={styles.statusSep}>Última sync: Hace 4 min</span>
        </div>
        <div className={styles.dateRange}>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} />
          <span>→</span>
          <input type="date" value={to} onChange={e => setTo(e.target.value)} />
          <button className={styles.btnLoad} onClick={loadAll}><RefreshCw size={13} /> Cargar</button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className={styles.kpiRow}>
        <StatCard
          label="Ticket Promedio"
          value={avgTicket ? `$${avgTicket.average.toFixed(2)}` : '—'}
          sub={avgTicket ? `${avgTicket.saleCount} ventas totales` : 'Cargando...'}
          trend="+12.5%" trendUp loading={loading.ticket} error={errors.ticket}
        />
        <StatCard
          label="Tasa Cancelación"
          value={cancelRate ? `${cancelRate.ratePercent.toFixed(1)}%` : '—'}
          sub={cancelRate ? `${cancelRate.cancelledReservations} de ${cancelRate.totalReservations} reservas` : 'Cargando...'}
          loading={loading.cancel} error={errors.cancel}
          warn={cancelRate != null && cancelRate.ratePercent > 20}
        />
        <StatCard
          label="Anomalías"
          value={String(anomalyList.length)}
          sub="Pendientes de revisión"
          warn={anomalyList.length > 0} warnLabel="Alerta"
          loading={loading.anomaly} error={errors.anomaly}
        />
        <StatCard
          label="Stock Crítico"
          value={String(lowStock.length)}
          sub={`${lowStock.filter(p => p.isDepleted).length} agotados`}
          danger={lowStock.some(p => p.isDepleted)} warnLabel="Crítico"
          loading={loading.stock} error={errors.stock}
        />
      </div>

      {/* Revenue Chart */}
      <section className={styles.chartCard}>
        <div className={styles.chartTop}>
          <div>
            <div className={styles.chartTitleRow}>
              <h3 className={styles.chartTitle}>Análisis de Ingresos</h3>
              <span className={styles.chartBadge}>Mensual</span>
            </div>
            {dailySummary.length > 0 && (
              <p className={styles.chartSub}>
                Rendimiento diario comparado contra la media de{' '}
                <strong className={styles.chartAvgHighlight}>${avgRevenue.toFixed(0)}</strong>
              </p>
            )}
          </div>
          <div className={styles.chartControls}>
            <div className={styles.periodTabs}>
              <button className={styles.periodActive}>Día</button>
              <button className={styles.periodBtn}>Semana</button>
              <button className={styles.periodBtn}>Mes</button>
            </div>
            <button className={styles.filterBtn}><Filter size={15} /></button>
          </div>
        </div>

        {loading.daily ? <p className={styles.loading}>Cargando...</p>
          : errors.daily ? <p className={styles.errorMsg}>{errors.daily}</p>
          : dailySummary.length === 0 ? <p className={styles.empty}>Sin datos para el rango seleccionado.</p>
          : (
            <>
              <div className={styles.barChartWrap}>
                <div className={styles.yAxis}>
                  <span>${maxRevenue.toFixed(0)}</span>
                  <span>${(maxRevenue * 0.5).toFixed(0)}</span>
                  <span>$0</span>
                </div>
                <div className={styles.barsArea}>
                  <div className={styles.gridLines}>
                    {[0,1,2].map(i => <div key={i} className={styles.gridLine} />)}
                  </div>
                  <div
                    className={styles.avgLine}
                    style={{ bottom: `calc(${(avgRevenue / maxRevenue) * 100}% + 40px)` }}
                  >
                    <span className={styles.avgLabel}>MEDIA ${avgRevenue.toFixed(0)}</span>
                  </div>
                  {dailySummary.map((d, i) => {
                    const isPeak   = d.totalRevenue === maxRevenue;
                    const isLow    = d.totalRevenue < avgRevenue * 0.8;
                    const isHov    = hoveredBar?.date === d.date;
                    const isSel    = selectedBar?.date === d.date;
                    const aboveAvg = d.totalRevenue > avgRevenue;
                    return (
                      <div
                        key={i}
                        className={`${styles.barCol} ${isHov || isSel ? styles.barColActive : ''}`}
                        onMouseEnter={() => setHoveredBar(d)}
                        onMouseLeave={() => setHoveredBar(null)}
                        onClick={() => setSelectedBar(isSel ? null : d)}
                      >
                        <div className={`${styles.barTooltip} ${isHov || isSel ? styles.barTooltipVisible : ''}`}>
                          <strong>{d.date.slice(5)}</strong>
                          <span>${d.totalRevenue.toFixed(2)}</span>
                          <div className={styles.tooltipArrow} />
                        </div>
                        {isPeak && <Flame size={11} className={styles.iconPeak} />}
                        {isLow && !isPeak && <AlertTriangle size={11} className={styles.iconLow} />}
                        <div
                          className={`${styles.bar} ${isSel ? styles.barSelected : aboveAvg ? styles.barHigh : styles.barLow}`}
                          style={{ height: `${(d.totalRevenue / maxRevenue) * 100}%` }}
                        >
                          <div className={styles.barShine} />
                        </div>
                        {(i % 5 === 0 || i === dailySummary.length - 1) && (
                          <span className={`${styles.barLabel} ${isHov || isSel ? styles.barLabelActive : ''}`}>
                            {d.date.slice(5)}
                          </span>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Selected context */}
              <div className={`${styles.selectedCtx} ${selectedBar ? styles.selectedCtxVisible : ''}`}>
                <div className={styles.ctxCard}>
                  <p className={styles.ctxLabel}>Día Seleccionado</p>
                  <div className={styles.ctxRow}>
                    <div className={styles.ctxIcon} style={{ color: '#2563eb' }}><Calendar size={18} /></div>
                    <div>
                      <p className={styles.ctxValue}>{selectedBar?.date.slice(5) ?? '---'}</p>
                      <p className={styles.ctxSub}>Histórico de ventas</p>
                    </div>
                  </div>
                </div>
                <div className={styles.ctxCard}>
                  <p className={styles.ctxLabel}>Ingresos</p>
                  <div className={styles.ctxRow}>
                    <div className={styles.ctxIcon} style={{ color: '#16a34a' }}><TrendingUp size={18} /></div>
                    <div>
                      <p className={styles.ctxValue}>${selectedBar?.totalRevenue.toFixed(2) ?? '0.00'}</p>
                      <p className={`${styles.ctxSub} ${selectedBar && selectedBar.totalRevenue > avgRevenue ? styles.ctxGreen : styles.ctxMuted}`}>
                        {selectedBar ? (selectedBar.totalRevenue > avgRevenue ? '+ Sobre la media' : '- Bajo la media') : '--'}
                      </p>
                    </div>
                  </div>
                </div>
                <div className={styles.ctxCard}>
                  <p className={styles.ctxLabel}>Acción Sugerida</p>
                  <div className={styles.ctxRow}>
                    <div className={styles.ctxIcon} style={{ color: '#d97706' }}><Zap size={18} /></div>
                    <p className={styles.ctxText}>
                      {selectedBar && selectedBar.totalRevenue > 100
                        ? 'Alto volumen: Reforzar personal.'
                        : 'Volumen estable: Sin cambios.'}
                    </p>
                  </div>
                </div>
              </div>
            </>
          )}
      </section>

      {/* Top Products + Low Stock */}
      <div className={styles.twoCol}>
        <div className={styles.panelCard}>
          <div className={styles.panelHeader}>
            <h3 className={styles.panelTitle}>
              <span className={styles.panelIconGreen}><TrendingUp size={18} /></span>
              Productos Estrella
            </h3>
            <span className={styles.panelSub}>{from.slice(5)} — {to.slice(5)}</span>
          </div>
          {loading.top ? <p className={styles.loading}>Cargando...</p>
            : errors.top ? <p className={styles.errorMsg}>{errors.top}</p>
            : topProducts.length === 0 ? <p className={styles.empty}>Sin ventas en el período.</p>
            : (
              <div className={styles.listRows}>
                {topProducts.map(p => (
                  <div key={p.productId} className={styles.productRow}>
                    <div className={styles.productRank}>{p.totalUnits}</div>
                    <div className={styles.productInfo}>
                      <p className={styles.productName}>{p.productName}</p>
                      <p className={styles.productUnits}>{p.totalUnits} unidades</p>
                    </div>
                    <span className={styles.productRevenue}>${p.totalRevenue.toFixed(2)}</span>
                  </div>
                ))}
              </div>
            )}
        </div>

        <div className={styles.panelCard}>
          <div className={styles.panelHeader}>
            <h3 className={styles.panelTitle}>
              <span className={styles.panelIconAmber}><AlertTriangle size={18} /></span>
              Stock Crítico
            </h3>
            {lowStock.length > 0 && (
              <span className={styles.alertCount}>{lowStock.length} ALERTAS</span>
            )}
          </div>
          {loading.stock ? <p className={styles.loading}>Cargando...</p>
            : errors.stock ? <p className={styles.errorMsg}>{errors.stock}</p>
            : lowStock.length === 0 ? <p className={styles.empty}>Todo el stock está en niveles normales.</p>
            : (
              <div className={styles.listRows}>
                {lowStock.map(p => (
                  <div key={p.productId} className={styles.stockRow}>
                    <div className={`${styles.stockDot} ${p.isDepleted ? styles.stockDotOut : styles.stockDotLow}`} />
                    <span className={styles.stockName}>{p.productName}</span>
                    <span className={`${styles.stockBadge} ${p.isDepleted ? styles.stockBadgeOut : p.currentStock <= 2 ? styles.stockBadgeCritical : styles.stockBadgeLow}`}>
                      {p.isDepleted ? 'Agotado' : `${p.currentStock} uds`}
                    </span>
                  </div>
                ))}
              </div>
            )}
        </div>
      </div>

      {/* Forecast */}
      <section className={styles.panelCard}>
        <div className={styles.panelHeader}>
          <div>
            <h3 className={styles.panelTitle}>
              <span className={styles.panelIconBlue}><TrendingUp size={18} /></span>
              Predicción próximos 7 días
            </h3>
            <p className={styles.panelDesc}>Proyección generada por motor de IA basado en históricos</p>
          </div>
          <span className={styles.mlBadge}>ML</span>
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

      {/* AI Insight */}
      <section className={styles.aiSection}>
        <div className={styles.aiGlow} />
        <div className={styles.aiInner}>
          <div className={styles.aiIconWrap}>
            <div className={styles.aiIconBlur} />
            <div className={styles.aiIcon}><Sparkles size={40} /></div>
          </div>
          <div className={styles.aiBody}>
            <div className={styles.aiTitleRow}>
              <h3 className={styles.aiTitle}>NexaAI Assistant</h3>
              <span className={styles.aiVersion}>v4.0 Pro</span>
            </div>
            {insightLoading
              ? <p className={styles.aiText}>Generando insight...</p>
              : insightError
                ? <p className={styles.aiError}>{insightError}</p>
                : mlInsight
                  ? (
                    <>
                      <p className={styles.aiText}>{mlInsight.insight}</p>
                      {mlInsight.context && Object.keys(mlInsight.context).length > 0 && (
                        <div className={styles.aiChips}>
                          {Object.entries(mlInsight.context).map(([k, v]) => (
                            <span key={k} className={styles.aiChip}>
                              <strong>{k.replace(/_/g, ' ')}:</strong>{' '}
                              {typeof v === 'number' ? v.toFixed(2) : v}
                            </span>
                          ))}
                        </div>
                      )}
                    </>
                  )
                  : <p className={styles.aiPlaceholder}>
                      Genera un análisis inteligente de tu negocio con un clic.
                    </p>}
            <div className={styles.aiActions}>
              <button className={styles.aiBtnPrimary} onClick={handleInsight} disabled={insightLoading}>
                {insightLoading ? 'Generando...' : 'Regenerar Insight'} <CheckCircle2 size={14} />
              </button>
            </div>
          </div>
        </div>
      </section>

    </div>
  );
};

// ─── Stat Card ────────────────────────────────────────────────────────────────
interface StatCardProps {
  label: string; value: string; sub: string;
  loading?: boolean; error?: string;
  trend?: string; trendUp?: boolean;
  warn?: boolean; danger?: boolean; warnLabel?: string;
}

const StatCard = ({ label, value, sub, loading, error, trend, trendUp, warn, danger, warnLabel }: StatCardProps) => (
  <div className={`${styles.kpiCard} ${danger ? styles.kpiDanger : warn ? styles.kpiWarn : ''}`}>
    <div className={styles.kpiTop}>
      <p className={styles.kpiLabel}>{label}</p>
      {(trend || warnLabel) && (
        <span className={`${styles.kpiBadge} ${danger ? styles.kpiBadgeDanger : warn ? styles.kpiBadgeWarn : trendUp ? styles.kpiBadgeUp : styles.kpiBadgeDown}`}>
          {danger || warn
            ? <AlertTriangle size={11} />
            : trendUp ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
          {warnLabel ?? trend}
        </span>
      )}
    </div>
    {loading ? <span className={styles.kpiLoading}>…</span>
      : error ? <span className={styles.kpiError}>{error}</span>
      : <h2 className={`${styles.kpiValue} ${danger ? styles.textDanger : warn ? styles.textWarn : ''}`}>{value}</h2>}
    <p className={styles.kpiSub}><Info size={11} /> {sub}</p>
  </div>
);
