using UnityEngine;
using RoR2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace ChefOvercooked;

public class CookedBuff : BuffBase
{
    protected override string Name => "ChefCookedMarked";
    public static BuffDef BuffDef;
    protected override Sprite IconSprite => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("texChefCookedMarked");
    protected override bool IsHidden => false;

    protected override void Initialize()
    {
        BuffDef = Value;

        IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
    }

    private void GlobalEventManager_OnCharacterDeath(ILContext il)
    {
        ILCursor cursor = new(il);

        int varOneIndex = -1;
        int varTwoIndex = -1;
        int varThreeIndex = -1;
        int varFourIndex = -1;

        if (cursor.TryGotoNext(
            x => x.MatchLdcR4(0),
            x => x.MatchStloc(out varOneIndex),
            x => x.MatchLdcR4(0),
            x => x.MatchStloc(out varTwoIndex),
            x => x.MatchLdcR4(0),
            x => x.MatchStloc(out varThreeIndex),
            x => x.MatchLdcR4(0),
            x => x.MatchStloc(out varFourIndex),
            x => x.MatchLdloc(out _),
            x => x.MatchLdsfld(typeof(DLC2Content.Buffs), nameof(DLC2Content.Buffs.CookingChopped))
        ))
        {
            if (cursor.TryGotoNext(
                x => x.MatchLdloc(out _),
                x => x.MatchLdsfld(typeof(DLC2Content.Buffs), nameof(DLC2Content.Buffs.CookingChopped)),
                x => x.MatchCallvirt(typeof(CharacterBody), nameof(CharacterBody.HasBuff))
            ))
            {
                cursor.MoveAfterLabels();

                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<DamageReport, bool>>((damageReport) =>
                {
                    CharacterBody victimBody = damageReport.victimBody;
                    return victimBody ? victimBody.HasBuff(BuffDef) : false;
                });

                ILLabel falseState = cursor.MarkLabel();
                cursor.Emit(OpCodes.Brfalse, falseState);

                cursor.Emit(OpCodes.Ldloc, varOneIndex);
                cursor.EmitDelegate<Func<float, float>>(num => num + 1f);
                cursor.Emit(OpCodes.Stloc, varOneIndex);

                cursor.Emit(OpCodes.Ldloc, varTwoIndex);
                cursor.EmitDelegate<Func<float, float>>(num => num + 2f);
                cursor.Emit(OpCodes.Stloc, varTwoIndex);

                cursor.Emit(OpCodes.Ldloc, varThreeIndex);
                cursor.EmitDelegate<Func<float, float>>(num => num + 0.04f);
                cursor.Emit(OpCodes.Stloc, varThreeIndex);

                cursor.Emit(OpCodes.Ldloc, varFourIndex);
                cursor.EmitDelegate<Func<float, float>>(num => num + 0.75f);
                cursor.Emit(OpCodes.Stloc, varFourIndex);

                cursor.MarkLabel(falseState);
            }
            else Log.Error(BuffDef.name + " failed to ILHook #2");
        }
        else Log.Error(BuffDef.name + " failed to ILHook #1");
    }
}
