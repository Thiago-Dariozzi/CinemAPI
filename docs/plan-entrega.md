# Plan de la entrega final — CinemAPI + CinemAPIFront

Entrega: **27/10/2026**. Ramas: `feature/entrega-final` en los dos repos (desde `refactor/buenas-practicas`, que no está mergeada a `main`). Las tareas son **verticales** (§6): cada una deja back y front funcionando juntos.

Consigna: (1) CRUD de las entidades pactadas + casos de uso del cine; (2) autenticación y roles para todos los tipos de usuario; (3) calidad de diseño y de código.

Se mantiene todo lo decidido en `CLAUDE.md` (Services + Repositories + Controllers, DTOs con DataAnnotations, ProblemDetails, soft delete con `IsActive`, Ids `Guid` generados en el servicio, `TreatWarningsAsErrors`). Cada tarea cierra con la Definition of Done de su repo y un commit por lote.

---

## 1. Roles

| Rol | Puede |
|---|---|
| **SuperAdmin** | Todo lo de Admin + alta/baja/edición de Admins + CRUD de cargos (`Fee`). No puede borrarse a sí mismo ni dejar el sistema sin ningún SuperAdmin activo. Uno inicial por seed con credenciales de configuración. |
| **Admin** | Catálogo (películas, géneros, salas, funciones), ve todas las ventas y los reportes, ve y da de baja clientes, cancela cualquier compra. |
| **Client** | Se registra solo, ve la cartelera, compra entradas, ve y cancela **sus** compras, edita su perfil, cambia su contraseña. |

Los roles son constantes en `Domain/Entities/Roles.cs` (`SuperAdmin`, `Admin`, `Client`); la columna `Users.Role` ya es `nvarchar(30)`.

Políticas (`WebApi`, `AddAuthorization`):

| Política | Roles |
|---|---|
| `Staff` | Admin, SuperAdmin |
| `SuperAdminOnly` | SuperAdmin |
| `ClientOnly` | Client |

`[Authorize(Policy = ...)]` en cada controller/acción; los GET públicos del catálogo con `[AllowAnonymous]`.

## 2. Autenticación

- **JWT propio**: `Microsoft.AspNetCore.Authentication.JwtBearer`. Claims: `sub` (id), `email`, `name`, `role`. Expira en **8 h**. Sin refresh token.
- **Configuración** (`JwtOptions`, sección `Jwt`): `Issuer`, `Audience`, `Key`, `ExpirationHours`. `Key` solo en **user-secrets** en desarrollo (`dotnet user-secrets set "Jwt:Key" ...`); se valida al arrancar (≥ 32 bytes) para fallar temprano con un mensaje claro.
- **Contraseñas**: `PasswordHasher<User>` (`Microsoft.Extensions.Identity.Core`), sin ASP.NET Identity completo.
- **Capas**:
  - `Application/Interfaces`: `IPasswordHasher` (Hash/Verify), `ITokenService` (CreateToken), `ICurrentUser` (UserId, Role, IsStaff).
  - `Infrastructure/Security/PasswordHasherAdapter` envuelve `PasswordHasher<User>` (Application no conoce Identity).
  - `WebApi/Security/JwtTokenService` y `HttpCurrentUser` (lee los claims con `IHttpContextAccessor`).
  - `Application/Services/AuthService`: register, login, me, cambiar contraseña, editar perfil.
- **Propiedad de los datos**: los servicios usan `ICurrentUser.UserId` (el id del token). Ningún endpoint de cliente recibe un `userId` en el body ni en la ruta.
- **Errores nuevos** (`Domain/Exceptions` → `GlobalExceptionHandler`): `InvalidCredentialsException` → 401 (mensaje único "Email o contraseña incorrectos", sin revelar cuál falló), `ForbiddenException` → 403 (ej. cancelar una compra ajena, borrar al último SuperAdmin). 401/403 de `[Authorize]` también salen como ProblemDetails (ya lo cubre `UseStatusCodePages`).
- **SuperAdmin inicial**: `IdentitySeeder` (corre en **todos** los ambientes, idempotente): si no hay ningún SuperAdmin activo, lo crea con `SeedSuperAdmin:Email/Name/Password` de configuración (user-secrets en desarrollo). Si faltan las credenciales y no hay SuperAdmin, la API no arranca y lo dice. Se documenta en el README.
- **Rehash de las contraseñas actuales** (texto plano): `LegacyPasswordUpgrader`, al arrancar, después de las migraciones. Un hash v3 de `PasswordHasher` es base64 de 61 bytes que empieza con `0x01`; todo lo que no tenga ese formato se considera texto plano y se reemplaza por su hash. Idempotente; loguea cuántos actualizó. No se puede hacer en SQL porque el hash se calcula en C#.

## 3. Modelo de datos final

```
User 1───* Purchase *───1 Showtime *───1 Movie *───1 Genre
               │              │
               1              *───1 Screen
               │
               *
             Ticket *───1 Showtime        Fee (a lo sumo una vigente)
```

| Entidad | Campos (✚ nuevo, ✎ cambia, ✖ se va) | Reglas |
|---|---|---|
| `User` | Id, Name, Email (único), ✎ `Password` → `PasswordHash`, Role (`Roles.*`), IsActive | Register siempre crea `Client` |
| `Genre` | sin cambios | |
| `Movie` | sin cambios (`ReleaseDate` pasa a cargarse desde el front, D2) | |
| `Screen` | Id, Name, ✚ `Rows`, ✚ `SeatsPerRow`, ✎ `Capacity` calculada (`Rows * SeatsPerRow`, no mapeada), IsActive | `Rows` 1–26, `SeatsPerRow` 1–40. No se reduce el tamaño si hay entradas vendidas en funciones futuras fuera del nuevo rango (409) |
| `Showtime` | sin cambios | No se borra una función con entradas confirmadas (409) |
| `Fee` | ✎ `Id` int → **Guid**, Name, `Percentage` (0–100, ej. `10` = 10 %), `FixedAmount` (≥ 0), ✚ `IsCurrent` ("activo/vigente"), IsActive | A lo sumo una con `IsCurrent`: el servicio desmarca la anterior y un índice único filtrado lo garantiza. Se llama `IsCurrent` para no confundirlo con `IsActive` (baja lógica), igual que `Movie.IsShowing` |
| ✚ `Purchase` | Id, UserId, ShowtimeId, `CreatedAt`, `Subtotal`, `FeeId?`, `FeeAmount`, `Total`, `Status` (`Confirmed`/`Cancelled`, string), `CancelledAt?`, IsActive, `Tickets` | `Total = Subtotal + Subtotal × Percentage / 100 + FixedAmount`, redondeado a 2 decimales (`AwayFromZero`). Se guarda: si cambia el cargo, las compras viejas no se recalculan |
| `Ticket` | Id, ✚ `PurchaseId`, ✚ `ShowtimeId`, ✚ `SeatRow`, ✚ `SeatNumber`, ✎ `FinalPrice` → `Price` (= `Showtime.Price` al comprar), ✚ `IsCancelled`, IsActive; ✖ `MovieId`, ✖ `ScreenId`, ✖ `UserId`, ✖ `BuyDate` | Índice único **filtrado** `(ShowtimeId, SeatRow, SeatNumber) WHERE IsCancelled = 0 AND IsActive = 1`: la carrera de dos compras de la misma butaca termina en `UniqueConstraintConflictException` → 409, como en #45 |
| `SeatCategory` | ✖ borrada (commit `d463e0d`) | Mejora futura: categorías de butaca con multiplicador de precio |

`ShowtimeId` está en `Purchase` (una compra es de una función) y también en `Ticket` para que el índice único de butaca no necesite un join.

### Reglas de compra (`PurchaseService`)

1. Cliente autenticado (`ClientOnly`); el usuario sale del token.
2. Función activa y que **no empezó** (`StartTime > ahora`).
3. Película activa y `IsShowing = true`.
4. Entre 1 y **10** butacas, sin repetidas, dentro de `Rows`/`SeatsPerRow` de la sala.
5. Ninguna butaca ocupada en esa función (validado en el servicio → 409 con la lista de butacas tomadas; el índice único cubre la carrera).
6. Precio de cada entrada = `Showtime.Price`; se aplica el cargo vigente si hay.

**Cancelación** (`POST /api/purchase/mine/{id}/cancel` el cliente, `POST /api/purchase/{id}/cancel` el staff): el cliente solo las suyas y hasta **2 horas antes** de la función; Admin/SuperAdmin cualquiera, sin límite de tiempo. Marca `Status = Cancelled`, `CancelledAt`, y `IsCancelled = true` en los tickets (libera las butacas). Cancelar dos veces → 409.

**Hora**: `TimeProvider` inyectado (viene con .NET, sin paquete) para poder testear "no empezó" y "2 horas antes". `StartTime` se guarda en hora local del cine (así la manda el front hoy); la comparación usa la hora local del servidor. Se documenta.

### Migraciones (en este orden, una por tarea)

| Migración | Contenido | Datos existentes |
|---|---|---|
| `AddScreenSeatLayout` | `Rows`, `SeatsPerRow`; borra `Capacity` | `SeatsPerRow = 10`, `Rows = CEILING(Capacity / 10.0)` (mínimo 1) antes de borrar la columna |
| `RenameUserPasswordToPasswordHash` | `RenameColumn` | El rehash lo hace `LegacyPasswordUpgrader` al arrancar |
| `AddFees` | Tabla `Fees` (Guid) + índice único filtrado `IsCurrent = 1 AND IsActive = 1` | — (la clase existía pero no la tabla) |
| `AddPurchasesAndSeatTickets` | Tabla `Purchases`; `Tickets` cambia a función + compra + butaca; índice único filtrado | **Decisión pendiente** (ver §8): los tickets actuales no tienen función ni butaca |

Todas revisadas a mano antes de aplicarlas (en especial los `Sql(...)` de datos) y probadas contra una base LocalDB nueva y contra una con los datos actuales.

## 4. Endpoints

Leyenda: 🌐 anónimo · 🔑 cualquier usuario autenticado · 👤 Client · 🛠 Staff (Admin + SuperAdmin) · 👑 SuperAdmin. **✚** nuevo · **✎** cambia el contrato · **✖** se elimina.

### Auth — `AuthController` ✚

| Verbo y ruta | Rol | Notas |
|---|---|---|
| `POST /api/auth/register` | 🌐 | Name, Email, Password (mín. 8). Siempre `Client`. 201 + token |
| `POST /api/auth/login` | 🌐 | Email, Password → `{ token, expiresAt, user }`. 401 si falla o el usuario está dado de baja |
| `GET /api/auth/me` | 🔑 | Datos del usuario del token |
| `PUT /api/auth/me` | 🔑 | Editar el nombre |
| `PUT /api/auth/password` | 🔑 | CurrentPassword + NewPassword. 400 si la actual no coincide |

### Usuarios — `UserController` ✎

| Verbo y ruta | Rol | Notas |
|---|---|---|
| `GET /api/user?role=Client\|Admin` | 🛠 | Admin solo puede pedir `Client`; SuperAdmin cualquiera |
| `GET /api/user/{id}` | 🛠 | Ídem |
| `POST /api/user` | 🛠 | ✎ Admin crea clientes; SuperAdmin crea clientes y Admins; nunca SuperAdmins (403) |
| `PUT /api/user/{id}` | 🛠 | Mismas reglas que el alta (Admin edita clientes; SuperAdmin clientes y Admins) |
| `DELETE /api/user/{id}` | 🛠 | Admin: solo clientes. SuperAdmin: cualquiera, salvo a sí mismo y salvo el último SuperAdmin activo (403) |
| ✖ `GET /api/user/email/{email}` | — | Solo lo usaba el login de demo |

### Catálogo (sin cambios de forma salvo Screen)

| Recurso | GET | POST/PUT/DELETE |
|---|---|---|
| `Genre`, `Movie`, `Showtime` | 🌐 (`/movie/active?genreId=&date=`, `/movie/{id}`, `/movie/{id}/showtimes`, `/showtime/movie/{id}`, …) | 🛠 |
| `GET /api/movie` (todas, incluye fuera de cartelera) | 🌐 | — |
| `Screen` ✎ | 🌐 | 🛠 — request con `rows` y `seatsPerRow`; response agrega `rows`, `seatsPerRow` y mantiene `capacity` (calculada) |
| ✚ `GET /api/showtime/{id}/seats` | 🌐 | Mapa: `rows`, `seatsPerRow`, `price`, butacas ocupadas `[{ row, number }]` |
| ✚ `GET /api/showtime/upcoming?movieId=` | 🌐 | Funciones futuras activas (para la compra) |

### Cargos — `FeeController` ✚

| Verbo y ruta | Rol |
|---|---|
| `GET /api/fee`, `GET /api/fee/{id}` | 👑 |
| `POST /api/fee`, `PUT /api/fee/{id}`, `DELETE /api/fee/{id}` | 👑 (`isCurrent` en el body; marcar una desmarca la anterior) |
| `GET /api/fee/current` | 🔑 (el cliente ve el cargo antes de confirmar; 204 si no hay) |

### Compras — `PurchaseController` ✚

| Verbo y ruta | Rol | Notas |
|---|---|---|
| `POST /api/purchase` | 👤 | `{ showtimeId, seats: [{ row, number }] }` → compra con tickets, subtotal, cargo y total |
| `GET /api/purchase/mine` | 👤 | Sus compras (con película, sala, horario y butacas) |
| `GET /api/purchase/{id}` | 👤 dueño / 🛠 | Ajena → 403 |
| `POST /api/purchase/mine/{id}/cancel` | 👤 dueño (> 2 h antes) | |
| `POST /api/purchase/{id}/cancel` | 🛠 | Cualquier compra, sin límite de tiempo |
| `GET /api/purchase?from=&to=&movieId=&status=` | 🛠 | Todas las ventas |

### Tickets — `TicketController` ✖

El CRUD libre de tickets desaparece: un ticket solo nace dentro de una compra y se cancela con ella. Las entradas se ven dentro de cada compra.

### Reportes — `ReportsController` ✚ (🛠)

| Ruta | Devuelve |
|---|---|
| `GET /api/reports/sales?from=&to=` | Total recaudado, cargos cobrados, entradas vendidas y canceladas, desglose por película |
| `GET /api/reports/occupancy?date=` | Por función del día: vendidas / capacidad / %, y el total del día (el pedido del 27/10 lo cambió de rango a un día) |

Consultas agregadas en el repositorio (`GroupBy` traducible a SQL, `AsNoTracking`, filtros por rango de fechas).

## 5. Pantallas del front por rol

Sesión real: `src/auth/session.js` guarda `{ token, user }` en `localStorage`; `httpClient` agrega `Authorization: Bearer` y ante un 401 cierra la sesión y vuelve a `/login`. `ProtectedRoute` recibe `roles={[...]}`. `session.js` deja de tener credenciales en el código (se borra el login de demo).

| Rol | Ruta | Pantalla |
|---|---|---|
| Anónimo | `/login` | Login (sin estilos inline, #22) |
| | `/registro` ✚ | Registro de cliente (`EntityForm`) |
| Anónimo y Client | `/cartelera` ✎ | Cartelera pública con filtros por día y género (antes `/panel/cartelera`, que ahora redirige) |
| | `/cartelera/:id` ✎ | Ficha + horarios por día con butacas libres (antes `/panel/peliculas/:id`) |
| Client | `/panel/funciones/:id/comprar` ✚ | Mapa de butacas (máx. 10), subtotal + cargo + total, confirmar |
| | `/panel/mis-compras` ✚ | Reemplaza "Mis Tickets": compras con butacas; cancelar si faltan ≥ 2 h |
| | `/panel/perfil` ✚ | Editar nombre/email y cambiar contraseña |
| Admin | `/admin/peliculas` | ABM + fecha de estreno (D2, #16) + funciones (sin N+1, #11) |
| | `/admin/generos`, `/admin/salas` | ABM; salas con filas × butacas |
| | `/admin/ventas` ✚ | Todas las compras, filtros, cancelar |
| | `/admin/reportes` ✚ | Ventas y ocupación |
| | `/admin/clientes` ✎ | Lista y baja de clientes (reemplaza `/admin/usuarios`) |
| SuperAdmin | lo de Admin + `/admin/administradores` ✚ | ABM de Admins |
| | `/admin/cargos` ✚ | ABM de cargos y cuál está vigente |

Se eliminan `/admin/tickets`, `pages/UserTickets.jsx`, `pages/AdminPanel.jsx`, `pages/UserPanel.jsx` (D4) y los componentes de `Tickets/`. `/panel/*` solo para `Client` (D3); el Sidebar arma el menú por rol.

Todas las pantallas nuevas con los genéricos (`EntityForm`/`EntityCard`/`EntityGrid`/`useEntityCrud`) salvo el mapa de butacas, que es un componente propio (`SeatMap`) con su hook (`useSeatSelection`).

## 6. Tareas verticales (back + front juntos)

Cada tarea toca los dos repos y termina con los dos funcionando de punta a punta (Definition of Done de cada repo, revisión con `revisor-backend` y `revisor-frontend`, commits en cada repo). Si se corta el tiempo, se entrega lo que esté terminado.

| Tarea | Back | Front | Hallazgos del front | Estado |
|---|---|---|---|---|
| **1. Autenticación y roles** | Hash + migración `RenameUserPasswordToPasswordHash` + rehash al arrancar, JWT, `AuthController`, políticas y `[Authorize]` en todos los controllers, `UserService` por rol, SuperAdmin inicial y usuarios demo, OpenAPI Bearer, tests de integración 401/403 | `session.js` real, `httpClient` con token y 401, `ProtectedRoute` por roles, Sidebar por rol, Login/Registro/Mi perfil, Clientes y Administradores, cartelera con `getActiveMovies`, páginas muertas fuera | #15 (D1), #19 (D4), #20 (D3), #22, deuda "login de demo" | ✅ back `87bd41c` · front `d8a2534` |
| **2. Cargos (Fee)** | `Fee` con Guid + `IsCurrent`, migración `AddFees`, `FeeService`/`FeeController` (SuperAdmin), `GET /api/fee/current` | Pantalla Cargos con genéricos (reemplaza la página pendiente) | — | ✅ (ABM en la entrega "Tarea 3 (SuperAdmin)") |
| **3. Salas con butacas y catálogo** | `Screen.Rows`/`SeatsPerRow`, migración `AddScreenSeatLayout`, `GET /api/showtime/{id}/seats`, `/showtime/upcoming` | Salas con filas × butacas; Movies: fecha de estreno, `MovieCard` → `EntityCard`, `MovieFunctionsPanel` → `useMovieShowtimes` + subcomponentes con salas/películas cargadas una vez | #8, #10 (Movies), #11, #13 (`useEntityList` con error), #16 (D2) | ◐ salas y butacas ✅; falta Movies (#8, #10, #11, #13, #16) |
| **4. Compra de entradas** | `Purchase`, `Ticket` por función + butaca, migración `AddPurchasesAndSeatTickets` (borra los tickets actuales, P1), `PurchaseService` (reglas, cargo, índice único), `POST /api/purchase`, `GET /mine`, cancelación | Detalle de película → funciones → `SeatMap` → confirmar; Mis compras con cancelación. Se borran `Tickets/` y `TicketController` | #7–#10, #12, #14, #18, #28 (Users/Tickets: Tickets desaparece; Clientes/Administradores pasan a genéricos acá) | ✅ (migración migra los tickets en lugar de borrarlos) |
| **5. Ventas y reportes** | `GET /api/purchase` con filtros, cancelación por staff, `ReportsController` | Ventas y Reportes | — | ✅ |
| **6. Tests** | Integración del índice de butacas y de cancelación; casos de autorización de los endpoints nuevos | Vitest + Testing Library (D5): `httpClient`, `session`, `fieldValidation`, `EntityForm`, `useEntityCrud`, `SeatMap` | #21 | ⏳ |
| **7. Cierre** | README final con diagramas, `docs/defensa.md`, `WebApi.http` completo, verificación completa | README con diagramas, `globalIgnores` de `CinemapiPhone` | #17, #18 restantes, #23 (solo si el profe lo confirma), #24, #29, #30 | ✅ README, defensa, `WebApi.http` y verificación (27/10); los hallazgos #17–#30 que quedan siguen anotados en la auditoría |

Los tests de cada servicio nuevo van **en la misma tarea** que el servicio (Definition of Done del backend), y los casos 401/403 de cada endpoint nuevo se suman a `AuthorizationTests`.

### Tarea 2 entregada: compra de entradas (26/10)

El pedido juntó en una sola tarea lo que acá eran las tareas 2, 3, 4 y la parte de ventas de la 5:

- Back: `Screen.Rows`/`SeatsPerRow`, `Fee` (Guid + `IsCurrent`, solo `GET /api/fee/current`; el ABM de cargos queda pendiente), `Purchase`, `Ticket` por butaca, **una** migración `AddSeatLayoutFeesAndPurchases`, `GET /api/showtime/{id}/seats`, `GET /api/showtime/upcoming`, `PurchaseController` completo (incluye `GET /api/purchase` con filtros para el staff), `TicketController` eliminado. Reglas extra: no se borra una función con entradas vendidas ni se le cambia el horario, la sala o la película; no se achica una sala con entradas vendidas para funciones futuras, y no se da de baja una sala con funciones futuras activas (409).
- Ajustes del 27/10 (respuestas a las decisiones abiertas): se bloquea también el cambio de horario con entradas vendidas; la cancelación se separa en `POST /api/purchase/mine/{id}/cancel` (`ClientOnly`) y `POST /api/purchase/{id}/cancel` (`Staff`), con política por endpoint; dar de baja una sala con funciones futuras da 409. Cuando se haga el ABM de cargos, sus endpoints van con `SuperAdminOnly` y tests de 403.
- Front: cartelera → funciones → mapa de butacas → resumen → confirmar; "Mis entradas" (próximas y pasadas, cancelar); "Ventas" para el staff con filtros; salas con filas × butacas. Se borraron `Tickets/`, `UserTickets` y `ticketApi`.
- Pendiente de las tareas originales: reportes, fecha de estreno y refactor de `MovieCard`/`MovieFunctionsPanel`.

### Tarea 3 (SuperAdmin) entregada (27/10)

- Back: ABM de cargos (`GET/POST/PUT/DELETE /api/fee`, `SuperAdminOnly` por endpoint; `GET /api/fee/current` sigue para cualquier logueado). Un solo vigente: marcar uno desmarca al anterior en una transacción de dos guardados (`IUnitOfWork.ExecuteInTransactionAsync`), porque EF no ordena los UPDATE según el índice único filtrado (lo encontró el test de integración). Usuarios: `GET /api/user` incluye a los dados de baja y acepta `search` (nombre o email); `POST /api/user/{id}/reactivate` con las mismas reglas que la baja.
- Front: Clientes y Administradores pasan a los genéricos (`EntityForm`/`EntityCard`/`EntityGrid`/`useEntityCrud`) con búsqueda, baja y reactivación; Cargos con genéricos (se borra `pages/FeesPage.jsx`). `useEntityCrud` suma `restoreFn`, `softDelete`, `reloadAfterMutation` y `reload`; `EntityCard`, `onRestore`.

### Cartelera pública entregada (pedido del 27/10)

- Back: `GET /api/movie/active` acepta `genreId` y `date` (películas con funciones ese día que no empezaron); `GET /api/movie/{id}/showtimes` devuelve las funciones futuras agrupadas por día con butacas libres (sin N+1). Ambos públicos.
- Front: cartelera pública en `/cartelera` y ficha en `/cartelera/:movieId` (afiche, sinopsis, duración, género, estreno y horarios por día con butacas libres), filtros por día y género en la URL, estados de carga, vacío y error. "Comprar" sin sesión lleva al login y vuelve a la película. El cliente usa la barra superior pública en todo su recorrido; la barra lateral queda para el staff. `/panel/cartelera` y `/panel/peliculas/:id` redirigen a las rutas nuevas. Las pantallas que no son públicas se cargan aparte (`React.lazy`).

### Reportes entregados (pedido del 27/10)

- Back: `ReportsController` (`/api/reports`, staff): `GET sales?from&to` (compras confirmadas por día de compra, máximo 366 días; totales, cargos, canceladas y desglose por película) y `GET occupancy?date` (ocupación por función y del día). Agregado en SQL con `IReportRepository`/`ReportRepository`; tests de servicio, de integración contra LocalDB y de permisos.
- Front: `/admin/reportes` con rango de fechas y atajos, tarjetas de totales, tabla por película y tabla de ocupación por función con barras (`ProgressBar` de react-bootstrap).

### Decisiones aplicadas en la Tarea 1 (cambian el plan original)

- `GET /api/movie` (todas) y los GET de salas también son públicos (P2: el catálogo completo es público).
- `PUT /api/auth/me` edita solo el **nombre**: el email identifica la cuenta y viaja en el token.
- `POST /api/user` lo usa el **staff**: Admin crea clientes; SuperAdmin crea clientes y Admins; nadie crea SuperAdmins por la API. Mismas reglas para editar y dar de baja.
- Hasta la Tarea 4, los tickets actuales siguen: listado completo solo staff; un cliente solo ve, crea, edita y borra los suyos (el usuario sale del token).
- Un usuario dado de baja pierde el acceso en el momento (se valida en cada request), aunque su token no haya vencido.

## 7. Riesgos y recortes

| Riesgo | Mitigación |
|---|---|
| **Un solo día** para todo | Tareas verticales en orden (§6): cada una deja los dos repos funcionando, así se puede cortar después de cualquiera y entregar lo que haya |
| La migración de `Tickets` pierde o rompe datos | Decisión explícita (§8) y prueba contra una copia de la base actual antes de aplicarla |
| Falta `Jwt:Key` o `SeedSuperAdmin:*` y la API no arranca | Validación al inicio con mensaje claro + README con los comandos de `user-secrets` |
| Carrera de dos compras de la misma butaca | Índice único filtrado + traducción a 409; test de integración |
| Horas: "no empezó" y "2 horas antes" según zona horaria | `TimeProvider` + hora local del cine documentada; tests con hora fija |
| CORS con el header `Authorization` | La política ya usa `AllowAnyHeader`; se prueba con `Origin: http://localhost:5173` |
| Cambios de contrato grandes (Screen, User, Ticket → Purchase) | Back y `src/api` del front en la misma tarea, como pide `CLAUDE.md` |

**Qué recortaría si falta tiempo** (en este orden, de lo primero que se cae a lo último):

1. Reporte de ocupación (queda solo el de ventas).
2. Filtros de `/admin/ventas` (lista completa ordenada por fecha).
3. Mapa visual de butacas → selector de fila + número (misma API).
4. Editar perfil (queda cambiar contraseña).
5. Vitest del front reducido a `httpClient` y `fieldValidation`.
6. Pasada de estilos/accesibilidad (#17, #24) y README del front.

No se recorta: auth + roles en todos los endpoints, compra con butacas y cargo, cancelación, CRUD de todas las entidades, migraciones y tests del backend.

## 8. Decisiones tomadas (26/10/2026)

| | Pregunta | Decisión |
|---|---|---|
| P1 | Tickets actuales sin función ni butaca | ~~Se borran~~ → Cambiado el 26/10: se migran a una función de esa película en esa sala (y una butaca libre); solo se borran los que no tienen ninguna función posible o no entran en la sala (ver `AddSeatLayoutFeesAndPurchases`) |
| P2 | ¿Catálogo sin login? | Sí: los GET de cartelera, películas, funciones, géneros (y salas) son públicos |
| P3 | ¿Compra ajena 403 o 404? | 403 (`ForbiddenException`) |
| — | Paquetes | Aprobados los 4; Vitest y Testing Library recién en la Tarea 6 |

## 9. Paquetes

| Paquete | Proyecto | Motivo |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13 | WebApi | Validar el JWT en cada request (`AddAuthentication().AddJwtBearer`). Trae `Microsoft.IdentityModel.JsonWebTokens` para emitirlo |
| `Microsoft.Extensions.Identity.Core` 10.0.13 | Infrastructure | `PasswordHasher<User>` (PBKDF2 con salt) sin ASP.NET Identity completo |
| `Microsoft.AspNetCore.Mvc.Testing` 10.0.13 | CinemAPI.Tests | Tests de integración de autorización (401/403 por rol) levantando la API en memoria con `WebApplicationFactory` |
| `vitest`, `jsdom`, `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom` | front (dev) | Tarea 6 (D5) |

Sin paquete: `dotnet user-secrets` (viene con el SDK; hace falta `dotnet user-secrets init` en WebApi, que agrega `UserSecretsId` al `.csproj`), `TimeProvider` (.NET), decodificar el token en el front (el usuario sale de `/api/auth/me` y de la respuesta del login).
