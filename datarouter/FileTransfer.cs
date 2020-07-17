using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace datarouter
{
    public class FileTransfer : IDisposable
    {
        private readonly FileStream _inputStream;
        private readonly FileStream _outputStream;
        private readonly string _filename;
        private readonly long _inputStreamLength;
        private int _bufferSize;
        private byte[] _buffer;
        private Stopwatch _stopwatch;
        private HashSet<KeyValuePair<long, int>> _sampleRate;

        public long StreamLength => this._inputStreamLength;
        
        public FileTransfer(FileStream inputStream, FileStream outputStream)
        {
            this._inputStream = inputStream;
            this._outputStream = outputStream;
            this._filename = new FileInfo(inputStream.Name).Name;
            this._inputStreamLength = this._inputStream.Length;
            this._stopwatch = new Stopwatch();
            this._sampleRate = new HashSet<KeyValuePair<long, int>>();
            this.Reset();
        }

        public void SetChunkSize(int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chunkSize), 
                    "must be greater than zero"
                );
            }
            
            this._bufferSize = chunkSize;
            this._buffer = new byte[this._bufferSize];
        }

        private void Reset()
        {
            this._bufferSize = -1;
            this._buffer = null;
        }

        private bool IsBufferInitialised()
        {
            return this._buffer != null;
        }

        public string Filename()
        {
            return this._filename;
        }

        public double Progression()
        {
            try
            {
                return (double)this._inputStream.Position / (double)this._inputStreamLength;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public long RemainBytes()
        {
            try
            {
                return this._inputStreamLength - this._inputStream.Position;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public double Speed()
        {
            var end = this._stopwatch.ElapsedMilliseconds;
            var start = end - 1000;
            this._sampleRate.RemoveWhere(x => x.Key < start);

            if (!this._sampleRate.Any())
            {
                return 0f;
            }
            
            return this._sampleRate.Sum(x => x.Value);
        }

        public bool IsCopying()
        {
            try
            {
                return this._inputStream.Position > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool HasFinished()
        {
            try
            {
                return this.RemainBytes() == 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public bool CopyData()
        {
            if (!this._stopwatch.IsRunning)
            {
                this._stopwatch.Start();
            }
            
            if (!this.IsBufferInitialised())
            {
                throw new NullReferenceException(
                    $"buffer must be initialised, use {nameof(this.SetChunkSize)}"
                );
            }

            var remainBytes = this.RemainBytes();

            if (remainBytes == 0)
            {
                this._stopwatch.Stop();;
                this.Reset();
                return false;
            }
            
            var bufferSize = remainBytes < this._bufferSize ? (int)remainBytes: this._bufferSize;

            this._inputStream.Read(this._buffer, 0, bufferSize);
            this._inputStream.Flush();

            this._outputStream.Write(this._buffer, 0, bufferSize);
            this._outputStream.Flush();

            this._sampleRate.Add(new KeyValuePair<long, int>(this._stopwatch.ElapsedMilliseconds, bufferSize));

            return true;
        }

        public void Dispose()
        {
            this._inputStream.Close();
            this._outputStream.Close();
            this._inputStream.Dispose();
            this._outputStream.Dispose();
        }
    }
}