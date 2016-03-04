﻿namespace Gu.ChangeTracking
{
    using System;
    using System.Reflection;

    public static partial class EqualBy
    {
        public static void VerifyCanEqualByPropertyValues<T>(
            BindingFlags bindingFlags = Constants.DefaultPropertyBindingFlags,
            ReferenceHandling referenceHandling = ReferenceHandling.Throw)
        {
            var settings = PropertiesSettings.GetOrCreate(bindingFlags, referenceHandling);
            VerifyCanEqualByPropertyValues<T>(settings);
        }

        public static void VerifyCanEqualByPropertyValues<T>(PropertiesSettings settings)
        {
            var type = typeof(T);
            VerifyCanEqualByPropertyValues(type, settings);
        }

        public static void VerifyCanEqualByPropertyValues(Type type, PropertiesSettings settings)
        {
            Verify.GetPropertyErrors(type, settings).ThrowIfHasErrors(type, settings);
        }

        public static void VerifyCanEqualByFieldValues<T>(
            BindingFlags bindingFlags = Constants.DefaultFieldBindingFlags,
            ReferenceHandling referenceHandling = ReferenceHandling.Throw)
        {
            var settings = FieldsSettings.GetOrCreate(bindingFlags, referenceHandling);
            VerifyCanEqualByFieldValues<T>(settings);
        }

        public static void VerifyCanEqualByFieldValues<T>(FieldsSettings settings)
        {
            var type = typeof(T);
            Verify.GetFieldsErrors(type, settings)
                .ThrowIfHasErrors(type, settings);
        }

        internal static class Verify
        {
            internal static void CanEqualByPropertyValues<T>(T x, T y, PropertiesSettings settings)
            {
                var type = x?.GetType() ?? y?.GetType() ?? typeof(T);
                GetPropertyErrors(type, settings)
                    .ThrowIfHasErrors(type, settings);
            }

            internal static Errors GetPropertyErrors(Type type, PropertiesSettings settings)
            {
                return VerifyCore(settings, type)
                    .OnlyValidProperties(type, settings, IsPropertyValid);
            }

            internal static void CanEqualByFieldValues<T>(T x, T y, FieldsSettings settings)
            {
                var type = x?.GetType() ?? y?.GetType() ?? typeof(T);
                GetFieldsErrors(type, settings)
                    .ThrowIfHasErrors(type, settings);
            }

            internal static Errors GetFieldsErrors(Type type, FieldsSettings settings)
            {
                return VerifyCore(settings, type)
                    .OnlyValidFields(type, settings, IsFieldValid);
            }

            private static Errors VerifyCore(IMemberSettings settings, Type type)
            {
                return ErrorBuilder.Start()
                                   .HasReferenceHandlingIfEnumerable(type, settings)
                                   .OnlySupportedIndexers(type, settings);
            }

            private static bool IsPropertyValid(PropertyInfo property, PropertiesSettings settings)
            {
                if (property.PropertyType.IsEquatable())
                {
                    return true;
                }

                return settings.ReferenceHandling != ReferenceHandling.Throw;
            }

            private static bool IsFieldValid(FieldInfo field, FieldsSettings settings)
            {
                if (field.FieldType.IsEquatable())
                {
                    return true;
                }

                return settings.ReferenceHandling != ReferenceHandling.Throw;
            }
        }
    }
}