﻿namespace Gu.ChangeTracking.Tests.EqualByTests.PropertyValues
{
    using System.Collections.Generic;

    public class Classes : ClassesTests
    {
        public override bool EqualMethod<T>(T x, T y)
        {
            return EqualBy.PropertyValues(x, y);
        }

        public override bool EqualMethod<T>(T x, T y, ReferenceHandling referenceHandling)
        {
            return EqualBy.PropertyValues(x, y, referenceHandling: referenceHandling);
        }

        public override bool EqualMethod<T>(T x, T y, params string[] excluded)
        {
            return EqualBy.PropertyValues(x, y, excludedProperties: excluded);
        }

        public override bool EqualMethod<T>(T x, T y, string excluded, ReferenceHandling referenceHandling)
        {
            var settings = PropertiesSettings.Create(x, y, Constants.DefaultPropertyBindingFlags, referenceHandling, new[] { excluded });
            return EqualBy.PropertyValues(x, y, settings);
        }

        public static IReadOnlyList<EqualByTestsShared.EqualsData> EqualsSource => EqualByTestsShared.EqualsSource;
    }
}
