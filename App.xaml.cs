using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SearchFilesTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize logger
            Logger.Initialize();
            Logger.Info("Application startup initiated");

            // Clean up old log files (older than 7 days)
            Logger.CleanupOldLogs(daysToKeep: 7);

            // Handle unhandled exceptions in the UI thread
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Handle unhandled exceptions in async operations
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // Handle WPF-specific dispatcher unhandled exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                Logger.Error($"Unhandled AppDomain Exception", ex);
            }
            else
            {
                Logger.Error($"Unhandled AppDomain Exception: {e.ExceptionObject}");
            }

            if (e.IsTerminating)
            {
                Logger.Error("Application is terminating due to unhandled exception");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            Logger.Error("Unhandled Task Exception", e.Exception);
            e.SetObserved(); // Prevent process termination
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled Dispatcher Exception", e.Exception);

            // Show user-friendly error message
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\n" +
                $"Details have been logged to:\n{Logger.GetLogFilePath()}",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Mark exception as handled to prevent application termination
            e.Handled = true;
        }
    }
}
