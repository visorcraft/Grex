using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Grex.Models;
using WinRT.Interop;

namespace Grex.Services
{
    public class ExportService
    {
        /// <summary>
        /// Export content search results to CSV format.
        /// </summary>
        public string ExportContentResultsToCsv(IEnumerable<SearchResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FileName,LineNumber,ColumnNumber,LineContent,FullPath,RelativePath");

            foreach (var result in results)
            {
                sb.AppendLine($"{EscapeCsvField(result.FileName)},{result.LineNumber},{result.ColumnNumber},{EscapeCsvField(result.LineContent)},{EscapeCsvField(result.FullPath)},{EscapeCsvField(result.RelativePath)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export file search results to CSV format.
        /// </summary>
        public string ExportFileResultsToCsv(IEnumerable<FileSearchResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FileName,Size,MatchCount,Extension,Encoding,DateModified,FullPath,RelativePath");

            foreach (var result in results)
            {
                sb.AppendLine($"{EscapeCsvField(result.FileName)},{result.Size},{result.MatchCount},{EscapeCsvField(result.Extension)},{EscapeCsvField(result.Encoding)},{result.DateModified:yyyy-MM-dd HH:mm:ss},{EscapeCsvField(result.FullPath)},{EscapeCsvField(result.RelativePath)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export content search results to JSON format.
        /// </summary>
        public string ExportContentResultsToJson(IEnumerable<SearchResult> results)
        {
            var exportData = results.Select(r => new
            {
                r.FileName,
                r.LineNumber,
                r.ColumnNumber,
                r.LineContent,
                r.FullPath,
                r.RelativePath,
                r.MatchCount
            });

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Export file search results to JSON format.
        /// </summary>
        public string ExportFileResultsToJson(IEnumerable<FileSearchResult> results)
        {
            var exportData = results.Select(r => new
            {
                r.FileName,
                r.Size,
                FormattedSize = r.FormattedSize,
                r.MatchCount,
                r.Extension,
                r.Encoding,
                r.DateModified,
                r.FullPath,
                r.RelativePath
            });

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Export content search results to clipboard format (tab-separated).
        /// </summary>
        public string ExportContentResultsToClipboard(IEnumerable<SearchResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FileName\tLine\tColumn\tContent\tPath");

            foreach (var result in results)
            {
                sb.AppendLine($"{result.FileName}\t{result.LineNumber}\t{result.ColumnNumber}\t{result.TrimmedLineContent}\t{result.RelativePath}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export file search results to clipboard format (tab-separated).
        /// </summary>
        public string ExportFileResultsToClipboard(IEnumerable<FileSearchResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FileName\tSize\tMatches\tExtension\tEncoding\tDateModified\tPath");

            foreach (var result in results)
            {
                sb.AppendLine($"{result.FileName}\t{result.FormattedSize}\t{result.MatchCount}\t{result.Extension}\t{result.Encoding}\t{result.FormattedDateModified}\t{result.RelativePath}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Copy content to clipboard.
        /// </summary>
        public void CopyToClipboard(string content)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        /// <summary>
        /// Save content to a file using file picker.
        /// </summary>
        /// <param name="content">Content to save</param>
        /// <param name="suggestedFileName">Suggested file name</param>
        /// <param name="fileTypeDescription">File type description (e.g., "CSV files")</param>
        /// <param name="fileExtension">File extension (e.g., ".csv")</param>
        /// <param name="hwnd">Window handle for the file picker</param>
        /// <returns>True if saved successfully, false if cancelled or error</returns>
        public async Task<bool> SaveToFileAsync(string content, string suggestedFileName, string fileTypeDescription, string fileExtension, IntPtr hwnd)
        {
            try
            {
                var savePicker = new FileSavePicker();
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.SuggestedFileName = suggestedFileName;
                savePicker.FileTypeChoices.Add(fileTypeDescription, new List<string> { fileExtension });

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, content);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Escape a field for CSV format.
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
