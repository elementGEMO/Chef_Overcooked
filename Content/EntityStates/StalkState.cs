using EntityStates;
using R2API;
using RoR2;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPathsBetter;

namespace ChefOvercooked;

public class StalkState : GenericCharacterMain
{
    private static EffectDef StalkEffect;

    private HurtBoxGroup characterHurtBoxGroup;
    private float stalkDuration;

    public static void CreateEffects()
    {
        ContentAddition.AddEntityState(typeof(StalkState), out _);

        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2_Base_Imp.ImpBlinkEffect_prefab).WaitForCompletion().InstantiateClone("StalkBlinkEffect");

        Transform particles = effectPrefab.transform.Find("Particles");
        Object.Destroy(particles.Find("Dash").gameObject);

        particles.Find("NoiseTrails").localScale = Vector3.one * 2f;

        StalkEffect = new()
        {
            prefab = effectPrefab,
            prefabName = "StalkBlinkEffect",
            prefabEffectComponent = effectPrefab.GetComponent<EffectComponent>()
        };

        ContentAddition.AddEffect(StalkEffect.prefab);
    }
    public override void OnEnter()
    {
        base.OnEnter();

        float fadeDuration = PluginConfig.Stalk_Base_Duration.Value + PluginConfig.Stalk_Duration_Stack.Value * (characterBody.inventory.GetItemCountEffective(WarpingStalkItem.ItemDef) - 1);

        characterHurtBoxGroup = characterBody.hurtBoxGroup;
        stalkDuration = fadeDuration;
        BlinkParticle();

        if (characterHurtBoxGroup)
            characterHurtBoxGroup.hurtBoxesDeactivatorCounter++;

        for (int i = 0; i < Mathf.CeilToInt(fadeDuration); i++)
            characterBody.AddTimedBuff(StalkBuff.BuffDef, fadeDuration / Mathf.CeilToInt(fadeDuration) * i);
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (fixedAge >= stalkDuration && isAuthority)
            outer.SetNextStateToMain();
    }
    public override void OnExit()
    {
        characterBody.RemoveBuff(StalkBuff.BuffDef);
        BlinkParticle();

        if (characterHurtBoxGroup)
            characterHurtBoxGroup.hurtBoxesDeactivatorCounter--;

        base.OnExit();
    }
    private void BlinkParticle()
    {
        Util.PlaySound("Play_voidBarnacle_m1_impact", gameObject);
        EffectManager.SpawnEffect(StalkEffect.prefab, new EffectData()
        {
            origin = Util.GetCorePosition(characterBody),
            rotation = Random.rotation
        }, true);
    }
}