-- =========================
-- VENTAS POR DÍA
-- =========================
SELECT 
    DATE(created_at) as day,
    SUM(total) as total_sales
FROM sales
WHERE tenant_id = $1
GROUP BY day
ORDER BY day;

-- =========================
-- TICKET PROMEDIO
-- =========================
SELECT 
    AVG(total) as avg_ticket
FROM sales
WHERE tenant_id = $1;

-- =========================
-- TOP PRODUCTOS
-- =========================
SELECT 
    p.name,
    SUM(si.quantity) as total_sold
FROM sale_items si
JOIN products p ON p.id = si.product_id
JOIN sales s ON s.id = si.sale_id
WHERE s.tenant_id = $1
GROUP BY p.name
ORDER BY total_sold DESC
LIMIT 10;

-- =========================
-- CONVERSIÓN RESERVA → VENTA
-- =========================
SELECT 
    COUNT(DISTINCT s.reservation_id) * 1.0 /
    NULLIF(COUNT(DISTINCT r.id), 0) as conversion_rate
FROM reservations r
LEFT JOIN sales s 
    ON s.reservation_id = r.id
    AND s.tenant_id = $1
WHERE r.tenant_id = $1;

-- =========================
-- VENTAS CON MEDIA MÓVIL
-- =========================
SELECT 
    day,
    daily_sales,
    AVG(daily_sales) OVER (
        ORDER BY day
        ROWS BETWEEN 6 PRECEDING AND CURRENT ROW
    ) as rolling_avg_7d
FROM (
    SELECT 
        DATE(created_at) as day,
        SUM(total) as daily_sales
    FROM sales
    WHERE tenant_id = $1
    GROUP BY day
) t
ORDER BY day;

-- =========================
-- HORA PICO
-- =========================
SELECT 
    EXTRACT(HOUR FROM created_at) as hour,
    SUM(total) as total_sales
FROM sales
WHERE tenant_id = $1
GROUP BY hour
ORDER BY total_sales DESC
LIMIT 1;

-- =========================
-- CORTE DE CLIENTES
-- =========================
WITH first_purchase AS (
    SELECT 
        customer_id,
        MIN(DATE(created_at)) as first_date
    FROM sales
    WHERE tenant_id = $1
    GROUP BY customer_id
),
activity AS (
    SELECT 
        s.customer_id,
        DATE(s.created_at) as activity_date,
        f.first_date
    FROM sales s
    JOIN first_purchase f 
        ON s.customer_id = f.customer_id
    WHERE s.tenant_id = $1
)
SELECT 
    first_date,
    activity_date,
    COUNT(DISTINCT customer_id)
FROM activity
GROUP BY first_date, activity_date
ORDER BY first_date, activity_date;

-- =========================
-- PRODUCTOS QUE NO ROTAN
-- =========================
SELECT 
    p.name,
    COALESCE(SUM(si.quantity), 0) as total_sold
FROM products p
LEFT JOIN sale_items si 
    ON si.product_id = p.id
LEFT JOIN sales s 
    ON s.id = si.sale_id
    AND s.tenant_id = $1
WHERE p.tenant_id = $1
GROUP BY p.name
HAVING COALESCE(SUM(si.quantity), 0) < 5
ORDER BY total_sold ASC;

-- =========================
-- DETECCIÓN DE ANOMALÍAS
-- =========================
WITH stats AS (
    SELECT 
        AVG(total) as mean,
        STDDEV(total) as stddev
    FROM sales
    WHERE tenant_id = $1
)
SELECT 
    s.id,
    s.total
FROM sales s, stats
WHERE 
    s.tenant_id = $1
    AND ABS(s.total - stats.mean) > 2 * stats.stddev;