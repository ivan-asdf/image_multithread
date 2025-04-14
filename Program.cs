using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

namespace ImageProcessor
{
    class Program
    {
        public static readonly string _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "images");
        private static volatile bool _isProcessing = true;
        private static int _activeWorkers = 0;

        static void Main()
        {
            Console.WriteLine($"Images path: {_imagesPath}");
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "output"));

            ConcurrentStack<Task> ioStack = new ConcurrentStack<Task>();
            ConcurrentQueue<Task> processsingQueue = new ConcurrentQueue<Task>();
            ioStack.Push(new FileLoadingTask(_imagesPath, ioStack, processsingQueue));


            int ioWorkerCount = Environment.ProcessorCount;
            var ioWorkers = new Thread[ioWorkerCount];

            for (int i = 0; i < ioWorkerCount; i++)
            {
                ioWorkers[i] = new Thread(() =>
                {
                    while (_isProcessing)
                    {
                        if (ioStack.TryPop(out Task task))
                        {
                            Interlocked.Increment(ref _activeWorkers);
                            try
                            {
                                task.Execute();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing {task}: {ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Decrement(ref _activeWorkers);
                            }
                        }
                        else
                        {
                            if (ioStack.IsEmpty && processsingQueue.IsEmpty && _activeWorkers == 0)
                                _isProcessing = false;
                        }
                        Thread.Sleep(100);
                    }
                });
                ioWorkers[i].Start();
            }


            int workerCount = Environment.ProcessorCount;
            var workers = new Thread[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = new Thread(() =>
                {
                    while (_isProcessing)
                    {
                        if (processsingQueue.TryDequeue(out Task task))
                        {
                            Interlocked.Increment(ref _activeWorkers);
                            try
                            {
                                task.Execute();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing {task}: {ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Decrement(ref _activeWorkers);
                            }
                        }
                        else
                        {
                            if (ioStack.IsEmpty && processsingQueue.IsEmpty && _activeWorkers == 0)
                                _isProcessing = false;
                        }
                        Thread.Sleep(100);
                    }
                });
                workers[i].Start();
            }

            foreach (var worker in workers)
                worker.Join();

            foreach (var worker in ioWorkers)
                worker.Join();

            Console.WriteLine("Processing complete");
        }
    }
}
