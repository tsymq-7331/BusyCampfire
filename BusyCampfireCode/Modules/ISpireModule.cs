using BusyCampfire.BusyCampfireCode.Runtime;

namespace BusyCampfire.BusyCampfireCode.Modules;

internal interface ISpireModule
{
    string Id { get; }
    ModuleKind Kind { get; }
    void Initialize();
    void OnModeChanged(RuntimeMode mode);
    void Shutdown();
}
