using UnityEngine;
using RoR2;

namespace ChefOvercooked;

public class CookingBuff : BuffBase
{
    protected override string Name => "ChefCooking";
    public static BuffDef BuffDef;
    protected override bool IsHidden => true;

    protected override void Initialize()
    {
        BuffDef = Value;

        On.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;
    }

    private void GlobalEventManager_ProcessHitEnemy(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);

        CharacterBody victimBody        = victim ? victim.GetComponent<CharacterBody>() : null;
        CharacterBody characterBody     = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
        CharacterMaster characterMaster = characterBody ? characterBody.master : null;

        if (characterMaster && victimBody)
        {
            if (characterBody.HasBuff(BuffDef)) victimBody.AddBuff(CookedBuff.BuffDef);
        }
    }
}
