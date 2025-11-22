using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Windows.Themes;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;





// TODO: audio, usiamo raylib?



namespace Minimal_Video_Editor
{



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int CurrentlySelectedTool
        {
            get { return (int)GetValue(CurrentlySelectedToolProperty); }
            set { SetValue(CurrentlySelectedToolProperty, value); }
        }
        public static readonly DependencyProperty CurrentlySelectedToolProperty =
            DependencyProperty.Register("CurrentlySelectedTool", typeof(int), typeof(MainWindow), new PropertyMetadata(1));



        private Project project = new ();

        private string currentProjectPath = null;




        public static readonly RoutedUICommand SelectTool = new RoutedUICommand("Show tool", "SelectTool", typeof(MainWindow));
        public static readonly RoutedUICommand CuttingTool = new RoutedUICommand("Show tool", "CuttingTool", typeof(MainWindow));
        
        public static readonly RoutedUICommand PlayPause = new RoutedUICommand("Play/pause", "PlayPause", typeof(MainWindow));


        VideoCapture captureFrame;
        Mat frame = new Mat();
        Mat frame_copy = new Mat();

        System.Timers.Timer PlayBackTimer = new System.Timers.Timer();
        int FPS = 30;

        //Capture Image from file
        private void GetVideoFrames(String Filename)
        {
            try
            {
                captureFrame = new VideoCapture(Filename);
                captureFrame.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 100);
                
                //captureFrame.ImageGrabbed += ShowFrame;
                
                PlayBackTimer.Interval = 1000 / FPS;
                PlayBackTimer.Elapsed += (_, _) => { CurrentFrameImage.Dispatcher.BeginInvoke(() => {
                    Mat frame = captureFrame.QueryFrame();
                    if(frame == null)
                    {
                        PlayBackTimer.Stop();
                        return;
                    }
                    CurrentFrameImage.Source = ginopino.ToImageSource(frame.ToBitmap()); }, DispatcherPriority.Background);
                };
                PlayBackTimer.Start();
                //captureFrame.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //Show in ImageBox
        private void ShowFrame(object sender, EventArgs e)
        {
            captureFrame.Retrieve(frame);
            frame_copy = frame;

            CurrentFrameImage.Dispatcher.Invoke(() => { CurrentFrameImage.Source = ginopino.ToImageSource(frame_copy.ToBitmap()); }, DispatcherPriority.Background);
        }

        


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }


        public void showCuttingTool(object sender, ExecutedRoutedEventArgs e)
        {
            CuttingToolRadioButton.IsChecked = true;
        }
        public void showSelectionTool(object sender, ExecutedRoutedEventArgs e)
        {
            SelectionToolRadioButton.IsChecked = true;
        }



        private void SelectionTool_Checked(object sender, RoutedEventArgs e)
        {
            CurrentlySelectedTool = 0;
        }

        private void CuttingTool_Checked(object sender, RoutedEventArgs e)
        {
            CurrentlySelectedTool = 1;
        }

        private void Start_Playing(object sender, RoutedEventArgs e)
        {
            GetVideoFrames("D:\\Videos\\pre alpha Highness footage.mkv");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PlayBackTimer.Stop();
        }

        private void PlayerPlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayBackTimer.Start();
        }

        private void PlayerPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayBackTimer.Stop();
        }

        private void PlayerTogglePlay(object sender, ExecutedRoutedEventArgs e) {
            if(PlayBackTimer.Enabled) { PlayBackTimer.Stop(); }
            else { PlayBackTimer.Start(); }
        }

        private void LoadFile(string Filename)
        {
            FileLoaderWrapPanel.Children.Add(new FilePreview(Filename));
            
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var f in files)
                {
                    bool allowed = new FileInfo(f).Extension.ToLower() switch
                    {
                        ".mp4" or ".mkv" => true,
                        _ => false
                    };
                    bool notadded = !project.files.Contains(f);
                    if (allowed) {
                        if(notadded)
                        {
                            LoadFile(f);
                            project.files.Add(f);
                        } else { MessageBox.Show("\"" + f + "\" was already added to this project.", "File Already Added Warning", MessageBoxButton.OK, MessageBoxImage.Warning); }
                    }
                    else { MessageBox.Show(new FileInfo(f).Extension + " is not an allowed file extension (" + new FileInfo(f).Name + ")", "File Extension Warning", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }

                
            }
        }



        private string SerializeProject()
        {
            string json = JsonSerializer.Serialize(project, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
            });
            return json;
        }

        private void SaveProject()
        {
            if (currentProjectPath == null || !File.Exists(currentProjectPath))
            {
                SaveAsProject();
            } else
            {
                string json = SerializeProject();
                File.WriteAllText(currentProjectPath, json);
            }
        }
        private void SaveAsProject()
        {
            string json = SerializeProject();
            
            var d = new Microsoft.Win32.SaveFileDialog() { Filter="Project file|*.json", DefaultExt=".json", FileName=(currentProjectPath is not null ? new FileInfo(currentProjectPath).Name : ""), RestoreDirectory=true};

            if (d.ShowDialog() ?? false)
            {
                File.WriteAllText(d.FileName, json);
                currentProjectPath = d.FileName;
            }
        }

        private void LoadProject()
        {
            var d = new Microsoft.Win32.OpenFileDialog() { Filter="Project file|*.json", DefaultExt=".json", Multiselect=false};

            if(d.ShowDialog() ?? false)
            {
                currentProjectPath = d.FileName;

                FileLoaderWrapPanel.Children.Clear();

                var json = File.ReadAllText(d.FileName);

                project = JsonSerializer.Deserialize<Project>(json, new JsonSerializerOptions { AllowTrailingCommas=true, ReadCommentHandling=JsonCommentHandling.Skip, IncludeFields=true})!;

                project.files.ForEach(f => { LoadFile(f); });
            }
        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadProject();
        }
        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveProject();
        }
        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveAsProject();
        }
    }

    static class ginopino
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static ImageSource ToImageSource(this Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}