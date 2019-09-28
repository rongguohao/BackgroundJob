using Hao.Hf.DyService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MovieProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false) // 在运行时修改强类型配置，无需设置reloadOnChange:true,默认就为true,只需要使用IOptionsSnapshot接口,IOptions<> 生命周期为Singleton,IOptionsSnapshot<> 生命周期为Scope
                .AddJsonFile($"appsettings.Development.json", optional: true)  //没有的话 默认读取appsettings.json
                .AddEnvironmentVariables()
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);
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

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var dyService = serviceProvider.GetService<IDyService>();

            await dyService.PullMovieJustOnce();
        }
    }
}
