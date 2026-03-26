// Luego crearemos el .module.scss para esta página
import styles from './InventoryPage.module.scss'; 

export const InventoryPage = () => {
  return (
    <div className={styles.container}>
      <h1>Gestión de Inventario</h1>
      <p>Aquí verás tus productos de óptica y odontología.</p>
    </div>
  );
};