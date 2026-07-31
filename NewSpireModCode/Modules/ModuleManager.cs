using NewSpireMod.NewSpireModCode.Config;
using NewSpireMod.NewSpireModCode.Runtime;

namespace NewSpireMod.NewSpireModCode.Modules;

internal sealed class ModuleManager
{
    private readonly RuntimeModeCoordinator _mode;
    private readonly List<ISpireModule> _modules = [];
    private readonly HashSet<string> _faulted = [];

    public ModuleManager(RuntimeModeCoordinator mode)
    {
        _mode = mode;
    }

    public void Register(ISpireModule module) => _modules.Add(module);

    public void InitializeAll()
    {
        foreach (ISpireModule module in _modules)
            RunSafely(module, module.Initialize);
    }

    public void NotifyModeChanged()
    {
        foreach (ISpireModule module in _modules)
        {
            if (_mode.MayRun(module.Kind))
                RunSafely(module, () => module.OnModeChanged(_mode.Current));
            else
                RunSafely(module, module.Shutdown);
        }
    }

    private void RunSafely(ISpireModule module, Action action)
    {
        if (_faulted.Contains(module.Id))
            return;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error($"模块 {module.Id} 发生异常：{exception}");
            if (SpireConfig.DisableFaultedModule)
                _faulted.Add(module.Id);
            else
                throw;
        }
    }
}
