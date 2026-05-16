using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PizzaOven
{
    public class PLUSRonnieAnimate
    {
        public static List<PLUSRonnieAnimate> Instances = new();

        public Canvas _overlayCanvas;
        private Window _window;
        private Image _image;

        private double _targetX;
        private double _targetY;
        private double _speed;

        private DispatcherTimer _timer;

        private readonly Dictionary<int, Canvas> _textboxes = new();
        private int _nextTextboxId = 1;

        public double GlideX { get => _targetX; set => _targetX = value; }
        public double GlideY { get => _targetY; set => _targetY = value; }

        public Action StepEvent;

        public PLUSRonnieAnimate()
        {
            EnsureRegistered();
        }

        private bool _registered;

        private void EnsureRegistered()
        {
            if (_registered) return;
            Instances.Add(this);
            _registered = true;
        }

        public static List<PLUSRonnieAnimate> GetActive()
        {
            return Instances.Where(r => r?._image != null && r._overlayCanvas != null).ToList();
        }

        public static void StepAll()
        {
            foreach (var r in GetActive())
                r.StepEvent?.Invoke();
        }

        public void Initialize(Window window, double startX, double startY, double scale = 1)
        {
            _window = window;

            _overlayCanvas = new Canvas
            {
                IsHitTestVisible = false
            };

            if (window.Content is Panel p)
            {
                p.Children.Add(_overlayCanvas);
            }
            else
            {
                var grid = new Grid();
                var old = window.Content as Control;

                window.Content = null;

                if (old != null)
                    grid.Children.Add(old);

                grid.Children.Add(_overlayCanvas);
                window.Content = grid;
            }

            _image = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://PizzaOven/OvenRonnie/normal.png"))),
                RenderTransform = new ScaleTransform(scale, scale),
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
            };

            _overlayCanvas.Children.Add(_image);

            Canvas.SetLeft(_image, startX);
            Canvas.SetTop(_image, startY);
        }

        public void SetExpression(string expression)
        {
            _image.Source =
                new Bitmap(AssetLoader.Open(new Uri($"avares://PizzaOven/OvenRonnie/{expression}.png")));
        }
        public void Destroy()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            if (_image != null && _overlayCanvas != null)
            {
                _overlayCanvas.Children.Remove(_image);
                _image = null;
            }

            if (_overlayCanvas != null)
            {
                if (_overlayCanvas.Parent is Panel parent)
                {
                    parent.Children.Remove(_overlayCanvas);
                }

                _overlayCanvas = null;
            }
        }
        public async Task RunThenDestroy(Func<Task> method)
        {
            if (method != null)
            {
                await method();
            }

           Destroy();
        }
        public int GetX()
        {
            if (_image == null)
                return 0;

            return (int)Canvas.GetLeft(_image);
        }
        public int GetY()
        {
            if (_image == null)
                return 0;

            return (int)Canvas.GetTop(_image);
        }

        public void MoveTo(double x, double y)
        {
            Canvas.SetLeft(_image, x);
            Canvas.SetTop(_image, y);
        }

        public void GlideTo(double x, double y, double speed = 5)
        {
            _targetX = x;
            _targetY = y;
            _speed = speed;

            if (_timer == null)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _timer.Tick += Timer_Tick;
            }

            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            double currentX = Canvas.GetLeft(_image);
            double currentY = Canvas.GetTop(_image);

            double dx = _targetX - currentX;
            double dy = _targetY - currentY;

            if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1)
            {
                Canvas.SetLeft(_image, _targetX);
                Canvas.SetTop(_image, _targetY);
                _timer.Stop();
                return;
            }

            double dist = Math.Sqrt(dx * dx + dy * dy);
            double moveX = dx / dist * _speed;
            double moveY = dy / dist * _speed;

            Canvas.SetLeft(_image, currentX + moveX);
            Canvas.SetTop(_image, currentY + moveY);
        }

        public void ShakeVisual(double magnitude, double seconds)
        {
            var group = _image.RenderTransform as TransformGroup ?? new TransformGroup();

            if (!(_image.RenderTransform is TransformGroup))
                _image.RenderTransform = group;

            var transform = group.Children.OfType<TranslateTransform>().FirstOrDefault();
            if (transform == null)
            {
                transform = new TranslateTransform();
                group.Children.Add(transform);
            }

            var random = new Random();
            var end = DateTime.Now.AddSeconds(seconds);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            timer.Tick += (_, __) =>
            {
                if (DateTime.Now >= end)
                {
                    transform.X = 0;
                    transform.Y = 0;
                    timer.Stop();
                    return;
                }

                transform.X = (random.NextDouble() * 2 - 1) * magnitude;
                transform.Y = (random.NextDouble() * 2 - 1) * magnitude;
            };

            timer.Start();
        }

        public async Task DanceAsync(int times, int delayMs = 200)
        {
            for (int i = 0; i < times; i++)
            {
                SetExpression("happy");
                await Task.Delay(delayMs);

                SetExpression("pointerup");
                await Task.Delay(delayMs);

                SetExpression("happy2");
                await Task.Delay(delayMs);

                SetExpression("pointerup");
                await Task.Delay(delayMs);
            }
        }

        public async Task WaitForClickOnImageAsync()
        {
            if (_image == null || _window == null)
                throw new InvalidOperationException();

            var tcs = new TaskCompletionSource<bool>();

            void Handler(object? sender, PointerPressedEventArgs e)
            {
                var pos = e.GetPosition(_overlayCanvas);

                double x = Canvas.GetLeft(_image);
                double y = Canvas.GetTop(_image);

                double w = _image.Bounds.Width;
                double h = _image.Bounds.Height;

                if (pos.X >= x && pos.X <= x + w &&
                    pos.Y >= y && pos.Y <= y + h)
                {
                    tcs.TrySetResult(true);
                }
            }

            _window.PointerPressed += Handler;
            await tcs.Task;
            _window.PointerPressed -= Handler;
        }
        private Image CreateTextboxImage(string path, double width)
        {
            return new Image
            {
                Source = new Bitmap(AssetLoader.Open(
                    new Uri($"avares://PizzaOven/{path}"))),
                Width = width,
                Stretch = Stretch.Fill
            };
        }

        public static double MeasureTextBlockHeight(string text, double boxWidth = 373, double sidePadding = 35, double fontSize = 21, double topHeight = 19, double bottomHeight = 19)
        {
            var tb = new TextBlock
            {
                Text = text,
                Width = boxWidth - sidePadding * 2,
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                Foreground = Brushes.Black
            };

            tb.Measure(new Size(tb.Width, double.PositiveInfinity));

            double textHeight = tb.DesiredSize.Height;

            int middleCount = (int)Math.Ceiling(textHeight);

            double totalHeight = topHeight + middleCount + bottomHeight;

            return totalHeight;
        }

        public int MakeTextbox(double x, double y, string text)
        {
            if (_overlayCanvas == null)
                return -1;

            const double boxWidth = 373;
            const double sidePadding = 35;

            var container = new Canvas();
            int id = _nextTextboxId++;
            container.Tag = id;

            var textBlock = new TextBlock
            {
                Text = text,
                Width = boxWidth - sidePadding * 2,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 21,
                Foreground = Brushes.Black
            };

            textBlock.Measure(new Size(textBlock.Width, double.PositiveInfinity));
            double textHeight = textBlock.DesiredSize.Height;

            double topHeight = 19;
            double bottomHeight = 19;

            double totalMiddleHeight = textHeight;
            int middleCount = (int)Math.Ceiling(totalMiddleHeight);

            var top = CreateTextboxImage("OvenRonnie/textbox_top.png", boxWidth);

            for (int i = 0; i < middleCount; i++)
            {
                var mid = CreateTextboxImage("OvenRonnie/textbox_middle.png", boxWidth);
                Canvas.SetTop(mid, topHeight + i);
                container.Children.Add(mid);
            }

            Canvas.SetTop(top, 0);
            container.Children.Add(top);

            var bottom = CreateTextboxImage("OvenRonnie/textbox_bottom.png", boxWidth);
            Canvas.SetTop(bottom, topHeight + middleCount);
            container.Children.Add(bottom);

            Canvas.SetLeft(textBlock, sidePadding);
            Canvas.SetTop(textBlock, topHeight);
            container.Children.Add(textBlock);

            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);

            _overlayCanvas.Children.Add(container);
            _textboxes[id] = container;

            return id;
        }
        public void SetVisible(bool? visible = null)
        {
            if (_image == null)
                return;

            if (visible.HasValue)
            {
                _image.IsVisible = visible.Value;
            }
            else
            {
                _image.IsVisible = !_image.IsVisible;
            }
        }
        public Canvas? GetTextbox(int id)
        {
            return _textboxes.TryGetValue(id, out var box) ? box : null;
        }

        public void DestroyTextbox(int id)
        {
            if (!_textboxes.TryGetValue(id, out var box))
                return;

            _overlayCanvas.Children.Remove(box);
            _textboxes.Remove(id);
        }

        public void SetTextboxText(int id, string newText)
        {
            if (!_textboxes.TryGetValue(id, out var container))
                return;

            foreach (var child in container.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.Text = newText;
                    return;
                }
            }
        }
        public async Task MakeSkipButtonAsync(Canvas parent, Action onClickAction)
        {
            var skipButton = new Button
            {
                Content = new Image
                {
                    Source = new Bitmap(AssetLoader.Open(
                        new Uri("avares://PizzaOven/OvenRonnie/skip.png"))),
                    Stretch = Avalonia.Media.Stretch.None
                },

                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),

                Focusable = false
            };

            parent.IsHitTestVisible = true;
            parent.Children.Add(skipButton);

            Canvas.SetRight(skipButton, 10);
            Canvas.SetBottom(skipButton, 30);

            var tcs = new TaskCompletionSource<bool>();

            skipButton.Click += (_, __) =>
            {
                tcs.TrySetResult(true);
            };

            await Task.Delay(1000);

            var finishedTask = await Task.WhenAny(
                tcs.Task,
                this.WaitForClickOnImageAsync()
            );

            if (parent.Children.Contains(skipButton))
                parent.Children.Remove(skipButton);

            if (finishedTask == tcs.Task)
                onClickAction?.Invoke();
        }
    }
}