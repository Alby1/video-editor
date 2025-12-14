using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private ClipFormat ClipData { get; init; }

    private Timeline Timeline {get; init;}

    public Clip(ClipFormat clip, Timeline tl)
    {
        InitializeComponent();

        ClipData = clip;
        Timeline = tl;


        FilenameLabel.Content = ClipData.Filename(tl.Project);
        MainGrid.Width = ClipData.Duration / 1000 * Timeline.PixelPerSecond;
    }

    private void Clip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (Timeline.CurrentlySelectedTool != ToolSelection.Select) return;

        MainGrid.Opacity = 0.7;

        DragDrop.DoDragDrop(this, this, DragDropEffects.Move);

        MainGrid.Opacity = 1;
    }

    private void Clip_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(Clip))) return;
       
        Clip from = (Clip)e.Data.GetData(typeof(Clip));

        Timeline.MoveClip(from, this, GetSide(e));

        HideSideIndicators();
    }

    private void HideSideIndicators()
    {
        ClipMovingDropSideLeftIndicatorGrid.Visibility = Visibility.Hidden;
        ClipMovingDropSideRightIndicatorGrid.Visibility = Visibility.Hidden;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if on the right side</returns>
    private bool GetSide(DragEventArgs e)
    {
        Point mouse = e.GetPosition(this);

        bool right = mouse.X > MainGrid.ActualWidth / 2;

        return right;
    }

    private void UserControl_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(Clip))) return;

        Clip from = (Clip)e.Data.GetData(typeof(Clip));

        if (this == from) return;

        if (GetSide(e)) // if right
        {
            ClipMovingDropSideRightIndicatorGrid.Visibility = Visibility.Visible;
            ClipMovingDropSideLeftIndicatorGrid.Visibility = Visibility.Hidden;
        }
        else
        {
            ClipMovingDropSideRightIndicatorGrid.Visibility = Visibility.Hidden;
            ClipMovingDropSideLeftIndicatorGrid.Visibility = Visibility.Visible;
        }
    }

    private void UserControl_DragLeave(object sender, DragEventArgs e)
    {
        HideSideIndicators();
    }
}
