﻿namespace ByteStore.Api.Middlewares
{
    public class CustomCorsMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomCorsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Set CORS headers for all responses
            context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:4200"); // Specific origin
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true"); // Required for credentials

            // if u don't allow to expose this header the angular app can't see it 
            context.Response.Headers.Add("Access-Control-Expose-Headers", "X-Pagination");

            // Handle preflight request
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 204; // No Content
                return;
            }

            await _next(context);
        }
    }

    // CorsMiddlewareExtensions.cs (for cleaner registration)
    public static class CorsMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomCors(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomCorsMiddleware>();
        }
    }
}
