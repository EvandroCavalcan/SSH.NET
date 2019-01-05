﻿using System;
using System.Threading;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Light implementation of SemaphoreSlim.
    /// </summary>
    public class SemaphoreLight
    {
        private readonly object _lock = new object();

        private int _currentCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreLight"/> class, specifying 
        /// the initial number of requests that can be granted concurrently.
        /// </summary>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCount"/> is a negative number.</exception>
        public SemaphoreLight(int initialCount)
        {
            if (initialCount < 0 )
                throw new ArgumentOutOfRangeException("initialCount", "The value cannot be negative.");

            _currentCount = initialCount;
        }

        /// <summary>
        /// Gets the current count of the <see cref="SemaphoreLight"/>.
        /// </summary>
        public int CurrentCount { get { return _currentCount; } }

        /// <summary>
        /// Exits the <see cref="SemaphoreLight"/> once.
        /// </summary>
        /// <returns>The previous count of the <see cref="SemaphoreLight"/>.</returns>
        public int Release()
        {
            return Release(1);
        }

        /// <summary>
        /// Exits the <see cref="SemaphoreLight"/> a specified number of times.
        /// </summary>
        /// <param name="releaseCount">The number of times to exit the semaphore.</param>
        /// <returns>The previous count of the <see cref="SemaphoreLight"/>.</returns>
        public int Release(int releaseCount)
        {
            var oldCount = _currentCount;

            lock (_lock)
            {
                _currentCount += releaseCount;

                Monitor.Pulse(_lock);
            }

            return oldCount;
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreLight"/>.
        /// </summary>
        public void Wait()
        {
            lock (_lock)
            {
                while (_currentCount < 1)
                {
                    Monitor.Wait(_lock);
                }

                _currentCount--;

                Monitor.Pulse(_lock);
            }
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreLight"/>, using a 32-bit signed
        /// integer that specifies the timeout.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or Infinite(-1) to wait indefinitely.</param>
        /// <returns>
        /// <c>true</c> if the current thread successfully entered the <see cref="SemaphoreLight"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Wait(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout", "The timeout must represent a value between -1 and Int32.MaxValue, inclusive.");

            return WaitWithTimeout(millisecondsTimeout);
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreLight"/>, using a <see cref="TimeSpan"/>
        /// to specify the timeout.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns>
        /// <c>true</c> if the current thread successfully entered the <see cref="SemaphoreLight"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Wait(TimeSpan timeout)
        {
            var timeoutInMilliseconds = timeout.TotalMilliseconds;
            if (timeoutInMilliseconds < -1d || timeoutInMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", "The timeout must represent a value between -1 and Int32.MaxValue, inclusive.");

            return WaitWithTimeout((int) timeoutInMilliseconds);
        }

        private bool WaitWithTimeout(int timeoutInMilliseconds)
        {
            lock (_lock)
            {
                if (timeoutInMilliseconds == Session.Infinite)
                {
                    while (_currentCount < 1)
                        Monitor.Wait(_lock);
                }
                else
                {
                    if (_currentCount < 1)
                    {
                        var remainingTimeInMilliseconds = timeoutInMilliseconds;
                        var startTicks = Environment.TickCount;

                        while (_currentCount < 1)
                        {
                            if (!Monitor.Wait(_lock, remainingTimeInMilliseconds))
                            {
                                return false;
                            }

                            var elapsed = Environment.TickCount - startTicks;
                            remainingTimeInMilliseconds -= elapsed;
                            if (remainingTimeInMilliseconds < 0)
                                return false;
                        }
                    }
                }

                _currentCount--;

                Monitor.Pulse(_lock);

                return true;
            }
        }
    }
}
