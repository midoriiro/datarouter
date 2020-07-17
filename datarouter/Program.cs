using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Serilog;
using Serilog.Core;

namespace datarouter
{
    class Program
    {
        private const string SourceFolderPath = @"D:\encode\processing\Hannibal\S03";
        private const string TargetFolderPath = @"D:\Temp";
        private const string CsvFile = @"statistics.csv";
        private static Logger _logger;
        
        private static readonly Regex RegexFileExtensions = new Regex(@"\.mp4|\.avi|\.mkv|\.mpeg$");

        static void Main(string[] args)
        {
            try
            {
                ConfigureLogger();
                Handle();
            }
            catch (Exception exception)
            {
                _logger.Error(exception.Message, exception);
                Environment.Exit(-1);
            }
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
            var files = RetrieveFiles();
            var report = Dispatcher.Copy(
                files,
                2,
                1000000
            );
            var csvlogger = new CsvLogger(CsvFile);
            csvlogger.WriteLine(
                report.Size,
                report.Threads,
                report.FileCount,
                report.ChunkSize,
                report.ElapsedTime,
                report.Speed
            );
            csvlogger.Dispose();
        }

        private static List<FileTransfer> RetrieveFiles()
        {
            if (!Directory.Exists(SourceFolderPath))
            {
                throw new DirectoryNotFoundException($"{SourceFolderPath}: the specified folder does not exist");
            }

            return Directory.GetFiles(SourceFolderPath)
                .Where(x => RegexFileExtensions.Match(x).Success)
                .Select(x =>
                {
                    var inputStream = File.Open(x, FileMode.Open);
                    var filename = new FileInfo(inputStream.Name).Name;
                    var destinationPath = Path.Combine(TargetFolderPath, filename);
                    var outputStream = File.OpenWrite(destinationPath);
                    return new FileTransfer(inputStream, outputStream);
                })
                .ToList();
        }
    }
}