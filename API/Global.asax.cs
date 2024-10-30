using Ninject;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Worker;

namespace API
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private readonly IKernel _kernel;
        public WebApiApplication()
        {
            _kernel = new StandardKernel(new DependencyInjectionModule());
        }

        protected void Application_Start()
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
            scheduler.JobFactory = _kernel.Get<IJobFactory>();

            // Define jobs and triggers  
            var jobKey = JobKey.Create(nameof(LoggingBackgroundJob));
            var job = JobBuilder.Create<LoggingBackgroundJob>()
                .WithIdentity(jobKey)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("LoggingJobTrigger")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
                .Build();

            scheduler.ScheduleJob(job, trigger);
            scheduler.Start();

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            // Shutdown the scheduler  
            var scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
            if (scheduler != null)
            {
                scheduler.Shutdown();
            }
        }
    }
}
