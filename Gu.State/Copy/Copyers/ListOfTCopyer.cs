namespace Gu.State
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class ListOfTCopyer : ICopyer
    {
        public static readonly ListOfTCopyer Default = new ListOfTCopyer();

        private ListOfTCopyer()
        {
        }

        public static bool TryGetOrCreate(object x, object y, out ICopyer comparer)
        {
            if (Is.IListsOfT(x, y))
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
            var itemType = source.GetType().GetItemType();
            var copyMethod = this.GetType()
                                        .GetMethod(nameof(Copy), BindingFlags.NonPublic | BindingFlags.Static)
                                        .MakeGenericMethod(itemType, typeof(TSettings));
            copyMethod.Invoke(null, new[] { source, target, syncItem, settings, referencePairs });
        }

        private static void Copy<T, TSettings>(
            IList<T> source,
            IList<T> target,
            Action<object, object, TSettings, ReferencePairCollection> syncItem,
            TSettings settings,
            ReferencePairCollection referencePairs)
            where TSettings : class, IMemberSettings
        {
            if (Is.IsFixedSize(source, target) && source.Count != target.Count)
            {
                throw State.Copy.Throw.CannotCopyFixesSizeCollections(source, target, settings);
            }

            var isImmutable = settings.IsImmutable(
                source.GetType()
                      .GetItemType());
            for (var i = 0; i < source.Count; i++)
            {
                var sv = source[i];
                var tv = target.ElementAtOrDefault(i);
                var copyItem = State.Copy.Item(sv, tv, syncItem, settings, referencePairs, isImmutable);
                if (i < target.Count)
                {
                    target[i] = copyItem;
                }
                else
                {
                    target.Add(copyItem);
                }
            }

            for (var i = target.Count - 1; i >= source.Count; i--)
            {
                target.RemoveAt(i);
            }
        }
    }
}