using Hao.Hf.DyService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace dyprogram
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
            services.AddHttpClient();

            services.AddTransient<IDyService, DyService>();
            services.AddTransient<IHttpHelper, HttpHelper>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var dyService = serviceProvider.GetService<IDyService>();

            await dyService.PullMovieJustOnce();
        }
    }
}
