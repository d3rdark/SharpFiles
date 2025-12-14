using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpFiles.MyHelpers
{
    public static class FileManagerPlus
    {
        // Cache optimizado con expiracion y tamaño limitado

        private static readonly ConcurrentDictionary<string, (List<FileInfo> files, DateTime timestamp)>
            _fileCache = new();

        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

        // Metodo principal para buscar archivos con streaming

        public static async IAsyncEnumerable<StreamingSearchResult> SearchFilesStreamingAsync(
            string rootPath, string fileExtension, IProgress<SearchProgress> progress = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                yield break;

            var cacheKey = $"{rootPath.ToLowerInvariant()}|{fileExtension.ToLowerInvariant()}";
            SearchStats stats = new SearchStats();
            DateTime startTime = DateTime.UtcNow;
            int filesFound = 0;
            long totalSize = 0;
            int directoriesScanned = 0;

            // Verificar cache primero
            if (_fileCache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.UtcNow - cached.timestamp) < _cacheExpiration)
            {
                stats.SearchTime = TimeSpan.Zero;
                stats.TotalFilesFound = cached.files.Count;
                stats.TotalSize = cached.files.Sum(f => f.Length);

                foreach (var file in cached.files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    yield return new StreamingSearchResult
                    {
                        File = file,
                        FileFoundSoFar = ++filesFound,
                        IsComplete = false,
                        Stats = stats,
                    };
                }
                yield return new StreamingSearchResult
                {
                    IsComplete = true,
                    Stats = stats
                };
                yield break;
            }

            var searchPattern = fileExtension.Equals("None", StringComparison.OrdinalIgnoreCase)
                    ? "*.*"
                    : $"*.{fileExtension.TrimStart('.')}";

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                BufferSize = 65536,
                AttributesToSkip = FileAttributes.System | FileAttributes.Temporary,
                MatchType = MatchType.Win32,
                ReturnSpecialDirectories = false
            };

            var foundFiles = new List<FileInfo>();
            bool unauthorizedAccess = false;
            var streamingResults = new List<StreamingSearchResult>();

            try
            {
                var files = Directory.EnumerateFiles(rootPath, searchPattern, options);
                foreach (var filePath in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            filesFound++;
                            totalSize += fileInfo.Length;
                            foundFiles.Add(fileInfo);

                            if (filesFound % 50 == 0 && progress != null)
                            {
                                progress.Report(new SearchProgress
                                {
                                    FilesFound = filesFound,
                                    IsSearching = true,
                                    CurrentFile = fileInfo.FullName
                                });
                            }

                            streamingResults.Add(new StreamingSearchResult
                            {
                                File = fileInfo,
                                FileFoundSoFar = filesFound,
                                IsComplete = false,
                                Stats = null
                            });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                }
                directoriesScanned = Directory.EnumerateDirectories
                    (rootPath, "*", SearchOption.AllDirectories).Count() + 1;

                stats.TotalFilesFound = filesFound;
                stats.TotalSize = totalSize;
                stats.DirectoriesScanned = directoriesScanned;
                stats.SearchTime = DateTime.UtcNow - startTime;

                foundFiles = foundFiles.OrderByDescending(f => f.LastWriteTimeUtc).ToList();

                if (filesFound > 0 && !cancellationToken.IsCancellationRequested)
                {
                    _fileCache[cacheKey] = (foundFiles, DateTime.UtcNow);
                }
            }
            catch (UnauthorizedAccessException)
            {
                unauthorizedAccess = true;
            }

            // Hacer yield return fuera del try-catch
            foreach (var result in streamingResults)
            {
                yield return result;
            }

            if (unauthorizedAccess)
            {
                yield return new StreamingSearchResult
                {
                    IsComplete = true,
                    Stats = new SearchStats { SearchTime = DateTime.UtcNow - startTime }
                };
            }
            else
            {
                yield return new StreamingSearchResult
                {
                    IsComplete = true,
                    Stats = stats
                };
            }
        }

        // Método para obtener estadísticas rápidas
        public static async Task<SearchStats> GetSearchStatsAsync(string rootPath, string fileExtension)
        {
            SearchStats stats = null;

            await foreach (var result in SearchFilesStreamingAsync(rootPath, fileExtension))
            {
                if (result.IsComplete && result.Stats != null)
                {
                    stats = result.Stats;
                    break;
                }
            }

            return stats ?? new SearchStats();
        }

        // Método para limpiar cache
        public static void ClearCache() => _fileCache.Clear();
        public static void ClearCacheForPath(string path)
        {
            var keysToRemove = _fileCache.Keys
                .Where(k => k.StartsWith(path.ToLowerInvariant() + "|"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _fileCache.TryRemove(key, out _);
            }
        }

        // Obtener extensiones
        public static IEnumerable<string> GetExtensions()
        {
            return Enum.GetValues(typeof(ExtensionesArchivos))
                .Cast<ExtensionesArchivos>()
                .Select(e => e.ToString().Replace("_", ""));
        }

        #region // Clases auxiliares
        public class SearchProgress
        {
            public int FilesFound { get; set; }
            public bool IsSearching { get; set; }
            public string? CurrentFile { get; set; }
        }

        enum ExtensionesArchivos
        {
            None,
            jpg, png, gif, bmp, tiff, jpeg, webp,
            txt, pdf, doc, docx,
            xls, xlsx, ods,
            ppt, pptx, odp,
            zip, rar, cbr, cbz,
            mp4, avi, mkv, mov, wmv, flv, webm, mpg, mpeg, _3gp,
            mp3, wav, flac, aac, ogg, m4a,
            sql, db, sqlite, mdf
        }

        #endregion

        #region // Estructuras de datos para resultados y estadísticas
        // Estadisticas de búsqueda
        public class SearchStats
        {
            public int TotalFilesFound { get; set; }

            public TimeSpan SearchTime { get; set; }
            public long TotalSize { get; set; }
            public int DirectoriesScanned { get; set; }
        }


        // Rsultados con streaming
        public class StreamingSearchResult
        {
            public FileInfo? File { get; set; }
            public int FileFoundSoFar { get; set; }
            public bool IsComplete { get; set; }
            public SearchStats? Stats { get; set; }

        }
        #endregion

    }
}
