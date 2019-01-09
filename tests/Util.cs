using nng.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

//[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace nng.Tests
{
    static class Traits
    {
        public const string PlatformName = "platform";
        public const string PlatformWindows = "windows";
        public const string PlatformPosix = "posix";
    }
    static class Util
    {
        public const int DefaultTimeoutMs = 1000;

        public static string UrlIpc() => "ipc://" + Guid.NewGuid().ToString();
        public static string UrlInproc() => "inproc://" + Guid.NewGuid().ToString();
        public static string UrlTcp() => "tcp://localhost:" + rng.Next(1000, 60000);
        public static string UrlWs() => "ws://localhost:" + rng.Next(1000, 60000);

        public static byte[] TopicRandom() => Guid.NewGuid().ToByteArray();

        public static Task WaitReady() => Task.Delay(100);
        public static Task WaitShort() => Task.Delay(25);

        public static async Task AssertWait(int timeoutMs, params Task[] tasks)
        {
            var timeout = Task.Delay(timeoutMs);
            Assert.NotEqual(timeout, await Task.WhenAny(timeout, Task.WhenAll(tasks)));
        }

        public static async Task CancelAndWait(CancellationTokenSource cts, int timeoutMs, params Task[] tasks)
        {
            cts.Cancel();
            try
            {
                await Task.WhenAny(Task.Delay(timeoutMs), Task.WhenAll(tasks));
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    // ok
                }
                else
                {
                    throw ex;
                }
            }
        }

        public static bool BytesEqual(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.SequenceEqual(rhs);
        }

        public static bool Equals(IMessage lhs, IMessage rhs)
        {
            return BytesEqual(lhs.AsSpan(), rhs.AsSpan()) && BytesEqual(lhs.Header.AsSpan(), rhs.Header.AsSpan());
        }

        public static void AssertGetSetOpts(IHasOpts options, string name)
        {
            Assert.Equal(0, options.GetOpt(name, out bool isSet));
            Assert.Equal(0, options.SetOpt(name, !isSet));
            options.GetOpt(name, out bool isSetNow);
            Assert.NotEqual(isSet, isSetNow);
        }

        public static void AssertGetSetOpts(IHasOpts options, string name, Func<int, int> newValueFunc)
        {
            Assert.Equal(0, options.GetOpt(name, out int data));
            var newData = newValueFunc(data);
            Assert.Equal(0, options.SetOpt(name, newData));
            options.GetOpt(name, out int nextData);
            Assert.Equal(newData, nextData);
        }

        public static void AssertGetSetOpts(IHasOpts options, string name, Func<nng_duration, nng_duration> newDataFunc)
        {
            Assert.Equal(0, options.GetOpt(name, out nng_duration data));
            var newData = newDataFunc(data);
            Assert.Equal(0, options.SetOpt(name, newData));
            options.GetOpt(name, out nng_duration nextData);
            Assert.Equal(newData, nextData);
        }

        public static void AssertGetSetOpts(IHasOpts options, string name, Func<UIntPtr, UIntPtr> newDataFunc)
        {
            Assert.Equal(0, options.GetOpt(name, out UIntPtr size));
            var newSize = newDataFunc(size);
            Assert.Equal(0, options.SetOpt(name, newSize));
            options.GetOpt(name, out UIntPtr nextSize);
            Assert.Equal(newSize, nextSize);
        }

        public static readonly Random rng = new Random();
    }

    static class FactoryExt
    {
        public static IMessage CreateTopicMessage(this IMessageFactory<IMessage> socket, byte[] topic)
        {
            var res = socket.CreateMessage();
            res.Append(topic);
            return res;
        }
    }

    class BadTransportsClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { string.Empty };
            //yield return new object[] { null };
            yield return new object[] { "badproto://localhost" };
            yield return new object[] { "inproc//missingcolon:9000" };
            yield return new object[] { "inproc:/missingslash:9000" };
            yield return new object[] { "tcp://badport:70000" };
            yield return new object[] { "tcp://192.168.1.300:10000" }; // Bad IP
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class TransportsClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Util.UrlIpc() };
            yield return new object[] { Util.UrlInproc() };
            yield return new object[] { Util.UrlTcp() };
            yield return new object[] { Util.UrlWs() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Some protocols seem to have issues with websocket transport (ws)
    /// </summary>
    class TransportsNoWsClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Util.UrlIpc() };
            yield return new object[] { Util.UrlInproc() };
            yield return new object[] { Util.UrlTcp() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class TransportsNoTcpWsClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Util.UrlIpc() };
            yield return new object[] { Util.UrlInproc() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}