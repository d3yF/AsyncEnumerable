﻿using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class AsyncEnumeratorTests
    {
        [Test]
        public async Task RaceConditionOnEndOfEnumeration()
        {
            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                await Task.Run(async () =>
                {
                    await yield.ReturnAsync(1);
                });
            },
            oneTimeUse: false);

            var moveResult1 = await enumerator.MoveNextAsync();
            var moveResult2 = await enumerator.MoveNextAsync();
            var moveResult3 = await enumerator.MoveNextAsync();

            Assert.IsTrue(moveResult1);
            Assert.IsFalse(moveResult2);
            Assert.IsFalse(moveResult3);
        }

        [Test]
        public void CancelEnumeration()
        {
            var cts = new CancellationTokenSource();

            var enumerator = new AsyncEnumerator<int>(yield =>
            {
                cts.Cancel();
                yield.CancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            },
            oneTimeUse: false);

            Assert.ThrowsAsync<OperationCanceledException>(() => enumerator.MoveNextAsync(cts.Token));
        }

        [Test]
        public async Task DisposeAfterPartialEnumeration()
        {
            var testDisposable = new TestDisposable();
            var enumerator = new AsyncEnumerator<int>(async yield =>
                {
                    using (testDisposable)
                    {
                        await yield.ReturnAsync(1);
                        await yield.ReturnAsync(2);
                        await yield.ReturnAsync(3);
                    }
                },
            oneTimeUse: false);

            await enumerator.MoveNextAsync();
            enumerator.Dispose();

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        private class TestDisposable : IDisposable
        {
            public bool HasDisposed { get; private set; }
            public void Dispose()
            {
                HasDisposed = true;
            }
        }
    }
}
