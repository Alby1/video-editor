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

        private bool FileExists;

        private readonly MainWindow mainwindow;
        public FilePreview(string filename)
        {
            InitializeComponent();
            Filename = filename;

            FileExists = File.Exists(Filename);

            Label.Content = new FileInfo(Filename).Name;

            if (FileExists)
            {
                ShellFile a = ShellFile.FromFilePath(Filename);
                BitmapSource? b = a.Thumbnail?.ExtraLargeBitmapSource;
                Image.Source = b;
            } else
            {
                Label.Foreground = Brushes.Red;

                BitmapImage bm = new();
                bm.BeginInit();
                bm.UriSource = new Uri("/assets/mediamissing.png", UriKind.Relative);
                bm.EndInit();
                Image.Source = bm;

                Cursor = Cursors.Hand;
            }


            mainwindow = (MainWindow)Application.Current.MainWindow;
        }

        private void SourceVideoPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileExists) mainwindow.PlaySourceVideo(Filename);
        }

        private void SourceVideoRemoveFromProject_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to remove " + Filename + " from this project?", "Are you sure?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                mainwindow.RemoveFile(Filename);
            }
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !FileExists) return;

            DragDrop.DoDragDrop(this, Filename, DragDropEffects.Move);
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FileExists) return;

            mainwindow.RecoverMedia(Filename);
        }
    }
}
