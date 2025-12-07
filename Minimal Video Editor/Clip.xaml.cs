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

namespace Minimal_Video_Editor;

/// <summary>
/// Interaction logic for Clip.xaml
/// </summary>
public partial class Clip : UserControl
{
    public string Filename { get; init; } = null!;

    public double Duration { get; init; } = 0d;

    public double Start {get; init;} = 0d;

    private Timeline Timeline {get; init;}

    public Clip(ClipFormat clip, Timeline tl)
    {
        InitializeComponent();

        Filename = clip.Filename;
        Duration = clip.Duration;
        Timeline = tl;


        FilenameLabel.Content = Filename;
        MainGrid.Width = Duration / 1000 * Timeline.PixelPerSecond;
    }

    private void Clip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
    }

    private void Clip_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Clip)))
        {
            Clip from = (Clip)e.Data.GetData(typeof(Clip));
        
            Point mouse = e.GetPosition(this);

            bool right = mouse.X > MainGrid.ActualWidth / 2;

            Timeline.MoveClip(from, this, right);
        }
    }
}
