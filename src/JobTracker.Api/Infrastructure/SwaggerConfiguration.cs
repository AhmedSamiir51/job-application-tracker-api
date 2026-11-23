using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JobTracker.Api.Infrastructure;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Job Application Tracker API",
                Version = "v1",
                Description = "Track job applications through the hiring pipeline."
            });

            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the access token returned by /api/auth/login."
            });

            o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
            if (File.Exists(xml)) o.IncludeXmlComments(xml);
        });
        return services;
    }
}
