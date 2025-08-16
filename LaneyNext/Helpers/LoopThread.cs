using System;
using Windows.System.Threading;
using Windows.UI.Xaml.Media;

// This is LoopThread from Unigram, but Timer changed to ThreadPoolTimer due to Timer instability.

namespace Elorucov.Laney.Helpers
{
    public class LoopThread
    {
        private TimeSpan _interval;
        private TimeSpan _elapsed;

        private ThreadPoolTimer _timerTick;

        private bool _dropInvalidate;

        public LoopThread(TimeSpan interval)
        {
            _interval = interval;
        }

        private void OnTick(ThreadPoolTimer timer)
        {
            _tick?.Invoke(timer, EventArgs.Empty);
        }

        private void OnInvalidate(object sender, object e)
        {
            var args = e as RenderingEventArgs;
            var diff = args.RenderingTime - _elapsed;

            if (_dropInvalidate || diff < _interval)
            {
                return;
            }

            _elapsed = args.RenderingTime;

            _dropInvalidate = true;
            _invalidate?.Invoke(sender, EventArgs.Empty);
            _dropInvalidate = false;
        }

        private event EventHandler _tick;
        public event EventHandler Tick
        {
            add
            {
                if (_tick == null) _timerTick = ThreadPoolTimer.CreatePeriodicTimer(OnTick, _interval);
                _tick += value;
            }
            remove
            {
                _tick -= value;
                if (_tick == null) _timerTick?.Cancel();
            }
        }

        private event EventHandler _invalidate;
        public event EventHandler Invalidate
        {
            add
            {
                if (_invalidate == null) CompositionTarget.Rendering += OnInvalidate;
                _invalidate += value;
            }
            remove
            {
                _invalidate -= value;
                if (_invalidate == null) CompositionTarget.Rendering -= OnInvalidate;
            }
        }

        #region Static

        [ThreadStatic]
        private static LoopThread _stickers = new LoopThread(TimeSpan.FromMilliseconds(1000 / 60));
        public static LoopThread Stickers => _stickers;

        [ThreadStatic]
        private static LoopThread _stickersLowFps = new LoopThread(TimeSpan.FromMilliseconds(1000 / 30));
        public static LoopThread StickersLowFps => _stickersLowFps;

        #endregion
    }
}
