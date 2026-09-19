using Connect4.Web;
using System.Text.Json.Serialization;

// Create web application builder, sharing one game service so the engine keeps its cache.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GameService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Create the web application and configure.
WebApplication app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapPost("/api/move", (MoveRequest request, GameService game) => game.PlayComputerMove(request.Moves));
app.Run();