namespace Gu.State
{
    using System;

    internal abstract class CollectionEqualByComparer<TCollection, TItem> : EqualByComparer<TCollection>
    {
        [Obsolete("It has state so Default field can't be used. Addign it here so subclasses do not add it.", error: true)]
        internal static readonly CollectionEqualByComparer<TCollection, TItem> Default;

        private EqualByComparer lazyItemComparer;

        internal static bool TryGetItemError(MemberSettings settings, out Error error)
        {
            var comparer = settings.GetEqualByComparer(typeof(TItem));
            if (comparer is ErrorEqualByComparer errorEqualByComparer)
            {
                error = new TypeErrors(typeof(TCollection), errorEqualByComparer.Error);
                return true;
            }

            if (comparer.TryGetError(settings, out var itemError))
            {
                error = itemError;
                return true;
            }

            error = null;
            return false;
        }

        internal override bool TryGetError(MemberSettings settings, out Error error) => TryGetItemError(settings, out error);

        protected EqualByComparer ItemComparer(MemberSettings settings)
        {
            if (this.lazyItemComparer is null)
            {
                this.lazyItemComparer = typeof(TItem).IsSealed
                    ? settings.GetEqualByComparer(typeof(TItem))
                    : new LazyEqualByComparer<TItem>();
            }

            return this.lazyItemComparer;
        }
    }
}