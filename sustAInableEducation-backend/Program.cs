using Microsoft.EntityFrameworkCore;
using sustAInableEducation_backend.Repository;
using sustAInableEducation_backend.Hubs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using sustAInableEducation_backend.Models;
using Serilog;

var AllowFrontendOrigin = "_allowFrontendOrigin";

try
{
    Log.Information("Starting sustAIanableEducation Backend");
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration.WriteTo.Console();
        loggerConfiguration.ReadFrom.Configuration(context.Configuration);

    });

    // Add services to the container.

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: AllowFrontendOrigin,
                          policy =>
                          {
                              policy.WithOrigins(builder.Configuration["FrontendHost"]!)
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials();
                          });
    });
    builder.Services.AddControllers();
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "sustAInableEducation API", Version = "v1" });
        options.AddSignalRSwaggerGen();
    });
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var connectionString = string.Format(
            builder.Configuration.GetConnectionString("ApplicationDatabase")!,
            builder.Configuration["Db:Host"],
            builder.Configuration["Db:User"],
            builder.Configuration["Db:Password"]
        );
        options.UseSqlServer(connectionString);
    }
    );
    builder.Services.AddAuthorization();
    builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.Password = new PasswordOptions()
        {
            RequiredLength = 8,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true
        };
    })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services.AddTransient<DataSeeder>();
    builder.Services.AddTransient<IUserValidator<ApplicationUser>, ApplicationUserValidator>();

    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddTransient<IAIService, AIService>();

    var app = builder.Build();

    app.UseStaticFiles();

    app.UseCors(AllowFrontendOrigin);

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthorization();

    app.MapControllers();

    using (var Scope = app.Services.CreateScope())
    {
        var context = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var seeder = Scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.Seed();
    }

    app.MapGroup("/account").MapIdentityApi<ApplicationUser>().WithTags("Account");

    app.MapHub<SpaceHub>("/spaceHub/{id}");

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "sustAIanableEducation Backend terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}