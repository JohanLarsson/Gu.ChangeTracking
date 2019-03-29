﻿namespace Gu.State.Tests.DiffTests.PropertyValues
{
    using System;

    public class Classes : ClassesTests
    {
        public override Diff DiffBy<T>(
            T x,
            T y,
            ReferenceHandling referenceHandling = ReferenceHandling.Structural,
            string excludedMembers = null,
            Type ignoredType = null,
            Type immutableType = null)
        {
            var builder = PropertiesSettings.Build();
            if (excludedMembers != null)
            {
                builder.IgnoreProperty<T>(excludedMembers);
            }

            if (ignoredType != null)
            {
                builder.IgnoreType(ignoredType);
            }

            if (immutableType != null)
            {
                builder.AddImmutableType(immutableType);
            }

            var settings = builder.CreateSettings(referenceHandling);
            return State.DiffBy.PropertyValues(x, y, settings);
        }
    }
}
