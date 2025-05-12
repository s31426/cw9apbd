using cw9cwcw.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IDbService, DbService>();
var host = builder.Build();

host.MapControllers();

host.Run();
