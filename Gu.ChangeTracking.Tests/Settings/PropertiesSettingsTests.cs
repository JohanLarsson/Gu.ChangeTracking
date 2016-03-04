﻿namespace Gu.ChangeTracking.Tests.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;

    using NUnit.Framework;

    public class PropertiesSettingsTests
    {
        [Test]
        public void IgnoresPropertyCtor()
        {
            var type = typeof(SettingsTypes.ComplexType);
            var nameProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Name));
            var valueProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Value));
            var settings = new PropertiesSettings(new[] { nameProperty }, null, Constants.DefaultPropertyBindingFlags, ReferenceHandling.Throw);
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));
            Assert.AreEqual(false, settings.IsIgnoringProperty(valueProperty));
        }

        [Test]
        public void IgnoresPropertyBuilder()
        {
            var type = typeof(SettingsTypes.ComplexType);
            var nameProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Name));
            var valueProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Value));
            var settings = PropertiesSettings.Build()
                                             .AddIgnoredProperty(nameProperty)
                                             .CreateSettings();
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));
            Assert.AreEqual(false, settings.IsIgnoringProperty(valueProperty));
        }

        [Test]
        public void IgnoresBaseClassPropertyLambda()
        {
            var settings = PropertiesSettings.Build()
                                             .AddIgnoredProperty<SettingsTypes.ComplexType>(x => x.Name)
                                             .CreateSettings();
            var nameProperty = typeof(SettingsTypes.ComplexType).GetProperty(nameof(SettingsTypes.ComplexType.Name));
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));

            nameProperty = typeof(SettingsTypes.Derived).GetProperty(nameof(SettingsTypes.Derived.Name));
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));
            Assert.AreEqual(false, settings.IsIgnoringProperty(typeof(SettingsTypes.Derived).GetProperty(nameof(SettingsTypes.Derived.Value))));
        }

        [Test]
        public void IgnoresInterfaceProperty()
        {
            var settings = PropertiesSettings.Build()
                                 .AddIgnoredProperty<SettingsTypes.IComplexType>(x => x.Name)
                                 .CreateSettings();
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.ComplexType).GetProperty(nameof(SettingsTypes.ComplexType.Name))));
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.IComplexType).GetProperty(nameof(SettingsTypes.ComplexType.Name))));
        }

        [Test]
        public void IgnoresTypeCtor()
        {
            var type = typeof(SettingsTypes.ComplexType);
            var nameProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Name));
            var valueProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Value));
            var settings = new PropertiesSettings(null, new[] { type }, Constants.DefaultPropertyBindingFlags, ReferenceHandling.Throw);
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));
            Assert.AreEqual(true, settings.IsIgnoringProperty(valueProperty));
            Assert.AreEqual(false, settings.IsIgnoringProperty(typeof(SettingsTypes.Immutable).GetProperty(nameof(SettingsTypes.Immutable.Value))));
        }

        [Test]
        public void IgnoresTypeBuilder()
        {
            var type = typeof(SettingsTypes.ComplexType);
            var nameProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Name));
            var valueProperty = type.GetProperty(nameof(SettingsTypes.ComplexType.Value));
            var settings = PropertiesSettings.Build()
                                             .AddImmutableType(type)
                                             .CreateSettings();
            Assert.AreEqual(true, settings.IsIgnoringProperty(nameProperty));
            Assert.AreEqual(true, settings.IsIgnoringProperty(valueProperty));
            Assert.AreEqual(false, settings.IsIgnoringProperty(typeof(SettingsTypes.Immutable).GetProperty(nameof(SettingsTypes.Immutable.Value))));
        }

        [Test]
        public void IgnoresBaseTypeBuilder()
        {
            var settings = PropertiesSettings.Build()
                                             .AddImmutableType<SettingsTypes.ComplexType>()
                                             .CreateSettings();
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.ComplexType).GetProperty(nameof(SettingsTypes.ComplexType.Name))));
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.ComplexType).GetProperty(nameof(SettingsTypes.ComplexType.Value))));
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.Derived).GetProperty(nameof(SettingsTypes.Derived.Name))));
            Assert.AreEqual(true, settings.IsIgnoringProperty(typeof(SettingsTypes.Derived).GetProperty(nameof(SettingsTypes.Derived.Value))));
            Assert.AreEqual(false, settings.IsIgnoringProperty(typeof(SettingsTypes.Derived).GetProperty(nameof(SettingsTypes.Derived.DoubleValue))));
        }

        [Test]
        public void IgnoresNull()
        {
            var settings = PropertiesSettings.GetOrCreate();
            Assert.AreEqual(true, settings.IsIgnoringProperty(null));
        }

        [TestCase(typeof(List<int>))]
        [TestCase(typeof(int[]))]
        [TestCase(typeof(Collection<int>))]
        [TestCase(typeof(ObservableCollection<int>))]
        [TestCase(typeof(Dictionary<int, int>))]
        public void IgnoresCollectionFields(Type type)
        {
            var settings = PropertiesSettings.GetOrCreate();
            var propertyInfos = type.GetProperties(Constants.DefaultFieldBindingFlags);
            if (type != typeof(int[]))
            {
                CollectionAssert.IsNotEmpty(propertyInfos);
            }

            foreach (var propertyInfo in propertyInfos)
            {
                Assert.AreEqual(true, settings.IsIgnoringProperty(propertyInfo));
            }
        }

        [TestCase(BindingFlags.Public, ReferenceHandling.Throw)]
        [TestCase(BindingFlags.Public, ReferenceHandling.Structural)]
        public void Cache(BindingFlags bindingFlags, ReferenceHandling referenceHandling)
        {
            var settings = PropertiesSettings.GetOrCreate(bindingFlags, referenceHandling);
            Assert.AreEqual(bindingFlags, settings.BindingFlags);
            Assert.AreEqual(referenceHandling, settings.ReferenceHandling);
            var second = PropertiesSettings.GetOrCreate(BindingFlags.Public, referenceHandling);
            Assert.AreSame(settings, second);
        }
    }
}