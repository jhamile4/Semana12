# Taller Mecánico — Laboratorio Semana 12

Web API REST para registrar los **clientes de un taller mecánico y sus vehículos**, desarrollada con ASP.NET Core .NET 10, Entity Framework Core y PostgreSQL.

La solución, el proyecto y el namespace se llaman **TallerMecanico**. La base de datos es **TallerMecanicoDb**. El código implementa controladores con DbContext directo y LINQ asíncrono, sin Repository ni DTOs.

## Requisitos

- SDK de .NET 10.
- Docker Desktop iniciado para PostgreSQL local.
- Node.js/npm para ejecutar las pruebas con Newman, o Postman para la ejecución manual.
- Windows con PowerShell para los lanzadores cmd incluidos.

## Iniciar localmente

Desde la raíz del proyecto:

```powershell
.\Start-Local.cmd
```

El script crea `.env` desde `.env.example` si no existe, carga sus variables, inicia PostgreSQL, espera su disponibilidad y ejecuta la API. La primera inicialización del volumen ejecuta `scrip.sql` y registra **dos clientes y tres vehículos**. Los arranques siguientes conservan los datos.

- Swagger: **http://localhost:5080/**.
- Swagger alternativo: **http://localhost:5080/swagger/index.html**.
- Documento OpenAPI: **http://localhost:5080/swagger/v1/swagger.json**.
- PostgreSQL local: **localhost:55434**, base **TallerMecanicoDb**.
- Solución para Visual Studio: **TallerMecanico.slnx**.

Detén la API con Ctrl+C. Para detener PostgreSQL sin eliminar sus datos:

```powershell
docker compose stop postgres
```

No inicies dos instancias en el mismo puerto. Los cambios de contraseña posteriores a la creación del volumen no modifican automáticamente al usuario guardado en PostgreSQL.

## Modelo de datos

**Cliente:** IdCliente, Nombre, Dni, Telefono y Email. Un cliente puede registrar varios vehículos; su colección Vehiculos usa JsonIgnore para evitar ciclos al serializar.

**Vehiculo:** IdVehiculo, Placa, Marca, Modelo, Anio, Kilometraje e IdCliente. La navegación `Cliente?` admite solicitudes JSON que envían solo la FK.

La relación es **1:N**. Se aplican Required, StringLength, Range, EmailAddress y RegularExpression. La placa utiliza el formato `ABC-123` en mayúsculas; el año admite 1900–2100 y el kilometraje 0–9999999.

Reglas de negocio con respuesta **409 Conflict**:

1. Impedir DNI o email duplicado al registrar o actualizar clientes.
2. Impedir la eliminación de clientes que tengan vehículos vinculados.
3. Impedir placas duplicadas al registrar o actualizar vehículos.

La base respalda estas reglas con índices únicos y una FK con ON DELETE RESTRICT. El email también dispone de un índice sobre lower(Email).

## Endpoints

Son **11 endpoints**, con dos filtros por query string y una ruta anidada.

**Clientes:**

- `GET /api/clientes` — listado; admite `?buscar=Jorge` por nombre, DNI o email.
- `GET /api/clientes/{id}` — detalle.
- `GET /api/clientes/{id}/vehiculos` — vehículos de un cliente.
- `POST /api/clientes` — creación.
- `PUT /api/clientes/{id}` — actualización.
- `DELETE /api/clientes/{id}` — eliminación si no tiene vehículos.

**Vehículos:**

- `GET /api/vehiculos` — listado con cliente; admite `?marca=Toyota`.
- `GET /api/vehiculos/{id}` — detalle con cliente.
- `POST /api/vehiculos` — creación con cliente existente.
- `PUT /api/vehiculos/{id}` — actualización.
- `DELETE /api/vehiculos/{id}` — eliminación.

Se devuelven 200 en consultas exitosas, 201 con Location en POST, 204 en PUT y DELETE, 400 en validaciones y FK inválida, 404 en recursos inexistentes y 409 en conflictos de negocio.

## Ejemplos JSON

POST `/api/clientes`:

```json
{
  "nombre": "Luis Torres",
  "dni": "45678912",
  "telefono": "987654320",
  "email": "luis.torres@taller.example"
}
```

POST `/api/vehiculos`, con un cliente existente:

```json
{
  "placa": "DEF-456",
  "marca": "Toyota",
  "modelo": "Yaris",
  "anio": 2022,
  "kilometraje": 32000,
  "idCliente": 1
}
```

En POST se omite el ID que genera la base. En PUT se incluyen todos los campos y el identificador de la entidad, idCliente o idVehiculo, coincidente con la ruta.

## Configuración de .env

[.env.example](.env.example) contiene los valores de desarrollo local. POSTGRES_DB debe ser TallerMecanicoDb; el usuario y la contraseña deben coincidir con los de ConnectionStrings__DefaultConnection.

El archivo `.env` es privado y se carga mediante Start-Local.cmd; no se lee automáticamente al ejecutar dotnet run o F5. Para Visual Studio consulta [GUIA.md](docs/GUIA.md).

Para Neon, crea una base nueva **TallerMecanicoDb**, ejecuta `scrip.sql` allí y reemplaza la conexión usando [config/neon.env.example](config/neon.env.example):

```dotenv
LocalDatabase__Enabled=false
ConnectionStrings__DefaultConnection=postgresql://USUARIO:CONTRASEÑA@HOST/TallerMecanicoDb?sslmode=require&channel_binding=require
```

La aplicación admite la URI de Neon o formato ADO.NET. Normaliza las URI con TLS y verificación de certificado. La conexión real no se publica en Git.

## Pruebas y Scaffold

Con la API encendida, ejecuta en otra terminal:

```powershell
.\Test-Api.cmd
.\Scaffold.cmd
```

La colección contiene **44 solicitudes y 61 aserciones**. Crea sus propios registros y los elimina al terminar. El reporte se guarda en artifacts/postman-local.json. Para Postman importa [la colección](postman/TallerMecanico.postman_collection.json) y selecciona el entorno [Local](postman/Local.postman_environment.json) o [Nube](postman/Nube.postman_environment.json).

La verificación local aprobó las 44 solicitudes y las 61 aserciones. También se comprobaron Scaffold, CORS y el contenedor en modo Production; las evidencias públicas requieren desplegar esta versión y repetir las pruebas sobre su dominio real.

Scaffold genera el modelo desde la base en una carpeta nueva de ScaffoldPreview, fuera del proyecto compilado. No sobrescribe los modelos anotados ni escribe la conexión en el código generado. El esquema usa Database First; la aplicación no ejecuta EnsureCreated ni Migrate al iniciar.

## Publicación

El [Dockerfile](Dockerfile) publica TallerMecanico.dll. [render.yaml](render.yaml) define el servicio taller-mecanico, puerto 8080, plan Free y health check `/api/clientes`. La base se configura por separado en Neon.

Variables de Render: ConnectionStrings__DefaultConnection, ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_HTTP_PORTS=8080, PORT=8080 y Cors__AllowedOrigins__0 con un origen concreto sin barra final. Swagger está habilitado también en producción y la interfaz evita reutilizar recursos almacenados en caché.

Después de publicar, sustituye el dominio y ejecuta:

```powershell
.\Test-Api.cmd -Cloud -BaseUrl https://TU-API-TALLER.onrender.com
```

El reporte público queda en artifacts/postman-nube.json. Las capturas del laboratorio deben mostrar la URL pública, no localhost. Este proyecto no incluye credenciales ni un despliegue público ya configurado.

## Estructura

```text
TallerMecanico.slnx
TallerMecanico/
  Controllers/ClientesController.cs
  Controllers/VehiculosController.cs
  Data/TallerContext.cs
  Data/DatabaseConnection.cs
  Models/Cliente.cs
  Models/Vehiculo.cs
  Properties/launchSettings.json
  Program.cs
  appsettings.json
  TallerMecanico.http
  TallerMecanico.csproj
scripts/
postman/
config/neon.env.example
docs/
scrip.sql
.env.example
compose.yaml
Dockerfile
render.yaml
dotnet-tools.json
Start-Local.cmd
Scaffold.cmd
Test-Api.cmd
```

## Documentación

- [Ejecución local y conexión a Neon](docs/INICIO.md).
- [Configuración técnica y despliegue](docs/GUIA.md).
- [Plan de pruebas](docs/PRUEBAS.md).
- [Matriz de solicitudes](docs/MATRIZ-PRUEBAS.md).
- [Guía de capturas](docs/GUIA-CAPTURAS.md).
- [Informe del taller mecánico](docs/INFORME.md).
"# Semana12" 
"# Semana12" 
