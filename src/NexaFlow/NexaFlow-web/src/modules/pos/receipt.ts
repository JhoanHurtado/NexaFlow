import type { SaleDTO } from '../../api/pos.api';

export function buildReceiptHTML(sale: SaleDTO, customerName: string): string {
  const items = (sale.items ?? []).map(i =>
    `<tr>
      <td>${i.productName}</td>
      <td style="text-align:center">${i.quantity}</td>
      <td style="text-align:right">$${i.unitPrice.toFixed(2)}</td>
      <td style="text-align:right">$${(i.quantity * i.unitPrice).toFixed(2)}</td>
    </tr>`
  ).join('');

  return `<!DOCTYPE html><html><head><meta charset="utf-8"/>
  <title>Comprobante ${sale.id}</title>
  <style>
    body{font-family:sans-serif;font-size:13px;color:#0f172a;margin:0;padding:24px}
    h2{text-align:center;font-size:16px;margin:0 0 4px}
    p.sub{text-align:center;color:#64748b;font-size:11px;margin:0 0 16px}
    .meta{background:#f8fafc;border-radius:8px;padding:10px 14px;margin-bottom:16px;font-size:12px}
    .meta div{display:flex;justify-content:space-between;padding:3px 0}
    .meta span:first-child{color:#94a3b8;text-transform:uppercase;font-size:10px;font-weight:700}
    table{width:100%;border-collapse:collapse;margin-bottom:16px}
    thead tr{border-bottom:2px solid #e2e8f0}
    th{text-align:left;padding:6px 4px;font-size:10px;text-transform:uppercase;color:#94a3b8;font-weight:700}
    td{padding:7px 4px;border-bottom:1px solid #f1f5f9}
    .totals{border-top:2px solid #e2e8f0;padding-top:10px}
    .totals div{display:flex;justify-content:space-between;font-size:12px;padding:2px 0;color:#64748b}
    .totals .grand{font-size:18px;font-weight:900;color:#0052cc;margin-top:6px}
    .stamp{text-align:center;margin-top:20px;padding:12px;border:2px dashed #e2e8f0;border-radius:8px;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:.15em}
  </style></head><body>
  <h2>NexaFlow POS</h2>
  <p class="sub">Comprobante de Venta</p>
  <div class="meta">
    <div><span>No. Documento</span><span>${sale.id}</span></div>
    <div><span>Emisión</span><span>${new Date(sale.createdAt).toLocaleString('es-ES')}</span></div>
    <div><span>Titular</span><span>${customerName}</span></div>
    <div><span>Método</span><span>Electrónico</span></div>
  </div>
  <table>
    <thead><tr><th>Producto</th><th style="text-align:center">Cant.</th><th style="text-align:right">P.Unit</th><th style="text-align:right">Total</th></tr></thead>
    <tbody>${items}</tbody>
  </table>
  <div class="totals">
    <div><span>Subtotal</span><span>$${sale.total.toFixed(2)}</span></div>
    <div><span>IVA (0%)</span><span>$0.00</span></div>
    <div class="grand"><span>Total de Venta</span><span>$${sale.total.toFixed(2)}</span></div>
  </div>
  <div class="stamp">Firma Digital Autorizada</div>
  </body></html>`;
}

export function printTicket(sale: SaleDTO, customerName: string) {
  const w = window.open('', '_blank', 'width=400,height=600');
  if (!w) return;
  w.document.write(buildReceiptHTML(sale, customerName));
  w.document.close();
  w.focus();
  w.print();
}

export function downloadPDF(sale: SaleDTO, customerName: string) {
  const html = buildReceiptHTML(sale, customerName);
  const blob = new Blob([html], { type: 'text/html' });
  const url = URL.createObjectURL(blob);
  const iframe = document.createElement('iframe');
  iframe.style.cssText = 'position:fixed;top:-9999px;left:-9999px;width:800px;height:1000px';
  document.body.appendChild(iframe);
  iframe.src = url;
  iframe.onload = () => {
    iframe.contentWindow?.focus();
    iframe.contentWindow?.print();
    setTimeout(() => { document.body.removeChild(iframe); URL.revokeObjectURL(url); }, 2000);
  };
}
