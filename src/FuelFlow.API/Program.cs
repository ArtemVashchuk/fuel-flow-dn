using FuelFlow.Features.Vouchers.Import;
using FuelFlow.Options;
using FuelFlow.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
    builder.Services.AddScoped<IImportVouchersDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
    builder.Services.AddDbContext<ApplicationDbContext>((services, options) =>
    {
        var dbOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        options.UseNpgsql(dbOptions.ConnectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
    });

    builder.Services.AddScoped<ImportVouchersCommandHandler>();
    builder.Services.AddScoped<IVoucherProviderParser, OkkoVoucherParser>();
    builder.Services.AddScoped<IVoucherProviderParser, WogVoucherParser>();
    builder.Services.AddTransient<IPdfRenderer, PdfRenderer>();
    builder.Services.AddTransient<IQrDecoder, QrDecoder>();
    builder.Services.AddTransient<IVoucherDetector, VoucherDetector>();
    builder.Services.AddTransient<IQrGenerator, QrGenerator>();
    builder.Services.AddScoped<GetVouchersQueryHandler>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "FuelFlow API", Version = "v1" });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment() || true)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FuelFlow API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            logger.LogInformation("Applying pending migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
