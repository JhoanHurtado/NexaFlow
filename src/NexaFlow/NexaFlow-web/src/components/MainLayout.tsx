import React from 'react';
import styles from './MainLayout.module.scss';
import { 
    BarChart3, 
    Calendar, 
    Package, 
    Settings, 
    LogOut, 
    Users, 
    ShoppingCart,
    ClipboardList,
    UserCircle
} from 'lucide-react';

interface Props {
    children: React.ReactNode;
}

export const MainLayout: React.FC<Props> = ({ children }) => {
    return (
        <div className={styles.layout}>
            <aside className={styles.sidebar}>
                <div className={styles.logoContainer}>
                    <div className={styles.logoIcon}>N</div>
                    <span className={styles.logoText}>NexaFlow</span>
                </div>

                <nav className={styles.navigation}>
                    {/* SECCIÓN: GESTIÓN DE TIEMPO */}
                    <div className={styles.navSection}>
                        <p className={styles.sectionTitle}>Planificación</p>
                        <div className={styles.navItem}>
                            <Calendar size={18} />
                            <span>Agenda</span>
                        </div>
                        <ul className={styles.submenu}>
                            <li className={styles.subItem}>Vista General</li>
                            <li className={styles.subItem}>Gestión de Turnos</li>
                        </ul>
                        
                        <div className={styles.navItem}>
                            <UserCircle size={18} />
                            <span>Clientes</span>
                        </div>
                        <ul className={styles.submenu}>
                            <li className={styles.subItem}>Base de Datos</li>
                            <li className={styles.subItem}>Expedientes</li>
                        </ul>
                    </div>

                    {/* SECCIÓN: OPERACIONES COMERCIALES */}
                    <div className={styles.navSection}>
                        <p className={styles.sectionTitle}>Comercial</p>
                        <div className={`${styles.navItem} ${styles.active}`}>
                            <Package size={18} />
                            <span>Inventario</span>
                        </div>
                        <ul className={styles.submenu}>
                            <li className={styles.subItem}>Productos</li>
                            <li className={styles.subItem}>Servicios</li>
                        </ul>

                        <div className={styles.navItem}>
                            <ShoppingCart size={18} />
                            <span>Punto de Venta</span>
                        </div>
                        <ul className={styles.submenu}>
                            <li className={styles.subItem}>Nueva Venta</li>
                            <li className={styles.subItem}>Historial de Pagos</li>
                        </ul>
                    </div>

                    {/* SECCIÓN: ANÁLISIS */}
                    <div className={styles.navSection}>
                        <p className={styles.sectionTitle}>Administración</p>
                        <div className={styles.navItem}>
                            <Users size={18} />
                            <span>Personal</span>
                        </div>
                        <div className={styles.navItem}>
                            <BarChart3 size={18} />
                            <span>Analytics</span>
                        </div>
                        <div className={styles.navItem}>
                            <ClipboardList size={18} />
                            <span>Reportes</span>
                        </div>
                    </div>
                </nav>

                <div className={styles.userSection}>
                    <div className={styles.userInfo}>
                        <p className={styles.userName}>Jhoan Hurtado</p>
                        <p className={styles.userRole}>Administrador</p>
                    </div>
                    <button className={styles.logoutBtn} title="Cerrar Sesión">
                        <LogOut size={18} />
                    </button>
                </div>
            </aside>

            <main className={styles.content}>
                <header className={styles.topBar}>
                    <div className={styles.breadcrumb}>
                        <span>Módulo</span> / <strong>Inventario</strong>
                    </div>
                    <div className={styles.topActions}>
                        <Settings size={20} className={styles.settingsIcon} />
                    </div>
                </header>
                <div className={styles.pageBody}>
                    {children}
                </div>
            </main>
        </div>
    );
};