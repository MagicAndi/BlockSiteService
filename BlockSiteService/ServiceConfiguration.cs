using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Topshelf;

namespace BlockSiteService
{
    internal static class ServiceConfiguration
    {
        internal static void Configure()
        {
            var serviceName = AppScope.Configuration.ApplicationTitle;

            HostFactory.Run(configure =>
            {
                configure.Service<Service>(service =>
                {
                    service.ConstructUsing(s => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                // Setup Account that window service use to run.  
                configure.RunAsLocalSystem();  // .RunAsLocalService();
                configure.StartAutomaticallyDelayed();
                configure.UseNLog();

                configure.SetServiceName(serviceName.Replace(" ", ""));
                configure.SetDisplayName(serviceName);
                configure.SetDescription("A .NET Windows Service created to block unproductive websites. Built with Topshelf.");
            });
        }
    }
}
