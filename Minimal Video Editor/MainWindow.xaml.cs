using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Windows.Themes;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

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



        private (RoutedCommand rc, ExecutedRoutedEventHandler func)[] KeyboardCommands;



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



            KeyboardCommands = [
                (new RoutedCommand(" ", typeof(MainWindow), [new KeyGesture(Key.C, ModifierKeys.Alt)]),
                showCuttingTool),
                (new RoutedCommand(" ", typeof(MainWindow), [new KeyGesture(Key.V, ModifierKeys.Alt)]),
                showSelectionTool),
                (new RoutedCommand(" ", typeof(MainWindow), [new KeyGesture(Key.Space, ModifierKeys.Alt)]),
                PlayerTogglePlay),
            ];

            for (int i = 0; i < KeyboardCommands.Length; i++)
            {
                CommandBindings.Add(new CommandBinding(KeyboardCommands[i].rc, KeyboardCommands[i].func));
            }

            
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

        private void PlayerTogglePlay(object sender, RoutedEventArgs e) {
            if(PlayBackTimer.Enabled) { PlayBackTimer.Stop(); }
            else { PlayBackTimer.Start(); }
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