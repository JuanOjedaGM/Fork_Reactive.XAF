﻿using System;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using Fasterflect;
using Hangfire;
using Hangfire.Dashboard;
using HarmonyLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Harmony;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.SecurityExtensions;
using StartupExtensions = DevExpress.ExpressApp.Blazor.Services.StartupExtensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Hangfire {
    public class UseHangfire : IStartupFilter {
        //static UseHangfire() =>
        //    typeof(StartupExtensions).Method(nameof(StartupExtensions.UseXaf),Fleags.StaticPublic)
        //        .PatchWith(postFix:new HarmonyMethod(typeof(UseHangfire),nameof(UseXaf)));
        private static readonly Harmony Harmony = new Harmony(nameof(UseHangfire));
        static UseHangfire() {
            var methodInfo = typeof(StartupExtensions).Method(nameof(StartupExtensions.UseXaf), Flags.StaticPublic);
            Harmony.Patch(methodInfo, postfix: new HarmonyMethod(typeof(UseHangfire), nameof(UseXaf))); //runtime patching of the UseXaf middleware
        }
        public static void UseXaf(IApplicationBuilder builder) => Dashboard?.Invoke(builder);

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) 
            => next;
        
        public static readonly Action<IApplicationBuilder> Dashboard = builder 
            => builder.UseHangfireDashboard(options:new DashboardOptions {
                Authorization = new[] {new DashboardAuthorization()}
        });
    }

    public class DashboardAuthorization : IDashboardAuthorizationFilter {
        public bool Authorize(DashboardContext context) {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity!.IsAuthenticated && httpContext.RequestServices.RunWithStorageAsync(application => {
                var security = application.Security;
                if (!security.IsSecurityStrategyComplex()) return true.Observe();
                if (security.IsActionPermissionGranted(nameof(JobSchedulerService.JobDashboard))) return true.Observe();
                using var objectSpace = application.CreateObjectSpace(security?.UserType);
                var user = (ISecurityUserWithRoles)objectSpace.FindObject(security?.UserType,
                    CriteriaOperator.Parse($"{nameof(ISecurityUser.UserName)}=?", httpContext.User.Identity.Name));
                var any = user.Roles.Cast<IPermissionPolicyRole>().Any(role => role.IsAdministrative);
                return any.Observe();

            }).Result;
        }
    }

    public class HangfireStartup : IHostingStartup{
        public void Configure(IWebHostBuilder builder) 
            => builder.ConfigureServices(services => services
                .AddHangfire(ConfigureHangfire)
                .AddHangfireServer()
                .AddSingleton<IStartupFilter, UseHangfire>()
                .AddSingleton<IHangfireJobFilter>(provider => new HangfireJobFilter(provider))
            );

        private static void ConfigureHangfire(IServiceProvider provider,IGlobalConfiguration configuration) {
            configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseDefaultTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseActivator(new ServiceJobActivator(provider.GetService<IServiceScopeFactory>()))
                .UseFilter(provider.GetService<IHangfireJobFilter>())
                .UseFilter(new AutomaticRetryAttribute() { Attempts = 0 });
            
            GlobalStateHandlers.Handlers.Add(new ChainJobState.Handler());
        }
    }
    


}