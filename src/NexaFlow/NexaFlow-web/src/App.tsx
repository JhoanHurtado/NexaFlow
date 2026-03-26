import { MainLayout } from './components/MainLayout';
import { InventoryPage } from './modules/inventory/InventoryPage';
import './styles/main.scss';

function App() {
  return (
    <MainLayout>
      {/* Aquí es donde luego pondrás el Router para cambiar entre Agenda e Inventario */}
      <InventoryPage />
    </MainLayout>
  );
}

export default App;