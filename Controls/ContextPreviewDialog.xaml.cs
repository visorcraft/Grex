using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Grex.Models;

namespace Grex.Controls
{
    public sealed partial class ContextPreviewDialog : UserControl
    {
        private static readonly SolidColorBrush MatchIndicatorBrush = new(Colors.DodgerBlue);
        private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
        private static readonly SolidColorBrush MatchLineBrush = new(Color.FromArgb(40, 30, 144, 255)); // Semi-transparent blue

        public ContextPreviewDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Sets the context preview data to display.
        /// </summary>
        public void SetData(ContextPreviewResult result)
        {
            if (result == null)
                return;

            FileInfoTextBlock.Text = $"{result.FileName} : Line {result.MatchLineNumber}";
            LinesItemsControl.ItemsSource = result.Lines;
        }

        /// <summary>
        /// Sets the file info text (used for localized title).
        /// </summary>
        public void SetFileInfo(string fileName, int lineNumber, string fileInfoFormat)
        {
            if (!string.IsNullOrEmpty(fileInfoFormat))
            {
                FileInfoTextBlock.Text = string.Format(fileInfoFormat, fileName, lineNumber);
            }
            else
            {
                FileInfoTextBlock.Text = $"{fileName} : Line {lineNumber}";
            }
        }

        /// <summary>
        /// Gets the brush for the match indicator based on whether the line is a match.
        /// </summary>
        public static Brush GetMatchIndicatorBrush(bool isMatchLine)
        {
            return isMatchLine ? MatchIndicatorBrush : TransparentBrush;
        }

        /// <summary>
        /// Gets the background brush for a line based on whether it's the match line.
        /// </summary>
        public static Brush GetLineBrush(bool isMatchLine)
        {
            return isMatchLine ? MatchLineBrush : TransparentBrush;
        }
    }
}
