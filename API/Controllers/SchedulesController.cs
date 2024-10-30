using API.Models;
using Quartz;
using Quartz.Impl.Matchers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;

namespace API.Controllers
{
    [RoutePrefix("api/schedules")]
    public class SchedulesController : ApiController
    {
        private readonly IScheduler _scheduler;

        public SchedulesController(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        [HttpGet]
        [Route("jobs")]
        public async Task<IHttpActionResult> Jobs()
        {
            var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var jobDetails = new List<string>();

            foreach (var jobKey in jobKeys)
            {
                var detail = await _scheduler.GetJobDetail(jobKey);
                jobDetails.Add($"Job: {jobKey.Name}, Group: {jobKey.Group}");
            }

            return Ok(jobDetails);
        }

        [HttpGet]
        [Route("jobs-trigger-info")]
        public async Task<IHttpActionResult> GetAllJobsWithTriggerInfo()
        {
            var jobGroups = await _scheduler.GetJobGroupNames();
            var jobsWithTriggers = new List<JobWithTriggerInfo>();

            foreach (var group in jobGroups)
            {
                var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                foreach (var jobKey in jobKeys)
                {
                    var jobDetail = await _scheduler.GetJobDetail(jobKey);
                    var triggers = await _scheduler.GetTriggersOfJob(jobKey);

                    foreach (var trigger in triggers)
                    {
                        var triggerInfo = new JobWithTriggerInfo
                        {
                            JobKey = jobKey.Name,
                            Group = jobKey.Group,
                            Description = jobDetail.Description,
                            TriggerKey = trigger.Key.Name,
                            TriggerGroup = trigger.Key.Group,
                            NextFireTime = trigger.GetNextFireTimeUtc()?.LocalDateTime,
                            PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.LocalDateTime,
                            StartAt = trigger.StartTimeUtc.LocalDateTime,
                            EndAt = trigger.EndTimeUtc?.LocalDateTime,
                            TriggerState = (await _scheduler.GetTriggerState(trigger.Key)).ToString()
                        };

                        switch (trigger)
                        {
                            case ISimpleTrigger simpleTrigger:
                                triggerInfo.RepeatInterval = simpleTrigger.RepeatInterval;
                                break;
                            case ICronTrigger cronTrigger:
                                triggerInfo.CronExpression = cronTrigger.CronExpressionString;
                                break;
                        }

                        jobsWithTriggers.Add(triggerInfo);
                    }
                }

            }

            return Ok(jobsWithTriggers);
        }

        [HttpGet]
        [Route("job-trigger-info/{jobName}/{group}")]
        public async Task<IHttpActionResult> GetJobWithTriggerInfoById(string jobName, string group)
        {
            var jobKey = new JobKey(jobName);

            var jobDetail = await _scheduler.GetJobDetail(jobKey);
            var triggers = await _scheduler.GetTriggersOfJob(jobKey);

            if (jobDetail == null)
                return Content(HttpStatusCode.NotFound, "Job not found");

            var jobsWithTriggers = new List<JobWithTriggerInfo>();
            foreach (var trigger in triggers)
            {
                var triggerInfo = new JobWithTriggerInfo
                {
                    JobKey = jobKey.Name,
                    Group = jobKey.Group,
                    Description = jobDetail.Description,
                    TriggerKey = trigger.Key.Name,
                    TriggerGroup = trigger.Key.Group,
                    NextFireTime = trigger.GetNextFireTimeUtc()?.LocalDateTime,
                    PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.LocalDateTime,
                    StartAt = trigger.StartTimeUtc.LocalDateTime,
                    EndAt = trigger.EndTimeUtc?.LocalDateTime,
                    TriggerState = (await _scheduler.GetTriggerState(trigger.Key)).ToString()
                };

                switch (trigger)
                {
                    case ISimpleTrigger simpleTrigger:
                        triggerInfo.RepeatInterval = simpleTrigger.RepeatInterval;
                        break;
                    case ICronTrigger cronTrigger:
                        triggerInfo.CronExpression = cronTrigger.CronExpressionString;
                        break;
                }

                jobsWithTriggers.Add(triggerInfo);
            }

            return Ok(jobsWithTriggers);
        }

        [HttpGet]
        [Route("available-job-classes")]
        public IHttpActionResult GetAvailableJobClasses()
        {
            var jobAssembly = Assembly.Load(nameof(Worker));
            var jobClasses = jobAssembly.GetTypes()
                .Where(t => typeof(IJob).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => new
                {
                    Name = t.Name,
                    FullName = t.FullName
                })
                .ToList();

            return Ok(jobClasses);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> CreateJob(string jobName, string jobClass, int intervalInSeconds)
        {
            var jobKey = new JobKey(jobName);

            if (await _scheduler.CheckExists(jobKey))
            {
                return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("Job already exists")
                });
            }

            var jobAssembly = Assembly.Load(nameof(Worker));
            var jobType = jobAssembly.GetType(jobClass);

            if (jobType == null)
                return BadRequest("Invalid job class");

            var job = JobBuilder
                .Create(jobType)
                .WithIdentity(jobKey)
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .ForJob(job)
                .WithSimpleSchedule(schedule =>
                    schedule.WithIntervalInSeconds(intervalInSeconds).RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(job, trigger);

            return Ok(new { message = $"Job {jobName} created successfully" });
        }

        [HttpDelete]
        [Route("delete/{jobName}")]
        public async Task<IHttpActionResult> DeleteJob(string jobName)
        {
            var jobKey = new JobKey(jobName);

            if (!await _scheduler.CheckExists(jobKey)) return Content(HttpStatusCode.NotFound, "Job not found");

            await _scheduler.DeleteJob(jobKey);
            return Ok(new { message = $"Job {jobName} deleted successfully" });
        }

        [HttpPut]
        [Route("pause/{jobName}")]
        public async Task<IHttpActionResult> PauseJob(string jobName)
        {
            var jobKey = new JobKey(jobName);

            if (!await _scheduler.CheckExists(jobKey)) return Content(HttpStatusCode.NotFound, "Job not found");

            await _scheduler.PauseJob(jobKey);
            // return Ok($"Job {jobName} paused successfully");
            return Ok(new { message = $"Job {jobName} paused successfully" });
        }

        [HttpPut]
        [Route("resume/{jobName}")]
        public async Task<IHttpActionResult> ResumeJob(string jobName)
        {
            var jobKey = new JobKey(jobName);

            if (!await _scheduler.CheckExists(jobKey)) return Content(HttpStatusCode.NotFound, "Job not found");

            await _scheduler.ResumeJob(jobKey);
            return Ok(new { message = $"Job {jobName} resumed successfully" });
        }

    }
}
