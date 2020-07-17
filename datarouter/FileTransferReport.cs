namespace datarouter
{
    public class FileTransferReport
    {
        public double Size { get; set; }
        public uint Threads { get; set; }
        public uint FileCount { get; set; }
        public double ChunkSize { get; set; }
        public double ElapsedTime { get; set; }
        public double Speed { get; set; }
    }
}