using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Redis;
using Hao.Hf.DyService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace backgroundjob
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false) // 在运行时修改强类型配置，无需设置reloadOnChange:true,默认就为true,只需要使用IOptionsSnapshot接口,IOptions<> 生命周期为Singleton,IOptionsSnapshot<> 生命周期为Scope
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)  //没有的话 默认读取appsettings.json
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x =>
            {
                var connectionString = Configuration.GetConnectionString("Hangfire.Redis");
                x.UseRedisStorage(connectionString, new RedisStorageOptions
                {
                    Prefix = "hao_hangfire:"
                });
            });

            services.AddHttpClient("dy", a => { a.Timeout = TimeSpan.FromMinutes(3); })
                    .AddPolicyHandler(Policy<HttpResponseMessage>
                    .Handle<SocketException>()
                    .Or<IOException>()
                    .Or<HttpRequestException>()
                    .WaitAndRetryForeverAsync(t => TimeSpan.FromSeconds(5), (ex, ts) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("重试" + ts);
                    }))
                    .ConfigureHttpMessageHandlerBuilder((c) =>
                    new HttpClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    });


            services.AddTransient<IDyService, DyService>();
            services.AddTransient<IHttpHelper, HttpHelper>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerName = "haohaoplay",
                WorkerCount = 5
            });
            app.UseHangfireDashboard("/hangfire", new DashboardOptions()
            {
                Authorization = new[] { new DashboardAuthorizationFilter() },
                
            });

            RecurringJob.AddOrUpdate<IDyService>(a => a.PullMovie(), Cron.Hourly());

            app.UseMvc();
        }
    }
}
