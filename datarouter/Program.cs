using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;

namespace datarouter
{
    class Program
    {
        private const string SourceFolderPath = "C:\\Users\\jrz\\Downloads";
        private const string TargetFolderPath = "\\\\FATNAS\\plex\\Library\\Movies\\";
        private static Logger _logger;
        private static int _filesCount;

        static void Main(string[] args)
        {
            ConfigureLogger();
            Handle();
        }

        private static void ConfigureLogger()
        {
            _logger = new LoggerConfiguration()
#if DEBUG
                .WriteTo.Console()
#endif
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        
        private static void Handle()
        {
            var listFiles = RetrieveFiles()
                .Where(x => x.EndsWith(".mp4") || x.EndsWith(".avi") || x.EndsWith(".mkv") || x.EndsWith(".mpeg"));
            if (!MoveFiles(listFiles))
            {
                throw new IOException("Something went wrong trying to move files from source to target !");
            }
            _logger.Information(_filesCount > 1
                ? $"Successfully moved {_filesCount} files"
                : $"Successfully moved {_filesCount} file");
        }

        private static IEnumerable<string> RetrieveFiles()
        {
            if (!Directory.Exists(SourceFolderPath))
            {
                throw new DirectoryNotFoundException($"{SourceFolderPath}: the specified folder does not exist");
            }
            return Directory.GetFiles(SourceFolderPath).ToList();
        }
        private static bool MoveFiles(IEnumerable<string> listFiles)
        {
            if (!Directory.Exists(TargetFolderPath))
            {
                throw new DirectoryNotFoundException($"{TargetFolderPath}: the specified folder does not exist");
            }
            _filesCount = 0;
            try
            {
                foreach (var file in listFiles)
                {
                    var fileName = GetName(file);
                    var fileTargetPath = TargetFolderPath + fileName;
                    if (File.Exists(fileTargetPath))
                    {
                        _logger.Debug($"File: {GetName(file)} already exist in the target folder ({TargetFolderPath})");
                        continue;
                    }
                    _logger.Debug($"Copying: {fileName} to: {fileTargetPath}  ");
                    File.Copy(file, fileTargetPath);
                    if (!File.Exists(fileTargetPath))
                    {
                        throw new IOException($"Something went wrong trying to copy {fileName} from source to target !");
                    }
                    _logger.Debug($"Copy successfull !");
                    File.Delete(file);
                    _filesCount++;
                }
            }
            catch (IOException e)
            {
                _logger.Error(e.Message);
                return false;
            }

            return true;
        }

        private static string GetName(string str)
        {
            return str.Substring(str.LastIndexOf('\\'));
        }
    }
}