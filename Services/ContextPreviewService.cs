using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grex.Models;

namespace Grex.Services
{
    /// <summary>
    /// Service for loading context lines around a search match.
    /// </summary>
    public class ContextPreviewService
    {
        private readonly IEncodingDetectionService _encodingDetectionService;

        public ContextPreviewService(IEncodingDetectionService encodingDetectionService)
        {
            _encodingDetectionService = encodingDetectionService;
        }

        /// <summary>
        /// Loads context lines around a specific line in a file.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        /// <param name="lineNumber">The 1-based line number of the match.</param>
        /// <param name="linesBefore">Number of lines to load before the match (default 5).</param>
        /// <param name="linesAfter">Number of lines to load after the match (default 5).</param>
        /// <returns>A ContextPreviewResult containing the context lines.</returns>
        private const long MaxContextReadBytes = 1024 * 1024; // 1 MB

        public async Task<ContextPreviewResult> GetContextAsync(
            string filePath,
            int lineNumber,
            int linesBefore = 5,
            int linesAfter = 5,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (lineNumber < 1)
                throw new ArgumentException("Line number must be at least 1.", nameof(lineNumber));

            var result = new ContextPreviewResult
            {
                FileName = Path.GetFileName(filePath),
                FullPath = filePath,
                MatchLineNumber = lineNumber
            };

            // Calculate the range of lines to read
            int startLine = Math.Max(1, lineNumber - linesBefore);
            int endLine = lineNumber + linesAfter;

            var lines = new List<ContextLine>();
            int matchIndexInList = -1;

            try
            {
                // Detect encoding
                var encodingResult = _encodingDetectionService.DetectFileEncoding(filePath);

                using var reader = new StreamReader(filePath, encodingResult.Encoding, detectEncodingFromByteOrderMarks: false);
                int currentLine = 0;
                long bytesRead = 0;

                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentLine++;
                    bytesRead += encodingResult.Encoding.GetByteCount(line) + 1;

                    // Bound how far we read into huge files before the match line.
                    if (bytesRead > MaxContextReadBytes && currentLine < startLine)
                    {
                        break;
                    }

                    // Skip lines before our range
                    if (currentLine < startLine)
                        continue;

                    // Stop if we're past our range
                    if (currentLine > endLine)
                        break;

                    bool isMatch = currentLine == lineNumber;
                    if (isMatch)
                    {
                        matchIndexInList = lines.Count;
                    }

                    lines.Add(new ContextLine
                    {
                        LineNumber = currentLine,
                        Content = line,
                        IsMatchLine = isMatch
                    });
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to read file: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied to file: {ex.Message}", ex);
            }

            result.Lines = lines;
            result.MatchLineIndex = matchIndexInList >= 0 ? matchIndexInList : 0;

            return result;
        }

        /// <summary>
        /// Checks if a path is a WSL path.
        /// </summary>
        public static bool IsWslPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.StartsWith("\\\\wsl$", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("\\\\wsl.localhost", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("\\mnt\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.Length > 0 && path[0] == '/')
            {
                return path.Length < 2 || path[1] != ':';
            }

            return false;
        }
    }
}
