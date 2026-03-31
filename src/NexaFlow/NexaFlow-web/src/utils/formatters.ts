export type FormatType = 'currency' | 'percent' | 'number' | 'compact' | 'phone' | 'date' | 'time' | 'full';

interface FormatterOptions {
  digits?: number;
  currency?: string;
  locale?: string;
  isRaw?: boolean;
}

export const formatValue = (
  value: number | string | null | undefined,
  type: FormatType = 'currency',
  options: FormatterOptions = {}
): string => {
  const {
    digits = 0,
    currency = 'COP',
    locale = 'es-CO',
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
      const num = typeof value === 'string' ? parseFloat(value) : value;
      return isNaN(num as number) ? '---' : new Intl.NumberFormat(locale, {
        style: 'currency', currency, minimumFractionDigits: digits, maximumFractionDigits: digits,
      }).format(num as number);
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
      
      // Formato manual para lograr: "31 mar04:39 a.m."
      const day = d.toLocaleDateString(locale, { day: 'numeric' });
      const month = d.toLocaleDateString(locale, { month: 'short' }).replace('.', '');
      const time = d.toLocaleTimeString(locale, { 
        hour: '2-digit', 
        minute: '2-digit', 
        hour12: true 
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