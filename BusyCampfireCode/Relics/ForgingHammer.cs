using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BusyCampfire.BusyCampfireCode.Relics;

[Pool(typeof(ForgingHammerPool))]
public sealed class ForgingHammer : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override int MerchantCost => 275;
    public override string PackedIconPath => "res://BusyCampfire/images/relics/forging_hammer.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;

    public override List<(string, string)>? Localization => LocManager.Instance.Language switch
    {
        "zhs" => new RelicLoc(
            "锻造锤",
            "在营火处成功锻造一张未附魔的牌后，为其添加一个随机且适用的原版附魔。",
            "锤声落下，旧铁也会记住火焰。"),
        _ => new RelicLoc(
            "Forging Hammer",
            "After successfully Smithing an unenchanted card at a Rest Site, give it a random applicable vanilla Enchantment.",
            "With every strike, even old iron remembers the flame.")
    };
}

public sealed class ForgingHammerPool : CustomRelicPoolModel
{
    public override bool IsShared => true;

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        yield return ModelDb.Relic<ForgingHammer>();
    }
}
