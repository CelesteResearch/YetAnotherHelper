using System;
using System.Collections.Generic;
using Celeste.Core.Components;

namespace Celeste.Mods.YetAnotherHelper.Presentation.Lighting
{
    internal sealed class LightSourceFrameCache<T> where T : SourceComponentState
    {
        private LightSourceFrame<T> available;

        internal LightSourceFrame<T> Rent(List<SourceComponentState> components)
        {
            var frame = available;
            if (frame == null)
                frame = new LightSourceFrame<T>();
            else
            {
                available = frame.Next;
                frame.Next = null;
            }

            frame.Capture(components);
            return frame;
        }

        internal void Return(LightSourceFrame<T> frame)
        {
            Array.Clear(frame.Snapshot, 0, frame.SnapshotCount);
            Array.Clear(frame.Affected, 0, frame.AffectedCount);
            frame.SnapshotCount = frame.AffectedCount = 0;
            frame.Next = available;
            available = frame;
        }
    }
}
