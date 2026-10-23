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
using System.Collections;
using System.Linq;
using Celeste.Core.Randomness;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Hazards.Lightning;
using Celeste.Core.Display;
using Celeste.Simulation.Presentation.Backdrops.Weather.Rain;

namespace Celeste.Mods.YetAnotherHelper.Triggers.Lightning
{
    [CustomEntity("YetAnotherHelper/LightningStrikeTrigger = Create")]
    [RuntimeTypeAlias("Celeste.Mod.YetAnotherHelper.Triggers.LightningStrikeTrigger")]
    public class LightningStrikeTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float PlayerOffset, VerticalOffset, StrikeHeight, Delay;
        private readonly int Seed;
        private readonly bool Raining, Flash, Constant;
        private int ConstantTimer = -1;
        private readonly ISourceRandom rand;
        public bool Activated { get; private set; }

        public static LightningStrikeTrigger Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public LightningStrikeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data, offset, data.Float("playerOffset", 0f), data.Float("verticalOffset", 0f),
                data.Float("height", 0f), data.Int("seed", 0), data.Float("delay", 0f), data.Bool("raining", true),
                data.Bool("flash", true), data.Bool("constant", false))
        {
        }

        public LightningStrikeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset, float playerOffset,
            float verticalOffset, float strikeHeight, int seed, float delay, bool raining, bool flash,
            bool constant) : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
            data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            PlayerOffset = playerOffset;
            VerticalOffset = verticalOffset;
            StrikeHeight = strikeHeight;
            Seed = seed;
            Delay = delay;
            Raining = raining;
            Flash = flash;
            Constant = constant;
            rand = ((ISourceRandomProcessEnvironment)engine).RandomProcess.Factory.CreateSeeded(seed);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (!Activated && !Constant)
            {
                Strike(player);
                Activated = true;
            }
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            if (!Constant) return;
            ConstantTimer -= 1;
            if (ConstantTimer <= 0)
            {
                Strike(player);
                ConstantTimer = rand.Next(40, 60);
            }
        }

        public void Strike(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)player.Scene;
            level.Scheduler.QueueSourceEntity(StrikeHeight == 0
                ? new LightningStrikeState(new Vector2(player.X + PlayerOffset, level.Bounds.Top), rand.Next(1, 100),
                    level.Bounds.Height, engine, Delay)
                : new LightningStrikeState(new Vector2(player.X + PlayerOffset, player.Y + VerticalOffset),
                    rand.Next(1, 100), StrikeHeight, engine, Delay));
            Add(new SimulationCoroutine(engine, ThunderEffect(level)));
            if (Raining && !level.Background.Backdrops.OfType<RainBackdropPresentation>().Any())
                level.Background.Backdrops.Add(new RainBackdropPresentation(engine));
        }

        private IEnumerator ThunderEffect(OriginalLevelSceneRoot level)
        {
            yield return Delay;
            ((IOriginalGameplayProcess)engine).AudioSession.Play(
                "event:/new_content/game/10_farewell/lightning_strike");
            level.Shake(.3f);
            if (Flash) level.Flash(PackedColor.White, false);
        }
    }
}
