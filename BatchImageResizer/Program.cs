using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace BatchImageResizer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: BatchImageResizer.exe <image_folder_path> [jpeg_quality]");
                return;
            }

            string folderPath = args[0];
            int jpegQuality = 90; // Default quality
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out jpegQuality) || jpegQuality < 1 || jpegQuality > 100)
                {
                    Console.WriteLine("Invalid jpeg_quality parameter. Using default quality 90.");
                    jpegQuality = 90;
                }
            }

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Directory does not exist: " + folderPath);
                return;
            }

            string[] imageFiles = Directory.GetFiles(folderPath, "*.jpg").Concat(Directory.GetFiles(folderPath, "*.png")).ToArray();
            if (imageFiles.Length == 0)
            {
                Console.WriteLine("No .jpg or .png files found in the directory.");
                return;
            }

            // Unoptimized: Load all images into memory first
            List<Image> loadedImages = new List<Image>();
            List<string> loadedImagePaths = new List<string>();
            foreach (string file in imageFiles)
            {
                try
                {
                    Image img = Image.FromFile(file);
                    loadedImages.Add(img);
                    loadedImagePaths.Add(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {file}: {ex.Message}");
                }
            }

            string convertedFolder = Path.Combine(folderPath, "converted");
            if (!Directory.Exists(convertedFolder))
                Directory.CreateDirectory(convertedFolder);

            Stopwatch totalStopwatch = Stopwatch.StartNew();
            int processedCount = 0;
            for (int i = 0; i < loadedImages.Count; i++)
            {
                Image img = loadedImages[i];
                string filePath = loadedImagePaths[i];
                Stopwatch fileStopwatch = Stopwatch.StartNew();
                int origWidth = img.Width;
                int origHeight = img.Height;
                int newWidth = origWidth / 2;
                int newHeight = origHeight / 2;
                Bitmap resized = new Bitmap(newWidth, newHeight);
                // Set DPI to match original
                resized.SetResolution(img.HorizontalResolution, img.VerticalResolution);
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.DrawImage(img, 0, 0, newWidth, newHeight);
                }
                string fileName = Path.GetFileName(filePath);
                string outPath = Path.Combine(convertedFolder, fileName);
                try
                {
                    // Set JPEG compression quality if saving as .jpg
                    if (fileName.ToLower().EndsWith(".jpg") || fileName.ToLower().EndsWith(".jpeg"))
                    {
                        var jpgEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                        if (jpgEncoder != null)
                        {
                            var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                            encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)jpegQuality);
                            resized.Save(outPath, jpgEncoder, encoderParams);
                        }
                        else
                        {
                            resized.Save(outPath); // fallback
                        }
                    }
                    else
                    {
                        resized.Save(outPath); // PNG or other
                    }
                    fileStopwatch.Stop();
                    Console.WriteLine($"Processed: {fileName} | Original: {origWidth}x{origHeight} | New: {newWidth}x{newHeight} | Time: {fileStopwatch.ElapsedMilliseconds} ms");
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save {fileName}: {ex.Message}");
                }
                finally
                {
                    resized.Dispose();
                }
            }
            totalStopwatch.Stop();
            Console.WriteLine($"Total files processed: {processedCount}");
            Console.WriteLine($"Total elapsed time: {totalStopwatch.ElapsedMilliseconds} ms");

            // Unoptimized: Do not dispose loaded images until all processing is done
            foreach (var img in loadedImages)
            {
                img.Dispose();
            }
        }
    }
}
