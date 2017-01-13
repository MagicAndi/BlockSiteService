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
            HostFactory.Run(configure =>
            {
                configure.Service<MyService>(service =>
                {
                    service.ConstructUsing(s => new MyService());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });

                //Setup Account that window service use to run.  
                configure.RunAsLocalSystem();
                configure.SetServiceName("BlockSiteService");
                configure.SetDisplayName("BlockSite Service");
                configure.SetDescription("A .NET Windows Service created to block unproductive websites. Built with Topshelf.");
            });
        }
    }
}
