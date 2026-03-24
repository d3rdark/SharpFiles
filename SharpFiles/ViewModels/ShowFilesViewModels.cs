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
using Ookii.Dialogs.Wpf;
namespace SharpFiles.ViewModels
{
    public partial class ShowFilesViewModels : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<FileInfo> _files = new ObservableCollection<FileInfo>();

        [ObservableProperty]
        List<string?> _fileExtensions = new List<string?>();

        [ObservableProperty]
        private string? _projectTitle;

        [ObservableProperty]
        private string? _rutaCarpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        [ObservableProperty]
        private string? _rutaDestino;

        [ObservableProperty]
        private string? _selectedExtension;

        [ObservableProperty]
        private string? _fileName;

        [ObservableProperty]
        private int _numFiles;

        [ObservableProperty]
        private FileInfo? _archivoSeleccionado;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string statusMessage = "Listo";

        public ShowFilesViewModels()
        {
            ProjectTitle = "Buscador de archivos";
            GetFileExtensions();

        }

        partial void OnSelectedExtensionChanged(string? value)
        {
            SearchFilesCommand.NotifyCanExecuteChanged();
        }

        partial void OnRutaCarpetaChanged(string? value)
        {
            SearchFilesCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void SearchFolder()
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                InitialDirectory = @"C:",
                Title = "Seleccionar carpeta donde se moveran los archivos",
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

            if (!string.IsNullOrWhiteSpace(RutaCarpeta) && !string.IsNullOrWhiteSpace(SelectedExtension))
            {
                var files = await FileManager.GetAllFilesAsync(RutaCarpeta, SelectedExtension);
                Files.Clear();
                foreach (var file in files)
                {
                    Files.Add(file);
                }
                NumFiles = Files.Count;

            }
        }

        [RelayCommand]
        private async Task MoveFiles()
        {
            // verificar si existen archivos que se pueden mover...

            if (Files == null || !Files.Any())
            {
                MessageBox.Show("No hay archivos para mover. Realiza primero una búsqueda.",
                              "Información",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }


            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Selecciona la carpeta donde se moverán los archivos",
                UseDescriptionForTitle = true,
                Multiselect = false,
                ShowNewFolderButton = true,
                SelectedPath = RutaCarpeta
            };


            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsBusy = true;
                    StatusMessage = $"Moviendo {Files.Count} archivos...";

                    var destinetionPath = dialog.SelectedPath;

                    // mover archivos de forma asincrona
                    await FileManager.MoveAllFiles(destinetionPath, Files, maxConcurrency: 4);

                    StatusMessage = $"✓ {Files.Count} archivos movidos correctamente";

                    MessageBox.Show($"Se movieron {Files.Count} archivos a:\n{destinetionPath}",
                                  "Operación exitosa",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    // Limpiar la lista después de mover
                    Files.Clear();
                    NumFiles = 0;
                }
                catch (UnauthorizedAccessException)
                {

                    StatusMessage = "Error: Sin permisos de acceso";
                    MessageBox.Show("No tienes permisos para acceder a esa carpeta",
                                  "Error de permisos",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                    MessageBox.Show($"Error al mover archivos:\n{ex.Message}",
                                  "Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }

                finally
                {
                   IsBusy = false;
                }
            }

        }


        private void GetFileExtensions()
        {
            foreach (var item in FileManager.GetExtension())
            {
                FileExtensions.Add(item);
            }
        }


        private bool CanShowFiles()
        {
            return (!string.IsNullOrWhiteSpace(SelectedExtension) && !string.IsNullOrWhiteSpace(RutaCarpeta));
        }

        [RelayCommand]
        private void ItemFile(FileInfo fileInfo)
        {
            ArchivoSeleccionado = fileInfo;

            //MessageBox.Show($"Este es el archivo con el nombre {ArchivoSeleccionado.Name}", "informacion de una archivo seleccionado", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            var rutaArchivo = ArchivoSeleccionado.FullName;
            Process.Start("explorer.exe", "/select,\"" + rutaArchivo + "\"");
        }
    }
}
