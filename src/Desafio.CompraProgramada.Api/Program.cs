using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Exceptions;
using Desafio.CompraProgramada.Infrastructure;
using Desafio.CompraProgramada.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            var payload = new ErrorResponse(
                errors.Count > 0 ? string.Join(" | ", errors) : "Requisicao invalida.",
                "REQUISICAO_INVALIDA");

            return new BadRequestObjectResult(payload);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Request-Id", Guid.NewGuid().ToString());
    await next();
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is BusinessException businessException)
        {
            context.Response.StatusCode = businessException.StatusCode;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(businessException.Message, businessException.Codigo));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ErrorResponse("Erro interno ao processar a solicitacao.", "ERRO_INTERNO"));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

public partial class Program;
