using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Mods.YetAnotherHelper.Modules;
using Celeste.Mods.YetAnotherHelper.Runtime.Spikes;
using Celeste.Simulation;

namespace Celeste.Mods.YetAnotherHelper.Entities.Controllers
{
    [RuntimeTypeAlias("Celeste.Mod.YetAnotherHelper.Entities.SpikeJumpThruController")]
    [CustomEntity("YetAnotherHelper/SpikeJumpThruController = Create")]
    public class SpikeJumpThruController : SceneEntityState
    {
        private readonly SpikeControllerProcess process;

        public static SpikeJumpThruController Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public SpikeJumpThruController(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine,
            data.Bool("persistent", false), offset)
        {
        }

        public SpikeJumpThruController(ISourceEngineContext engine, bool persistent, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, offset)
        {
            var module = YetAnotherHelperModule.For(engine);
            process = module.Spikes;
            module.Session.SpikeJumpThruHooked = persistent;
            Add(new RoomTransitionListener
            {
                OnIn = p => process.TransitionProgress = p, OnOut = p => process.TransitionProgress = p,
                OnInBegin = () => process.TransitionProgress = 0f, OnInEnd = () => process.TransitionProgress = -1f
            });
        }

        public override void Update()
        {
            base.Update();
            if (process.TransitionProgress == -1f && process.CurrentController == null)
            {
                process.CurrentController = this;
                process.NextController = null;
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            process.NextController = this;
            process.EnableSpikeHook();
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            process.CurrentController = process.NextController;
            process.NextController = null;
            process.TransitionProgress = -1f;
            if (process.CurrentController == null) process.DisableSpikeHook();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            process.CurrentController = process.NextController = null;
            process.DisableSpikeHook();
        }
    }
}
