﻿namespace Gu.ChangeTracking
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    /// <summary>
    /// Tracks if there is a difference between the properties of x and y
    /// </summary>
    public class DirtyTracker<T> : INotifyPropertyChanged, IDisposable, IDirtyTrackerNode
        where T : class, INotifyPropertyChanged
    {
        private readonly T x;
        private readonly T y;
        private readonly HashSet<PropertyInfo> diff = new HashSet<PropertyInfo>();
        private readonly PropertiesDirtyTracker propertyTracker;
        private readonly ItemsDirtyTracker itemTrackers;

        internal DirtyTracker(T x, T y, ReferenceHandling referenceHandling)
            : this(x, y, Constants.DefaultPropertyBindingFlags, referenceHandling)
        {
        }

        internal DirtyTracker(T x, T y, BindingFlags bindingFlags, ReferenceHandling referenceHandling)
                        : this(x, y, new DirtyTrackerSettings(null, bindingFlags, referenceHandling))
        {
        }

        public DirtyTracker(T x, T y, params string[] ignoreProperties)
            : this(x, y, Constants.DefaultPropertyBindingFlags, ignoreProperties)
        {
        }

        public DirtyTracker(T x, T y, BindingFlags bindingFlags, params string[] ignoreProperties)
            : this(x, y, new DirtyTrackerSettings(x?.GetType().GetIgnoreProperties(bindingFlags, ignoreProperties), bindingFlags, ReferenceHandling.Throw))
        {
        }

        public DirtyTracker(T x, T y, DirtyTrackerSettings settings)
            : this(x, y, settings, true)
        {
        }

        protected DirtyTracker(T x, T y, DirtyTrackerSettings settings, bool validateArguments)
        {
            Ensure.NotNull(x, nameof(x));
            Ensure.NotNull(y, nameof(y));
            if (validateArguments)
            {
                Ensure.NotSame(x, y, nameof(x), nameof(y));
            }

            Ensure.SameType(x, y);
            Verify(settings);
            this.x = x;
            this.y = y;
            this.Settings = settings;
            this.propertyTracker = PropertiesDirtyTracker.Create(x, y, this);
            this.itemTrackers = ItemsDirtyTracker.Create(x, y, this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsDirty => this.diff.Count != 0;

        PropertyInfo IDirtyTrackerNode.PropertyInfo => null;

        public IEnumerable<PropertyInfo> Diff => this.diff;

        public DirtyTrackerSettings Settings { get; }

        /// <summary>
        /// Check if <typeparamref name="T"/> can be tracked
        /// </summary>
        public static void Verify(params string[] ignoreProperties)
        {
            Verify(Constants.DefaultPropertyBindingFlags, ignoreProperties);
        }

        /// <summary>
        /// Check if <typeparamref name="T"/> can be tracked
        /// </summary>
        public static void Verify(BindingFlags bindingFlags, params string[] ignoreProperties)
        {
            Verify(new DirtyTrackerSettings(typeof(T).GetIgnoreProperties(bindingFlags, ignoreProperties), bindingFlags, ReferenceHandling.Throw));
        }

        public static void Verify(DirtyTrackerSettings settings)
        {
            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                if (settings.ReferenceHandling == ReferenceHandling.Throw ||
                    (settings.ReferenceHandling != ReferenceHandling.Throw && !typeof(INotifyCollectionChanged).IsAssignableFrom(typeof(T))))
                {
                    throw new NotSupportedException("Not supporting IEnumerable unless ReferenceHandling is specified and the collection is INotifyCollectionChanged");
                }
            }

            foreach (var propertyInfo in typeof(T).GetProperties(settings.BindingFlags))
            {
                if (settings.IsIgnoringProperty(propertyInfo))
                {
                    continue;
                }

                if (!EqualBy.IsEquatable(propertyInfo.PropertyType) && settings.ReferenceHandling == ReferenceHandling.Throw)
                {
                    var message = $"Only equatable properties are supported without specifying {typeof(ReferenceHandling).Name}\r\n" +
                                  $"Property {typeof(T).Name}.{propertyInfo.Name} is not IEquatable<{propertyInfo.PropertyType.Name}>.\r\n" +
                                  "Use the overload DirtyTracker.Track(x, y, ReferenceHandling) if you want to track a graph";
                    throw new NotSupportedException(message);
                }
            }
        }

        public void Dispose()
        {
            this.propertyTracker?.Dispose();
            this.itemTrackers?.Dispose();
        }

        void IDirtyTrackerNode.Update(IDirtyTrackerNode child)
        {
            var before = this.diff.Count;
            if (child.IsDirty)
            {
                this.diff.Add(child.PropertyInfo);
            }
            else
            {
                var itemDirtyTracker = child as ItemDirtyTracker;
                if (itemDirtyTracker != null)
                {
                    if (!this.itemTrackers.IsDirty)
                    {
                        this.diff.Remove(child.PropertyInfo);
                    }
                }
                else
                {
                    this.diff.Remove(child.PropertyInfo);
                }
            }

            this.NotifyChanges(before);
        }

        protected void NotifyChanges(int diffsBefore)
        {
            if (this.diff.Count != diffsBefore)
            {
                if ((diffsBefore == 0 && this.diff.Count > 0) || (diffsBefore > 0 && this.diff.Count == 0))
                {
                    this.OnPropertyChanged(nameof(this.IsDirty));
                }

                this.OnPropertyChanged(nameof(this.Diff));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class PropertiesDirtyTracker : IDisposable
        {
            private static readonly IEnumerable<PropertyInfo> IndexerProperty = new[] { ItemDirtyTracker.IndexerProperty };
            private readonly INotifyPropertyChanged x;
            private readonly INotifyPropertyChanged y;
            private readonly DirtyTracker<T> parent;
            private readonly PropertyCollection propertyTrackers;

            private PropertiesDirtyTracker(INotifyPropertyChanged x, INotifyPropertyChanged y, DirtyTracker<T> parent)
            {
                this.x = x;
                this.y = y;
                this.parent = parent;
                this.Reset();
                x.PropertyChanged += this.OnTrackedPropertyChanged;
                y.PropertyChanged += this.OnTrackedPropertyChanged;
                List<PropertyCollection.PropertyAndDisposable> items = null;
                foreach (var propertyInfo in x.GetType()
                                              .GetProperties(parent.Settings.BindingFlags))
                {
                    if (parent.Settings.IsIgnoringProperty(propertyInfo))
                    {
                        continue;
                    }

                    if (!Copy.IsCopyableType(propertyInfo.PropertyType))
                    {
                        var sv = propertyInfo.GetValue(x);
                        var tv = propertyInfo.GetValue(y);
                        if (items == null)
                        {
                            items = new List<PropertyCollection.PropertyAndDisposable>();
                        }

                        var tracker = this.CreatePropertyTracker((INotifyPropertyChanged)sv, (INotifyPropertyChanged)tv, propertyInfo);
                        items.Add(new PropertyCollection.PropertyAndDisposable(propertyInfo, tracker));
                    }
                }

                if (items != null)
                {
                    this.propertyTrackers = new PropertyCollection(items);
                }
            }

            public static PropertiesDirtyTracker Create(INotifyPropertyChanged x, INotifyPropertyChanged y, DirtyTracker<T> parent)
            {
                if (x == null && y == null)
                {
                    return null;
                }

                if (x == null || y == null)
                {
                    var type = x?.GetType() ?? y.GetType();
                    var propertyInfos = type.GetProperties(parent.Settings.BindingFlags)
                                            .Where(p => !parent.Settings.IsIgnoringProperty(p));
                    parent.diff.UnionWith(propertyInfos);
                    return null;
                }

                return new PropertiesDirtyTracker(x, y, parent);
            }

            public void Dispose()
            {
                this.x.PropertyChanged -= this.OnTrackedPropertyChanged;
                this.y.PropertyChanged -= this.OnTrackedPropertyChanged;
                this.propertyTrackers?.Dispose();
            }

            /// <summary>
            /// Clears the <see cref="Diff"/> and calculates a new.
            /// Notifies if there are changes.
            /// </summary>
            private void Reset()
            {
                var before = this.parent.diff.Count;
                this.parent.diff.IntersectWith(IndexerProperty);
                foreach (var propertyInfo in this.x.GetType().GetProperties(this.parent.Settings.BindingFlags))
                {
                    if (this.parent.Settings.IsIgnoringProperty(propertyInfo))
                    {
                        continue;
                    }

                    var xv = propertyInfo.GetValue(this.x);
                    var yv = propertyInfo.GetValue(this.y);
                    if (this.propertyTrackers != null && this.propertyTrackers.Contains(propertyInfo))
                    {
                        var propertyTracker = this.CreatePropertyTracker((INotifyPropertyChanged)xv, (INotifyPropertyChanged)yv, propertyInfo);
                        this.propertyTrackers[propertyInfo] = propertyTracker;
                    }
                    else if (!Equals(xv, yv))
                    {
                        this.parent.diff.Add(propertyInfo);
                    }
                }

                this.parent.NotifyChanges(before);
            }

            private void OnTrackedPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (string.IsNullOrEmpty(e.PropertyName))
                {
                    this.Reset();
                    return;
                }

                var propertyInfo = sender.GetType().GetProperty(e.PropertyName, this.parent.Settings.BindingFlags);
                if (propertyInfo == null)
                {
                    return;
                }

                if (this.parent.Settings.IsIgnoringProperty(propertyInfo))
                {
                    return;
                }

                var xv = propertyInfo.GetValue(this.x);
                var yv = propertyInfo.GetValue(this.y);
                if (this.propertyTrackers != null && this.propertyTrackers.Contains(propertyInfo))
                {
                    var propertyTracker = this.CreatePropertyTracker((INotifyPropertyChanged)xv, (INotifyPropertyChanged)yv, propertyInfo);
                    this.propertyTrackers[propertyInfo] = propertyTracker;
                    ((IDirtyTrackerNode)this.parent).Update(propertyTracker);
                    return;
                }

                var before = this.parent.diff.Count;
                if (!Equals(xv, yv))
                {
                    this.parent.diff.Add(propertyInfo);
                }
                else
                {
                    this.parent.diff.Remove(propertyInfo);
                }

                this.parent.NotifyChanges(before);
            }

            private IDirtyTrackerNode CreatePropertyTracker(INotifyPropertyChanged x, INotifyPropertyChanged y, PropertyInfo propertyInfo)
            {
                if (x == null && y == null)
                {
                    return new NeverDirtyNode(propertyInfo);
                }

                if (x == null || y == null)
                {
                    return new AlwaysDirtyNode(propertyInfo);
                }

                return new PropertyDirtyTracker(x, y, this.parent, propertyInfo);
            }
        }

        private class ItemsDirtyTracker : IDisposable
        {
            private readonly INotifyCollectionChanged x;
            private readonly INotifyCollectionChanged y;
            private readonly DirtyTracker<T> parent;
            private readonly ItemCollection<IDirtyTrackerNode> itemTrackers;

            private ItemsDirtyTracker(INotifyCollectionChanged x, INotifyCollectionChanged y, DirtyTracker<T> parent)
            {
                this.x = x;
                this.y = y;
                this.parent = parent;
                this.x.CollectionChanged += this.OnTrackedCollectionChanged;
                this.y.CollectionChanged += this.OnTrackedCollectionChanged;
                var xList = (IList)x;
                var yList = (IList)y;
                this.itemTrackers = new ItemCollection<IDirtyTrackerNode>();
                bool anyDirty = false;
                for (int i = 0; i < Math.Max(xList.Count, yList.Count); i++)
                {
                    var itemTracker = this.CreateItemTracker(i);
                    this.itemTrackers[i] = itemTracker;
                    anyDirty |= itemTracker.IsDirty;
                }

                if (anyDirty)
                {
                    parent.diff.Add(ItemDirtyTracker.IndexerProperty);
                }
            }

            public bool IsDirty => this.itemTrackers.Any(it => it?.IsDirty == true);

            internal static ItemsDirtyTracker Create(object x, object y, DirtyTracker<T> parent)
            {
                var xCollectionChanged = x as INotifyCollectionChanged;
                var yCollectionChanged = y as INotifyCollectionChanged;
                if (xCollectionChanged != null && yCollectionChanged != null)
                {
                    return new ItemsDirtyTracker(xCollectionChanged, yCollectionChanged, parent);
                }

                return null;
            }

            public void Dispose()
            {
                this.x.CollectionChanged -= this.OnTrackedCollectionChanged;
                this.y.CollectionChanged -= this.OnTrackedCollectionChanged;
                this.itemTrackers?.Dispose();
            }

            private void OnTrackedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                var before = this.parent.diff.Count;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems.Count; i++)
                        {
                            var itemTracker = this.CreateItemTracker(i);
                            this.itemTrackers[i] = itemTracker;
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        for (int i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems.Count; i++)
                        {
                            this.itemTrackers.RemoveAt(i);
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                        this.itemTrackers.RemoveAt(e.NewStartingIndex);
                        this.itemTrackers.Insert(e.NewStartingIndex, this.CreateItemTracker(e.NewStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        this.itemTrackers[e.OldStartingIndex] = this.CreateItemTracker(e.OldStartingIndex);
                        this.itemTrackers[e.NewStartingIndex] = this.CreateItemTracker(e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        {
                            var xList = (IList)this.x;
                            var yList = (IList)this.y;
                            this.itemTrackers.Clear();
                            for (int i = 0; i < Math.Max(xList.Count, yList.Count); i++)
                            {
                                var itemTracker = this.CreateItemTracker(i);
                                this.itemTrackers[i] = itemTracker;
                            }

                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (this.IsDirty)
                {
                    this.parent.diff.Add(ItemDirtyTracker.IndexerProperty);
                }
                else
                {
                    this.parent.diff.Remove(ItemDirtyTracker.IndexerProperty);
                }

                this.parent.NotifyChanges(before);
            }

            private IDirtyTrackerNode CreateItemTracker(int index)
            {
                var xList = (IList)this.x;
                var xv = xList.Count > index
                             ? xList[index]
                             : null;

                var yList = (IList)this.y;
                var yv = yList.Count > index
                             ? yList[index]
                             : null;
                if (xv == null && yv == null)
                {
                    return new NeverDirtyNode(ItemDirtyTracker.IndexerProperty);
                }
                else if (xv == null || yv == null)
                {
                    return new AlwaysDirtyNode(ItemDirtyTracker.IndexerProperty);
                }
                else
                {
                    if (EqualBy.IsEquatable(xv.GetType()))
                    {
                        return Equals(xv, yv)
                                   ? (IDirtyTrackerNode)new NeverDirtyNode(ItemDirtyTracker.IndexerProperty)
                                   : new AlwaysDirtyNode(ItemDirtyTracker.IndexerProperty);
                    }
                    else
                    {
                        return new ItemDirtyTracker((INotifyPropertyChanged)xv, (INotifyPropertyChanged)yv, this.parent);
                    }
                }
            }
        }
    }
}
