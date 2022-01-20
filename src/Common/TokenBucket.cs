﻿// <copyright file="TokenBucket.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
    /// </summary>
    internal sealed class TokenBucket : ITokenBucket, IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenBucket"/> class.
        /// </summary>
        /// <param name="capacity">The bucket capacity.</param>
        /// <param name="interval">The interval at which tokens are replenished.</param>
        public TokenBucket(long capacity, int interval)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than or equal to 1");
            }

            Capacity = capacity;
            CurrentCount = Capacity;

            Clock = new System.Timers.Timer(interval);
            Clock.Elapsed += (sender, e) => _ = Reset();
            Clock.Start();
        }

        /// <summary>
        ///     Gets the bucket capacity.
        /// </summary>
        public long Capacity { get; private set; }

        private System.Timers.Timer Clock { get; set; }
        private long CurrentCount { get; set; }
        private bool Disposed { get; set; }
        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> WaitForReset { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Sets the bucket capacity to the supplied <paramref name="capacity"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="capacity">The bucket capacity.</param>
        public void SetCapacity(long capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            Capacity = capacity;
        }

        /// <summary>
        ///     Asynchronously retrieves the specified token <paramref name="count"/> from the bucket.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the requested <paramref name="count"/> exceeds the bucket <see cref="Capacity"/>, the request is lowered to
        ///         the capacity of the bucket.
        ///     </para>
        ///     <para>If the bucket has tokens available, but fewer than the requested amount, the available tokens are returned.</para>
        ///     <para>
        ///         If the bucket has no tokens available, execution waits for the bucket to be replenished before servicing the request.
        ///     </para>
        /// </remarks>
        /// <param name="count">The number of tokens to retrieve.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when tokens have been provided.</returns>
        public Task<int> GetAsync(int count, CancellationToken cancellationToken = default)
        {
            return GetInternalAsync(Math.Min(count, (int)Math.Min(int.MaxValue, Capacity)), cancellationToken);
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Clock.Dispose();
                    SyncRoot.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task Reset()
        {
            await SyncRoot.WaitAsync().ConfigureAwait(false);

            try
            {
                CurrentCount = Capacity;

                WaitForReset.SetResult(true);
                WaitForReset = new TaskCompletionSource<bool>();
            }
            finally
            {
                SyncRoot.Release();
            }
        }

        private async Task<int> GetInternalAsync(int count, CancellationToken cancellationToken = default)
        {
            Task waitTask = Task.CompletedTask;

            await SyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // if the bucket has enough tokens to fulfil the request, return them
                // and decrement the bucket
                if (CurrentCount >= count)
                {
                    CurrentCount -= count;
                    return count;
                }

                // if the bucket doesn't have enough tokens to fulfil the request, but
                // has some available, return the available tokens and zero the bucket
                if (CurrentCount > 0)
                {
                    var availableCount = CurrentCount;
                    CurrentCount = 0;
                    return (int)availableCount;
                }

                // if the bucket is empty, make the caller wait until the bucket is replenished
                waitTask = WaitForReset.Task;
            }
            finally
            {
                SyncRoot.Release();
            }

            await waitTask.ConfigureAwait(false);
            return await GetAsync(count, cancellationToken).ConfigureAwait(false);
        }
    }
}