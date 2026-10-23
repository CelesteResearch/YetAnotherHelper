using System;
using System.Collections.Generic;
using Celeste.Core.Components;

namespace Celeste.Mods.YetAnotherHelper.Presentation.Lighting
{
    internal sealed class LightSourceFrame<T> where T : SourceComponentState
    {
        internal struct Entry
        {
            internal T Component;
            internal float Alpha;
        }

        internal LightSourceFrame<T> Next;
        internal SourceComponentState[] Snapshot = Array.Empty<SourceComponentState>();
        internal Entry[] Affected = Array.Empty<Entry>();
        internal int SnapshotCount, AffectedCount;

        internal void Capture(List<SourceComponentState> components)
        {
            int count = components.Count;
            if (count > Snapshot.Length)
            {
                int capacity = Math.Max(count, Math.Max(4, Snapshot.Length * 2));
                Snapshot = new SourceComponentState[capacity];
                Affected = new Entry[capacity];
            }

            components.CopyTo(Snapshot);
            SnapshotCount = count;
        }

        internal void Add(T component, float alpha)
        {
            Affected[AffectedCount++] = new Entry { Component = component, Alpha = alpha };
        }
    }
}
