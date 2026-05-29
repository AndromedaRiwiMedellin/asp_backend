using Microsoft.EntityFrameworkCore;
using asp_backend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Andromeda API",
        Version = "v1",
        Description = "Backend for the Andromeda event and ticketing management system."
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//  CORS agregado
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://tickets.andromeda.andrescortes.dev",
                "https://andromeda.andrescortes.dev"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try {
        db.Database.ExecuteSqlRaw("ALTER TABLE tickets ADD COLUMN IF NOT EXISTS seller_id uuid;");
        db.Database.ExecuteSqlRaw("ALTER TABLE tickets DROP CONSTRAINT IF EXISTS tickets_seller_id_fkey;");
        db.Database.ExecuteSqlRaw("ALTER TABLE tickets ADD CONSTRAINT tickets_seller_id_fkey FOREIGN KEY (seller_id) REFERENCES users(id);");
    } catch { }
}
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Andromeda API Documentation";
    options.DisplayRequestDuration();
});

app.UseCors("Frontend"); //  antes de MapControllers
app.UseAuthorization();
app.MapControllers();

app.Run();
