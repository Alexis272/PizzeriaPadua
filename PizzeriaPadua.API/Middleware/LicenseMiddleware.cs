using PizzeriaPadua.API.Services;

namespace PizzeriaPadua.API.Middleware
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, LicensingService licensing)
        {
            var path = context.Request.Path.Value ?? "";

            if (path.StartsWith("/PizzeriaPadua", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("/PizzeriaPadua".Length);
                if (string.IsNullOrEmpty(path))
                {
                    path = "/";
                }
            }

            var isAllowed = path == "/"
                || path == "/hc"
                || path == "/__test"
                || path == "/debug/ping"
                || path == "/admin.html"
                || path == "/gestion.html"
                || path == "/index.html"
                || path == "/modo.html"
                || path == "/politica-de-privacidad.html"
                || path.StartsWith("/css/")
                || path.StartsWith("/js/")
                || path.StartsWith("/img/")
                || path.StartsWith("/promo/")
                || path.StartsWith("/api/v1/License")
                || path.StartsWith("/api/v1/Events")
                || path.StartsWith("/api/v1/Accounts")
                || path.StartsWith("/api/v1/Orders")
                || path.StartsWith("/api/v1/Payments")
                || path.StartsWith("/api/v1/Products")
                || path.StartsWith("/api/v1/Categories")
                || path.StartsWith("/api/v1/Settings")
                || path.StartsWith("/api/v1/Reviews")
                || path.StartsWith("/api/v1/Pos")
                || path.StartsWith("/api/v1/Tables")
                || path.StartsWith("/api/v1/Employees");

            if (!isAllowed && !licensing.IsLicensed)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "License expired or not found",
                        message = "Contact the administrator to renew the license"
                    }));
                return;
            }

            await _next(context);
        }
    }

    public static class LicenseMiddlewareExtensions
    {
        public static IApplicationBuilder UseLicenseMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LicenseMiddleware>();
        }
    }
}
