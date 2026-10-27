# CinemAPI

API REST para la gestión de un complejo de cines (.NET 10, EF Core 10, SQL Server LocalDB). La consume el front web `CinemAPIFront/cine-frontend`.

## Requisitos

- .NET SDK 10
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
- Herramienta de EF: `dotnet tool install --global dotnet-ef`

## Configuración (una sola vez)

Los secretos **no** van en `appsettings.json`: en desarrollo se guardan con `dotnet user-secrets` (quedan en el perfil del usuario, fuera del repo). Desde la carpeta del repo:

```bash
# Clave para firmar los JWT (mínimo 32 bytes; cualquier texto largo y aleatorio)
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-de-al-menos-32-caracteres>" --project WebApi

# SuperAdmin inicial: se crea al arrancar si no hay ningún SuperAdmin activo
dotnet user-secrets set "SeedSuperAdmin:Email" "superadmin@cinemapi.com" --project WebApi
dotnet user-secrets set "SeedSuperAdmin:Name" "Super Admin" --project WebApi
dotnet user-secrets set "SeedSuperAdmin:Password" "<mínimo 8 caracteres>" --project WebApi

# Contraseña de los usuarios demo (solo Development)
dotnet user-secrets set "DemoUsers:Password" "<mínimo 8 caracteres>" --project WebApi
```

Para generar una clave: `node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"`.

Si falta `Jwt:Key` (o es corta) o no hay SuperAdmin y faltan `SeedSuperAdmin:*`, la API no arranca y el error dice qué configurar. Fuera de desarrollo, los mismos valores van como variables de entorno (`Jwt__Key`, `SeedSuperAdmin__Email`, ...).

## Base de datos y arranque

```bash
dotnet ef database update -p Infrastructure -s WebApi
dotnet run --project WebApi --launch-profile http     # http://localhost:5288
```

Al arrancar, en todos los ambientes, se hashean las contraseñas que hubieran quedado en texto plano de versiones anteriores y se crea el SuperAdmin inicial si hace falta. En **Development** además se cargan datos de prueba (Bogus) y los usuarios demo.

OpenAPI (solo Development): `http://localhost:5288/openapi/v1.json`. Las operaciones protegidas declaran el esquema `Bearer`.

## Usuarios y roles

| Rol | Cómo se crea | Puede |
|---|---|---|
| SuperAdmin | Seed (`SeedSuperAdmin:*`) | Todo lo de Admin + administrar Admins (alta con contraseña inicial, edición, baja y reactivación) y el ABM de cargos |
| Admin | Lo crea un SuperAdmin (`POST /api/user`) | Catálogo, ventas (ver y cancelar compras), clientes (buscar, dar de baja y reactivar) |
| Client | Se registra solo (`POST /api/auth/register`) | Cartelera, comprar y cancelar sus entradas, su perfil y contraseña |

Usuarios demo en Development (contraseña `DemoUsers:Password`): `admin@cinemapi.com` (Admin) y `cliente@cinemapi.com` (Client). Los usuarios generados por Bogus son clientes con la misma contraseña.

| Endpoint | Quién | Qué hace |
|---|---|---|
| `GET /api/user?role=Client&search=` | staff | Usuarios del rol (Admin solo ve clientes), **incluidos los dados de baja** (`isActive`); `search` busca en nombre y email |
| `POST /api/user` / `PUT /api/user/{id}` | staff | Admin crea y edita clientes; SuperAdmin, clientes y Admins. Nadie crea ni edita SuperAdmins (403) |
| `DELETE /api/user/{id}` | staff | Baja lógica con las mismas reglas; nadie se da de baja a sí mismo y siempre queda un SuperAdmin activo (403) |
| `POST /api/user/{id}/reactivate` | staff | Deshace la baja (mismas reglas; ya activo → 409). A un usuario dado de baja solo se lo reactiva: `GET`/`PUT`/`DELETE /api/user/{id}` responden 404 |
| `GET /api/fee`, `GET /api/fee/{id}` | SuperAdmin | Cargos (el vigente primero) |
| `POST /api/fee` / `PUT /api/fee/{id}` | SuperAdmin | `{ name, percentage (0–100), fixedAmount (≥ 0), isCurrent }`, hasta 2 decimales. `isCurrent: true` desmarca al vigente anterior |
| `DELETE /api/fee/{id}` | SuperAdmin | Baja lógica; si era el vigente, las compras nuevas quedan sin cargo |

Autenticación: `POST /api/auth/login` devuelve `{ token, expiresAt, user }`; el token (8 h) va en `Authorization: Bearer <token>`. Sin token → 401; con un rol que no alcanza → 403 (ambos como ProblemDetails).

## Cartelera (pública)

| Endpoint | Qué hace |
|---|---|
| `GET /api/movie/active?genreId=&date=2026-11-02` | Películas en cartelera; `genreId` filtra por género y `date` (`yyyy-MM-dd`, hora local del cine) deja solo las que tienen alguna función ese día que todavía no empezó, en una sala activa (un día pasado no tiene ninguna). Fecha o Guid inválidos → 400 |
| `GET /api/movie/{id}/showtimes` | Funciones futuras de la película agrupadas por día: `[{ date, showtimes: [{ id, startTime, screenId, screenName, price, totalSeats, availableSeats }] }]`. Vacío si no está en cartelera; 404 si no existe; omite las funciones de salas dadas de baja. Tres consultas en total (funciones, salas y ventas agrupadas) |

## Reportes (staff)

| Endpoint | Qué devuelve |
|---|---|
| `GET /api/reports/sales?from=2026-10-01&to=2026-10-31` | Compras confirmadas hechas en esos días (ambos incluidos, máximo 366): compras, entradas vendidas, entradas canceladas, recaudación de entradas, cargos cobrados, total y el desglose por película (de mayor a menor) |
| `GET /api/reports/occupancy?date=2026-10-27` | Funciones que empiezan ese día con capacidad, vendidas y `occupancyPercentage` (un decimal), y la ocupación del día (butacas vendidas / butacas totales) |

Fecha faltante o inválida, `from` posterior a `to` o más de 366 días → 400 (ProblemDetails). Sin token → 401; cliente → 403.

Las sumas y los conteos los hace SQL Server (`GROUP BY` con proyecciones en `ReportRepository`); el servicio solo arma los totales con las filas ya agrupadas. Las películas y salas dadas de baja después de vender siguen en el reporte. La capacidad es la de la sala hoy.

## Compra de entradas

| Endpoint | Quién | Qué hace |
|---|---|---|
| `GET /api/showtime/upcoming?movieId=` | público | Funciones que todavía no empezaron |
| `GET /api/showtime/{id}/seats` | público | Mapa: `rows`, `seatsPerRow`, `price`, `isOnSale` y `takenSeats` (`[{ row, number }]`, fila 1 = "A") |
| `GET /api/fee/current` | logueado | Cargo vigente (`percentage`, `fixedAmount`) o 204 si no hay |
| `POST /api/purchase` | Client | `{ showtimeId, seats: [{ row, number }] }` → 201 con la compra |
| `GET /api/purchase/mine` | Client | Sus compras con película, sala, horario y butacas |
| `GET /api/purchase/{id}` | dueño o staff | Una compra (ajena → 403) |
| `POST /api/purchase/mine/{id}/cancel` | Client (dueño) | Cancela una compra propia (hasta 2 h antes) y libera las butacas |
| `POST /api/purchase/{id}/cancel` | staff | Cancela cualquier compra, sin límite de tiempo |
| `GET /api/purchase?movieId=&showtimeId=&from=&to=&status=` | staff | Ventas; `from`/`to` son días de compra (incluidos), `status` `Confirmed` o `Cancelled` |

Reglas: la función no empezó, la película está en cartelera, de 1 a 10 butacas que existan en la sala y estén libres (si no, 400 / 409 con las butacas en el mensaje). Total = subtotal + subtotal × `percentage` / 100 + `fixedAmount`, redondeado a centavos y guardado en la compra. El cliente cancela solo lo suyo y mientras falten más de 2 horas; el staff, cualquier compra y en cualquier momento. Las horas son la hora local del cine (la del servidor). Una función con entradas vendidas no se puede borrar ni cambiar de horario, sala o película (sí de precio), y una sala con funciones próximas no se puede dar de baja (409).

La migración `AddSeatLayoutFeesAndPurchases` convierte la capacidad de cada sala en filas × 10 butacas y pasa los tickets viejos a compras con butaca; los que no tienen ninguna función de su película en su sala (o no entran en ella) se borran. El detalle está en el comentario de la migración.

## Verificación

```bash
dotnet build CinemAPI.slnx
dotnet test CinemAPI.slnx             # incluye integración contra LocalDB (bases temporales)
dotnet format CinemAPI.slnx --verify-no-changes
```
