using UnityEngine;
using RoR2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace ChefOvercooked;

public class MeatTimerBuff : BuffBase
{
    protected override string Name => "MeatTimerBuff";
    public static BuffDef BuffDef;
    protected override bool IsHidden => true;
    protected override bool IsStackable => false;

    protected override void Initialize()
    {
        BuffDef = Value;

        On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
    }

    private void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
    {
        if (buffDef != BuffDef) orig(self, buffDef);

        bool hasBuff    = self.HasBuff(BuffDef);
        bool hasMeat    = self.inventory ? self.inventory.GetItemCountEffective(MonsterMeatItem.ItemDef) > 0 : false;
        bool isChef     = self ? self.GetComponent<ChefController>() : false;

        if (!hasBuff && hasMeat && isChef)
        {
            EntityStateMachine.FindByCustomName(self.gameObject, "Weapon").SetState(new CookingState());
        }

        orig(self, buffDef);
    }
}
