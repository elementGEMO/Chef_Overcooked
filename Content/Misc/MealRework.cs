using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths.Version_1_35_0;
using R2API;
namespace ChefOvercooked;

public class MealRework
{
    public MealRework()
    {
        if (!PluginConfig.Rework_Meals.Value)
            return;

        IL.RoR2.Inventory.TempItemsStorage.SyncStackToDecay += TempItemsStorage_SyncStackToDecay;
        On.RoR2.Inventory.TempItemsStorage.GiveItemTemp += TempItemsStorage_GiveItemTemp;
    }

    private delegate int DecayDelegate(ref Inventory.TempItemsStorage self, ItemIndex itemIndex, int num);
    private void TempItemsStorage_SyncStackToDecay(ILContext il)
    {
        ILCursor cursor = new(il);
        int numIndex    = -1;

        cursor.TryGotoNext(
            x => x.MatchLdfld(typeof(Inventory.TempItemsStorage), nameof(Inventory.TempItemsStorage.invDecayDuration)),
            x => x.MatchMul(),
            x => x.MatchCall(typeof(Mathf), nameof(Mathf.CeilToInt)),
            x => x.MatchStloc(out numIndex)
        );

        if (numIndex != -1 && cursor.TryGotoNext(
            x => x.MatchLdloc(out _),
            x => x.MatchLdloc(numIndex)
        ))
        {
            cursor.MoveAfterLabels();

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, numIndex);

            cursor.EmitDelegate<DecayDelegate>((ref Inventory.TempItemsStorage self, ItemIndex itemIndex, int num) =>
            {
                bool isMeal = ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.FoodTier;
                bool doRemove = isMeal && self.tempItemStacks.GetStackValue(itemIndex) > num;

                return doRemove ? 0 : num;
            });

            cursor.Emit(OpCodes.Stloc, numIndex);
        }
        else Log.Error("MEALREWORK_SYNCSTACKTODECAY failed to ILHook");
    }

    private float TempItemsStorage_GiveItemTemp(On.RoR2.Inventory.TempItemsStorage.orig_GiveItemTemp orig, ref Inventory.TempItemsStorage self, ItemIndex itemIndex, float decayValue)
    {
        if (ItemCatalog.GetItemDef(itemIndex)?.tier == ItemTier.FoodTier)
        {
            int currentTempStack = self.inventory.GetItemCountTemp(itemIndex);
            Run.FixedTimeStamp fixedTimeStamp = Run.FixedTimeStamp.now + self.decayDuration * (PluginConfig.Meal_Duration.Value / 80f) + currentTempStack * self.decayDuration;
            self.decayToZeroTimeStamps.SetValue((SparseIndex)itemIndex, fixedTimeStamp);

            self.inventory.SetDirtyBit(16U);
            self.SyncStackToDecay(itemIndex);

            return (fixedTimeStamp.timeUntilClamped - Run.FixedTimeStamp.now.timeUntilClamped) * self.invDecayDuration;
        }
        else return orig(ref self, itemIndex, decayValue);
    }
}
