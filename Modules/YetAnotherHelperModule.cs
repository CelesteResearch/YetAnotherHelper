using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.YetAnotherHelper.Configuration;
using Celeste.Mods.YetAnotherHelper.Runtime.Spikes;
using Celeste.Mods.YetAnotherHelper.Presentation.Lighting;

namespace Celeste.Mods.YetAnotherHelper.Modules
{
    public sealed class YetAnotherHelperModule(ISourceEngineContext engine) : SourceEverestModule(engine)
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, YetAnotherHelperModule> owners = new();
        private SpikeControllerProcess spikeProcess;
        private IDisposable lightHooks;
        public ISourceEngineContext Engine => SourceEngine;
        public YetAnotherHelperSession Session => (YetAnotherHelperSession)_Session;
        public override Type SessionType => typeof(YetAnotherHelperSession);

        public static YetAnotherHelperModule For(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var owner)
                ? owner
                : throw new InvalidOperationException("YetAnotherHelper is not loaded for this engine.");

        public SpikeControllerProcess Spikes => spikeProcess;

        public override void Load()
        {
            owners.Add(SourceEngine, this);
            try
            {
                lightHooks = LightSourceHooks.Install(SourceEngine);
                spikeProcess = new SpikeControllerProcess(this);
            }
            catch
            {
                lightHooks?.Dispose();
                lightHooks = null;
                owners.Remove(SourceEngine);
                throw;
            }
        }

        public override void Unload()
        {
            lightHooks?.Dispose();
            lightHooks = null;
            spikeProcess?.Dispose();
            spikeProcess = null;
            owners.Remove(SourceEngine);
        }
    }
}
