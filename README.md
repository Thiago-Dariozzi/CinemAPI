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
| SuperAdmin | Seed (`SeedSuperAdmin:*`) | Todo lo de Admin + administrar Admins (y el ABM de cargos, próximamente) |
| Admin | Lo crea un SuperAdmin (`POST /api/user`) | Catálogo, ventas (ver y cancelar compras), ver y dar de baja clientes |
| Client | Se registra solo (`POST /api/auth/register`) | Cartelera, comprar y cancelar sus entradas, su perfil y contraseña |

Usuarios demo en Development (contraseña `DemoUsers:Password`): `admin@cinemapi.com` (Admin) y `cliente@cinemapi.com` (Client). Los usuarios generados por Bogus son clientes con la misma contraseña.

Autenticación: `POST /api/auth/login` devuelve `{ token, expiresAt, user }`; el token (8 h) va en `Authorization: Bearer <token>`. Sin token → 401; con un rol que no alcanza → 403 (ambos como ProblemDetails).

## Compra de entradas

| Endpoint | Quién | Qué hace |
|---|---|---|
| `GET /api/showtime/upcoming?movieId=` | público | Funciones que todavía no empezaron |
| `GET /api/showtime/{id}/seats` | público | Mapa: `rows`, `seatsPerRow`, `price`, `isOnSale` y `takenSeats` (`[{ row, number }]`, fila 1 = "A") |
| `GET /api/fee/current` | logueado | Cargo vigente (`percentage`, `fixedAmount`) o 204 si no hay |
| `POST /api/purchase` | Client | `{ showtimeId, seats: [{ row, number }] }` → 201 con la compra |
| `GET /api/purchase/mine` | Client | Sus compras con película, sala, horario y butacas |
| `GET /api/purchase/{id}` | dueño o staff | Una compra (ajena → 403) |
| `POST /api/purchase/{id}/cancel` | dueño o staff | Cancela y libera las butacas |
| `GET /api/purchase?movieId=&showtimeId=&from=&to=&status=` | staff | Ventas; `from`/`to` son días de compra (incluidos), `status` `Confirmed` o `Cancelled` |

Reglas: la función no empezó, la película está en cartelera, de 1 a 10 butacas que existan en la sala y estén libres (si no, 400 / 409 con las butacas en el mensaje). Total = subtotal + subtotal × `percentage` / 100 + `fixedAmount`, redondeado a centavos y guardado en la compra. El cliente cancela solo lo suyo y mientras falten más de 2 horas; el staff, cualquier compra y en cualquier momento. Las horas son la hora local del cine (la del servidor).

La migración `AddSeatLayoutFeesAndPurchases` convierte la capacidad de cada sala en filas × 10 butacas y pasa los tickets viejos a compras con butaca; los que no tienen ninguna función de su película en su sala (o no entran en ella) se borran. El detalle está en el comentario de la migración.

## Verificación

```bash
dotnet build CinemAPI.slnx
dotnet test CinemAPI.slnx             # incluye integración contra LocalDB (bases temporales)
dotnet format CinemAPI.slnx --verify-no-changes
```
