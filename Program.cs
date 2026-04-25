using MathAnalysisAI.Data;
using MathAnalysisAI.Server.Services;
using MathAnalysisAI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 控制器
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<LLMService>();

var app = builder.Build();

app.UseDefaultFiles();   // 自动打开 index.html
app.UseStaticFiles();    // 允许访问 wwwroot

// Swagger中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();