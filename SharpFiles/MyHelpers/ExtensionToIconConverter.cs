using FontAwesome.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SharpFiles.MyHelpers
{
    public class ExtensionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string ext)
            {
                ext = ext.ToLowerInvariant();

                return ext switch
                {
                    // Imagenes
                    "jpg" or "jpeg" or "png" or "gif" or "bmp" or "tiff" or "webp"
                        => FontAwesomeIcon.FileImageOutline,

                    // Textos
                    "txt" => FontAwesomeIcon.FileTextOutline,

                    // PDF
                    "pdf" => FontAwesomeIcon.FilePdfOutline,

                    // Documentos de Word
                    "doc" or "docx" => FontAwesomeIcon.FileWordOutline,

                    // Hojas de cálculo
                    "xls" or "xlsx" or "ods"
                        => FontAwesomeIcon.FileExcelOutline,

                    // Presentaciones
                    "ppt" or "pptx" or "odp"
                        => FontAwesomeIcon.FilePowerpointOutline,

                    // Comprimidos
                    "zip" or "rar" or "cbr" or "cbz"
                        => FontAwesomeIcon.FileArchiveOutline,

                    // Video
                    "mp4" or "m4v" or "avi" or "mkv" or "mov" or "wmv" or "flv" or "webm" or "mpg" or "mpeg" or "3gp"
                        => FontAwesomeIcon.FileVideoOutline,

                    // Audio
                    "mp3" or "wav" or "flac" or "aac" or "ogg" or "m4a"
                        => FontAwesomeIcon.FileAudioOutline,

                    // Bases de datos
                    "sql" or "db" or "sqlite" or "mdf"
                        => FontAwesomeIcon.Database,

                    // Default
                    _ => FontAwesomeIcon.FileOutline
                };
            }
            return FontAwesomeIcon.FileOutline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
