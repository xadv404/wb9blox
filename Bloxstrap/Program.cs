using System.Windows;

namespace Bloxstrap
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Any(arg => arg.Equals("-startupupdate", StringComparison.OrdinalIgnoreCase)))
            {
                Environment.Exit(StartupUpdater.Run(args));
                return;
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
