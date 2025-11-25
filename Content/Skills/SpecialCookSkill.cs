using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2BepInExPack.GameAssetPathsBetter;
using R2API;
using RoR2;
using BepInEx;
using EntityStates.Chef;
using EntityStates;
using MonoMod.RuntimeDetour;

namespace ChefOvercooked;
using static HelperFontColor;
using static HelperLanguage;
public class SpecialCookSkill : SkillBase
{
    public static SkillDef SkillDef;
    protected override string Name => "CookSkill";

    protected override string DisplaySkillName  => "Cook";
    protected override string SkillDescription => string.Format(
        "Cleave".Style(FontColor.cIsDamage) + ". " + "Stunning".Style(FontColor.cIsDamage) + ". Rapidly prepare a meal out of enemies for " + "6x80% ".Style(FontColor.cIsDamage) + "damage. Slain enemies become tasty " + "temporary meal items".Style(FontColor.cIsUtility) + "."
    );

    protected override Sprite SkillSprite       => ChefOverCookedPlugin.Bundle.LoadAsset<Sprite>("TemporarySkillIcon");

    protected override InterruptPriority InterruptPriority          => InterruptPriority.Frozen;
    protected override string ActivationStateMachine                => "Weapon";
    protected override SerializableEntityStateType ActivationState  => new(typeof(CookState));
    protected override bool IsCombatSkill                           => true;
    protected override bool MustKeyPress                            => true;

    protected override bool CancelSprintOnActivation    => true;
    protected override bool ForceSprintDuringState      => false;
    protected override bool CanceledFromSprinting       => false;

    protected override float BaseCooldown   => 10f;
    protected override int BaseMaxStock     => 1;
    protected override int RechargeStock    => 1;
    protected override int RequiredStock    => 1;
    protected override int StockToConsume   => 1;

    protected override void Initialize()
    {
        SkillDef = Value;

        SkillDef.keywordTokens = [
            LanguageAdd(ChefOverCookedPlugin.TokenPrefix + "KEYWORD_CLEAVE", "Cleave".Style(FontColor.cKeywordName) + "Instantly kills enemies below ".Style(FontColor.cSub) + "10% health".Style(FontColor.cIsHealth) + ".".Style(FontColor.cSub)),
            "KEYWORD_STUNNING"
        ];

        ContentAddition.AddEntityState(typeof(CookState), out _);
        ContentAddition.AddEntityState(typeof(CookingState), out _);

        SkillFamily chefSpecialFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_DLC2_Chef.ChefSpecialFamily_asset).WaitForCompletion();
        HG.ArrayUtils.ArrayAppend(ref chefSpecialFamily.variants, new SkillFamily.Variant { skillDef = SkillDef });
    }
}
