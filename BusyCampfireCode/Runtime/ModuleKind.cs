namespace BusyCampfire.BusyCampfireCode.Runtime;

internal enum ModuleKind
{
    /// <summary>UI, calculations, local VFX, and other changes invisible to peers.</summary>
    ClientOnly,

    /// <summary>Anything that changes deterministic run or combat state.</summary>
    Gameplay
}
