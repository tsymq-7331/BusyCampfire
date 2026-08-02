using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using BaseLib.Config;
using BusyCampfire.BusyCampfireCode.Config;
using BusyCampfire.BusyCampfireCode.Patches;
using BusyCampfire.BusyCampfireCode.Runtime;
using BusyCampfire.BusyCampfireCode.Relics;

namespace BusyCampfire.BusyCampfireCode;

// 建议将 C# 代码放在 BusyCampfireCode，将图片和本地化等资源放在 BusyCampfire。
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "BusyCampfire";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
    internal static RuntimeModeCoordinator RuntimeMode { get; private set; } = null!;
    internal static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
     
        _ = new ForgingHammer();
        _ = new ForgingHammerPool();
        ModConfig.Load<BusyCampfireConfig>();
        RuntimeMode = new RuntimeModeCoordinator();

        Harmony harmony = new(ModId);
        harmony.PatchAll();
        IsInitialized = true;
        Logger.Info("火堆更加忙碌已成功加载。");
    }
}
