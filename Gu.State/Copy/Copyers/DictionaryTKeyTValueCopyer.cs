﻿namespace Gu.State
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    public class DictionaryTKeyTValueCopyer : ICopyer
    {
        public static readonly DictionaryTKeyTValueCopyer Default = new DictionaryTKeyTValueCopyer();

        private DictionaryTKeyTValueCopyer()
        {
        }

        public static bool TryGetOrCreate(object x, object y, out ICopyer comparer)
        {
            if (Is.IDictionaryOfTKeyTValue(x, y))
            {
                comparer = Default;
                return true;
            }

            comparer = null;
            return false;
        }

        public void Copy<TSettings>(
            object source,
            object target,
            Action<object, object, TSettings, ReferencePairCollection> syncItem,
            TSettings settings,
            ReferencePairCollection referencePairs)
            where TSettings : class, IMemberSettings
        {
            var genericArguments = source.GetType()
                                 .GetInterface("IDictionary`2")
                                 .GetGenericArguments();
            Debug.Assert(genericArguments.Length == 2, "genericArguments.Length != 2");

            var copyMethod = this.GetType()
                                 .GetMethod(nameof(State.Copy), BindingFlags.NonPublic | BindingFlags.Static)
                                 .MakeGenericMethod(genericArguments[0], genericArguments[1], typeof(TSettings));
            copyMethod.Invoke(null, new[] { source, target, syncItem, settings, referencePairs });
        }

        internal static void Copy<TKey, TValue, TSettings>(
            IDictionary<TKey, TValue> source,
            IDictionary<TKey, TValue> target,
            Action<object, object, TSettings, ReferencePairCollection> syncItem,
            TSettings settings,
            ReferencePairCollection referencePairs)
            where TSettings : class, IMemberSettings
        {
            using (var toRemove = ListPool<TKey>.Borrow())
            {
                foreach (var key in target.Keys)
                {
                    if (!source.ContainsKey(key))
                    {
                        toRemove.Value.Add(key);
                    }
                    else
                    {
                        // Synchronize key?
                    }
                }

                foreach (var key in toRemove.Value)
                {
                    target.Remove(key);
                }
            }

            foreach (var key in source.Keys)
            {
                var sv = source[key];
                TValue tv;
                target.TryGetValue(key, out tv);
                var copyItem = State.Copy.Item(sv, tv, syncItem, settings, referencePairs, settings.IsImmutable(sv.GetType()));
                target[key] = copyItem;
            }
        }
    }
}