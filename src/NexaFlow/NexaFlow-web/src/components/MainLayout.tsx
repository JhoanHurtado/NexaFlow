import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import styles from './MainLayout.module.scss';
import { BarChart3, Calendar, Package, Settings, LogOut, ShoppingCart } from 'lucide-react';

interface Props { children: React.ReactNode; }

const NAV = [
  {
    section: 'Planificación',
    items: [
      { to: '/app/reservations', icon: <Calendar size={18} />, label: 'Reservas' },
    ],
  },
  {
    section: 'Comercial',
    items: [
      { to: '/app/inventory', icon: <Package size={18} />,     label: 'Inventario' },
      { to: '/app/pos',       icon: <ShoppingCart size={18} />, label: 'Punto de Venta' },
    ],
  },
  {
    section: 'Administración',
    items: [
      { to: '/app/analytics', icon: <BarChart3 size={18} />, label: 'Analytics' },
    ],
  },
];

export const MainLayout: React.FC<Props> = ({ children }) => {
  const navigate = useNavigate();
  const userName = localStorage.getItem('userName') ?? 'Usuario';
  const userRole = localStorage.getItem('role') ?? 'admin';

  const handleLogout = () => {
    localStorage.clear();
    navigate('/login');
  };

  return (
    <div className={styles.layout}>
      <aside className={styles.sidebar}>
        <div className={styles.logoContainer}>
          <div className={styles.logoIcon}>N</div>
          <span className={styles.logoText}>NexaFlow</span>
        </div>

        <nav className={styles.navigation}>
          {NAV.map(group => (
            <div key={group.section} className={styles.navSection}>
              <p className={styles.sectionTitle}>{group.section}</p>
              {group.items.map(item => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
                >
                  {item.icon}
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        <div className={styles.userSection}>
          <div className={styles.userInfo}>
            <div className={styles.userAvatar}>{userName.charAt(0).toUpperCase()}</div>
            <div className={styles.userMeta}>
              <p className={styles.userName}>{userName}</p>
              <p className={styles.userRole}>{userRole}</p>
            </div>
          </div>
          <button className={styles.logoutBtn} title="Cerrar Sesión" onClick={handleLogout}>
            <LogOut size={16} />
          </button>
        </div>
      </aside>

      <main className={styles.content}>
        <header className={styles.topBar}>
          <Settings size={20} className={styles.settingsIcon} />
        </header>
        <div className={styles.pageBody}>
          {children}
        </div>
      </main>
    </div>
  );
};
