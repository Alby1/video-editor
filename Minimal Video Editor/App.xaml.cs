using Emgu.CV.Ocl;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Windows;

namespace Minimal_Video_Editor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if(!Settings.Default.RegEdited)
        {
            if (MessageBox.Show("Would you like to associate .mveproj (Malbyx Video Editor Project) files to open with this software?", "File association prompt", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    Process process = new();
                    ProcessStartInfo startInfo = new()
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "reg",
                        Arguments = $"add HKCR\\.mveproj\\Shell\\Open\\Command /t REG_SZ /ve /f /d \"\\\"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}.exe\\\" \\\"%1\\\"\"",
                        Verb = "runas",
                        UseShellExecute = true,
                    };
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();


                    Settings.Default.RegEdited = true;
                    Settings.Default.Save();
                }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
            }

        }



        MainWindow mw = new();
        if(e.Args is [var filename] )
        {
            mw.LoadProject(filename);
        }
        mw.Show();
    }
}

