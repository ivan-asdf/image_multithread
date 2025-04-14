using System;
using System.IO;
using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageProcessor
{
    public abstract class Task
    {
        public abstract void Execute();
    }

    public class FileLoadingTask : Task
    {
        private ConcurrentStack<Task> _ioStack;
        private ConcurrentQueue<Task> _processingQueue;
        public string _FilePath { get;  set; }
        public FileLoadingTask(string filePath, ConcurrentStack<Task> ioQueue, ConcurrentQueue<Task> processingQueue)
        {
            _FilePath = filePath;
            _ioStack = ioQueue;
            _processingQueue = processingQueue;
        }

        public override void Execute()
        {
            if (Directory.Exists(_FilePath))
            {
                var entries = Directory.GetFileSystemEntries(_FilePath);
                foreach (var entry in entries)
                    _ioStack.Push(new FileLoadingTask(entry, _ioStack, _processingQueue));
            }
            else
            {
                byte[] imageData = System.IO.File.ReadAllBytes(_FilePath);
                _processingQueue.Enqueue(new ImageProcessingTask(imageData, _FilePath, _ioStack));
            }
        }
    }

    public class ImageProcessingTask : Task
    {
        private ConcurrentStack<Task> _ioStack;

        private byte[] _ImageData;
        private string _ImagePath;

        public ImageProcessingTask(byte[] imageData, string imagePath, ConcurrentStack<Task> ioStack)
        {
            _ImageData = imageData;
            _ImagePath = imagePath;
            _ioStack = ioStack;
        }

        public override void Execute()
        {
            try
            {
                Console.WriteLine($"Processing image: {Path.GetFileName(_ImagePath)}");
                
                using (var image = Image.Load(_ImageData))
                {
                    var targetWidth = 1280;
                    var targetHeight = 720;
                    
                    var ratio = Math.Min((float)targetWidth / image.Width, (float)targetHeight / image.Height);
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);

                    image.Mutate(x => x.Resize(newWidth, newHeight));

                    using (var memoryStream = new MemoryStream())
                    {
                        image.SaveAsJpeg(memoryStream);
                        _ioStack.Push(new ImageSavingTask(memoryStream.ToArray(), _ImagePath));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image {_ImagePath}: {ex.Message}");
            }
        }
    }

    public class ImageSavingTask : Task
    {
        private byte[] _ImageData;
        private string _ImagePath;

        private static readonly string OutputRoot = Path.Combine(Directory.GetCurrentDirectory(), "output");

        public ImageSavingTask(byte[] imageData, string imagePath)
        {
            _ImageData = imageData;
            _ImagePath = imagePath;
        }

        public override void Execute()
        {
            try
            {
                using (var image = Image.Load(_ImageData))
                {
                    string relativePath = Path.GetRelativePath(Program._imagesPath, _ImagePath);
                    string outputPath = Path.Combine(OutputRoot, relativePath);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    
                    image.Save(outputPath);
                    Console.WriteLine($"Saved image: {relativePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image {_ImagePath}: {ex.Message}");
            }
        }
    }
} 