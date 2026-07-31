using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using BaseLib.Config;
using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Modules;
using BusyCampfire.BusyCampfireCode.Runtime;
using BusyCampfire.BusyCampfireCode.Relics;
using BusyCampfire.BusyCampfireCode.Ui;

namespace BusyCampfire.BusyCampfireCode;

// 建议将 C# 代码放在 BusyCampfireCode，将图片和本地化等资源放在 BusyCampfire。
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "BusyCampfire";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
    internal static RuntimeModeCoordinator RuntimeMode { get; private set; } = null!;
    internal static ModuleManager Modules { get; private set; } = null!;
    internal static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
     
        _ = new ForgingHammer();
        _ = new ForgingHammerPool();
        ModConfig.Load<SpireConfig>();
        RuntimeMode = new RuntimeModeCoordinator();
        Modules = new ModuleManager(RuntimeMode);

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        Modules.InitializeAll();
        IsInitialized = true;
        if (NGame.Instance != null)
            GlobalStatusHud.Attach(NGame.Instance);
        Logger.Info("火堆更加忙碌已成功加载。");
    }
}
