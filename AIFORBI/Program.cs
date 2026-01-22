using AIFORBI;
using AIFORBI.Services;
using DBCONNECTOR.Interfaces;
using DBCONNECTOR.Repositories;

Console.WriteLine("--> STARTING APPLICATION...");

try 
{
    var builder = WebApplication.CreateBuilder(args);

    AppConfig.Configuration = builder.Configuration;

    // Get connection string from configuration dynamically
    var connectorType = builder.Configuration["ConnStrs:DbConnector:Type"] ?? "Mssql";
    var connectionString = builder.Configuration[$"ConnStrs:DbConnector:{connectorType}:ConnStr"] 
        ?? throw new InvalidOperationException($"Database connection string is not configured for {connectorType}.");

    // Register Repositories (Scoped - new instance per request)
    builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(connectionString));
    builder.Services.AddScoped<IChatRepository>(_ => new ChatRepository(connectionString));
    builder.Services.AddScoped<IDatabaseInitializer>(_ => new DatabaseInitializer(connectionString));

    // Register Database Connector Implementation (IDbConnector)
    builder.Services.AddScoped<IDbConnector>(sp => DbConnectorFactory.Create(sp.GetRequiredService<IConfiguration>()));

    // Register Services
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<SettingsService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Init DB (Try to create tables using DI)
    try 
    {
        Console.WriteLine("--> Initializing Database Tables...");
        using var scope = app.Services.CreateScope();
        var dbInit = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        dbInit.CreateAppTables();
        Console.WriteLine("--> Database Tables Initialized.");
    }
    catch(Exception ex)
    {
        Console.WriteLine($"--> DB INIT ERROR (Continuing anyway): {ex.Message}");
    }

    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AIFORBI V1");
    });

    app.MapControllers();  

    Console.WriteLine("--> Ready to Run...");
    app.Run();
}
catch(Exception ex)
{
    Console.WriteLine($"--> FATAL STARTUP ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.WriteLine("Press Enter to exit...");
    Console.ReadLine();
}
