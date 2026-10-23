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
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.YetAnotherHelper.Triggers.Lighting
{
    [CustomEntity("YetAnotherHelper/RemoveLightSourcesTrigger = Create")]
    [RuntimeTypeAlias("Celeste.Mod.YetAnotherHelper.Triggers.RemoveLightSourcesTrigger")]
    [SceneTracked]
    public class RemoveLightSourcesTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float offsetTo, offsetFrom;
        private readonly PositionModes positionMode;
        private readonly bool onlyOnce;

        public static RemoveLightSourcesTrigger Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public RemoveLightSourcesTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            offsetTo = data.Float("offsetTo", 1f);
            offsetFrom = data.Float("offsetFrom", 1f);
            positionMode = data.Enum<PositionModes>("positionMode");
            onlyOnce = data.Bool("onlyOnce");
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            YetAnotherHelperModule.For(engine).Session.LightSourceAlpha =
                ((ISourceScalarEnvironment)engine).ScalarArithmetic.Lerp(offsetFrom, offsetTo,
                    GetPositionLerp(player, positionMode));
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (onlyOnce) RemoveSelf();
        }
    }
}
