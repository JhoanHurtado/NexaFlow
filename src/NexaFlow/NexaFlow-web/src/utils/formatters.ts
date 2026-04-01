export type FormatType = 'currency' | 'percent' | 'number' | 'compact' | 'phone' | 'date' | 'time' | 'full';

interface FormatterOptions {
  digits?: number;
  currency?: string;
  locale?: string;
  isRaw?: boolean;
}

/** Maps currency code → best-fit locale for formatting */
const CURRENCY_LOCALE: Record<string, string> = {
  COP: 'es-CO',
  USD: 'en-US',
  EUR: 'de-DE',
  MXN: 'es-MX',
  ARS: 'es-AR',
  PEN: 'es-PE',
  CLP: 'es-CL',
  BRL: 'pt-BR',
};

/**
 * Formats a numeric value as currency, automatically picking the right locale
 * for the given currency code.
 *
 * @example
 * formatCurrency(32800, 'COP')  // "$ 32.800"
 * formatCurrency(32.8,  'USD')  // "$32.80"
 * formatCurrency(32.8,  'EUR')  // "32,80 €"
 */
export const formatCurrency = (
  value: number | string | null | undefined,
  currency = 'COP',
  digits?: number,
): string => {
  if (value === null || value === undefined || value === '') return '---';
  const num = typeof value === 'string' ? parseFloat(value) : value;
  if (isNaN(num)) return '---';
  const locale = CURRENCY_LOCALE[currency.toUpperCase()] ?? 'es-CO';
  const fractionDigits = digits ?? (currency === 'COP' || currency === 'CLP' ? 0 : 2);
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: currency.toUpperCase(),
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(num);
};

export const formatValue = (
  value: number | string | null | undefined,
  type: FormatType = 'currency',
  options: FormatterOptions = {}
): string => {
  const {
    digits = 0,
    currency = 'COP',
    locale = CURRENCY_LOCALE[currency.toUpperCase()] ?? 'es-CO',
    isRaw = false,
  } = options;

  if (value === null || value === undefined || value === '') return '---';

  // Helper para asegurar que tengamos un objeto Date válido
  const toDate = (v: any) => {
    const d = new Date(v);
    return isNaN(d.getTime()) ? null : d;
  };

  switch (type) {
    case 'currency': {
      return formatCurrency(value, currency, digits);
    }

    case 'date': {
      const d = toDate(value);
      return d ? d.toLocaleDateString(locale, { day: '2-digit', month: 'short', year: 'numeric' }) : '---';
    }

    case 'time': {
      const d = toDate(value);
      return d ? d.toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit', hour12: true }) : '---';
    }

    case 'full': {
      const d = toDate(value);
      if (!d) return '---';
      const day   = d.toLocaleDateString(locale, { day: 'numeric' });
      const month = d.toLocaleDateString(locale, { month: 'short' }).replace('.', '');
      const time  = d.toLocaleTimeString(locale, {
        hour: '2-digit', minute: '2-digit', hour12: true,
      }).toLowerCase();
      return `${day} ${month}${time}`;
    }

    case 'phone': {
      const cleaned = ('' + value).replace(/\D/g, '');
      const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
      return match ? `+57 ${match[1]} ${match[2]} ${match[3]}` : value.toString();
    }

    case 'percent': {
      const num = typeof value === 'string' ? parseFloat(value) : value;
      return new Intl.NumberFormat(locale, {
        style: 'percent', minimumFractionDigits: digits || 1,
      }).format(isRaw ? (num as number) : (num as number) / 100);
    }

    case 'number': {
      const num = typeof value === 'string' ? parseFloat(value) : value;
      return new Intl.NumberFormat(locale, {
        minimumFractionDigits: digits, maximumFractionDigits: 2,
      }).format(num as number);
    }

    default:
      return value.toString();
  }
};