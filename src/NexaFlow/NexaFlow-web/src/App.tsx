import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LandingPage }        from './modules/landing/LandingPage';
import { LoginPage }          from './modules/landing/LoginPage';
import { TenantPortalPage }   from './modules/portal/TenantPortalPage';
import { MainLayout }       from './components/MainLayout';
import { InventoryPage }    from './modules/inventory/InventoryPage';
import { ReservationsPage } from './modules/reservations/ReservationsPage';
import { PosPage }          from './modules/pos/PosPage';
import { AnalyticsPage }    from './modules/analytics/AnalyticsPage';
import { SettingsPage }     from './modules/settings/SettingsPage';
import './styles/main.scss';

const PrivateRoute = ({ children }: { children: React.ReactNode }) => {
  const token = localStorage.getItem('token');
  return token ? <>{children}</> : <Navigate to="/login" replace />;
};

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Públicas */}
        <Route path="/"               element={<LandingPage />} />
        <Route path="/login"          element={<LoginPage />} />
        <Route path="/book/:tenantId" element={<TenantPortalPage />} />
        <Route path="/book/menu/:tenantId" element={<TenantPortalPage />} />

        {/* Panel privado */}
        <Route
          path="/app"
          element={
            <PrivateRoute>
              <MainLayout><Navigate to="/app/reservations" replace /></MainLayout>
            </PrivateRoute>
          }
        />
        <Route path="/app/reservations" element={<PrivateRoute><MainLayout><ReservationsPage /></MainLayout></PrivateRoute>} />
        <Route path="/app/inventory"    element={<PrivateRoute><MainLayout><InventoryPage /></MainLayout></PrivateRoute>} />
        <Route path="/app/pos"          element={<PrivateRoute><MainLayout><PosPage /></MainLayout></PrivateRoute>} />
        <Route path="/app/analytics"    element={<PrivateRoute><MainLayout><AnalyticsPage /></MainLayout></PrivateRoute>} />
        <Route path="/app/settings"     element={<PrivateRoute><MainLayout><SettingsPage /></MainLayout></PrivateRoute>} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
