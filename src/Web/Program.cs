using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Infrastructure.Data;
using SAPFIAI.Web.Middleware;
using Scalar.AspNetCore;

try
{
    // Load .env file unconditionally so ASPNETCORE_ENVIRONMENT and all secrets
    // are available before WebApplication.CreateBuilder reads configuration.
    // Variables already set in the process (e.g. from hosting panel) are never overwritten.
    var possibleEnvPaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env")
    };

    foreach (var envPath in possibleEnvPaths)
    {
        if (File.Exists(envPath))
        {
            Console.WriteLine($"Cargando variables de entorno desde: {Path.GetFullPath(envPath)}");
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // ASPNETCORE_ENVIRONMENT defined in .env always wins — it is the single source
                // of truth for environment selection. launchSettings.json sets it before startup
                // but .env must be able to override it so switching environments only requires
                // editing .env.
                // All other variables respect the host environment (hosting panel variables win).
                var alreadySet = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key));
                if (!alreadySet || key == "ASPNETCORE_ENVIRONMENT")
                    Environment.SetEnvironmentVariable(key, value);
            }
            break;
        }
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddWebServices(builder.Configuration, builder.Environment);

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    var skipDatabaseInitialization = builder.Configuration.GetValue<bool>("SkipDatabaseInitialization")
        || string.Equals(Environment.GetEnvironmentVariable("SKIP_DB_INIT"), "true", StringComparison.OrdinalIgnoreCase)
        || Environment.GetEnvironmentVariable("SKIP_DB_INIT") == "1";

    if (app.Environment.IsDevelopment())
        app.UseDeveloperExceptionPage();
    else
        app.UseHsts();

    if (!skipDatabaseInitialization)
    {
        using var scope = app.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync(); // migraciones
        await initialiser.SeedAsync();       // seed — corre en todos los entornos
    }

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "SAPFIAI API";
        options.Theme = ScalarTheme.Purple;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

    app.UseHealthChecks("/health");

    // UseHttpsRedirection causes redirect loops behind IIS reverse proxy.
    // HTTPS termination is handled by IIS — only redirect in direct Kestrel scenarios.
    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseStaticFiles();

    // CORS: política correcta según entorno
    app.UseCors(app.Environment.IsDevelopment() ? "DevelopmentCors" : "ProductionCors");

    // Bootstrap Permit.io — se ejecuta dentro de SeedAsync junto con la sincronizacion del admin.
    // No se llama aqui para evitar doble ejecucion.

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // RBAC con Permit.io — después de autenticación/autorización de Identity
    app.UseMiddleware<PermitAuthorizationMiddleware>();

    app.UseExceptionHandler();
    app.Map("/", () => Results.Redirect("/scalar/v1"));
    app.MapEndpoints();

    app.Run();
}
catch (Exception ex)
{
    var errorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.txt");
    var content = $"[{DateTime.UtcNow:u}] STARTUP FAILED\n\n{ex}\n\nEnvironment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}\nCWD: {Directory.GetCurrentDirectory()}\nBase: {AppDomain.CurrentDomain.BaseDirectory}\n";
    File.WriteAllText(errorPath, content);
    throw;
}

public partial class Program { }
