using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace BusyCampfire.BusyCampfireCode.Compatibility;

internal static class MultiEnchantmentCompatibility
{
    private const string ApiTypeName = "MultiEnchantmentMod.Api.MultiEnchantmentApi";

    private static Type? ApiType => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(ApiTypeName, throwOnError: false))
        .FirstOrDefault(type => type != null);

    internal static bool IsAvailable => ApiType != null;

    internal static HashSet<Type> GetAttachedEnchantmentTypes(CardModel card)
    {
        HashSet<Type> result = [];
        Type? apiType = ApiType;
        MethodInfo? getEnchantments = apiType?.GetMethod(
            "GetEnchantments",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(CardModel)],
            modifiers: null);

        if (getEnchantments?.Invoke(null, [card]) is not IEnumerable enchantments)
            return result;

        foreach (object? enchantment in enchantments)
        {
            if (enchantment != null)
                result.Add(enchantment.GetType());
        }

        return result;
    }

    internal static bool TryEnchant(CardModel card, EnchantmentModel enchantment, int amount)
    {
        Type? apiType = ApiType;
        MethodInfo? enchant = apiType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "Enchant" &&
                method.GetParameters() is { Length: 4 } parameters &&
                parameters[0].ParameterType == typeof(CardModel) &&
                parameters[1].ParameterType == typeof(EnchantmentModel));

        return enchant?.Invoke(null, [card, enchantment, (decimal)amount, null]) is EnchantmentModel;
    }
}
