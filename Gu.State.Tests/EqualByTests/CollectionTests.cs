﻿namespace Gu.State.Tests.EqualByTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using NUnit.Framework;

    using static EqualByTypes;

    public abstract class CollectionTests
    {
        public abstract bool EqualBy<T>(T source, T target, ReferenceHandling referenceHandling)
            where T : class;

        [TestCase(ReferenceHandling.References)]
        [TestCase(ReferenceHandling.Structural)]
        public void WithEquatableIntCollection(ReferenceHandling referenceHandling)
        {
            var x = new With<EquatableIntCollection>(new EquatableIntCollection(1));
            var y = new With<EquatableIntCollection>(new EquatableIntCollection(1));
            var result = this.EqualBy(x, y, referenceHandling);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void WithIntCollectionReferences()
        {
            var x = new With<IntCollection>(new IntCollection(1));
            var y = new With<IntCollection>(new IntCollection(1));
            var result = this.EqualBy(x, y, ReferenceHandling.References);
            Assert.AreEqual(false, result);
        }

        [TestCase(ReferenceHandling.Throw)]
        [TestCase(ReferenceHandling.References)]
        [TestCase(ReferenceHandling.Structural)]
        public void ListOfIntsToEmpty(ReferenceHandling referenceHandling)
        {
            var x = new List<int> { 1, 2, 3 };
            var y = new List<int>();
            var result = this.EqualBy(x, y, referenceHandling);
            Assert.AreEqual(false, result);

            result = this.EqualBy(y, x, referenceHandling);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void ListOfIntsToLonger()
        {
            var x = new List<int> { 1, 2, 3 };
            var y = new List<int> { 1, 2, 3, 4 };
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void ListOfWithSimples()
        {
            var x = new List<WithSimpleProperties>
            {
                new WithSimpleProperties(
                    1,
                    2,
                    "a",
                    StringSplitOptions.RemoveEmptyEntries),
            };
            var y = new List<WithSimpleProperties>
            {
                new WithSimpleProperties(
                    1,
                    2,
                    "a",
                    StringSplitOptions.RemoveEmptyEntries),
            };
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void ListOfComplex()
        {
            var x = new List<ComplexType> { new ComplexType("b", 2), new ComplexType("c", 3) };
            var y = new List<ComplexType> { new ComplexType("b", 2), new ComplexType("c", 3) };
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);

            result = this.EqualBy(x, y, ReferenceHandling.References);
            Assert.AreEqual(false, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void WithListOfPoints()
        {
            var x = new With<List<Point>>(new List<Point> { new Point(1, 2), new Point(1, 2) });
            var y = new With<List<Point>>(new List<Point> { new Point(1, 2), new Point(1, 2) });

            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);

            result = this.EqualBy(x, y, ReferenceHandling.References);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void ListOfComplexSameItems()
        {
            var x = new List<ComplexType> { new ComplexType("b", 2), new ComplexType("c", 3) };
            var y = new List<ComplexType>(x);
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);

            result = this.EqualBy(x, y, ReferenceHandling.References);
            Assert.AreEqual(true, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void ObservableCollectionOfIntsToEmpty()
        {
            var x = new ObservableCollection<int> { 1, 2, 3 };
            var y = new ObservableCollection<int>();
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void ObservableCollectionOfIntsToLonger()
        {
            var x = new ObservableCollection<int> { 1, 2, 3 };
            var y = new ObservableCollection<int> { 1, 2, 3, 4 };
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void ObservableCollectionOfComplexType()
        {
            var x = new ObservableCollection<ComplexType> { new ComplexType("b", 2), new ComplexType("c", 3) };
            var y = new ObservableCollection<ComplexType> { new ComplexType("b", 2), new ComplexType("c", 3) };
            var result = this.EqualBy(x, y, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);

            result = this.EqualBy(y, x, ReferenceHandling.Structural);
            Assert.AreEqual(true, result);
        }
    }
}