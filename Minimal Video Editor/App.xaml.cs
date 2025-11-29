using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Security;
using System.Windows;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if(!Settings.Default.RegEdited)
            {
                try
                {
                    Registry.SetValue("HKEY_CLASSES_ROOT\\.mveproj\\Shell\\Open\\Command", "", $"\"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}.exe\" \"%1\"", RegistryValueKind.String);
                    Settings.Default.RegEdited = true;
                    Settings.Default.Save();
                }
                catch (UnauthorizedAccessException) {  }
                catch (SecurityException) {  }
            }



            MainWindow mw = new();
            if(e.Args is [var filename] )
            {
                mw.LoadProject(filename);
            }
            mw.Show();
        }
    }

}
