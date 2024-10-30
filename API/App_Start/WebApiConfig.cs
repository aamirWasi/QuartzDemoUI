using System.Web.Http;
using System.Web.Http.Cors;

namespace API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.EnableCors(new EnableCorsAttribute("http://localhost:4200", "*", "*"));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            /*var kernel = new StandardKernel(new JobModule());
            config.DependencyResolver = new NinjectDependencyResolver(kernel);*/
        }
    }
}
