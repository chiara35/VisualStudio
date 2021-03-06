﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GitHub.Services;
using Microsoft.VisualStudio.Shell;

namespace GitHub.Extensions
{
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Safe variant of GetService that doesn't throw exceptions if the service is
        /// not found.
        /// </summary>
        /// <returns>The service, or null if not found</returns>
        public static object TryGetService(this IServiceProvider serviceProvider, Type type)
        {
            var ui = serviceProvider as IUIProvider;
            if (ui != null)
                return ui.TryGetService(type);

            object ret = null;
            try
            {
                ret = serviceProvider.GetService(type);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                VisualStudio.VsOutputLogger.WriteLine("GetServiceAndCache: Could not obtain instance of '{0}'", type);
            }
            return ret;
        }

        /// <summary>
        /// Calls <see cref="TryGetService(IServiceProvider, Type)" with type <typeparamref name="Ret"/>
        /// and returns the service obtained cast to type <typeparamref name="T"/>. Use this for services
        /// like <code>GetService(typeof(SVsTextManager) as IVsTextManager</code>
        /// Does not throw exceptions
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static Ret GetService<T, Ret>(this IServiceProvider provider)
            where Ret : class
            where T : class
        {
            return provider.TryGetService(typeof(T)) as Ret;
        }
        public static T GetExportedValue<T>(this IServiceProvider serviceProvider) where T : class
        {
            var ui = serviceProvider as IUIProvider;
            return ui != null
                ? ui.TryGetService(typeof(T)) as T
                : VisualStudio.Services.ComponentModel.DefaultExportProvider.GetExportedValueOrDefault<T>();
        }

        /// <summary>
        /// Safe generic variant that calls <see cref="TryGetService(IServiceProvider, Type)"/>
        /// so it doesn't throw exceptions if the service is not found
        /// </summary>
        /// <returns>The service, or null if not found</returns>
        public static T TryGetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.TryGetService(typeof(T)) as T;
        }

        /// <summary>
        /// Safe generic variant of GetService that doesn't throw exceptions if the service
        /// is not found (calls <see cref="TryGetService(IServiceProvider, Type)"/>)
        /// </summary>
        /// <returns>The service, or null if not found</returns>
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.TryGetService(typeof(T)) as T;
        }

        public static void AddCommandHandler(this IServiceProvider provider,
            Guid guid,
            int cmdId,
            EventHandler eventHandler)
        {
            var mcs = provider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Debug.Assert(mcs != null, "No IMenuCommandService? Something is wonky");
            if (mcs == null)
                return;
            var id = new CommandID(guid, cmdId);
            var item = new MenuCommand(eventHandler, id);
            mcs.AddCommand(item);
        }

        public static OleMenuCommand AddCommandHandler(this IServiceProvider provider,
            Guid guid,
            int cmdId,
            Func<bool> canEnable,
            Action execute,
            bool disable = false)
        {
            var mcs = provider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Debug.Assert(mcs != null, "No IMenuCommandService? Something is wonky");
            if (mcs == null)
                return null;
            var id = new CommandID(guid, cmdId);
            var item = new OleMenuCommand(
                (s, e) => execute(),
                (s, e) => { },
                (s, e) =>
                {
                    if (disable)
                        ((OleMenuCommand)s).Enabled = canEnable();
                    else
                        ((OleMenuCommand)s).Visible = canEnable();
                },
                id);
            mcs.AddCommand(item);
            return item;
        }
    }
}
