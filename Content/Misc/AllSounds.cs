using RoR2;

namespace ChefOvercooked;
using static SoundHelper;

public class AllSounds
{
    public static NetworkSoundEventDef Play_Meat_Pickup;
    public AllSounds()
    {
        // for CookDamageType.cs
        Play_Meat_Pickup = CreateNetworkSoundDef("Play_UI_item_pickup");
    }
}
