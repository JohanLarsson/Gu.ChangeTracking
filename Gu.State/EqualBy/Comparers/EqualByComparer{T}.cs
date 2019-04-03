﻿namespace Gu.State
{
    using System.Diagnostics;

    [DebuggerDisplay("Comparer<{typeof(T).PrettyName()}>")]
    internal abstract class EqualByComparer<T> : EqualByComparer
    {
        internal override bool Equals(object x, object y, MemberSettings settings, ReferencePairCollection referencePairs)
        {
            if (TryGetEitherNullEquals(x, y, out var result))
            {
                return result;
            }

            return this.Equals((T)x, (T)y, settings, referencePairs);
        }

        internal abstract bool Equals(T x, T y, MemberSettings settings, ReferencePairCollection referencePairs);
    }
}
