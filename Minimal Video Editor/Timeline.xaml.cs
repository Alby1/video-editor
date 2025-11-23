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
using System.Diagnostics;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public double ScaleX
        {
            get { return (double)GetValue(ScaleXProperty); }
            set { SetValue(ScaleXProperty, value); }
        }
        public static readonly DependencyProperty ScaleXProperty =
            DependencyProperty.Register("ScaleX", typeof(double), typeof(Timeline), new PropertyMetadata(1d));

        private const int PixelPerSecond = 60;
        private const int TickThickness = 1;

        //timelinewidth / pixelpersecond = visibleseconds

        public Timeline()
        {
            InitializeComponent();

            
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            double pxToScale = 240;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) 
            ScaleX = Math.Clamp(ScaleX + e.Delta / pxToScale, 0.5, 10);

            else
            {
                TimelineScrollViewer.ScrollToHorizontalOffset(TimelineScrollViewer.HorizontalOffset - e.Delta);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            int visibleseconds = (int)(TimelineStackPanel.ActualWidth / PixelPerSecond);

            for (int i = 0; i < visibleseconds+1; i++)
            {
                TicksStackPanel.Children.Add(new Rectangle() { Width = TickThickness, Margin = new Thickness(PixelPerSecond-TickThickness, 0, 0, 0), Fill = Brushes.DodgerBlue });
            }
        }
    }
}
