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
        ObservableCollection<FileInfo> _files = new ObservableCollection<FileInfo>();

        [ObservableProperty]
        List<string?> _fileExtensions = new List<string?>();

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
        private FileInfo? _archivoSeleccionado;

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


        


        //[RelayCommand]
        //private void ShowFolder()
        //{
        //    Process.Start("explorer.exe", RutaCarpeta);
        //}

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
