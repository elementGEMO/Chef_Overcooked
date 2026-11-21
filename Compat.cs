using RoR2;
using RoR2.Skills;
using System.Runtime.CompilerServices;

namespace ChefOvercooked;
internal static class LoadAllCompats
{
    public static void Init()
    {
        if (CompatAncientScepter.CompatExists)
            CompatAncientScepter.Init();
    }
}
internal static class CompatAncientScepter
{
    public const string GUID = "com.DestroyedClone.AncientScepter";
    public static bool CompatExists => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GUID);
    public static void Init() => RegisterSkills();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterSkills() => AncientScepter.AncientScepterItem.instance.RegisterScepterSkill(SpecialScepterCookSkill.SkillDef, "ChefBody", SpecialCookSkill.SkillDef);
}