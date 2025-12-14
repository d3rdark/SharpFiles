using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SharpFiles.MyHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SharpFiles.ViewModels
{
    public partial class ShowFilesViewModels : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<FileInfo> _files = new();

        [ObservableProperty]
        private List<string?> _fileExtensions = new();

        [ObservableProperty]
        private string? _projectTitle;

        [ObservableProperty]
        private string? _rutaCarpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        [ObservableProperty]
        private string? _selectedExtension;

        [ObservableProperty]
        private string? _fileName;

        [ObservableProperty]
        private int _numFiles;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _searchStatus = "Listo";

        [ObservableProperty]
        private int _searchProgress;

        [ObservableProperty]
        private string _searchTime;

        [ObservableProperty]
        private string _totalSize;

        [ObservableProperty]
        private FileInfo? _archivoSeleccionado;

        // para cancelar la busqueda

        private CancellationTokenSource _currentSearchCts;

        public ShowFilesViewModels()
        {
            ProjectTitle = "Buscador de archivos";
            GetFileExtensions();
            
        }

        partial void OnSelectedExtensionChanged(string value)
        {
            SearchFilesCommand.NotifyCanExecuteChanged();
            ClearSearchStats();
        }

        partial void OnRutaCarpetaChanged(string value)
        {
            SearchFilesCommand.NotifyCanExecuteChanged();
            ClearSearchStats();
            // Limpiar cache para la ruta anterior si es necesario
            if (!string.IsNullOrWhiteSpace(value))
            {
                FileManagerPlus.ClearCacheForPath(value);
            }
        }

        [RelayCommand]
        private void SearchFolder()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = @"C:",
                Title = "Seleccionar carpeta donde se encuentran los archivos",
                Filter = "Carpetas|*.none",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Selecciona tu carpeta"
            };

            if (dialog.ShowDialog() == true)
            {
                RutaCarpeta = Path.GetDirectoryName(dialog.FileName);
            }
        }

        [RelayCommand(CanExecute = nameof(CanShowFiles))]
        private async Task SearchFiles()
        {
            // Cancelar búsqueda anterior si existe
            CancelCurrentSearch();

            try
            {
                ResetSearchState();
                IsSearching = true;
                SearchStatus = "Buscando archivos...";

                _currentSearchCts = new CancellationTokenSource();

                // Usar streaming para mostrar resultados en tiempo real
                await foreach (var result in FileManagerPlus.SearchFilesStreamingAsync(
                    RutaCarpeta,
                    SelectedExtension,
                    new Progress<FileManagerPlus.SearchProgress>(UpdateSearchProgress),
                    _currentSearchCts.Token))
                {
                    if (result.File != null)
                    {
                        // Agregar a la colección (usar dispatcher si es necesario)
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            Files.Add(result.File);
                        }, System.Windows.Threading.DispatcherPriority.Background);

                        NumFiles = result.FileFoundSoFar;
                    }

                    if (result.IsComplete && result.Stats != null)
                    {
                        UpdateSearchStats(result.Stats);
                        break;
                    }

                    if (_currentSearchCts.Token.IsCancellationRequested)
                        break;
                }

                SearchStatus = _currentSearchCts.Token.IsCancellationRequested
                    ? "Búsqueda cancelada"
                    : "Búsqueda completada";
            }
            catch (OperationCanceledException)
            {
                SearchStatus = "Búsqueda cancelada";
            }
            catch (UnauthorizedAccessException ex)
            {
                SearchStatus = "Error: Acceso denegado";
                MessageBox.Show($"No se tiene acceso a la carpeta: {ex.Message}",
                    "Error de acceso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SearchStatus = "Error en la búsqueda";
                MessageBox.Show($"Error al buscar archivos: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
                _currentSearchCts?.Dispose();
                _currentSearchCts = null;
            }
        }

        [RelayCommand]
        private void CancelSearch()
        {
            CancelCurrentSearch();
            SearchStatus = "Cancelando...";
        }

        [RelayCommand]
        private void ItemFile(FileInfo fileInfo)
        {
            if (fileInfo == null) return;

            try
            {
                ArchivoSeleccionado = fileInfo;
                Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el archivo: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenContainingFolder()
        {
            if (!string.IsNullOrWhiteSpace(RutaCarpeta) && Directory.Exists(RutaCarpeta))
            {
                try
                {
                    Process.Start("explorer.exe", RutaCarpeta);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ClearResults()
        {
            CancelCurrentSearch();
            Files.Clear();
            NumFiles = 0;
            ClearSearchStats();
            SearchStatus = "Listo";
        }

        private void CancelCurrentSearch()
        {
            if (_currentSearchCts != null && !_currentSearchCts.IsCancellationRequested)
            {
                _currentSearchCts.Cancel();
                _currentSearchCts.Token.WaitHandle.WaitOne(1000);
            }
        }

        private void ResetSearchState()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                Files.Clear();
            });
            NumFiles = 0;
            SearchProgress = 0;
            ClearSearchStats();
        }

        private void UpdateSearchProgress(FileManagerPlus.SearchProgress progress)
        {
            SearchProgress = progress.FilesFound;
            SearchStatus = $"Encontrados: {progress.FilesFound} archivos...";
        }

        private void UpdateSearchStats(FileManagerPlus.SearchStats stats)
        {
            SearchTime = $"Tiempo: {stats.SearchTime.TotalSeconds:F2}s";
            TotalSize = $"Tamaño: {FormatFileSize(stats.TotalSize)}";
            NumFiles = stats.TotalFilesFound;
        }

        private void ClearSearchStats()
        {
            SearchTime = string.Empty;
            TotalSize = string.Empty;
            SearchProgress = 0;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void GetFileExtensions()
        {
            FileExtensions.Clear();
            foreach (var item in FileManagerPlus.GetExtensions())
            {
                FileExtensions.Add(item);
            }

            // Seleccionar "None" por defecto si existe
            if (FileExtensions.Contains("None"))
            {
                SelectedExtension = "None";
            }
        }

        private bool CanShowFiles()
        {
            return !string.IsNullOrWhiteSpace(SelectedExtension) &&
                   !string.IsNullOrWhiteSpace(RutaCarpeta) &&
                   Directory.Exists(RutaCarpeta) &&
                   !IsSearching;
        }


    }
}
