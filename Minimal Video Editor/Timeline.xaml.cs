using Emgu.CV;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        private readonly MainWindow mainwindow;

        const double defaultScale = 2d;

        public double ScaleX
        {
            get { return (double)GetValue(ScaleXProperty); }
            set { SetValue(ScaleXProperty, value); InverseScaleX = 1 / (value != 0 ? value: 1); }
        }
        public static readonly DependencyProperty ScaleXProperty =
            DependencyProperty.Register("ScaleX", typeof(double), typeof(Timeline), new PropertyMetadata(defaultScale));

        public double InverseScaleX
        {
            get { return (double)GetValue(InverseScaleXProperty); }
            set { SetValue(InverseScaleXProperty, value); }
        }
        public static readonly DependencyProperty InverseScaleXProperty =
            DependencyProperty.Register("InverseScaleX", typeof(double), typeof(Timeline), new PropertyMetadata(1/defaultScale));

        public Thickness TicksMargin
        {
            get { return (Thickness)GetValue(TicksMarginProperty); }
            set { SetValue(TicksMarginProperty, value); }
        }
        public static readonly DependencyProperty TicksMarginProperty =
            DependencyProperty.Register("TicksMargin", typeof(Thickness), typeof(Timeline), new PropertyMetadata(default));

        public const int PixelPerSecond = 30;
        private const int TickThickness = 1;

        //timelinewidth / pixelpersecond = visibleseconds

        public Timeline()
        {
            InitializeComponent();

            TicksMargin = new((PixelPerSecond - TickThickness) * ScaleX, 0, 0, 0);

            mainwindow = (MainWindow)Application.Current.MainWindow;
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            double pxToScale = 240;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ScaleX = Math.Clamp(ScaleX + e.Delta / pxToScale, 0.5, 10);
                TicksMargin = new((PixelPerSecond - TickThickness) * ScaleX, 0, 0, 0);
                
            }

            else
            {
                TimelineScrollViewer.ScrollToHorizontalOffset(TimelineScrollViewer.HorizontalOffset - e.Delta);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTicks();
        }

        private void UpdateTicks()
        {
            TicksStackPanel.Children.Clear();

            int visibleseconds = (int)(TimelineStackPanel.ActualWidth / PixelPerSecond);

            Binding binbin = new() { Source = this, Path = new("TicksMargin"), Mode = BindingMode.TwoWay };

            for (int i = 0; i < visibleseconds + 1; i++)
            {
                Rectangle rect = new() { Width = TickThickness, Margin = TicksMargin, Fill = Brushes.DodgerBlue };
                BindingOperations.SetBinding(rect, Rectangle.MarginProperty, binbin);
                TicksStackPanel.Children.Add(rect);
            }
        }

        private void TimelineStackPanel_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.Text)) return;
            
            string filename = e.Data.GetData(DataFormats.Text).ToString()!;


            VideoCapture captureFrame = new(filename);
            double FPS = captureFrame.Get(Emgu.CV.CvEnum.CapProp.Fps);
            double frames = captureFrame.Get(Emgu.CV.CvEnum.CapProp.FrameCount);

            double ms = frames * 1000/FPS;

            ClipFormat clip = new() { Filename=filename, FramesCount = frames, FPS=FPS, duration = ms };

            TimelineStackPanel.Children.Add(new Clip(clip));

            mainwindow.AddClip(clip);

            UpdateTicks();
        }
    }
}
