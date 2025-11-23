using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for FilePreview.xaml
    /// </summary>
    public partial class FilePreview : UserControl
    {
        public string Filename;

        private MainWindow mainwindow;
        public FilePreview(string Filename)
        {
            InitializeComponent();

            this.Filename = Filename;
            Label.Content = new FileInfo(Filename).Name;

            ShellFile a = ShellFile.FromFilePath(Filename);
            BitmapSource? b = a.Thumbnail?.ExtraLargeBitmapSource;
            Image.Source = b;

            mainwindow = (MainWindow)Application.Current.MainWindow;
        }

        private void SourceVideoPlayButton_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.PlaySourceVideo(Filename);
        }

        private void SourceVideoRemoveFromProject_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove " + Filename + " from this project?", "Are you sure?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                mainwindow.RemoveFile(Filename);
            }
        }
    }
}
