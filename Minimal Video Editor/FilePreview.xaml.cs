using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Shell;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for FilePreview.xaml
    /// </summary>
    public partial class FilePreview : UserControl
    {
        public string Filename;
        public FilePreview(string Filename)
        {
            InitializeComponent();

            this.Filename = Filename;
            Label.Content = new FileInfo(Filename).Name;

            ShellFile a = ShellFile.FromFilePath(Filename);
            BitmapSource? b = a.Thumbnail?.ExtraLargeBitmapSource;
            Image.Source = b;
        }

        private void SourceVideoPlayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = (MainWindow)Application.Current.MainWindow;

            window.PlaySourceVideo(Filename);
        }
    }
}
