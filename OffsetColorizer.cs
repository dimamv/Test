using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.Win32;
using System.Net;
using System.ComponentModel;
using System.Reflection;
using Squirrel;
using System.Xml;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace GitParser
{
    public class OffsetColorizer : DocumentColorizingTransformer
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        SolidColorBrush color;

        public OffsetColorizer(SolidColorBrush color)
        {
            this.color = color;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if(line.Length == 0)
                return;

            if(line.Offset < StartOffset || line.Offset > EndOffset)
                return;

            int start = line.Offset > StartOffset ? line.Offset : StartOffset;
            int end = EndOffset > line.EndOffset ? line.EndOffset : EndOffset;

            ChangeLinePart(start, end, element =>
                element.TextRunProperties.SetBackgroundBrush(color));
        }
    }
}
