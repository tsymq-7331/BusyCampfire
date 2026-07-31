using NewSpireMod.NewSpireModCode.Runtime;

namespace NewSpireMod.NewSpireModCode.Modules;

internal interface ISpireModule
{
    string Id { get; }
    ModuleKind Kind { get; }
    void Initialize();
    void OnModeChanged(RuntimeMode mode);
    void Shutdown();
}
