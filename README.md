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
| SuperAdmin | Seed (`SeedSuperAdmin:*`) | Todo lo de Admin + administrar Admins (y cargos, próximamente) |
| Admin | Lo crea un SuperAdmin (`POST /api/user`) | Catálogo, ventas, ver y dar de baja clientes |
| Client | Se registra solo (`POST /api/auth/register`) | Cartelera, sus entradas, su perfil y contraseña |

Usuarios demo en Development (contraseña `DemoUsers:Password`): `admin@cinemapi.com` (Admin) y `cliente@cinemapi.com` (Client). Los usuarios generados por Bogus son clientes con la misma contraseña.

Autenticación: `POST /api/auth/login` devuelve `{ token, expiresAt, user }`; el token (8 h) va en `Authorization: Bearer <token>`. Sin token → 401; con un rol que no alcanza → 403 (ambos como ProblemDetails).

## Verificación

```bash
dotnet build CinemAPI.slnx
dotnet test CinemAPI.slnx             # incluye integración contra LocalDB (bases temporales)
dotnet format CinemAPI.slnx --verify-no-changes
```
