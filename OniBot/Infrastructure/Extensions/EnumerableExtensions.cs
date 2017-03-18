﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OniBot
{
    public static class EnumerableExtensions
    {
        public static Random _random = new Random();

        public static T Random<T>(this IList<T> items)
        {
            var index = _random.Next(0, items.Count);
            var item = items[index];

            return item;
        }

        public static KeyValuePair<TKey, TValue> Random<TKey, TValue>(this IDictionary<TKey, TValue> items)
        {
            var index = _random.Next(0, items.Count);
            var item = items.ElementAtOrDefault(index);

            return item;
        }
    }
}
