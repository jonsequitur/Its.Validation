// Copyright (c) Microsoft Corporation. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Its.Validation
{
    [DebuggerStepThrough]
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<T> Do<T>(this IEnumerable<T> items, Action<T> action)
        {
            return items.Select(item =>
            {
                action(item);
                return item;
            });
        }

        internal static void Run<TSource>(this IEnumerable<TSource> source)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                }
            }
        }

        internal static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
        {
            source?.Do(action).Run();
        }
    }
}