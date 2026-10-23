using System;
using Celeste.Core.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Game.Presentation.Effects.Bloom;
using Celeste.Game.Presentation.Lighting;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Mods.YetAnotherHelper.Modules;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.YetAnotherHelper.Presentation.Lighting
{
    public sealed class LightSourceHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly YetAnotherHelperModule module;
        private readonly Hook bloomHook, lightingHook;
        private readonly LightSourceFrameCache<BloomPointPresentation> bloomFrames = new();
        private readonly LightSourceFrameCache<VertexLightState> lightingFrames = new();
        private float Alpha => module.Session.LightSourceAlpha;

        private LightSourceHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            module = YetAnotherHelperModule.For(engine);
            bloomHook = new Hook(typeof(BloomRendererPresentation).GetMethod(nameof(BloomRendererPresentation.Apply)),
                (Action<Action<BloomRendererPresentation, VirtualRenderTarget, object>, BloomRendererPresentation,
                    VirtualRenderTarget, object>)ApplyBloom);
            lightingHook = new Hook(
                typeof(LightingRendererPresentation).GetMethod(nameof(LightingRendererPresentation.BeforeRender)),
                (Action<Action<LightingRendererPresentation, object>, LightingRendererPresentation, object>)
                BeforeLighting);
        }

        public static IDisposable Install(ISourceEngineContext engine) => new LightSourceHooks(engine);

        public void Dispose()
        {
            lightingHook.Dispose();
            bloomHook.Dispose();
        }

        private bool Owns(object scene) =>
            scene is ISourceEngineContextScene actual && ReferenceEquals(actual.EngineContext, engine);

        private void ApplyBloom(Action<BloomRendererPresentation, VirtualRenderTarget, object> orig,
            BloomRendererPresentation self, VirtualRenderTarget target, object scene)
        {
            if (!Owns(scene) || !(Alpha < 1f))
            {
                orig(self, target, scene);
                return;
            }

            var frame = bloomFrames.Rent(((ISourceScene)scene).SourceScene.Components
                .GetTrackedComponents("Celeste.BloomPoint"));
            try
            {
                for (int i = 0; i < frame.SnapshotCount; i++)
                {
                    var point = (BloomPointPresentation)frame.Snapshot[i];
                    if (!point.Visible) continue;
                    frame.Add(point, point.Alpha);
                    point.Alpha *= Alpha;
                }

                orig(self, target, scene);
                for (int i = 0; i < frame.AffectedCount; i++)
                    frame.Affected[i].Component.Alpha = frame.Affected[i].Alpha;
            }
            finally
            {
                bloomFrames.Return(frame);
            }
        }

        private void BeforeLighting(Action<LightingRendererPresentation, object> orig,
            LightingRendererPresentation self, object scene)
        {
            if (!Owns(scene) || !(Alpha < 1f))
            {
                orig(self, scene);
                return;
            }

            var frame = lightingFrames.Rent(((ISourceScene)scene).SourceScene.Components
                .GetTrackedComponents("Celeste.VertexLight"));
            try
            {
                for (int i = 0; i < frame.SnapshotCount; i++)
                {
                    var light = (VertexLightState)frame.Snapshot[i];
                    if (!light.Visible || light.Spotlight) continue;
                    frame.Add(light, light.Alpha);
                    light.Alpha *= Alpha;
                }

                orig(self, scene);
                for (int i = 0; i < frame.AffectedCount; i++)
                    frame.Affected[i].Component.Alpha = frame.Affected[i].Alpha;
            }
            finally
            {
                lightingFrames.Return(frame);
            }
        }
    }
}
