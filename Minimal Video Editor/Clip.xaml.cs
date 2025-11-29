using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Minimal_Video_Editor
{
    /// <summary>
    /// Interaction logic for Clip.xaml
    /// </summary>
    public partial class Clip : UserControl
    {
        public string Filename { get; init; } = null!;

        public double Duration { get; init; } = 0d;

        public Clip(ClipFormat clip)
        {
            InitializeComponent();

            Filename = clip.Filename;
            Duration = clip.duration;


            FilenameLabel.Content = Filename;
            MainGrid.Width = Duration / 1000 * Timeline.PixelPerSecond;
        }
    }
}
