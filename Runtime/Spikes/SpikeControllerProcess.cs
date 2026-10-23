using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.YetAnotherHelper.Modules;
using Celeste.Mods.YetAnotherHelper.Entities.Controllers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.YetAnotherHelper.Runtime.Spikes
{
    public sealed class SpikeControllerProcess : IDisposable
    {
        private readonly YetAnotherHelperModule module;
        private readonly Hook loadHook;
        private Hook spikeHook;
        public SpikeJumpThruController CurrentController;
        public SpikeJumpThruController NextController;
        public float TransitionProgress = -1f;
        public bool SpikeHooked => spikeHook?.IsApplied == true;

        public SpikeControllerProcess(YetAnotherHelperModule module)
        {
            this.module = module;
            loadHook = new Hook(typeof(OriginalLevelSceneRoot).GetMethod("LoadLevel"),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)LoadLevel);
        }

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig, OriginalLevelSceneRoot level,
            PlayerIntroType intro, bool fromLoader)
        {
            orig(level, intro, fromLoader);
            if (!ReferenceEquals(level.EngineContext, module.Engine)) return;
            if (module.Session.SpikeJumpThruHooked &&
                !((Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData)level.Session.LevelData).Entities.Any(e =>
                    e.Name == "YetAnotherHelper/SpikeJumpThruController"))
            {
                level.SourceScene.QueueSourceEntity(new SpikeJumpThruController(module.Engine,
                    module.Session.SpikeJumpThruHooked, Vector2.Zero));
                level.SourceScene.CommitQueuedEntityLists();
            }
        }

        public void EnableSpikeHook()
        {
            if (spikeHook != null)
            {
                spikeHook.Apply();
                return;
            }

            spikeHook = new Hook(
                typeof(SpikesState).GetMethod("OnCollide", BindingFlags.NonPublic | BindingFlags.Instance),
                (Action<Action<SpikesState, RoomPlayerSession>, SpikesState, RoomPlayerSession>)OnCollide);
        }

        private void OnCollide(Action<SpikesState, RoomPlayerSession> orig, SpikesState spikes,
            RoomPlayerSession player)
        {
            if (!ReferenceEquals(player.EngineContext, module.Engine) || player.Machine.State != 8)
                orig(spikes, player);
        }

        public void DisableSpikeHook()
        {
            spikeHook?.Undo();
        }

        public void Dispose()
        {
            spikeHook?.Dispose();
            spikeHook = null;
            loadHook.Dispose();
            CurrentController = NextController = null;
            TransitionProgress = -1f;
        }
    }
}
