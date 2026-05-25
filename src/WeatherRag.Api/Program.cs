using WeatherRag.Inference;
using WeatherRag.Rag;
using WeatherRag.Rag.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WeatherRag API", Version = "v1" });
});

builder.Services.AddRagServices(builder.Configuration);
builder.Services.AddInferenceServices(builder.Configuration);

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .AllowAnyOrigin()//("http://localhost:5500", "http://127.0.0.1:5500")
        .AllowAnyMethod()
        .AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Load vector store on startup
var store = app.Services.GetRequiredService<IVectorStore>();
await store.LoadAsync();

// Warm embedding model session so first request avoids cold-start latency.
var embeddingWarmup = app.Services.GetRequiredService<IEmbeddingWarmupService>();
await embeddingWarmup.WarmupAsync();

app.Run();
