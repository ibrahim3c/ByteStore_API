﻿using System.Threading.RateLimiting;

namespace ByteStore.Api.Extenstions
{
    public static class RateLimitingService
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            //services.AddRateLimiter(rateLimiterOptions =>
            //{
            //    // لما يحصل تجاوز → يرجّع 429
            //    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                //// Fixed Window Limiter
                //rateLimiterOptions.AddFixedWindowLimiter("fixed", options =>
                //{
                //    options.Window = TimeSpan.FromSeconds(10); // كل 10 ثواني
                //    options.PermitLimit = 3; // مسموح 3 requests فقط
                //});

                // Sliding Window Limiter
                //rateLimiterOptions.AddSlidingWindowLimiter("sliding", options =>
                //{
                //    options.Window = TimeSpan.FromSeconds(15); // نافذة 15 ثانية
                //    options.SegmentsPerWindow = 3;             // تقسيمها لـ 3 مقاطع
                //    options.PermitLimit = 15;                  // مسموح 15 طلب
                //});

                //rateLimiterOptions.AddTokenBucketLimiter("token", options =>
                //{
                //    options.TokenLimit = 100;
                //    options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                //    options.TokensPerPeriod = 10;
                //});

                //rateLimiterOptions.AddConcurrencyLimiter("concurrency", options =>
                //{
                //    options.PermitLimit = 5;
                //});

            //});


            services.AddRateLimiter(options =>
            {
                // لما يحصل تجاوز → يرجّع 429
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // هنا بنسجل policy باسم "fixed" مثلاً
                options.AddPolicy("fixed", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                   partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,                              // أقصى عدد طلبات
                            Window = TimeSpan.FromSeconds(10),           // المدة الزمنية
                            QueueLimit = 3,           // أقصى عدد ممكن يستنى في الطابور
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
                
                options.AddPolicy("sliding", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 15, // أقصى عدد طلبات
                            Window = TimeSpan.FromSeconds(15), // المدة
                            SegmentsPerWindow = 3, // نقطع الـ window لأجزاء صغيرة عشان الدقة
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.AddPolicy("token", context =>
                    RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 100, // الجردل يشيل كام توكن
                            TokensPerPeriod = 10, // كام توكن يضاف كل فترة
                                                 // هو الوقت اللي بنستناه علشان نضيف توكنز جديدة للجردل.
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10), // this is Period
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.AddPolicy("concurrency", context =>
                  RateLimitPartition.GetConcurrencyLimiter(
                  partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                      factory: _ => new ConcurrencyLimiterOptions
                      {
                          PermitLimit = 5, // كام طلب يشتغل في نفس الوقت
                          QueueProcessingOrder = QueueProcessingOrder.OldestFirst,// الطلبات الجديدة تستنى
                          QueueLimit = 5 // عدد الطلبات اللي ممكن تستنى في الصف
                      }));
            });

            return services;
        }
    }
}
