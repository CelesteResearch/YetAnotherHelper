using Celeste.Everest.Modules.Persistence;

namespace Celeste.Mods.YetAnotherHelper.Configuration
{
    public class YetAnotherHelperSession : EverestModuleSession
    {
        public float LightSourceAlpha { get; set; } = 1f;
        public bool SpikeJumpThruHooked { get; set; }
    }
}
