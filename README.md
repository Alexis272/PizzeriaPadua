# Pizzería Padua — Punto de Venta (POS) y Tienda en Línea

Solución integral de Punto de Venta (POS), Control Financiero de Caja, Seguimiento Gerencial mediante KPIs, Bitácora de Auditoría y Tienda en Línea para **Pizzería Padua**, construida con ASP.NET Core 8, Entity Framework Core (SQLite / PostgreSQL) y una interfaz web responsiva compatible con pantalla táctil y teclado.

## Características Principales

- **Tienda en Línea (`/PizzeriaPadua/`)**: Menú interactivo de pizzas, tamaños, masas, ingredientes extra, combos, bebidas y postres; carrito de compras; seguimiento de pedidos en tiempo real (SSE); reseñas y pagos con Mercado Pago.
- **Punto de Venta POS (`/PizzeriaPadua/gestion.html`)**:
  - **Terminal de Mostrador y Mesas**: Búsqueda en tiempo real por nombre, SKU, categoría o código de barras; selección de variantes (Chica, Mediana, Grande, Familiar), masas y modificadores/extras; notas de cocina; asociación de cliente, mesa, canal y repartidor.
  - **Cuentas Abiertas / Pendientes**: Creación de comandas por mesa o cliente, pre-cuenta imprimible, adición/eliminación de productos con bitácora, transferencia de mesa con autorización y cobro total o dividido.
  - **Cobro Multimodal y Pagos Mixtos**: Efectivo (cálculo automático de cambio y validación), Tarjeta de Débito, Tarjeta de Crédito, Transferencia, QR/Cartera Digital, Vales y Pagos Mixtos (con validación exacta de suma e ingreso de referencias/últimos 4 dígitos).
  - **Control de Caja Completo**:
    - **Arranque de Caja**: Selección de sucursal, caja y turno, captura de fondo inicial por denominaciones ($1000 a $0.50) o monto directo, folio único e impresión de comprobante.
    - **Movimientos de Caja**: Entradas extraordinarias, retiros parciales/depósitos, salidas autorizadas y ajustes con folio y autorizador.
    - **Corte Parcial (Arqueo)**: Arqueo ciego o supervisado sin cerrar caja con cálculo de diferencias por método de pago.
    - **Corte Final y Cierre**: Validación de cuentas abiertas, conteo físico por denominación, conciliación de medios electrónicos, fórmula de efectivo esperado, explicación obligatoria ante diferencias y autorización de supervisor.
  - **Descuentos, Cancelaciones y Devoluciones**:
    - Conservación del precio original base, registro de regla/promoción o cupón, motivo obligatorio y autorización por PIN/credenciales de rol superior.
    - Devolución total o parcial referenciando el ticket original, método de reembolso y disposición de inventario (reincorporar, merma o revisión).
  - **Acumulado del Día en Tiempo Real**: Distinción entre Venta Registrada, Venta Cobrada, Venta Neta y Efectivo Teórico con filtros por sucursal, caja, turno y cajero.
  - **Panel Ejecutivo, 19 KPIs y Reportes Exportables**:
    - Tablero con alertas operativas automáticas, cumplimiento de metas, hora pico, ticket promedio, unidades por ticket, tasas de descuento/devolución/cancelación y diferencia de caja.
    - Exportación a CSV/Excel e impresión de tickets térmicos (80mm).

## Usuarios de Prueba Preconfigurados

| Usuario | Contraseña | PIN Autorizador | Rol |
| :--- | :--- | :--- | :--- |
| `admin` | `padua2026` | `1234` | Administrador |
| `gerente` | `gerente2026` | `2222` | Gerente / Encargado |
| `supervisor` | `super2026` | `3333` | Supervisor de Turno |
| `cajero1` | `caja2026` | `4444` | Cajero |
| `auditor` | `audit2026` | `5555` | Consulta / Auditoría |

## Ejecutar

```powershell
dotnet run --project .\PizzeriaPadua.API\PizzeriaPadua.API.csproj
```

- **Tienda en línea**: `http://localhost:5000/PizzeriaPadua/`
- **Acceso Administrativo / POS**: `http://localhost:5000/PizzeriaPadua/admin.html`
- **Panel POS y Gestión**: `http://localhost:5000/PizzeriaPadua/gestion.html`
