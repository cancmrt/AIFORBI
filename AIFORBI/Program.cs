

using AIFORBI;


using DBCONNECTOR.Connectors; // For DB Init

Console.WriteLine("--> STARTING APPLICATION...");

try 
{
    var builder = WebApplication.CreateBuilder(args);

    AppConfig.Configuration = builder.Configuration;

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Init DB (Try to create tables)
    try 
    {
        Console.WriteLine("--> Initializing Database Tables...");
        var mscon = new MssqlConnector(
            AppConfig.Configuration["ConnStrs:Mssql:ConnStr"], 
            AppConfig.Configuration["ConnStrs:Mssql:DatabaseName"], 
            AppConfig.Configuration["ConnStrs:Mssql:Schema"]
        );
        mscon.CreateAppTables();
        Console.WriteLine("--> Database Tables Initialized.");
    }
    catch(Exception ex)
    {
        Console.WriteLine($"--> DB INIT ERROR (Continuing anyway): {ex.Message}");
    }


    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AIFORBI V1");
    });

    // app.UseHttpsRedirection();
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
