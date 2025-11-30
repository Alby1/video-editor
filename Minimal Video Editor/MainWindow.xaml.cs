using Emgu.CV;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;





// TODO: audio, usiamo raylib?


// TODO: undo/redo: undo history



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



        private Project project = new();

        private string CurrentProjectPath { get => field; set { field = value; SetWindowTitle(); } } = null!;


        private bool HasUnsavedChanges { get => field; set { field = value; SetWindowTitle(); } } = false;


        public static readonly RoutedUICommand SelectTool = new("Show tool", "SelectTool", typeof(MainWindow));
        public static readonly RoutedUICommand CuttingTool = new("Show tool", "CuttingTool", typeof(MainWindow));

        public static readonly RoutedUICommand PlayPause = new("Play/pause", "PlayPause", typeof(MainWindow));

        public static readonly RoutedUICommand ImportMediaCommand = new("Import media", "ImportMediaCommand", typeof(MainWindow));
        
        public static readonly RoutedUICommand NewProjectThisWindow = new("New project in this window", "NewProjectThisWindow", typeof(MainWindow));
        public static readonly RoutedUICommand NewProjectNewWindow = new("New project in a new window", "NewProjectNewWindow", typeof(MainWindow));


        VideoCapture captureFrame = null!;

        private readonly System.Timers.Timer PlayBackTimer = new();
        double FPS = 10;

        //Capture Image from file
        private void GetVideoFrames(string Filename)
        {
            try
            {
                captureFrame = new VideoCapture(Filename);
                FPS = captureFrame.Get(Emgu.CV.CvEnum.CapProp.Fps);
                //captureFrame.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 100);

                //captureFrame.ImageGrabbed += ShowFrame;

                PlayBackTimer.Interval = 1000 / FPS;
                StartPlayback();
                //captureFrame.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void PlayFrame(object? sender, ElapsedEventArgs e)
        {
            CurrentFrameImage.Dispatcher.BeginInvoke(() =>
            {
                Mat frame = captureFrame?.QueryFrame()!;
                if (frame == null)
                {
                    PausePlayback();
                    return;
                }
                CurrentFrameImage.Source = ImageConvertor.ToImageSource(frame.ToBitmap());
            }, DispatcherPriority.Background);
        }

        public void PlaySourceVideo(string Filename)
        {
            GetVideoFrames(Filename);
        }

        public void RemoveFile(string Filename)
        {
            project.files.Remove(Filename);

            var toremove = FileLoaderWrapPanel.Children
                .OfType<FilePreview>()
                .Where(e => (e as FilePreview)?.Filename == Filename)
                .FirstOrDefault();

            FileLoaderWrapPanel.Children.Remove(toremove);

            HasUnsavedChanges = true;

            if (FileLoaderWrapPanel.Children.Count <= 0) {
                NoFilesInFileLoaderLabel.Visibility = Visibility.Visible;
            }
        }




        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SetWindowTitle();

            PlayBackTimer.Elapsed += PlayFrame;

            PausePlayback();
        }

        private void SetWindowTitle()
        {
            string projectName = (CurrentProjectPath is not null ? CurrentProjectPath : "New Project");
            string unsavedChanges = (HasUnsavedChanges ? " *" : "");

            Title = projectName + unsavedChanges + " - Malbyx Video Editor";
        }


        public void ShowCuttingTool(object sender, ExecutedRoutedEventArgs e)
        {
            CuttingToolRadioButton.IsChecked = true;
        }
        public void ShowSelectionTool(object sender, ExecutedRoutedEventArgs e)
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PausePlayback();
            // TOFIX: se hai modifiche non salvate non chiudere
        }

        private void PlayerPlayButton_Click(object sender, RoutedEventArgs e)
        {
            StartPlayback();
        }

        private void StartPlayback()
        {
            PlayBackTimer.Start();
            PlayerPlayButton.Tag = "current";
            PlayerPauseButton.Tag = "";
        }

        private void PlayerPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PausePlayback();
        }

        private void PausePlayback()
        {
            PlayBackTimer.Stop();
            PlayerPauseButton.Tag = "current";
            PlayerPlayButton.Tag = "";
        }

        private void PlayerTogglePlay(object sender, ExecutedRoutedEventArgs e) {
            if (PlayBackTimer.Enabled) { PausePlayback(); }
            else { StartPlayback(); }
        }

        private void LoadFile(string Filename)
        {
            FileLoaderWrapPanel.Children.Add(new FilePreview(Filename));

            NoFilesInFileLoaderLabel.Visibility = Visibility.Collapsed;
        }

        private static readonly string[] SupportedExtensions = [".mp4", ".mkv"];

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var f in files)
                {
                    bool allowed = SupportedExtensions.Contains(new FileInfo(f).Extension.ToLower());

                    if (allowed) {
                        AddFile(f);
                    }
                    else { MessageBox.Show(new FileInfo(f).Extension + " is not an allowed file extension (" + new FileInfo(f).Name + ")", "File Extension Warning", MessageBoxButton.OK, MessageBoxImage.Warning); }
                }

                
            }
        }

        private void AddFile(string Filename)
        {
            bool notadded = !project.files.Contains(Filename);
            if (notadded)
            {
                LoadFile(Filename);
                project.files.Add(Filename);
                HasUnsavedChanges = true;
            }
            else { MessageBox.Show("\"" + Filename + "\" was already added to this project.", "File Already Added Warning", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }


        public void AddClip(ClipFormat clip)
        {
            project.clips.Add(clip);
        }


        private static readonly JsonSerializerOptions jsonSerializationOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true,
        };

        private string SerializeProject()
        {
            string json = JsonSerializer.Serialize(project, jsonSerializationOptions);
            return json;
        }

        private void SaveProject()
        {   
            if (CurrentProjectPath == null || !File.Exists(CurrentProjectPath))
            {
                SaveAsProject();
            } else
            {
                string json = SerializeProject();
                File.WriteAllText(CurrentProjectPath, json);
                HasUnsavedChanges = false;
            }
        }
        private void SaveAsProject()
        {
            string json = SerializeProject();
            
            var d = new SaveFileDialog() { Filter="Project file|*.mveproj", DefaultExt= ".mveproj", FileName=(CurrentProjectPath is not null ? new FileInfo(CurrentProjectPath).Name : ""), RestoreDirectory=true};

            if (d.ShowDialog() ?? false)
            {
                File.WriteAllText(d.FileName, json);
                CurrentProjectPath = d.FileName;
                HasUnsavedChanges = false;
            }
        }

        private static readonly JsonSerializerOptions jsonDeserilazionOptions = new() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, IncludeFields = true };

        private void LoadProject()
        {
            var d = new OpenFileDialog() { Filter= "Project file|*.mveproj", DefaultExt= ".mveproj", Multiselect=false};

            if(d.ShowDialog() ?? false)
            {
                LoadProject(d.FileName);
            }
        }

        public void LoadProject(string filename)
        {
            CurrentProjectPath = filename;

            FileLoaderWrapPanel.Children.Clear();
            NoFilesInFileLoaderLabel.Visibility = Visibility.Visible;

            var json = File.ReadAllText(filename);

            project = JsonSerializer.Deserialize<Project>(json, jsonDeserilazionOptions)!;

            project.files.ForEach(f => { LoadFile(f); });

            HasUnsavedChanges = false;
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

        private void ImportMediaCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ImportMedia();
        }
        private void ImportMedia()
        {
            OpenFileDialog opf = new() { Filter = $"Media files|*{string.Join(";*", SupportedExtensions)}", Multiselect = true };

            if (opf.ShowDialog() ?? false)
            {
                foreach (var f in opf.FileNames)
                {
                    AddFile(f);
                }
            }
        }


        private void NewProjectThisWindowCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if(HasUnsavedChanges)
            {
                MessageBoxResult a = MessageBox.Show("You have unsaved changes in this project. Do you want to save them now before opening a new project?", "New project?", MessageBoxButton.YesNoCancel);
                
                // if Cancel = don't open new project
                if (a == MessageBoxResult.Cancel) return;
                
                if (a == MessageBoxResult.Yes)
                {
                    // if Yes: proceed with saving current
                    SaveProject();
                }
                // if No: proceed without saving

                project = new Project();
                CurrentProjectPath = null!;
                Timeline.Clear();
                FileLoaderWrapPanel.Children.Clear();
                NoFilesInFileLoaderLabel.Visibility = Visibility.Visible;
                HasUnsavedChanges = false;
            }
        }

        private void NewProjectNewWindowCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = $"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}.exe",
                UseShellExecute = true,
            };
            process.StartInfo = startInfo;
            process.Start();
        }

        public void RecoverMedia(string originalFilename)
        {
            string originalPath = new FileInfo(originalFilename).DirectoryName ?? "";
            OpenFileDialog opf = new() { Filter = $"Media files|*{string.Join(";*", SupportedExtensions)}", Multiselect = false, InitialDirectory=originalPath };

            if (opf.ShowDialog() ?? false)
            {
                RemoveFile(originalFilename);

                AddFile(opf.FileName);

                if(MessageBox.Show("File recovered, do you want to save now?", "Recovery completed", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    SaveProject();
                }
            }
        }

        public void IHaveMadeChanges()
        {
            HasUnsavedChanges = true;
        }
    }

#pragma warning disable SYSLIB1054
    static class ImageConvertor
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
    #pragma warning restore SYSLIB1054
}