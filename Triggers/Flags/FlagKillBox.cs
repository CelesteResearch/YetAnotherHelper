using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.YetAnotherHelper.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.YetAnotherHelper.Triggers.Flags
{
    [CustomEntity("YetAnotherHelper/FlagKillBox = Create")]
    [RuntimeTypeAlias("Celeste.Mod.YetAnotherHelper.Triggers.FlagKillBox")]
    public class FlagKillBox : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        public bool KillBoxActive { get; private set; }
        public string ReferenceFlag { get; private set; }

        public static FlagKillBox Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public FlagKillBox(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine, data, offset,
            data.Bool("killBoxActive", false))
        {
        }

        public FlagKillBox(ISourceEngineContext engine, EntityData data, Vector2 offset, bool killBoxActive) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            KillBoxActive = killBoxActive;
            ReferenceFlag = data.Attr("flag");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            KillBoxActive = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(ReferenceFlag);
            if (KillBoxActive)
                player.Die(
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(player.Position - Position));
        }
    }
}
