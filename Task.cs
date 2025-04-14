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
            try 
            {
                if (Directory.Exists(_FilePath))
                {
                    var entries = Directory.GetFileSystemEntries(_FilePath);
                    Console.WriteLine($"Read dir: {Path.GetFileName(_FilePath)}");
                    foreach (var entry in entries)
                        _ioStack.Push(new FileLoadingTask(entry, _ioStack, _processingQueue));
                }
                else
                {
                    byte[] imageData = System.IO.File.ReadAllBytes(_FilePath);
                    Console.WriteLine($"Read file: {Path.GetFileName(_FilePath)}");
                    _processingQueue.Enqueue(new ImageProcessingTask(imageData, _FilePath, _ioStack));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file {_FilePath}: {ex.Message}");
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
                using (var image = Image.Load(_ImageData))
                {
                    var targetWidth = 1280;
                    var targetHeight = 720;
                    
                    var ratio = Math.Min((float)targetWidth / image.Width, (float)targetHeight / image.Height);
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);

                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    Console.WriteLine($"Processed image: {Path.GetFileName(_ImagePath)}");

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
                    string relativePath = Path.GetRelativePath(Program.ImagesPath, _ImagePath);
                    string outputPath = Path.Combine(Program.OutputPath, relativePath);
                    
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