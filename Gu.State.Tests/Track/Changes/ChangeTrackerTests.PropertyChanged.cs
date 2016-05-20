﻿// ReSharper disable RedundantArgumentDefaultValue
namespace Gu.State.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using NUnit.Framework;

    using static ChangeTrackerTypes;

    public partial class ChangeTrackerTests
    {
        public class PropertyChanged
        {
            [TestCase(ReferenceHandling.Throw)]
            [TestCase(ReferenceHandling.Structural)]
            [TestCase(ReferenceHandling.References)]
            public void WithImmutable(ReferenceHandling referenceHandling)
            {
                var source = new With<Immutable>();

                var propertyChanges = new List<string>();
                var changes = new List<EventArgs>();

                using (var tracker = Track.Changes(source, referenceHandling))
                {
                    tracker.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);
                    tracker.Changed += (_, e) => changes.Add(e);
                    CollectionAssert.IsEmpty(propertyChanges);
                    CollectionAssert.IsEmpty(changes);

                    source.Value = new Immutable();
                    Assert.AreEqual(1, tracker.Changes);
                    CollectionAssert.AreEqual(new[] { "Changes" }, propertyChanges);
                    var node = ChangeTrackerNode.GetOrCreate(source, tracker.Settings, false).Value;
                    var expected = new[] { RootChangeEventArgs.Create(node, new PropertyChangeEventArgs(source.GetType().GetProperty(nameof(source.Value)))) };
                    CollectionAssert.AreEqual(expected, changes, EventArgsComparer.Default);
                }
            }

            [Test]
            public void TracksCollectionProperty()
            {
                var source = new Level { Next = new Level { Levels = new ObservableCollection<Level>(new[] { new Level(), }) } };
                var propertyChanges = new List<string>();
                var changes = new List<EventArgs>();

                using (var tracker = Track.Changes(source, ReferenceHandling.Structural))
                {
                    tracker.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);
                    tracker.Changed += (_, e) => changes.Add(e);
                    CollectionAssert.IsEmpty(propertyChanges);
                    CollectionAssert.IsEmpty(changes);

                    source.Next.Levels[0].Value++;
                    Assert.AreEqual(1, tracker.Changes);
                    CollectionAssert.AreEqual(new[] { "Changes" }, propertyChanges);
                    var node = ChangeTrackerNode.GetOrCreate(source, tracker.Settings, false).Value;
                    var expected = new[] { RootChangeEventArgs.Create(node, new PropertyChangeEventArgs(source.GetType().GetProperty(nameof(source.Next)))) };
                    CollectionAssert.AreEqual(expected, changes, EventArgsComparer.Default);
                }
            }
        }
    }
}
