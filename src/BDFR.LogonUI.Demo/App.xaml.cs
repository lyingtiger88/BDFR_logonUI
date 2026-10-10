using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BDFR.LogonUI.Demo;

public partial class App : Application
{
    private static bool _secureLockMode;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _secureLockMode = e.Args.Any(arg =>
            string.Equals(arg, "--secure-lock", StringComparison.OrdinalIgnoreCase));

        try
        {
            var window = new MainWindow(_secureLockMode);
            MainWindow = window;
            window.Show();
            window.Activate();
        }
        catch (Exception ex)
        {
            ReportFatalStartupError(ex);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog("DispatcherUnhandledException", e.Exception);

        if (_secureLockMode)
        {
            e.Handled = true;
            Current.Shutdown(-2);
            return;
        }

        MessageBox.Show(
            $"BDFR LogonUI Demo encountered an error.\n\n{e.Exception.Message}\n\nA diagnostic log was saved to:\n{GetCrashLogPath()}",
            "BDFR LogonUI Demo",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteCrashLog("AppDomain.UnhandledException", ex);
    }

    private static void ReportFatalStartupError(Exception ex)
    {
        WriteCrashLog("Startup failure", ex);

        if (_secureLockMode)
            return;

        MessageBox.Show(
            $"BDFR LogonUI Demo could not start.\n\n{ex.Message}\n\nA diagnostic log was saved to:\n{GetCrashLogPath()}",
            "BDFR LogonUI Demo - Startup Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteCrashLog(string category, Exception ex)
    {
        try
        {
            var path = GetCrashLogPath();
            var directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);

            var text = new StringBuilder()
                .AppendLine("BDFR LogonUI Demo diagnostic log")
                .AppendLine($"Timestamp UTC: {DateTimeOffset.UtcNow:O}")
                .AppendLine($"Category: {category}")
                .AppendLine($"OS: {Environment.OSVersion}")
                .AppendLine($".NET: {Environment.Version}")
                .AppendLine()
                .AppendLine(ex.ToString())
                .AppendLine(new string('-', 80))
                .ToString();

            File.AppendAllText(path, text);
        }
        catch
        {
            // Logging must never hide the original error.
        }
    }

    private static string GetCrashLogPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI",
            "crash.log");
}
