# 🛠️ FerreAlmacén — Sistema Web de Gestión de Almacén e Inventario

> **Proyecto de Diseño y Arquitectura de Software (DIARS) — Ciclo 2026-2**  
> **Universidad Privada del Norte (UPN)**  
> **Empresa:** Ferretería El Constructor S.A.C. (Cajamarca, Perú)  
> **Autor:** Olano Cauchos Oscar Andre  

---

## 📋 Descripción del Proyecto

**FerreAlmacén** es un sistema web integral desarrollado con **ASP.NET Core 8 MVC** y **Entity Framework Core**, diseñado para resolver la problemática de control de inventario físico y valorizado en micro y pequeñas ferreterías (MYPEs).

El sistema sustituye las planillas manuales y cuadernos por un control automatizado en tiempo real, garantizando:
- **Trazabilidad total** de entradas y salidas de mercadería.
- **Kárdex Valorizado** bajo el método contable de **Costo Promedio Ponderado** (estándar SUNAT).
- **Alertas automáticas de stock mínimo** para evitar quiebres de existencias en artículos clave de construcción.
- **Control de existencias por ubicación en almacén** (Pasillo, Estante, Nivel).
- **Validación estricta de stock disponible** para prevenir inventario en negativo.

---

## 🏛️ Arquitectura del Software

El sistema implementa una **Arquitectura en Capas** con el patrón **Modelo-Vista-Controlador (MVC)**, alineada a los estándares **ISO/IEC 25010** y las guías de Clean Architecture:

```
├── Capa de Presentación (Controllers + Razor Views + Bootstrap 5)
├── Capa de Aplicación y Negocio (InventarioService, DTOs, Validaciones)
├── Capa de Dominio (Entidades: Producto, Movimiento, Detalle, Kardex, Proveedor, Ubicacion)
└── Capa de Infraestructura / Persistencia (FerreteriaDbContext, EF Core 8, SQLite / SQL Server)
```

---

## 🚀 Tecnologías Utilizadas

- **Backend:** C# / .NET 8 (o superior) con ASP.NET Core MVC.
- **ORM:** Entity Framework Core 8 con Migraciones y sembrado automático.
- **Base de Datos:**
  - **SQLite:** Por defecto (portabilidad inmediata sin configuración adicional).
  - **Microsoft SQL Server 2022:** Script DDL y semilla incluido en `database/schema_sqlserver.sql`.
- **Frontend:** HTML5, CSS3, Bootstrap 5.3, Bootstrap Icons, Google Fonts (Inter).
- **Gestor de versiones:** Git & GitHub.

---

## 📦 Módulos del Sistema

1. **Panel de Control (Dashboard):**
   - KPI Cards: Valor total del inventario en S/, total de artículos, alertas de stock bajo y movimientos del día.
   - Resumen visual por categorías y tabla de reposición rápida.
2. **Catálogo de Productos:**
   - Registro, edición, ficha técnica detallada y desactivación lógica.
   - Búsqueda en vivo por código interno, descripción o escaneo de código de barras.
   - Filtros por categoría y por estado de inventario (Normal, Bajo, Agotado).
3. **Recepción / Entradas de Almacén:**
   - Selección de proveedor y registro de documento de compra (Factura / Guía).
   - Ingreso dinámico de múltiples productos con recálculo en vivo de costos.
   - Actualización atómica de stock y recálculo de costo promedio ponderado.
4. **Despacho / Salidas de Almacén:**
   - Selección del motivo de salida (Venta, Consumo, Merma, Devolución).
   - Control estricto de existencias (bloquea salidas si la cantidad supera el stock).
5. **Kárdex Valorizado:**
   - Reporte con formato oficial SUNAT: Entradas, Salidas y Saldos con costo promedio.
   - Filtro por producto y rango de fechas con opción de impresión directa.
6. **Alertas de Stock Mínimo:**
   - Vista especializada que calcula el déficit exacto de productos por reponer.
7. **Directorio de Proveedores:**
   - Mantenimiento con validación de RUC de 11 dígitos, teléfono y correo electrónico.

---

## 💻 Instrucciones para Ejecutar Localmente

### Requisitos previos:
- [.NET SDK 8.0 o superior](https://dotnet.microsoft.com/download) instalado.

### Pasos:

1. **Abrir la terminal o PowerShell** en la carpeta del proyecto:
   ```bash
   cd src/FerreteriaWeb
   ```

2. **Ejecutar la aplicación:**
   ```bash
   dotnet run
   ```

3. **Abrir en el navegador:**
   La aplicación se iniciará automáticamente y escuchará en:
   ```
   http://localhost:5290
   ```
   *(La base de datos `ferreteria.db` se creará e inicializará con datos de prueba automáticamente al primer arranque).*

---

## 🗄️ Base de Datos en SQL Server (Opcional)

Si deseas utilizar Microsoft SQL Server en lugar de SQLite:
1. Abre **SQL Server Management Studio (SSMS)**.
2. Abre y ejecuta el script:
   ```
   database/schema_sqlserver.sql
   ```
3. En `appsettings.json`, cambia la cadena de conexión o activa `SqlServerConnection`.

---

## 📤 Cómo Subir este Proyecto a GitHub

1. Crea un nuevo repositorio vacío en tu cuenta de [GitHub](https://github.com/new) (por ejemplo: `ferreteria-sistema-web`).
2. En la carpeta raíz de este proyecto ejecuta los siguientes comandos en tu terminal:
   ```bash
   git init
   git add .
   git commit -m "feat: Sistema Web Ferretería El Constructor - DIARS 2026-2"
   git branch -M main
   git remote add origin https://github.com/TU_USUARIO/ferreteria-sistema-web.git
   git push -u origin main
   ```

---

## 📄 Licencia y Entrega
Este proyecto forma parte de los entregables académicos del curso **Diseño y Arquitectura de Software (DIARS)** - Facultad de Ingeniería - UPN.
