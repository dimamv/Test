using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using System.Reflection;
using Squirrel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using GitParser.UserControls;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Threading;

namespace GitParser
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EnhancedScrollBar scrollBar;
        ScrollBar vScrollBar;
        ScrollBar hScrollBar;
        SelectFloatPanel FloatPanel;
        BacgroundUpdater bacgroundUpdater;

        Brush ColorUpdateYes;
        Brush ColorUpdateLoad;
        Brush ColorUpdateApply;
        Brush ColorUpdateNo;
        Brush ColorUpdateError;
        DispatcherTimer dispatcherTimer;

        public MainWindow()
        {
            InitializeComponent();

            scrollBar = new EnhancedScrollBar(textEditor);
            //AvalonEdit init events
            textEditor.Loaded += TextEditor_Loaded;
            textEditor.TextArea.PreviewMouseUp += TextArea_PreviewMouseUp;
            textEditor.SizeChanged += TextEditor_SizeChanged;

            textBlockStatus.Text = "Ready...";
            var assembly = Assembly.GetExecutingAssembly();
            labelVersion.Content = "Version:  " + assembly.GetName().Version.ToString(3);

            bacgroundUpdater = new BacgroundUpdater(@"e:\Projects\GitParser\Releases\", labelUpdate, 2);
            bacgroundUpdater.bgwUpdate.RunWorkerAsync();


            //bacgroundUpdater = new BacgroundUpdater(@"e:\Projects\GitParser\Releases\", labelUpdate, 2);
            ColorUpdateYes = new SolidColorBrush(Colors.LightBlue);
            ColorUpdateLoad = new SolidColorBrush(Colors.LightCyan);
            ColorUpdateApply = new SolidColorBrush(Colors.LightGreen);
            ColorUpdateNo = new SolidColorBrush(Colors.LightGray);
            ColorUpdateError = new SolidColorBrush(Colors.LightPink);



            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 4);
            //dispatcherTimer.Start();

        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            try
            {
                using(var mgr = new UpdateManager(@"e:\Projects\GitParser\Releases\"))
                {
                    var updateInfo = mgr.CheckForUpdate().Result;
                    if(updateInfo.CurrentlyInstalledVersion.Version < updateInfo.FutureReleaseEntry.Version)
                    {
                        labelUpdate.Content = "Y";
                        labelUpdate.Background = ColorUpdateYes;
                        mgr.DownloadReleases(updateInfo.ReleasesToApply).Wait();
                        labelUpdate.Content = "D";
                        labelUpdate.Background = ColorUpdateLoad;
                        mgr.ApplyReleases(updateInfo).Wait();
                        labelUpdate.Content = "A";
                        labelUpdate.Background = ColorUpdateApply;
                    }
                    else
                    {
                        labelUpdate.Content = "?";
                        labelUpdate.Background = ColorUpdateNo;
                    }
                }
            }
            catch(Exception e1)
            {
                labelUpdate.Content = "!";
                labelUpdate.Background = ColorUpdateError;
            }
            dispatcherTimer.Start();


                //            bacgroundUpdater.bgwUpdate.RunWorkerAsync();
        }

        #region AvalonEdit events execute
        private void TextEditor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(FloatPanel != null)
                FloatPanel.Redraw();
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            textEditor.ApplyTemplate();
            var scrollViewer = (ScrollViewer)textEditor.Template.FindName("PART_ScrollViewer", textEditor);
            if(scrollViewer == null)
                return;
            scrollViewer.ApplyTemplate();
            vScrollBar = (ScrollBar)scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer);
            if(vScrollBar == null)
                return;
            scrollBar.SetScrollTrack(vScrollBar);
            vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            hScrollBar = (ScrollBar)scrollViewer.Template.FindName("PART_HorizontalScrollBar", scrollViewer);
            if(hScrollBar == null)
                return;
            hScrollBar.ValueChanged += HScrollBar_ValueChanged;
            //float panel
            FloatPanel = new SelectFloatPanel(textEditor, vScrollBar, hScrollBar);

            textEditor.TextArea.TextView.Layers.Add(FloatPanel);
            FloatPanel.Visibility = Visibility.Hidden;
        }

        private void HScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(FloatPanel != null)
                FloatPanel.HScrolBarValueChanged(e.NewValue);
        }

        private void VScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(FloatPanel != null)
                FloatPanel.VScrolBarValueChanged(e.NewValue);
        }

        private void TextArea_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(FloatPanel != null)
                FloatPanel.Redraw();
        }
        #endregion

        private void buttonOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if(dlg.ShowDialog() ?? false)
            {
                //textBlockStatus.Text = "Go...";
                var currentFileName = dlg.FileName;
                try
                {
                    textEditor.Load(currentFileName);
                    this.Title = "Git parser: " + currentFileName;
                    scrollBar.numberLinesAdd.Clear();
                    scrollBar.numberLinesSub.Clear();
                    textEditor.TextArea.TextView.LineTransformers.Clear();
                }
                catch(Exception e1)
                {
                    //textBlockStatus.Text = "Ready...";
                    MessageBox.Show(e1.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var customHighlighting =
                    HighlightingManager.Instance.HighlightingDefinitions.Where(p => p.Name == "TeX").FirstOrDefault();
                textEditor.SyntaxHighlighting = customHighlighting;

                var lines = textEditor.Text.Split(new Char[] { '\n' });
                for(int i = 0; i < lines.Length; i++)
                {
                    if(String.IsNullOrEmpty(lines[i]))
                    {
                        continue;
                    }

                    if(lines[i][0] == '+')
                    {
                        scrollBar.numberLinesAdd.Add(i);
                    }
                    else
                    {
                        if(lines[i][0] == '-')
                        {
                            scrollBar.numberLinesSub.Add(i);
                        }
                    }
                }
                foreach(var item in scrollBar.numberLinesSub)
                {
                    SetColorLine(item + 1, Brushes.LightPink);
                }
                foreach(var item in scrollBar.numberLinesAdd)
                {
                    SetColorLine(item + 1, Brushes.LightGreen);
                }
                textBlockStatus.Text = "Completed";
            }
            else
            {
                textBlockStatus.Text = "Cancel file load";
            }
        }

        private void SetColorLine(int numberLine, SolidColorBrush brush)
        {
            var currentLine = textEditor.Document.GetLineByNumber(numberLine);
            var _offsetColorizer = new OffsetColorizer(brush);
            _offsetColorizer.StartOffset = currentLine.Offset;
            _offsetColorizer.EndOffset = currentLine.EndOffset;
            textEditor.TextArea.TextView.LineTransformers.Add(_offsetColorizer);
        }
    }
}
