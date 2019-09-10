using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace synchronizationContext
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<Point> _bonusPoints = new List<Point>();


        private readonly Point _startingPoint = new Point(100, 100);
        private Point _curPoint;
        private int _length = 10;
        private int _score = 0;
        private int _direction = 0;
        private Random _rnd = new Random();
        private readonly int _headSize = (int)10;
        private enum Movingdirection
        {
            Upwards = 8,
            Downwards = 2,
            Toleft = 4,
            Toright = 6
        };

        #region Dpattache
        public static string GetMyProperty(DependencyObject p)
        {
            return (string)p.GetValue(MyPropertyProperty);
        }

        public static void SetMyProperty(DependencyObject p, string value)
        {
            p.SetValue(MyPropertyProperty,value);
        }
        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.RegisterAttached("MyProperty", typeof(string), typeof(MainWindow), new PropertyMetadata("false",AllowOnlyString));

        private int _previousDirection=0;

        private static void AllowOnlyString(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox)
            {
                TextBox txtObj = (TextBox)d;
                txtObj.TextChanged += (s, arg) =>
                {
                    TextBox txt = s as TextBox;
                    if (!Regex.IsMatch(txt.Text, "^[a-zA-Z]*$"))
                    {
                        txtObj.BorderBrush = Brushes.Red;
                        MessageBox.Show("Only letter allowed!");
                    }
                };
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            var timer = new DispatcherTimer {Interval = new TimeSpan(100000)};
            timer.Tick += TickTime;
            timer.Start();
            this.KeyDown += KeyDownHandle;
            DrawSnake(_startingPoint);
            _curPoint = _startingPoint;

            DrawBonus();
        }

        private void KeyDownHandle(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    if (_previousDirection != (int) Movingdirection.Upwards)
                        _direction = (int) Movingdirection.Downwards;
                    break;
                case Key.Up:
                    if (_previousDirection != (int) Movingdirection.Downwards)
                        _direction = (int) Movingdirection.Upwards;
                    break;
                case Key.Left:
                    if (_previousDirection != (int) Movingdirection.Toright)
                        _direction = (int) Movingdirection.Toleft;
                    break;
                case Key.Right:
                    if (_previousDirection != (int) Movingdirection.Toleft)
                        _direction = (int) Movingdirection.Toright;
                    break;
            }
        }

        private void TickTime(object sender, EventArgs e)
        {
            switch (_direction)
            {
                case (int) Movingdirection.Downwards:
                    _curPoint.Y += 1;
                    DrawSnake(_curPoint);
                    break;
                case (int) Movingdirection.Upwards:
                    _curPoint.Y -= 1;
                    DrawSnake(_curPoint);
                    break;
                case (int) Movingdirection.Toleft:
                    _curPoint.X -= 1;
                    DrawSnake(_curPoint);
                    break;
                case (int) Movingdirection.Toright:
                    _curPoint.X += 1;
                    DrawSnake(_curPoint);
                    break;
            }
            if ((_curPoint.X < 5) || (_curPoint.X > 420) ||
                (_curPoint.Y < 5) || (_curPoint.Y > 620))
                GameOver();

            var n = 0;
            foreach (var point in _bonusPoints)
            {
                if ((Math.Abs(point.X - _curPoint.X) < _headSize) &&
                    (Math.Abs(point.Y - _curPoint.Y) < _headSize))
                {
                    _length += 10;
                    _score += 10;

                    // In the case of food consumption, erase the food object
                    // from the list of bonuses as well as from the canvas
                    _bonusPoints.RemoveAt(n);
                    paintCanvas.Children.RemoveAt(n);
                    DrawBonus(n);
                    break;

                }
                n++;
            }
        }

        public void ThreadInvoke()
        {
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(2);
            List<Task> tasks = new List<Task>();

            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(lcts);
            CancellationTokenSource cts = new CancellationTokenSource();

            // Use our factory to run a set of tasks. 
            Object lockObj = new Object();
            int outputItem = 0;

            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t = factory.StartNew(() => {
                    for (int i = 0; i < 1000; i++)
                    {
                        lock (lockObj)
                        {
                            Debug.WriteLine("{0} in task t-{1} on thread {2}   ",
                                          i, iteration, Thread.CurrentThread.ManagedThreadId);
                            outputItem++;
                            if (outputItem % 3 == 0)
                                Debug.WriteLine("\n");
                        }
                    }
                }, cts.Token);
                tasks.Add(t);
            }
            // Use it to run a second set of tasks.                       
            for (int tCtr = 0; tCtr <= 4; tCtr++)
            {
                int iteration = tCtr;
                Task t1 = factory.StartNew(() => {
                    for (int outer = 0; outer <= 10; outer++)
                    {
                        for (int i = 0x21; i <= 0x7E; i++)
                        {
                            lock (lockObj)
                            {
                                Debug.WriteLine("'{0}' in task t1-{1} on thread {2}   ",
                                              Convert.ToChar(i), iteration, Thread.CurrentThread.ManagedThreadId);
                                outputItem++;
                                if (outputItem % 3 == 0)
                                    Debug.WriteLine("\n");
                            }
                        }
                    }
                }, cts.Token);
                tasks.Add(t1);
            }

            // Wait for the tasks to complete before displaying a completion message.
            Task.WaitAll(tasks.ToArray());
            cts.Dispose();
            Debug.WriteLine("\n\nSuccessful completion.");
        }

        private void DrawSnake(Point startPoint)
        {
            Ellipse e = new Ellipse() {Fill = Brushes.Red, Width = 10, Height = 10};
            Canvas.SetTop(e, startPoint.Y);
            Canvas.SetLeft(e, startPoint.X);
            paintCanvas.Children.Add(e);

            int childCount = paintCanvas.Children.Count;
            if (childCount > _length)
            {
                paintCanvas.Children.RemoveAt(childCount - _length);
            }
        }

        private void DrawBonus(int x=0)
        {
            Point pp = new Point(_rnd.Next(10, 300), _rnd.Next(10, 300));
            var e = new Ellipse()
            {
                Fill = Brushes.MidnightBlue,
                Height = 10,
                Width = 10
            };
            Canvas.SetTop(e,pp.Y);
            Canvas.SetLeft(e,pp.X);
            paintCanvas.Children.Insert(x,e);
            _bonusPoints.Insert(x,pp);
        }

        private void GameOver()
        {
            MessageBox.Show($@"You Lose! Your score is { _score}", "Game Over", MessageBoxButton.OK, MessageBoxImage.Hand);
            this.Close();
        }
    }


    public class MyThread
    {
        void Foo()
        {
            var j = new TaskFactory();
            j.StartNew(() => { });
            
        }
    }

    // Provides a task scheduler that ensures a maximum concurrency level while 
    // running on top of the thread pool.
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        // The maximum concurrency level allowed by this scheduler. 
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items. 
        private int _delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism. 
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler. 
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another. 
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }

        // Attempts to execute the specified task on the current thread. 
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
                // Try to run the task. 
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        // Gets the maximum concurrency level supported by this scheduler. 
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
    // The following is a portion of the output from a single run of the example:
    //    'T' in task t1-4 on thread 3   'U' in task t1-4 on thread 3   'V' in task t1-4 on thread 3   
    //    'W' in task t1-4 on thread 3   'X' in task t1-4 on thread 3   'Y' in task t1-4 on thread 3   
    //    'Z' in task t1-4 on thread 3   '[' in task t1-4 on thread 3   '\' in task t1-4 on thread 3   
    //    ']' in task t1-4 on thread 3   '^' in task t1-4 on thread 3   '_' in task t1-4 on thread 3   
    //    '`' in task t1-4 on thread 3   'a' in task t1-4 on thread 3   'b' in task t1-4 on thread 3   
    //    'c' in task t1-4 on thread 3   'd' in task t1-4 on thread 3   'e' in task t1-4 on thread 3   
    //    'f' in task t1-4 on thread 3   'g' in task t1-4 on thread 3   'h' in task t1-4 on thread 3   
    //    'i' in task t1-4 on thread 3   'j' in task t1-4 on thread 3   'k' in task t1-4 on thread 3   
    //    'l' in task t1-4 on thread 3   'm' in task t1-4 on thread 3   'n' in task t1-4 on thread 3   
    //    'o' in task t1-4 on thread 3   'p' in task t1-4 on thread 3   ']' in task t1-2 on thread 4   
    //    '^' in task t1-2 on thread 4   '_' in task t1-2 on thread 4   '`' in task t1-2 on thread 4   
    //    'a' in task t1-2 on thread 4   'b' in task t1-2 on thread 4   'c' in task t1-2 on thread 4   
    //    'd' in task t1-2 on thread 4   'e' in task t1-2 on thread 4   'f' in task t1-2 on thread 4   
    //    'g' in task t1-2 on thread 4   'h' in task t1-2 on thread 4   'i' in task t1-2 on thread 4   
    //    'j' in task t1-2 on thread 4   'k' in task t1-2 on thread 4   'l' in task t1-2 on thread 4   
    //    'm' in task t1-2 on thread 4   'n' in task t1-2 on thread 4   'o' in task t1-2 on thread 4   
    //    'p' in task t1-2 on thread 4   'q' in task t1-2 on thread 4   'r' in task t1-2 on thread 4   
    //    's' in task t1-2 on thread 4   't' in task t1-2 on thread 4   'u' in task t1-2 on thread 4   
    //    'v' in task t1-2 on thread 4   'w' in task t1-2 on thread 4   'x' in task t1-2 on thread 4   
    //    'y' in task t1-2 on thread 4   'z' in task t1-2 on thread 4   '{' in task t1-2 on thread 4   
    //    '|' in task t1-2 on thread 4   '}' in task t1-2 on thread 4   '~' in task t1-2 on thread 4   
    //    'q' in task t1-4 on thread 3   'r' in task t1-4 on thread 3   's' in task t1-4 on thread 3   
    //    't' in task t1-4 on thread 3   'u' in task t1-4 on thread 3   'v' in task t1-4 on thread 3   
    //    'w' in task t1-4 on thread 3   'x' in task t1-4 on thread 3   'y' in task t1-4 on thread 3   
    //    'z' in task t1-4 on thread 3   '{' in task t1-4 on thread 3   '|' in task t1-4 on thread 3
    

}
