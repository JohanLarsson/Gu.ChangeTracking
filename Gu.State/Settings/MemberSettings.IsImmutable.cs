﻿namespace Gu.State
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;

    public abstract partial class MemberSettings
    {
        private static readonly ConcurrentDictionary<Type, bool> ImmutableCheckedTypes = new ConcurrentDictionary<Type, bool>
        {
            [typeof(Type)] = true,
            [typeof(CultureInfo)] = true,
            [typeof(DateTime)] = true,
            [typeof(DateTimeOffset)] = true,
            [typeof(TimeSpan)] = true,
            [typeof(string)] = true,
            [typeof(double)] = true,
            [typeof(float)] = true,
            [typeof(decimal)] = true,
            [typeof(int)] = true,
            [typeof(uint)] = true,
            [typeof(long)] = true,
            [typeof(ulong)] = true,
            [typeof(short)] = true,
            [typeof(ushort)] = true,
            [typeof(sbyte)] = true,
            [typeof(byte)] = true,
        };

        protected static bool IsImmutableCore(Type type)
        {
            return ImmutableCheckedTypes.GetOrAdd(type, x => CheckIfIsImmutable(x, null));
        }

        private static bool CheckIfIsImmutable(Type type, List<Type> checkedTypes)
        {
            bool result;
            if (ImmutableCheckedTypes.TryGetValue(type, out result))
            {
                return result;
            }

            if (type.IsNullable())
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                var isImmutable = CheckIfIsImmutable(underlyingType, checkedTypes);
                ImmutableCheckedTypes.TryAdd(type, isImmutable);
                return isImmutable;
            }

            if (type.IsEnum)
            {
                ImmutableCheckedTypes.TryAdd(type, true);
                return true;
            }

            if (type.IsImmutableList() ||
                type.IsImmutableArray() ||
                type.IsImmutableHashSet())
            {
                var itemType = type.GetItemType();
                var isImmutable = CheckIfIsImmutable(itemType, checkedTypes);
                ImmutableCheckedTypes.TryAdd(type, isImmutable);
                return isImmutable;
            }

            if (!CanBeImmutable(type))
            {
                ImmutableCheckedTypes.TryAdd(type, false);
                return false;
            }

            var propertyInfos = type.GetProperties(Constants.DefaultFieldBindingFlags);
            foreach (var propertyInfo in propertyInfos)
            {
                if (!propertyInfo.IsGetReadOnly() ||
                    (propertyInfo.GetIndexParameters().Length > 0 && propertyInfo.SetMethod != null))
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }

                if (!CanBeImmutable(propertyInfo.PropertyType))
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }

                if (!ShouldCheckPropertyOrField(type, ref checkedTypes))
                {
                    continue;
                }

                if (!CheckIfIsImmutable(propertyInfo.PropertyType, checkedTypes))
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }
            }

            var fieldInfos = type.GetFields(Constants.DefaultFieldBindingFlags);
            foreach (var fieldInfo in fieldInfos)
            {
                if (fieldInfo.IsEventField())
                {
                    continue;
                }

                if (!fieldInfo.IsInitOnly)
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }

                if (!CanBeImmutable(fieldInfo.FieldType))
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }

                if (!ShouldCheckPropertyOrField(type, ref checkedTypes))
                {
                    continue;
                }

                if (!CheckIfIsImmutable(fieldInfo.FieldType, checkedTypes))
                {
                    ImmutableCheckedTypes.TryAdd(type, false);
                    return false;
                }
            }

            ImmutableCheckedTypes.TryAdd(type, true);
            return true;
        }

        private static bool CanBeImmutable(Type type)
        {
            return type.IsValueType || type.IsSealed;
        }

        private static bool ShouldCheckPropertyOrField(Type type, ref List<Type> checkedTypes)
        {
            if (checkedTypes == null)
            {
                checkedTypes = new List<Type>(1) { type };
            }
            else
            {
                if (checkedTypes.Contains(type))
                {
                    return false;
                }

                checkedTypes.Add(type);
            }

            return true;
        }
    }
}
