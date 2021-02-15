#if NETSTANDARD2_0 || NETSTANDARD2_1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jaytwo.ThrottledTasks
{
    internal static class IEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                yield return await Task.FromResult(item);
            }
        }

        public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IEnumerable<TIn> enumerable, Func<TIn, Task<TOut>> action)
        {
            foreach (var item in enumerable)
            {
                yield return await action(item);
            }
        }

        public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IAsyncEnumerable<TIn> enumerable, Func<TIn, TOut> action)
        {
            await foreach (var item in enumerable)
            {
                yield return action(item);
            }
        }

        public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IAsyncEnumerable<TIn> enumerable, Func<TIn, Task<TOut>> action)
        {
            await foreach (var item in enumerable)
            {
                yield return await action(item);
            }
        }
    }
}

#endif
