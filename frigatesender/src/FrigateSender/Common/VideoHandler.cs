using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Serilog;

namespace FrigateSender.Common
{
    public class VideoHandler
    {
        private readonly ILogger _logger;

        public VideoHandler( ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Split video file to smaller files specified as paramater
        /// </summary>
        /// <param name="videoPath">Path of file to split.</param>
        /// <param name="maxSizeMb">Max size per file.</param>
        /// <returns></returns>
        public async Task<List<string>> SplitVideoToSize(string videoPath, int maxSizeMb, CancellationToken ct)
        {
            _logger.Information("Splitting video file.");
            var result = new List<string>();

            if (File.Exists(videoPath))
            {
                bool probeSuccess = false;

                try
                {
                    var fileInfo = new FileInfo(videoPath); 
                    var mediaInfo = await FFProbe.AnalyseAsync(videoPath);

                    var fileSizeMb = fileInfo.Length.ConvertBytesToMegabytes();
                    var segments = (int) Math.Ceiling(fileSizeMb / maxSizeMb);
                    var lengthPerSegment = mediaInfo.Duration.TotalSeconds / segments;

                    var targetPath = Path.GetDirectoryName(videoPath);
                    var targetFileName = Path.GetFileNameWithoutExtension(videoPath);
                    var targetSuffix = fileInfo.Extension;

                    var targetSplitFileName = Path.Join(targetPath, targetFileName + "_split_{{i}}" + targetSuffix);

                    _logger.Information("Split calculation: Segments: {0}, LengthPerSegment: {1}, Total Size: {2}, Estimated segment size: {3}.",
                    segments, lengthPerSegment, fileSizeMb, Math.Round((fileSizeMb/segments), 2));

                    probeSuccess = true;

                    foreach (var segment in Enumerable.Range(1, segments))
                    {
                        var name = targetSplitFileName.Replace("{{i}}", segment.ToString());
                        result.Add(name);
                        await FFMpegArguments
                            .FromFileInput(videoPath)
                            .OutputToFile(name, false, options => options
                                .Seek(TimeSpan.FromSeconds(lengthPerSegment * (segment - 1)))
                                .WithDuration(TimeSpan.FromSeconds(lengthPerSegment))
                                .OverwriteExisting()
                                .WithFastStart()
                            )
                            .ProcessAsynchronously();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to split video file in VideoHandler, probeSuccess: {0}", probeSuccess);
                }
            }
            else
            {
                _logger.Information("Split file failed, file did not exist.");
            }

            return result;
        }
    }
}
