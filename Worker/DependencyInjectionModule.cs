using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ninject;
using Ninject.Modules;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Threading.Tasks;

namespace Worker
{
    [DisallowConcurrentExecution]
    public class LoggingBackgroundJob : IJob
    {
        private readonly ILogger<LoggingBackgroundJob> _logger;

        public LoggingBackgroundJob(ILogger<LoggingBackgroundJob> logger)
        {
            _logger = logger;
        }
        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("{UtcNow}", DateTime.UtcNow);

            return Task.CompletedTask;
        }
    }

    [DisallowConcurrentExecution]
    public class LoggingBackgroundSampleJob : IJob
    {
        private readonly ILogger<LoggingBackgroundSampleJob> _logger;

        public LoggingBackgroundSampleJob(ILogger<LoggingBackgroundSampleJob> logger)
        {
            _logger = logger;
        }
        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("{UtcNow}", DateTime.UtcNow);

            return Task.CompletedTask;
        }
    }

    public class DependencyInjectionModule : NinjectModule
    {
        public override void Load()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();

            var loggerFactory = serviceProvider
                .GetRequiredService<ILoggerFactory>();

            Bind<ILoggerFactory>().ToConstant(loggerFactory).InSingletonScope();

            Bind<IJob>().To<LoggingBackgroundJob>().InTransientScope();
            Bind<IJob>().To<LoggingBackgroundSampleJob>().InTransientScope();

            Bind<ILogger<LoggingBackgroundJob>>()
                .ToMethod(context =>
                        {
                            return loggerFactory.CreateLogger<LoggingBackgroundJob>();
                        });
            Bind<ILogger<LoggingBackgroundSampleJob>>()
                .ToMethod(context =>
                        {
                            return loggerFactory.CreateLogger<LoggingBackgroundSampleJob>();
                        });

            Bind<IJobFactory>().To<NinjectJobFactory>();
            //Bind<ISchedulerFactory>().To<StdSchedulerFactory>().InSingletonScope();
            Bind<IScheduler>().ToMethod(r =>
            {
                ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
                var scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
                scheduler.Start();
                return scheduler;
            }).InSingletonScope();
        }
    }

    public class NinjectJobFactory : IJobFactory
    {
        private readonly IKernel _kernel;

        public NinjectJobFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _kernel.Get(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            if (job is IDisposable disposable)
                disposable.Dispose();
        }
    }
}