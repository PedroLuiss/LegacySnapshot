# Legacy Snapshot

Aplicación de escritorio y móvil para **diagnosticar y explorar bases de datos SQL Server** desde un solo lugar. Permite guardar perfiles de conexión, revisar tablas y su tamaño, ejecutar consultas SQL y analizar el rendimiento de los procedimientos almacenados.

Construida con **.NET 10** y **.NET MAUI**, con una interfaz en español, arquitectura MVVM y conexión directa a SQL Server mediante `Microsoft.Data.SqlClient`.

> **Estado:** versión 1.0 en desarrollo. Compila sin errores ni advertencias para Windows y Android.

---

## Características

### 🔌 Gestión de conexiones
- Guarda múltiples perfiles con nombre, servidor, base de datos y usuario.
- Entornos etiquetados como **Dev**, **Staging** o **Prod**, con color distintivo.
- Autenticación **SQL Server** en todas las plataformas y **Windows integrada** solo en Windows.
- Marca conexiones como **favoritas** y muéstralas primero.
- Botón **Probar conexión** con traducción de errores comunes de SQL Server (login fallido, servidor no encontrado, timeout).
- Selección de una **conexión activa** que comparten las demás pestañas.
- Registro de la fecha de la última conexión exitosa.

### 🗂️ Explorador de tablas
- Lista todas las tablas de la base activa con su **esquema**, **cantidad de filas** y **tamaño total**.
- Búsqueda por nombre de tabla o esquema.
- Ordenamiento por **tamaño**, **nombre** o **filas**.
- Detalle de cada tabla: tamaño de datos e índices, **columnas** (tipo, nulabilidad, etc.) e **índices**.
- Indicador de tablas vacías.

### ⌨️ Query Runner
- Editor SQL con ejecución asíncrona y botón **Cancelar**.
- Resultados en tabla con desplazamiento horizontal.
- Estadísticas de ejecución: filas devueltas o afectadas y **tiempo en milisegundos**.
- Límite de seguridad de **10.000 filas** (avisa cuando se trunca el resultado).
- **Historial local** de las últimas 20 consultas, reutilizable con un toque.
- **Confirmación obligatoria** antes de ejecutar en un entorno `Prod`.
- Advertencia adicional si la consulta contiene `xp_cmdshell`.
- **Exportación a CSV** con codificación UTF-8 (incluye BOM para Excel) y hoja de compartir del sistema.
- Protección contra *CSV injection* en la exportación.

### ⚙️ Procedimientos almacenados
- Top 20 de procedimientos de usuario por consumo, usando `sys.dm_exec_procedure_stats`.
- Métricas: ejecuciones, tiempo total, promedio, mínimo y máximo, CPU total y lecturas lógicas.
- Ordenamiento por tiempo total, promedio, CPU o ejecuciones.
- Visualización del **código fuente** del procedimiento con `OBJECT_DEFINITION`.

### 🎨 Extras
- Tema **Sistema / Claro / Oscuro** persistido entre sesiones.
- Interfaz con tarjetas, tipografía y estilos personalizados.
- Navegación por pestañas: **Conexiones · Explorador · Query · Procedimientos · Ajustes**.

---

## Tecnologías

| Área | Tecnología |
|---|---|
| Framework | .NET 10 / .NET MAUI |
| Plataformas | Android, iOS, Mac Catalyst y Windows |
| Patrón UI | MVVM con `CommunityToolkit.Mvvm` 8.4.2 (`[ObservableProperty]`, `[RelayCommand]`) |
| Utilidades UI | `CommunityToolkit.Maui` 15.0.1 |
| Acceso a datos | `Microsoft.Data.SqlClient` 7.1.1 |
| Inyección de dependencias | `Microsoft.Extensions.DependencyInjection` |
| Almacenamiento local | `Preferences` y `SecureStorage` de MAUI |
| Serialización | `System.Text.Json` |
| Generación de XAML | `MauiXamlInflator=SourceGen` |

> `System.Security.Cryptography.Pkcs` 10.0.14 se referencia explícitamente para reemplazar la versión transitiva vulnerable que arrastraba `Microsoft.Data.SqlClient`.

---

## Arquitectura

El proyecto sigue una separación por capas con **MVVM**, **inyección de dependencias**, **repositorios** y el patrón **Strategy** para la exportación.

```
LegacySnapshot.slnx
└── LegacySnapshot/
    ├── App.xaml / App.xaml.cs          # Punto de entrada y ventana principal
    ├── AppShell.xaml / .cs             # TabBar con las cinco pestañas
    ├── MauiProgram.cs                  # Registro de servicios, VMs y páginas (DI)
    ├── Models/                         # Entidades de dominio
    ├── Interfaces/                     # Contratos de repositorios y servicios
    ├── Services/                       # Implementaciones (SQL Server, almacenamiento, exportación)
    ├── ViewModels/                     # Lógica de presentación con MVVM Toolkit
    ├── Views/                          # Páginas XAML
    ├── Resources/                      # Estilos, colores, iconos, fuentes e imágenes
    └── Platforms/                      # Código específico por plataforma
```

### Servicios principales

| Interfaz | Implementación | Responsabilidad |
|---|---|---|
| `IConnectionRepository` | `LocalConnectionRepository` | Guarda metadatos en `Preferences` y contraseñas en `SecureStorage`. |
| `IDatabaseRepository` | `SqlServerDatabaseRepository` | Consulta tablas, columnas, índices, procedimientos y definiciones. |
| `IActiveConnectionService` | `ActiveConnectionService` | Mantiene la conexión activa y construye su cadena de conexión. |
| `IQueryExecutor` | `SqlQueryExecutor` | Ejecuta SQL, limita filas y mide tiempos. |
| `IQueryHistoryRepository` | `LocalQueryHistoryRepository` | Historial local de las últimas 20 consultas. |
| `IExportStrategy` | `CsvExportStrategy` | Serializa resultados a CSV de forma segura. |

### Flujo típico

1. Se crea o selecciona una conexión en **Conexiones** y se pulsa **Conectar**.
2. La conexión queda activa y las demás pestañas la usan automáticamente.
3. En **Explorador** se navega por la estructura de la base.
4. En **Query** se ejecutan consultas y se exportan resultados.
5. En **Procedimientos** se identifican los SP con mayor consumo.

---

## Requisitos

- **.NET SDK 10.0** (probado con 10.0.402).
- Workloads de MAUI instalados:
  ```bash
  dotnet workload install maui
  ```
  Según la plataforma: `android`, `ios`, `maccatalyst` y/o `maui-windows`.
- **SQL Server** accesible desde el dispositivo:
  - Autenticación SQL habilitada (o Windows integrada, solo en Windows).
  - TCP/IP habilitado y puerto 1433 accesible si se conecta en red.
  - Permisos de lectura sobre las vistas del sistema. Para estadísticas de procedimientos se recomienda `VIEW SERVER STATE` o `VIEW SERVER PERFORMANCE STATE`.
- Visual Studio 2026 (o superior) con la carga de trabajo **.NET Multi-platform App UI**, o la CLI de `dotnet`.

---

## Cómo ejecutar

```bash
# Restaurar dependencias
dotnet restore LegacySnapshot.slnx

# Compilar y ejecutar en Windows
dotnet build -f net10.0-windows10.0.19041.0

# Compilar y ejecutar en Android (requiere emulador o dispositivo)
dotnet build -t:Run -f net10.0-android
```

También puedes abrir `LegacySnapshot.slnx` en Visual Studio, elegir el framework de destino y presionar **F5**.

### Conectarse a SQL Server desde el emulador de Android

El emulador no puede usar `localhost` para llegar a la máquina anfitriona. Usa:

- Servidor: `10.0.2.2,1433`
- Habilita **TCP/IP** en SQL Server Configuration Manager.
- Permite el puerto en el firewall de Windows.
- Habilita **SQL Server Authentication** y crea un login de prueba.
- En entorno **Dev** puedes activar **TrustServerCertificate** si el certificado no es de confianza.

> En un dispositivo físico, usa la IP de la máquina en la red local y asegúrate de que el puerto 1433 sea accesible.

---

## Seguridad

- Las contraseñas **nunca se guardan en el repositorio**: se almacenan con `SecureStorage` del dispositivo.
- El campo de contraseña no se serializa ni se escribe en `Preferences`.
- Las cadenas de conexión se generan con `SqlConnectionStringBuilder` y siempre usan `Encrypt=true` y `PersistSecurityInfo=false`.
- **TrustServerCertificate** solo se permite en entorno **Dev**; la clase `SqlConnectionStringFactory` lanza una excepción si se intenta en Staging o Prod.
- La autenticación integrada de Windows solo está permitida en Windows.
- Antes de ejecutar en **Prod** se muestra una confirmación; `xp_cmdshell` añade una advertencia adicional.
- La exportación CSV neutraliza fórmulas que comienzan con `=`, `+`, `-`, `@`, tabulador o retorno de carro.
- Las consultas se ejecutan con los permisos del usuario SQL configurado. **Revisa siempre el destino y el contenido antes de ejecutar en producción.**

> ⚠️ Esta aplicación se conecta **directamente** a SQL Server (protocolo TDS), sin backend intermedio. No expongas el puerto 1433 a Internet; usa VPN, red privada o una API intermedia en escenarios reales.

---

## Limitaciones conocidas

- **iOS y Mac Catalyst:** `Microsoft.Data.SqlClient` no ofrece soporte funcional para conexiones SQL directas en estas plataformas (limitaciones de sockets nativos). Los targets compilan, pero la conexión a SQL Server no funcionará; se necesitaría un backend/API intermedio.
- **Autenticación Windows:** solo disponible en Windows.
- **Resultados:** máximo de 10.000 filas por consulta (se indica cuándo se trunca).
- **Historial:** últimas 20 consultas, almacenadas localmente por dispositivo; no se sincroniza.
- La base de datos y las conexiones guardadas son locales a cada instalación.
- No hay pruebas automatizadas todavía.

---

## Autor

**Pedro Luis Rodríguez** — [@PedroLuiss](https://github.com/PedroLuiss)

Repositorio: <https://github.com/PedroLuiss/LegacySnapshot>
