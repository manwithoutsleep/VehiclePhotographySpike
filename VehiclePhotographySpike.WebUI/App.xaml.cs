// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Reflection;
using System;
using System.Threading;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VehiclePhotographySpike.WebUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        private Window m_window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Services = ConfigureServices();

            this.InitializeComponent();
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            //services.AddSingleton<IFilesService, FilesService>();
            //services.AddSingleton<ISettingsService, SettingsService>();
            //services.AddSingleton<IClipboardService, ClipboardService>();
            //services.AddSingleton<IShareService, ShareService>();
            //services.AddSingleton<IEmailService, EmailService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        /// <summary>
        /// Register attributed classes with your Ioc container.
        /// </summary>
        /// <param name="services">The ServiceCollection to be used.</param>
        private static void RegisterViewModelsWithIoc(ServiceCollection services)
        {
            string localname = (typeof(App)).GetTypeInfo().Assembly.GetName().Name;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;

                if (assemblyName != localname && !assemblyName.EndsWith("ViewModels"))
                    continue;

                var types = assembly.GetTypes().Select(
                    t => new {
                        T = t,
                        Mode = t.GetCustomAttribute<Microsoft.Extensions.DependencyInjection.RegisterWithIocAttribute>()?.Mode
                    }
                ).Where(o => o.Mode != null && o.Mode != InstanceMode.None);

                foreach (var t in types)
                {
                    var type = t.T;
                    if (t.Mode == InstanceMode.Singleton)
                        services.AddSingleton(type);
                    else if (t.Mode == InstanceMode.Transient)
                        services.AddTransient(type);
                }
            }
        }
    }
}
