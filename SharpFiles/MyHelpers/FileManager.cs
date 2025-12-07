using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpFiles.MyHelpers
{
    public static class FileManager
    {

        public static IEnumerable<FileInfo> GetAllFiles(string rutaPadre, string tipoExtension)
        {
            IEnumerable<FileInfo> files;

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            if (tipoExtension.Contains("None"))
            {
                files = Directory.GetFiles(rutaPadre, "*", options)
                        .Select(arch => new FileInfo(arch)).OrderByDescending(arch => arch.LastWriteTime);

            }
            else
            {
                files = Directory.GetFiles(rutaPadre, $"*.{tipoExtension}", options)
                        .Select(arch => new FileInfo(arch)).OrderByDescending(arch => arch.LastWriteTime);

            }
            return files.Where(f => f != null && !string.IsNullOrWhiteSpace(f.Name));
        }

        public static async Task<IEnumerable<FileInfo>> GetAllFilesAsync(string rutaPadre, string tipoExtension)
        {
            return await Task.Run(() => GetAllFiles(rutaPadre, tipoExtension));
        }

        public static IEnumerable<string> GetExtension()
        {
            var listaExtension = Enum.GetValues(typeof(ExtensionesArchivos))
                .Cast<ExtensionesArchivos>().Select(e => e.ToString().Replace("_", ""));

            return listaExtension;
        }

    }
    enum ExtensionesArchivos
    {
        None,
        //Imagenes
        jpg, png, gif, bmp, tiff, jpeg, webp,
        //Textos
        txt, pdf, doc, docx,
        //Hojas de calculo
        xls, xlsx, ods,
        // Presentaciones
        ppt, pptx, odp,
        // Archivos comprimidos
        zip, rar, cbr, cbz,
        // Archivos de video
        mp4, avi, mkv, mov, wmv, flv, webm, mpg, mpeg, _3gp,
        // Archivos de audio
        mp3, wav, flac, aac, ogg, m4a,
        // Archivos de base de datos
        sql, db, sqlite, mdf
    }
}
