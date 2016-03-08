﻿namespace Gu.State
{
    using System;

    internal static partial class TrackerNode
    {
        private sealed class ChildNode<TTracker> : INode<TTracker>
            where TTracker : ITracker
        {
            private readonly object source;
            private readonly Lazy<DisposingMap<IReference, ChildNode<TTracker>>> children = new Lazy<DisposingMap<TKey, ChildNode<TKey, TTracker>>>(() => new DisposingMap<TKey, ChildNode<TKey, TTracker>>());

            internal ChildNode(TKey source, TTracker tracker, INode<TKey, TTracker> parent)
            {
                this.source = source;
                this.Tracker = tracker;
                this.Parent = parent;
                this.Tracker.Changed += this.OnTrackerChanged;
            }

            public IRootNode<TKey, TTracker> Root
            {
                get
                {
                    var rootNode = this.Parent as IRootNode<TKey, TTracker>;
                    if (rootNode != null)
                    {
                        return rootNode;
                    }

                    return this.Parent.Root;
                }
            }

            public TTracker Tracker { get; }

            internal INode<TKey, TTracker> Parent { get; }

            public INode<TKey, TTracker> AddChild(TKey childKey, Func<TTracker> trackerFactory)
            {
                var tracker = this.Root.Cache.GetOrAdd(childKey, trackerFactory);
                var node = new ChildNode<TKey, TTracker>(childKey, tracker, this);
                this.children.Value.SetValue(childKey, node);
                return node;
            }

            public void RemoveChild(TKey key)
            {
                this.children.Value.SetValue(key, null);
            }

            public void Dispose()
            {
                this.Parent.RemoveChild(this.source);
                this.Tracker.Changed += this.OnTrackerChanged;
                if (this.children.IsValueCreated)
                {
                    this.children.Value.Dispose();
                }
            }

            private void OnTrackerChanged(object sender, EventArgs e)
            {
                this.Parent.Tracker.ChildChanged(this.Tracker);
            }
        }
    }
}