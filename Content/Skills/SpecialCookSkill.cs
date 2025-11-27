using RoR2.Skills;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2BepInExPack.GameAssetPathsBetter;
using R2API;
using EntityStates;

namespace ChefOvercooked;
using static HelperFontColor;
using static HelperLanguage;
public class SpecialCookSkill : SkillBase
{
    public static SkillDef SkillDef;
    protected override string Name => "CookSkill";

    protected override string DisplaySkillName  => "Cook";
    protected override string SkillDescription => string.Format(
        "Stunning".Style(FontColor.cIsDamage) + ". Rapidly " + "Cleave ".Style(FontColor.cIsDamage) + "enemies for " + "6x150% damage".Style(FontColor.cIsDamage) + ". Slain enemies become already discovered " + "temporary food items".Style(FontColor.cIsUtility) + "."
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
            "KEYWORD_STUNNING",
            LanguageAdd(ChefOverCookedPlugin.TokenPrefix + "KEYWORD_CLEAVE", "Cleave".Style(FontColor.cKeywordName) + "Instantly kills enemies below ".Style(FontColor.cSub) + "10% health".Style(FontColor.cIsHealth) + ".".Style(FontColor.cSub))
        ];

        ContentAddition.AddEntityState(typeof(CookState), out _);
        ContentAddition.AddEntityState(typeof(CookingState), out _);

        SkillFamily chefSpecialFamily = Addressables.LoadAssetAsync<SkillFamily>(RoR2_DLC2_Chef.ChefSpecialFamily_asset).WaitForCompletion();
        HG.ArrayUtils.ArrayAppend(ref chefSpecialFamily.variants, new SkillFamily.Variant { skillDef = SkillDef });
    }
}
