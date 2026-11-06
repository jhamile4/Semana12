using Microsoft.EntityFrameworkCore;
using TallerMecanico.Data;

var builder = WebApplication.CreateBuilder(args);
// El proveedor de configuración lee ConnectionStrings__DefaultConnection del entorno.
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection; consulte .env.example.");
connection = DatabaseConnection.Normalize(connection);
builder.Configuration["ConnectionStrings:DefaultConnection"] = connection;
builder.Services.AddDbContext<TallerContext>(options => options.UseNpgsql(connection));
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new()
{
    Title = "TallerMecanicoDb - Gestion de Taller Mecanico",
    Version = "v1",
    Description = "API REST para clientes y vehiculos. Relacion 1:N y validaciones de negocio."
}));
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5080"];
if (origins.Length == 0 || origins.Any(origin => origin == "*" ||
    !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
    (uri.Scheme != "http" && uri.Scheme != "https") || uri.AbsolutePath != "/" ||
    uri.Query.Length > 0 || uri.Fragment.Length > 0 || origin.EndsWith('/')))
    throw new InvalidOperationException("CORS requiere orígenes HTTP(S) concretos sin ruta ni barra final.");
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseExceptionHandler();
// Render termina HTTPS en su proxy. El contenedor escucha HTTP internamente.
// En el perfil local HTTP no hay certificado/puerto HTTPS que redirigir.
if (app.Environment.IsDevelopment() && builder.Configuration["LocalDatabase:Enabled"] != "true")
    app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Frontend");
// Evita reutilizar HTML o scripts de Swagger guardados durante un despliegue.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path is "/" or "/index.html" or "/index.js" or "/index.css" ||
        path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            return Task.CompletedTask;
        });
    }
    await next();
});
// Swagger se publica también en producción por requisito del laboratorio.
app.UseSwagger();
// Ruta alternativa con recursos propios para recuperar la interfaz sin cache previa.
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "TallerMecanicoDb - API de Taller Mecanico";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TallerMecanicoDb - API de Taller Mecanico v1");
    options.RoutePrefix = "swagger";
});
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "TallerMecanicoDb - API de Taller Mecanico";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TallerMecanicoDb - API de Taller Mecanico v1");
    options.RoutePrefix = string.Empty;
});
app.MapControllers();


app.Run();
