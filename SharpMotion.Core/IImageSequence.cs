using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Video.FFMPEG;

namespace SharpMotion.Core
{
    public interface IImageSequence
    {
        IEnumerable<Bitmap> Images { get; }
    }

    public static class ImageSequenceExtensions
    {
        /// <summary>
        /// Write the images to disk as a video.
        /// </summary>
        public static string WriteTempVideo(this IImageSequence sequence, int width, int height, int frameRate, VideoCodec videoCodec)
        {
            var tempFileName = Path.GetTempFileName();
            // If we don't force the original filename to .avi when the file is created, the AForge library does
            // something stupid internally (don't know what) and writes the file incorrectly.
            tempFileName = Path.Combine(Path.GetDirectoryName(tempFileName) ?? string.Empty,
                Path.GetFileNameWithoutExtension(tempFileName) + ".avi");

            using (VideoFileWriter writer = new VideoFileWriter())
            {
                writer.Open(tempFileName, width, height, frameRate, videoCodec, 4000000);
                foreach (var bitmap in sequence.Images)
                {
                    Bitmap clone = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
                    using (Graphics gr = Graphics.FromImage(clone))
                    {
                        gr.DrawImage(bitmap, new Rectangle(0, 0, clone.Width, clone.Height));
                    } 
                    writer.WriteVideoFrame(clone);
                }
                writer.Close();
            }
            return tempFileName;
        }
    }
}