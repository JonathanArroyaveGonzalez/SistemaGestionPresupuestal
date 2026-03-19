using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Infrastructure.Data;
using SAPFIAI.Web.Middleware;
using Scalar.AspNetCore;

try
{
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
                if (parts.Length == 2)
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
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
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // CORS: política correcta según entorno
    app.UseCors(app.Environment.IsDevelopment() ? "DevelopmentCors" : "ProductionCors");

    // Bootstrap Permit.io — crea recursos/roles/políticas si no existen
    var skipPermitBootstrap = builder.Configuration.GetValue<bool>("SkipPermitBootstrap")
        || string.Equals(Environment.GetEnvironmentVariable("SKIPPERMITBOOTSTRAP"), "true", StringComparison.OrdinalIgnoreCase);

    if (!skipPermitBootstrap)
    {
        using var permitScope = app.Services.CreateScope();
        var permitProvisioning = permitScope.ServiceProvider.GetRequiredService<IPermitProvisioningService>();
        await permitProvisioning.EnsureAuthorizationModelAsync();
    }

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
