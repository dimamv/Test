using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit;

namespace GitParser
{
    /// <summary>
    /// Scrollbar that shows markers.
    /// </summary>
    public class EnhancedScrollBar : IDisposable
    {
        readonly TextEditor editor;
        TrackAdorner trackAdorner;
        public List<int> numberLinesAdd { get; set; }
        public List<int> numberLinesSub { get; set; }

        public EnhancedScrollBar(TextEditor editor)
        {
            if(editor == null)
                throw new ArgumentNullException("editor");
            this.editor = editor;
            numberLinesAdd = new List<int>();
            numberLinesSub = new List<int>();
        }

        public void Dispose()
        {
            if(trackAdorner != null)
            {
                trackAdorner.Remove();
                trackAdorner = null;
            }
        }

        #region Initialize UI
        public ScrollBar vScrollBar { get; private set; }

        //set track for vertical scrolling
        public void SetScrollTrack(ScrollBar vScrollBar)
        {
            this.vScrollBar = vScrollBar;
            Track track = (Track)vScrollBar.Template.FindName("PART_Track", vScrollBar);
            if(track == null)
                return;
            Grid grid = VisualTreeHelper.GetParent(track) as Grid;
            if(grid == null)
                return;
            var layer = AdornerLayer.GetAdornerLayer(grid);
            if(layer == null)
                return;
            trackAdorner = new TrackAdorner(this, grid);
            layer.Add(trackAdorner);
        }
        #endregion

        static Brush GetBrush(Color markerColor)
        {
            var brush = new SolidColorBrush(markerColor);
            brush.Freeze();
            return brush;
        }

        #region TrackAdorner
        sealed class TrackAdorner : Adorner
        {
            readonly TextEditor editor;
            EnhancedScrollBar enhanchedScrollBar;

            public TrackAdorner(EnhancedScrollBar enhanchedScrollBar, Grid trackGrid)
                : base(trackGrid)
            {
                this.enhanchedScrollBar = enhanchedScrollBar;
                this.editor = enhanchedScrollBar.editor;
                editor.TextArea.TextView.VisualLinesChanged += VisualLinesChanged;
            }

            public void Remove()
            {
                editor.TextArea.TextView.VisualLinesChanged -= VisualLinesChanged;

                var layer = AdornerLayer.GetAdornerLayer(AdornedElement);
                if(layer != null)
                    layer.Remove(this);
            }

            void RedrawRequested(object sender, EventArgs e)
            {
                InvalidateVisual();
            }

            void VisualLinesChanged(object sender, EventArgs e)
            {
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var renderSize = this.RenderSize;
                var document = editor.Document;
                var textView = editor.TextArea.TextView;
                var documentHeight = textView.DocumentHeight;

                var startRender = (renderSize.Height - enhanchedScrollBar.vScrollBar.Track.ActualHeight) / 2;
                var heightLabelLine = renderSize.Height / document.LineCount;
                if(heightLabelLine < 1)
                {
                    heightLabelLine = 1;
                }
                var labelWidth = renderSize.Width - 10;
                foreach(var item in enhanchedScrollBar.numberLinesSub)
                {
                    var visualTop = textView.GetVisualTopByDocumentLine(item);
                    var renderPos = (visualTop * enhanchedScrollBar.vScrollBar.Track.ActualHeight)
                         / documentHeight + startRender;
                    var brush = GetBrush(Colors.Red);
                    drawingContext.DrawRectangle(brush, null, new Rect(renderSize.Width - labelWidth,
                        renderPos, labelWidth, heightLabelLine));
                }
                foreach(var item in enhanchedScrollBar.numberLinesAdd)
                {
                    var visualTop = textView.GetVisualTopByDocumentLine(item);
                    var renderPos = (visualTop * enhanchedScrollBar.vScrollBar.Track.ActualHeight)
                         / documentHeight + startRender;
                    var brush = GetBrush(Colors.Green);
                    drawingContext.DrawRectangle(brush, null, new Rect(renderSize.Width - labelWidth,
                        renderPos, labelWidth, heightLabelLine));
                }

            }
        }
        #endregion
    }
}
