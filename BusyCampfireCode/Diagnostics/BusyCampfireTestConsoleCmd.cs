using BusyCampfire.BusyCampfireCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BusyCampfire.BusyCampfireCode.Diagnostics;

/// <summary>
/// Gives the issuing player every relic needed to exercise Busy Campfire's
/// rest-site changes. The game discovers mod console commands automatically.
/// </summary>
public sealed class BusyCampfireTestConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "busycampfire_test";
    public override string Args => string.Empty;
    public override string Description =>
        "Adds all relics used to test Busy Campfire rest-site features";

    // Vanilla forwards networked console commands through the run action queue.
    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length > 0)
            return new CmdResult(false, $"Usage: {CmdName}");

        if (issuingPlayer == null)
            return new CmdResult(false, "A run is currently not in progress!");

        List<RelicModel> relics = TestRelics().ToList();
        List<RelicModel> missing = relics
            .Where(relic => issuingPlayer.GetRelicById(relic.Id) == null)
            .ToList();
        List<string> skipped = relics
            .Where(relic => issuingPlayer.GetRelicById(relic.Id) != null)
            .Select(relic => relic.Id.Entry)
            .ToList();

        if (missing.Count == 0)
        {
            return new CmdResult(
                true,
                $"Busy Campfire test relics are already present ({skipped.Count} skipped).");
        }

        Task obtainTask = ObtainAll(missing, issuingPlayer);
        string added = string.Join(", ", missing.Select(relic => relic.Id.Entry));
        string message = $"Adding {missing.Count} Busy Campfire test relics: {added}";
        if (skipped.Count > 0)
            message += $"\nSkipped {skipped.Count} already-owned relics: {string.Join(", ", skipped)}";

        return new CmdResult(obtainTask, true, message);
    }

    private static IEnumerable<RelicModel> TestRelics()
    {
        yield return ModelDb.Relic<VenerableTeaSet>();
        yield return ModelDb.Relic<FakeVenerableTeaSet>();
        yield return ModelDb.Relic<EternalFeather>();
        yield return ModelDb.Relic<Girya>();
        yield return ModelDb.Relic<PumpkinCandle>();
        yield return ModelDb.Relic<PaelsGrowth>();
        yield return ModelDb.Relic<MiniatureTent>();
        yield return ModelDb.Relic<RegalPillow>();
        yield return ModelDb.Relic<TinyMailbox>();
        yield return ModelDb.Relic<DreamCatcher>();
        yield return ModelDb.Relic<StoneHumidifier>();
        yield return ModelDb.Relic<MeatCleaver>();
        yield return ModelDb.Relic<Shovel>();
        yield return ModelDb.Relic<ForgingHammer>();
    }

    private static async Task ObtainAll(IEnumerable<RelicModel> relics, Player player)
    {
        foreach (RelicModel relic in relics)
            await RelicCmd.Obtain(relic.ToMutable(), player);
    }
}
