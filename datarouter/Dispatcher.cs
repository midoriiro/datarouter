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
    public class Dispatcher
    {
        private static void DisplayTransfers(
            FileTransferReport report,
            List<FileTransfer> files
        )
        {
            var output = new StringBuilder();
        
            var elapsedTime = TimeSpan.FromSeconds(report.ElapsedTime);
        
            output.Append($"Elapsed time: {elapsedTime.ToString(@"hh\:mm\:ss")}");
            output.Append(Environment.NewLine);
        
            for (var index = 0; index < files.Count; index++)
            {
                var file = files[index];
                output.Append($"[{file.Filename()}]");
                output.Append(Strings.Space(1));
        
                if (file.HasFinished())
                {
                    output.Append($"Transferred successfully");
                }
                else if (!file.IsCopying())
                {
                    output.Append($"Queued, waiting for copy...");
                }
                else
                {
                    var progression = file.Progression();
                    var percentage = progression * 100;
                    var bytes = new UnitOf.DataStorage().FromBytes(file.RemainBytes()).ToSIUnitGigabytes();
                    var speed = new UnitOf.DataStorage().FromBytes(file.Speed()).ToSIUnitMegabytes();
                    output.Append($"{percentage:F}% / 100%");
                    output.Append(" - ");
                    output.Append($"{bytes:F} GB");
                    output.Append(" - ");
                    output.Append($"{speed:F} MB/s");
                }
        
                if (index < files.Count - 1)
                {
                    output.Append(Environment.NewLine);
                }
                else
                {
                    output.Append("\r");
                }
            }
        
            Console.Clear();
            Console.Out.Write(output);
            Console.Out.Flush();
        }

        public static FileTransferReport Copy(
            List<FileTransfer> files,
            int maxDegreeOfParallelism = 2,
            int chunkSize = 10000000
        )
        {
            var report = new FileTransferReport
            {
                Size = files.Sum(x => x.StreamLength),
                FileCount = (uint)files.Count,
                ChunkSize = (uint) chunkSize,
            };
            
            files.ForEach(x => x.SetChunkSize(chunkSize));

            var concurrentFiles = new ConcurrentDictionary<string, FileTransfer>(
                files.ToDictionary(
                    x => x.Filename(),
                    x => x
                )
            );
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Parallel.ForEach(
                concurrentFiles, 
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                }, 
                (concurrentFile, state) =>
                {
                    var (filename, file) = concurrentFile;

                    while (true)
                    {
                        report.ElapsedTime = stopWatch.Elapsed.TotalSeconds;

                        var hasData = file.CopyData();

                        DisplayTransfers(report, files);

                        if (hasData)
                        {
                            continue;
                        }

                        var isRemoved = concurrentFiles.Remove(filename, out _);

                        if (!isRemoved)
                        {
                            throw new InvalidOperationException($"Cannot remove {filename}");
                        }

                        file.Dispose();

                        break;
                    }
                });

            stopWatch.Stop();

            var elapsedTime = stopWatch.Elapsed.TotalSeconds;
            report.ElapsedTime = elapsedTime;
            report.ChunkSize = new UnitOf.DataStorage().FromBytes(report.ChunkSize).ToSIUnitMegabytes();
            report.Speed = new UnitOf.DataStorage().FromBytes(report.Size / report.ElapsedTime).ToSIUnitMegabytes();
            report.Size = new UnitOf.DataStorage().FromBytes(report.Size).ToSIUnitMegabytes();

            files.ForEach(x => x.Dispose());

            return report;
        }
    }
}