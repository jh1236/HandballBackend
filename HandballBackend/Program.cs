using HandballBackend;
using HandballBackend.Arguments;
using HandballBackend.Converters;
using HandballBackend.Utils;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddJsonOptions(options => {
    // Global settings: use the defaults, but serialize enums as strings
    // (because it really should be the default)
    options.JsonSerializerOptions.Converters.Add(new NumberConverter());
});
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpLogging(o => { });

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

ArgsHandler.Parse(args, builder);

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || Config.REQUEST_LOGGING) {
    app.UseMiddleware<RequestLogger>();
}

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection();


app.UseCors();


app.MapControllers();

app.Run();