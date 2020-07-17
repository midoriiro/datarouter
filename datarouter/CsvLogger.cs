using System;
using System.IO;

namespace datarouter
{
    public class CsvLogger : IDisposable
    {
        private string _filename;
        private StreamWriter _streamWriter;

        public CsvLogger(string filename)
        {
            this._filename = filename;

            if (!File.Exists(this._filename))
            {
                this._streamWriter = File.CreateText(this._filename);
                this.WriteHeaders();
            }
            else
            {
                this._streamWriter = File.AppendText(this._filename);
            }
        }

        public void WriteHeaders()
        {
            var headers = $"size (MB), thread(s), file count, chunk size (MB), elapsed time(s), speed(MB)";
            this._streamWriter.WriteLine(headers);
        }

        public void WriteLine(
            double size,
            uint threads,
            uint fileCount,
            double chunkSize,
            double elapsedTime,
            double speed
        )
        {
            var line = $"{size},{threads},{fileCount},{chunkSize},{elapsedTime},{speed}";
            this._streamWriter.WriteLine(line);
        }

        public void Dispose()
        {
            _streamWriter?.Dispose();
        }
    }
}