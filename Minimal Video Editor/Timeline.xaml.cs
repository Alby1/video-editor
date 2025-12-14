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

        const double defaultScale = 4d;
        public const int PixelPerSecond = 10;
        private const double TickThickness = 1.5;

        //timelinewidth / pixelpersecond = visibleseconds

        public Project Project { get { return mainwindow.Project; } }

        public ToolSelection CurrentlySelectedTool { get { return mainwindow.CurrentlySelectedTool; } }
        
        public MainWindow MainWindow { get { return mainwindow; } }

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
        
        public double TextSpacing
        {
            get { return (double)GetValue(TextSpacingProperty); }
            set { SetValue(TextSpacingProperty, value); }
        }
        public static readonly DependencyProperty TextSpacingProperty =
            DependencyProperty.Register("TextSpacing", typeof(double), typeof(Timeline), new PropertyMetadata(default));


        public Timeline()
        {
            InitializeComponent();

            UpdateTicksSize();

            mainwindow = (MainWindow)Application.Current.MainWindow;
        }

        private void UpdateTicksSize()
        {
            TicksMargin = new((PixelPerSecond * ScaleX) - TickThickness, 0, 0, 0);
            TextSpacing = PixelPerSecond * ScaleX;

            NumbersStackPanel.ShowOneForEach((int)(75 / TextSpacing));
        }

        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            double pxToScale = 240;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ScaleX = Math.Clamp(ScaleX + e.Delta / pxToScale, 0.5, 10);
                UpdateTicksSize();
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
            NumbersStackPanel.Children.Clear();

            double visibleseconds = Project.clips.Count != 0 ? Project.clips.Sum(clip => clip.Duration) / 1000 : this.ActualWidth / PixelPerSecond; 

            Binding binbin = new() { Source = this, Path = new("TicksMargin"), Mode = BindingMode.TwoWay };

            for (int i = 0; i < (int)visibleseconds + 1; i++)
            {
                Rectangle rect = new() { Width = TickThickness, Margin = TicksMargin, Fill = Brushes.DodgerBlue };
                BindingOperations.SetBinding(rect, Rectangle.MarginProperty, binbin);
                TicksStackPanel.Children.Add(rect);


                string time = TimeSpan.FromSeconds(i + 1).ToString("mm':'ss");
                Label lb = new() { Content = time, Padding = new Thickness(0), FontFamily=new FontFamily("Cascadia Mono") };
                NumbersStackPanel.Children.Add(lb);
            }
        }

        private void TimelineStackPanel_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(Tuple))) return;

            (Guid reference, string filename) = (Tuple<Guid, string>)e.Data.GetData(typeof(Tuple));

            mainwindow.AddClip(AddClip(reference, filename));

            mainwindow.IHaveMadeChanges();
        }

        private ClipFormat AddClip(Guid reference, string filename)
        {
            VideoCapture captureFrame = new(filename);
            double FPS = captureFrame.Get(Emgu.CV.CvEnum.CapProp.Fps);
            double frames = captureFrame.Get(Emgu.CV.CvEnum.CapProp.FrameCount);

            double ms = frames * 1000 / FPS;

            ClipFormat clip = new() { Reference = reference, FramesCount = frames, FPS = FPS, Duration = ms };

            AddClip(clip);

            return clip;
        }

        public void AddClip(ClipFormat clip)
        {
            TimelineStackPanel.Children.Add(new Clip(clip, this));

            UpdateTicks();
        }

        public void Clear()
        {
            TimelineStackPanel.Children.Clear();
            UpdateTicks();
            ScaleX = defaultScale;
        }

        public void MoveClip(Clip moving, Clip pivot, bool right)
        {
            if (moving == pivot) return;


            TimelineStackPanel.Children.Remove(moving);

            int id = TimelineStackPanel.Children.IndexOf(pivot);

            int righter = right ? 1 : 0;
            TimelineStackPanel.Children.Insert(id + righter, moving);
        }
    }




    public class CenterSpacedPanel : Panel
    {
        public double CenterSpacing
        {
            get => (double)GetValue(CenterSpacingProperty);
            set => SetValue(CenterSpacingProperty, value);
        }

        public static readonly DependencyProperty CenterSpacingProperty =
            DependencyProperty.Register(
                nameof(CenterSpacing),
                typeof(double),
                typeof(CenterSpacedPanel),
                new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        protected override Size MeasureOverride(Size availableSize)
        {
            double maxHeight = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }

            double totalWidth = 0;
            if (InternalChildren.Count > 0)
            {
                // one spacing before first, then between centers, then last half-width
                double centers = (InternalChildren.Count - 1) * CenterSpacing;
                double firstHalf = InternalChildren[0].DesiredSize.Width / 2.0;
                double lastHalf = InternalChildren[InternalChildren.Count - 1].DesiredSize.Width / 2.0;

                totalWidth = CenterSpacing + centers + firstHalf + lastHalf;
            }

            return new Size(totalWidth, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (InternalChildren.Count == 0)
                return finalSize;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null) continue;

                double w = child.DesiredSize.Width;
                double h = child.DesiredSize.Height;

                // center position
                double centerX = (i + 1) * CenterSpacing;

                // left so that center is at centerX
                double left = centerX - w / 2.0;
                double top = (finalSize.Height - h) / 2.0;

                child.Arrange(new Rect(new Point(left, top), child.DesiredSize));
            }

            return finalSize;
        }

        public void ShowOneForEach(int n)
        {
            if (InternalChildren.Count == 0) return;
            if (n == 0) return;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                UIElement child = InternalChildren[i];
                if (child == null) continue;

                if(i % n != 0) child.Visibility = Visibility.Collapsed;
                else child.Visibility = Visibility.Visible;
            }
        }

    }
}
