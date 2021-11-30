using System;
using System.Threading;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class Throttle
    {
        private readonly Timer _timer;
        private readonly Action _callback;
        private readonly object _lockObj = new object();
        private readonly TimeSpan _throttlingTime;
        private bool _running;
        private int _throttling;

        private const int THROTTLING_FREE = 0;
        private const int THROTTLING_BUSY = 1;

        public Throttle(Action callback, TimeSpan throttlingTime)
        {
            _callback = callback;
            _throttlingTime = throttlingTime;
            _timer = new Timer(
                callback: s => ((Throttle)s).FireTimer(),
                state: this,
                dueTime: Timeout.Infinite,
                period: Timeout.Infinite);
            _throttling = THROTTLING_FREE;
        }

        private void FireTimer()
        {
            try
            {
                // This module uses locking. Is it necessary? I don't know. The
                // module was rewritten in this fork without locking:
                // https://github.com/mstepura/serilog-sinks-amazonkinesis/blob/master/src/Serilog.Sinks.Amazon.Kinesis/Primitives/Throttle.cs
                lock (_lockObj)
                {
                    if (_running)
                    {
                        _callback();
                    }
                }
            }
            finally
            {
                // ensure throttling flag is reset to THROTTLING_FREE if it was THROTTLING_BUSY, 
                // otherwise it will not run again
                Interlocked.CompareExchange(ref _throttling, THROTTLING_FREE, THROTTLING_BUSY);
            }
        }

        public void Flush()
        {
            FireTimer();
        }

        public bool ThrottleAction()
        {
            if (Interlocked.CompareExchange(ref _throttling, THROTTLING_BUSY, THROTTLING_FREE) == THROTTLING_FREE)
            {
                _running = true;
                return _timer.Change(_throttlingTime, Timeout.InfiniteTimeSpan);
            }
            return false;
        }

        /// <summary>
        /// Stops the timer and synchronously waits for any running callbacks to finish running
        /// </summary>    
        public void Stop()
        {
            lock (_lockObj)
            {
                // FireTimer is *not* running _callback (since we got the lock)
                _timer.Change(
                    dueTime: Timeout.Infinite,
                    period: Timeout.Infinite);

                _running = false;
            }
        }

        public void Dispose()
        {
            FireTimer();
            _timer.Dispose();
        }
    }
}
