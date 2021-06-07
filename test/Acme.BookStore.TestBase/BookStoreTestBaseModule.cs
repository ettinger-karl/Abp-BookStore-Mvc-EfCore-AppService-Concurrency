using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.IdentityServer;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Acme.BookStore
{
  [DependsOn(
      typeof(AbpAutofacModule),
      typeof(AbpTestBaseModule),
      typeof(AbpAuthorizationModule),
      typeof(BookStoreDomainModule)
      )]
  public class BookStoreTestBaseModule : AbpModule
  {
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
      PreConfigure<AbpIdentityServerBuilderOptions>(options =>
      {
        options.AddDeveloperSigningCredential = false;
      });

      PreConfigure<IIdentityServerBuilder>(identityServerBuilder =>
      {
        identityServerBuilder.AddDeveloperSigningCredential(false, System.Guid.NewGuid().ToString());
      });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
      string logFileName = @"G:\BFSTST\temp\log\Acme.BookStore.Test_.log";

      Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
        .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.File(logFileName, rollingInterval: RollingInterval.Day)
        .CreateLogger();

      context.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger));

      Configure<AbpBackgroundJobOptions>(options =>
      {
        options.IsJobExecutionEnabled = false;
      });

      context.Services.AddAlwaysAllowAuthorization();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
      SeedTestData(context);
    }

    private static void SeedTestData(ApplicationInitializationContext context)
    {
      AsyncHelper.RunSync(async () =>
      {
        using (var scope = context.ServiceProvider.CreateScope())
        {
          await scope.ServiceProvider
              .GetRequiredService<IDataSeeder>()
              .SeedAsync();
        }
      });
    }
  }
}
