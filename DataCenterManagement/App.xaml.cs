using SQLitePCL;
using System.Windows;

namespace DataCenterManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Batteries.Init(); // Fixes XDG0003: SQLitePCL.raw.SetProvider() must be called
            base.OnStartup(e);
        }
    }
}