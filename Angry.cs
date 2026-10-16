using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
namespace AngryBirds
{
    public class AngryBirds
    {
        public static void OnLoad()
        {
            ModAPI.RegisterInput("Activate last shot bird", "Activate last shot bird", KeyCode.T);
			ModAPI.RegisterInput("Slingshot X: activate birds", "Slingshot X: activate birds", KeyCode.X);
			ModAPI.RegisterInput("Slingshot X: launch", "Slingshot X: launch", KeyCode.T);
        }

        public static void Main()
        {
            CategoryBuilder.Create("Angry Birds Mod", "Birds, Pigs", ModAPI.LoadSprite("categoryicon.png"));
            CategoryBuilder.Create("Angry Birds Blocks", "Blocks from Angry Birds Mod.", ModAPI.LoadSprite("categoryicon2.png"));
            ModAPI2.metaString = ModAPI.Metadata.MetaLocation;

            //red
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Red", // Name.
                    NameToOrderByOverride = "1 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Red is the leader of the flock. nothing special but he is always angry. His ability is flying objects off for seconds when activated.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetred.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Red.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if(!Instance.GetComponent<Red>())
                            Instance.AddComponent<Red>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "red";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Red";
                        Instance.GetComponent<AngryBird>().glassMultiplier = 2;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 2;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );






            //mighty feathers red
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Mighty Feathers Red",
                    NameToOrderByOverride = "1 1 1", // must sort after "1 1" (Red) and before "1 2" (Chuck)
                    DescriptionOverride = "Red with the power of the Mighty Feathers. When activated, he flies at your mouse.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeetmightyred.png", 5f),   // NEW png: catalog icon
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 3f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("MightyRed.png", 1f);  // NEW png
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
 
                        // your ability class goes here INSTEAD of <Red>
                        if (!Instance.GetComponent<MightyRed>())
                            Instance.AddComponent<MightyRed>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
 
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
 
                        // codeName       -> picks the VOICE files: AudioClip/<codeName>_voice_*.wav
                        // codeName_sprite -> picks the SPRITE files: <codeName_sprite>.png and <codeName_sprite>2.png
                        // Using "red" for codeName means he just reuses Red's voice lines.
                        Instance.GetComponent<AngryBird>().codeName = "red";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "MightyRed";
 
                        Instance.GetComponent<AngryBird>().glassMultiplier = 3;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 3;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 3;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
			
			//telebird
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Telebird",
                    NameToOrderByOverride = "1 1 2",   // after Red ("1 1") and Mighty Feathers Red ("1 1 1")
                    DescriptionOverride = "Telebird can teleport. When activated, he vanishes into a vortex, then reappears at your mouse with all of his momentum.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeettelebird.png", 5f),   // NEW png
                    AfterSpawn = (Instance) =>
                    {
                        // Same stats as Red
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 3f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Telebird_collider.png", 1f);   // NEW png
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
 
                        if (!Instance.GetComponent<Telebird>())
                            Instance.AddComponent<Telebird>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
 
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
 
                        // "red" = Red's voice files, reused. codeName_sprite = Telebird.png / Telebird2.png
                        Instance.GetComponent<AngryBird>().codeName = "red";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Telebird";
 
                        Instance.GetComponent<AngryBird>().glassMultiplier = 3;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 3;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 3;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
			
			//homing bird
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Homing Bird",
                    NameToOrderByOverride = "1 1 3",   // after Telebird ("1 1 2")
                    DescriptionOverride = "Homing Bird has a field around him that turns red when a pig comes close. Then he stops, turns toward the pig and dashes at it.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeethomingbird.png", 5f),   // NEW png
                    AfterSpawn = (Instance) =>
                    {
                        // Same stats as Red
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 3f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 3f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("HomingBird.png", 1f);   // NEW png
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
 
                        if (!Instance.GetComponent<HomingBird>())
                            Instance.AddComponent<HomingBird>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
 
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
 
                        // "red" = Red's voice files, reused. codeName_sprite = HomingBird.png / HomingBird2.png
                        Instance.GetComponent<AngryBird>().codeName = "red";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "HomingBird";
 
                        Instance.GetComponent<AngryBird>().glassMultiplier = 3;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 3;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 3;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );

            //chuck
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Chuck", // Name.
                    NameToOrderByOverride = "1 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Chuck is the fastest bird of the flock. He thinks he's the best. When activated, he charges to his direction and it makes him more destructive.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetchuck.png", 5f),
                    AfterSpawn = (Instance) =>
                    {

                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Chuck.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<Chuck>())
                            Instance.AddComponent<Chuck>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "yellow";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Chuck";
                        Instance.GetComponent<AngryBird>().glassMultiplier = 1;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 8;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 1;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );

            //bomb
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Bomb", // Name.
                    NameToOrderByOverride = "1 3", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Bomb is a bomb. he can explode things. good at breaking stones. His ability is explosion. He doesn't die and will be able to explode over and over.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetbomb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Bomb_collider.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        if (!Instance.GetComponent<Bomb>())
                            Instance.AddComponent<Bomb>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "black";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Bomb";
                        Instance.gameObject.AddComponent<ExplosiveBehaviour>();
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Range = 24f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().FragmentForce = 48f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ShotExplodeChance = -1f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BurnPower = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ExplodesOnFragmentHit = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().CanExplodeMultipleTimes = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BigExplosion = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().DestroyOnExplode = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ArmOnUse = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Delay = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().TemperatureLimit = float.MaxValue;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 1;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 1;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 16;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
			
			//shockwave
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Shockwave",
                    NameToOrderByOverride = "1 3 1", // after Bomb ("1 3"), before The Blues ("1 4")
                    DescriptionOverride = "Shockwave is Bomb's quieter cousin. When activated, he sends out a shockwave that blasts everything away and leaves pigs stunned or worse.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeetshockwave.png", 5f),   // NEW png
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 6f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 6f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 6f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Bomb_collider.png", 1f);   // NEW png
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
 
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        if (!Instance.GetComponent<Shockwave>())
                            Instance.AddComponent<Shockwave>();
 
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
 
                        // "black" = Bomb's voice files (AudioClip/black_voice_*.wav), reused
                        Instance.GetComponent<AngryBird>().codeName = "black";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Shockwave";
 
                        Instance.GetComponent<AngryBird>().glassMultiplier = 3;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 3;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 24;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );

            //blue
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "The Blues", // Name.
                    NameToOrderByOverride = "1 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The Blues are triplets. They are small and weak but clever children.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetblues.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 0.5f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 0.5f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 0.5f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Blues.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<Blue>())
                            Instance.AddComponent<Blue>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "blue";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Blues";
                        Instance.GetComponent<AngryBird>().glassMultiplier = 20;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 00f;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );

            //terence
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Terence", // Name.
                    NameToOrderByOverride = "1 6", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Da Biggest Bird. He will destroy literally everything except for map blocks in high speed.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetterence.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().Disintegratable = false;
                        Instance.GetComponent<PhysicalBehaviour>().BulletPenetration = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Terence.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<Terence>())
                            Instance.AddComponent<Terence>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "terence";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Terence";
                        Instance.GetComponent<AngryBird>().AI = false;
                        Instance.GetComponent<AngryBird>().isTerence = true;
                        Instance.gameObject.GetComponent<Terence>().explosionSource = Instance.gameObject.AddComponent<ExplosiveBehaviour>();
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Range = 8f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().FragmentForce = 20f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ShotExplodeChance = -1f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BurnPower = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ExplodesOnFragmentHit = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().CanExplodeMultipleTimes = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BigExplosion = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().DestroyOnExplode = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Delay = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ArmOnUse = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().TemperatureLimit = float.MaxValue;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //tony
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Tony", // Name.
                    NameToOrderByOverride = "1 7", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Terence's cousin living in Finland. ",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeettony.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 5000f;
                        Instance.GetComponent<PhysicalBehaviour>().Disintegratable = false;
                        Instance.GetComponent<PhysicalBehaviour>().BulletPenetration = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Tony.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<Terence>())
                            Instance.AddComponent<Terence>();
                        if (!Instance.GetComponent<Tony>())
                            Instance.AddComponent<Tony>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "terence";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Tony";
                        Instance.GetComponent<AngryBird>().AI = false;
                        Instance.GetComponent<AngryBird>().isTerence = true;
                        Instance.gameObject.GetComponent<Terence>().explosionSource = Instance.gameObject.AddComponent<ExplosiveBehaviour>();
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Range = 8f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().FragmentForce = 20f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ShotExplodeChance = -1f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BurnPower = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ExplodesOnFragmentHit = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().CanExplodeMultipleTimes = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().BigExplosion = true;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().DestroyOnExplode = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().Delay = 0f;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().ArmOnUse = false;
                        Instance.gameObject.GetComponent<ExplosiveBehaviour>().TemperatureLimit = float.MaxValue;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 2f;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //matilda
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Metal Wheel"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Matilda", // Name.
                    NameToOrderByOverride = "1 5", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "She should have been in chickens mod. She lays bomb egg and takes off higher. Let's hope the bomb egg isn't an actual egg.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetmat.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Matilda.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<Matilda>())
                            Instance.AddComponent<Matilda>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "matilda";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Matilda";
                        Instance.GetComponent<AngryBird>().isTerence = false;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 2;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 2;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //hal
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Hal", // Name.
                    NameToOrderByOverride = "1 8", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Our dear friend that Rovio abandoned 1. When activated, he will accelerate backward. If he has enough time, he will get even mroe destructive than Terence.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeethal.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Hal.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<Hal>())
                            Instance.AddComponent<Hal>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "greenbird";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Hal";
                        Instance.GetComponent<AngryBird>().isTerence = false;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 8;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 8;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 3;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //bubbles
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "The Bubbles", // Name.
                    NameToOrderByOverride = "1 9", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Our dear friend that Rovio abandoned 2. When activated, he injects air in his body, ends up being bigger than Terence (ofc not heavier. He's just a balloon.)",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetbubbles.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 1;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 1;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 1;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Bub.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        if (!Instance.GetComponent<Bubbles>())
                            Instance.AddComponent<Bubbles>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "orangebird";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Bub";
                        Instance.GetComponent<AngryBird>().isTerence = false;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 8;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 8;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 2;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //stella
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stella", // Name.
                    NameToOrderByOverride = "1 9 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The pink bird living in the Golden Island. When activated, she makes bubbles around her and the bibbles will lift things.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetstella.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Stella.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        if (!Instance.GetComponent<Stella>())
                            Instance.AddComponent<Stella>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "stella";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Stella";
                        Instance.GetComponent<AngryBird>().isTerence = false;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 2;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 2;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 1;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //icebird
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Ice Bird", // Name.
                    NameToOrderByOverride = "1 9 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeeticebird.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Icebird_collider.png", 1f);
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Ice");
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
                        if (!Instance.GetComponent<Icebird>())
                            Instance.AddComponent<Icebird>();
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().codeName = "icebird";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "Icebird";
                        Instance.GetComponent<AngryBird>().isTerence = false;
                        Instance.GetComponent<AngryBird>().glassMultiplier = 1;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 1;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 1;
                        Instance.GetComponent<AngryBird>().missingVoice = true;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
			//space egg
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Space Egg",
                    NameToOrderByOverride = "1 9 3",   // after Ice Bird ("1 9 2")
                    DescriptionOverride = "A mysterious egg from space. When activated, it turns into a wormhole that sucks in everything around it, then disappears.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeetspaceegg.png", 5f),   // NEW png
                    AfterSpawn = (Instance) =>
                    {
                        // Same stats as Red
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("SpaceEgg_collider.png", 1f);   // NEW png
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Human");
 
                        if (!Instance.GetComponent<SpaceEgg>())
                            Instance.AddComponent<SpaceEgg>();
                        if (!Instance.GetComponent<AngryBird>())
                            Instance.AddComponent<AngryBird>();
 
                        Instance.GetComponent<AngryBird>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<AngryBird>().mouth.mute = true;   // NO VOICELINES: the egg is silent
 
                        // "red" is only used so the voice loading finds files; the mute above keeps it silent.
                        // codeName_sprite = SpaceEgg.png / SpaceEgg2.png
                        Instance.GetComponent<AngryBird>().codeName = "red";
                        Instance.GetComponent<AngryBird>().codeName_sprite = "SpaceEgg";
 
                        Instance.GetComponent<AngryBird>().glassMultiplier = 4;
                        Instance.GetComponent<AngryBird>().woodMultiplier = 4;
                        Instance.GetComponent<AngryBird>().stoneMultiplier = 4;
                        Instance.GetComponent<AngryBird>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
			//mighty eagle (sardine can)
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Mighty Eagle",
                    NameToOrderByOverride = "1 9 4",   // after Space Egg ("1 9 3")
                    DescriptionOverride = "A can of sardines. Activate it and the Mighty Eagle comes swooping in to obliterate everything in his path.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeeteagle.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("idontknowitsname.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
 
                        if (!Instance.GetComponent<MightyEagleCan>())
                            Instance.AddComponent<MightyEagleCan>();
 
                        Instance.transform.tag = "Player";   // friendly, so Terence & co. don't flatten it
                    }
                }
            );
            //space eagle (can with a wormhole)
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Space Eagle",
                    NameToOrderByOverride = "1 9 5",   // right after Mighty Eagle ("1 9 4")
                    DescriptionOverride = "A strange can. Activate it and a wormhole opens. A second later the Space Eagle bursts out, blasting everything away.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeetspaceeagle.png", 5f),   // NEW png
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("idontknowitsname.png", 1f);   // same can image as Mighty Eagle
                        Instance.AddComponent<PolygonCollider2D>();
 
                        if (!Instance.GetComponent<SpaceEagleCan>())
                            Instance.AddComponent<SpaceEagleCan>();
 
                        Instance.transform.tag = "Player";   // friendly, so Terence & co. don't flatten it
                    }
                }
            );
            //minionpig
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Minion Pig", // Name.
                    NameToOrderByOverride = "2 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "They are poor little piggies. very fragile. They will flee when seeing a bird",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetpig.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 1f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Pig.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 4;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "minion_pig";
                        Instance.GetComponent<BadPig>().codeName_sprite = "Pig";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Coward);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //construction pig
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Construction Pig", // Name.
                    NameToOrderByOverride = "2 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Pigs that construct things. They will throw screwdrivers to birds.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetconstructionpig.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 1f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("ConstructionPig.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        if (!Instance.GetComponent<ConstructionPig>())
                            Instance.AddComponent<ConstructionPig>();
                        Instance.GetComponent<BadPig>().hp = 6;
                        Instance.GetComponent<BadPig>().personality = BadPig.Personality.Coward;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "minion_pig";
                        Instance.GetComponent<BadPig>().codeName_sprite = "ConstructionPig";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Coward);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //soldier pig
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Soldier Pig", // Name.
                    NameToOrderByOverride = "2 5", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Pigs that serve King Pig and are commanded by Corporal Pig to fight the birds to get the eggs.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetsoldierpig.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 1f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 1f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("SoldierPig.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 10;
                        Instance.GetComponent<BadPig>().personality = BadPig.Personality.Brave;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "minion_pig";
                        Instance.GetComponent<BadPig>().codeName_sprite = "SoldierPig";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //corporal
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Corporal Pig", // Name.
                    NameToOrderByOverride = "2 6", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The leader of Pig Kingdom Army. He will bravely attack all the birds in sight but to be brutally murdered by them.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetpigH.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Cop.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 12;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "corporal";
                        Instance.GetComponent<BadPig>().codeName_sprite = "Cop";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Brave);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //chef
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Chef", // Name.
                    NameToOrderByOverride = "2 7", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The villan of villans. He wants to trait his king and take his crown. But he's still a great cook.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetchef.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 2f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 2f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Chef.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 20;
                        Instance.GetComponent<BadPig>().personality = BadPig.Personality.Coward;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "chef";
                        Instance.GetComponent<BadPig>().codeName_sprite = "Chef";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Coward);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //foreman
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Foreman Pig", // Name.
                    NameToOrderByOverride = "2 8", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The most smart engineer of The Pig Kindom.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetforeman.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Foreman.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 30;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "moustache";
                        Instance.GetComponent<BadPig>().codeName_sprite = "Foreman";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Coward);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //king
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "King Pig", // Name.
                    NameToOrderByOverride = "2 9", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The most famous example of problem of succession of power. Still wonders taste of the eggs.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetking.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 6f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 6f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 6f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("King.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        Instance.GetComponent<BadPig>().hp = 50;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "king";
                        Instance.GetComponent<BadPig>().codeName_sprite = "King";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Coward);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //el porkador
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "El Porkador", // Name.
                    NameToOrderByOverride = "2 9 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The biggest pig of The Pig Island. He is birdproof and can't move by himself.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetel.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 128f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 128f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 128f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("El.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        if (!Instance.GetComponent<BadPig>())
                            Instance.AddComponent<BadPig>();
                        if (!Instance.GetComponent<ElPorkador>())
                            Instance.AddComponent<ElPorkador>();
                        Instance.GetComponent<BadPig>().hp = 100;
                        Instance.GetComponent<BadPig>().personality = BadPig.Personality.Idle;
                        Instance.GetComponent<BadPig>().torque = 2;
                        Instance.GetComponent<BadPig>().isNormallyInvincible = true;
                        Instance.GetComponent<BadPig>().mouth = Instance.AddComponent<AudioSource>();
                        Instance.GetComponent<BadPig>().codeName = "el";
                        Instance.GetComponent<BadPig>().codeName_sprite = "El";
                        Instance.GetComponent<BadPig>().SetPersonality(ABcharacters.Personality.Idle);
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //planet pig
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "The Planet Pig", // Name.
                    NameToOrderByOverride = "2 9 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "An alien pig that is so big that he has his own gravitational field and atmosphere.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetplanet.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        foreach (Collider2D c in Instance.GetComponents<Collider2D>())
                            Collider2D.Destroy(c);
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 80000f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 80000f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 80000f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Planet.png", 1f);
                        if(!Instance.GetComponent<CircleCollider2D>())
                            Instance.AddComponent<CircleCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Rubber");
                        if (!Instance.GetComponent<PlanetPig>())
                            Instance.AddComponent<PlanetPig>();
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //yaman
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Rod"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Yaman", // Name.
                    NameToOrderByOverride = "2 9 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "You found him.",// Description
                    CategoryOverride = ModAPI.FindCategory("None"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetyaman.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<BoxCollider2D>().enabled = false;
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 3000f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 3000f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 3000f;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Yaman.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Gorse");
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //egg
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "The egg", // Name.
                    NameToOrderByOverride = "3 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "The Eggs they are looking for. Somehow they haven't hatched for over 10 years and haven't been broken.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetegg.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 0.2f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 0.2f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 0.2f; //Zooi's made a mistake.
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Egg.png", 1f);
                        Instance.AddComponent<PolygonCollider2D>();
                        if(Instance.GetComponent<Egg>())
                            Instance.AddComponent<Egg>();

                        Instance.transform.tag = "Player";
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //slingshot
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Slingshot", // Name.
                    NameToOrderByOverride = "3 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "Functioning slingshot that launches birds.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetsling.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 8f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 8f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 8f; //Zooi's made a mistake.
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Slingshot_body.png", 1f);
                        if (!Instance.GetComponent<Slingshot>())
                            Instance.AddComponent<Slingshot>();
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.transform.tag = "Player";
                    }
                }
            );
			//slingshot x
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Brick"),
                    NameOverride = "Slingshot X",
                    NameToOrderByOverride = "3 2",   // right after the old Slingshot ("3 1")
                    DescriptionOverride = "A slingshot that catches any bird that touches it and launches them all in one direction at the same speed. Press X to activate their powers.",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),
                    ThumbnailOverride = ModAPI.LoadSprite("yeetsling.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 8f;
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Slingshot.png", 1f);
 
                        // Swap the brick's box collider for one that matches the sprite
                        UnityEngine.Object.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.AddComponent<PolygonCollider2D>();
 
                        if (!Instance.GetComponent<SlingshotX>())
                            Instance.AddComponent<SlingshotX>();
 
                        Instance.transform.tag = "Player";
                    }
                }
            );
            //crown
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Bowling Pin"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Crown", // Name.
                    NameToOrderByOverride = "3 3", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "It's King Pig's.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetcrown.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 0.1f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 0.1f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 0.1f; //Zooi's made a mistake.
                        Instance.transform.localScale = new Vector2(2f, 2f); //The scale of the object itself.
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("Crown.png", 2f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        Instance.GetComponent<SpriteRenderer>().sortingOrder = 1;
                        Instance.transform.tag = "Player";

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );
            //hat
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Bowling Pin"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Chef Hat", // Name.
                    NameToOrderByOverride = "3 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "It's Chef's.",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Mod"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeetchefhat.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 0.1f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 0.1f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 0.1f; //Zooi's made a mistake.
                        Instance.transform.localScale = new Vector2(2f, 2f); //The scale of the object itself.
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("chefhat.png", 2f);
                        Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Soft");
                        Instance.GetComponent<SpriteRenderer>().sortingOrder = 1;
                        Instance.transform.tag = "Player";

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );
            //glassblock
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Glass Pane"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Glass Block", // Name.
                    NameToOrderByOverride = "3 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("glassthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("glasspole.png", 1f);
                        if(!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Glass;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glasspole
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Glass Pane"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Glass Pole", // Name.
                    NameToOrderByOverride = "3 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("glassthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 4f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 4f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 4f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("glassstick.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Glass;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glassbox
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Glass Pane"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Glass Box", // Name.
                    NameToOrderByOverride = "3 3", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("glassthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 6f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 6f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 6f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("glassbox.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Glass;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glasstriangle
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Glass Pane"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Glass Triangle", // Name.
                    NameToOrderByOverride = "3 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("glassthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 6f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 6f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 6f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("glassilluminati.png", 1f);
                        if (!Instance.GetComponent<PolygonCollider2D>())
                            Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Glass;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glassrock
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Glass Pane"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Glass Circle", // Name.
                    NameToOrderByOverride = "3 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("glassthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 16f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 16f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 16f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("glassrock.png", 1f);
                        if (!Instance.GetComponent<CircleCollider2D>())
                            Instance.AddComponent<CircleCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Glass");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Glass;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );














            //wood1
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Wooden Block", // Name.
                    NameToOrderByOverride = "4 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("woodthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        foreach (BoxCollider2D e in Instance.GetComponents<BoxCollider2D>())
                            GameObject.Destroy(e);
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 12f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 12f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 12f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("woodblock.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Wood;
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glasspole
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Wooden Stick", // Name.
                    NameToOrderByOverride = "4 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("woodthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        foreach (BoxCollider2D e in Instance.GetComponents<BoxCollider2D>())
                            GameObject.Destroy(e);
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 12f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 12f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 12f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("woodpole.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Wood;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glassbox
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Wooden Box", // Name.
                    NameToOrderByOverride = "4 3", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("woodthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        foreach (BoxCollider2D e in Instance.GetComponents<BoxCollider2D>())
                            GameObject.Destroy(e);
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 18f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 18f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 18f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("woodbox.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Wood;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glasstriangle
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Wooden Triangle", // Name.
                    NameToOrderByOverride = "4 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("woodthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        foreach (BoxCollider2D e in Instance.GetComponents<BoxCollider2D>())
                            GameObject.Destroy(e);
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 18f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 18f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 18f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("woodtri.png", 1f);
                        if (!Instance.GetComponent<PolygonCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Wood;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glassrock
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Wooden Circle", // Name.
                    NameToOrderByOverride = "4 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("woodthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        foreach (BoxCollider2D e in Instance.GetComponents<BoxCollider2D>())
                            GameObject.Destroy(e);
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 48f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 48f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 48f; //Zooi's made a mistake.

                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("woodcircle.png", 1f);
                        if (!Instance.GetComponent<CircleCollider2D>())
                            Instance.AddComponent<CircleCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Wood;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );









            //wood1
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stone Block", // Name.
                    NameToOrderByOverride = "5 1", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("stonethumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 20f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 20f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 20f; //Zooi's made a mistake.
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("stoneblock.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Brick");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Stone;
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glasspole
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stone Stick", // Name.
                    NameToOrderByOverride = "5 2", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("stonethumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 20f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 20f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 20f; //Zooi's made a mistake.
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("stonepole.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Brick");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Stone;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // This rotates the magnet to sit up instead of facing right.
                        };
                    }
                }
            );

            //glassbox
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stone Box", // Name.
                    NameToOrderByOverride = "5 3", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("stonethumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 30f; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 30f; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 30f; //Zooi's made a mistake.
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("stonebox.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Brick");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Stone;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glasstriangle
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stone Triangle", // Name.
                    NameToOrderByOverride = "5 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("stonethumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 30; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 30; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 30; //Zooi's made a mistake.
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("stonetri.png", 1f);
                        if (!Instance.GetComponent<PolygonCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Brick");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Stone;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            //everything inside this delegate will be executed
                            //show a notification to the player
                        };
                    }
                }
            );

            //glassrock
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "Stone Circle", // Name.
                    NameToOrderByOverride = "5 4", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("stonethumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        //GameObject.Destroy(Instance.GetComponent<BoxCollider2D>());
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 80; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 80; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 80; //Zooi's made a mistake.
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("stonecircle.png", 1f);
                        if (!Instance.GetComponent<CircleCollider2D>())
                            Instance.AddComponent<CircleCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Brick");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Stone;

                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {

                        };
                    }
                }
            );
			
			            //iron iron block
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"),
                    NameOverride = "Iron Block",
                    NameToOrderByOverride = "6 1",   // after the stone blocks ("5 x")
                    DescriptionOverride = "",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),
                    ThumbnailOverride = ModAPI.LoadSprite("ironthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 40f;       // 2x the stone block's
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 40f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 40f;
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("ironblock.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Iron;
 
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // sits upright
                        };
                    }
                }
            );
 
            //iron iron stick
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"),
                    NameOverride = "Iron Stick",
                    NameToOrderByOverride = "6 2",   // after the stone blocks ("5 x")
                    DescriptionOverride = "",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),
                    ThumbnailOverride = ModAPI.LoadSprite("ironthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 40f;       // 2x the stone block's
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 40f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 40f;
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("ironpole.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Iron;
 
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            Instance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // sits upright
                        };
                    }
                }
            );
 
            //iron iron box
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"),
                    NameOverride = "Iron Box",
                    NameToOrderByOverride = "6 3",   // after the stone blocks ("5 x")
                    DescriptionOverride = "",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),
                    ThumbnailOverride = ModAPI.LoadSprite("ironthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 60f;       // 2x the stone block's
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 60f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 60f;
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("ironbox.png", 1f);
                        if (!Instance.GetComponent<BoxCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Iron;
 
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            // nothing extra
                        };
                    }
                }
            );
 
            //iron iron triangle
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"),
                    NameOverride = "Iron Triangle",
                    NameToOrderByOverride = "6 4",   // after the stone blocks ("5 x")
                    DescriptionOverride = "",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),
                    ThumbnailOverride = ModAPI.LoadSprite("ironthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 60f;       // 2x the stone block's
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 60f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 60f;
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("irontri.png", 1f);
                        if (!Instance.GetComponent<PolygonCollider2D>() || !Instance.GetComponent<Collider2D>().enabled)
                            Instance.AddComponent<PolygonCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Iron;
 
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            // nothing extra
                        };
                    }
                }
            );
 
            //iron iron circle
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Small Boulder"),
                    NameOverride = "Iron Circle",
                    NameToOrderByOverride = "6 5",   // after the stone blocks ("5 x")
                    DescriptionOverride = "",
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),
                    ThumbnailOverride = ModAPI.LoadSprite("ironthumb.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 160f;       // 2x the stone block's
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 160f;
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 160f;
                        Instance.GetComponent<RandomSpriteBehaviour>().enabled = false;
                        Instance.GetComponent<Collider2D>().enabled = false;
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("ironcircle.png", 1f);
                        if (!Instance.GetComponent<CircleCollider2D>())
                            Instance.AddComponent<CircleCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Metal");
                        if (!Instance.GetComponent<Block>())
                            Instance.AddComponent<Block>();
                        Instance.GetComponent<Block>().material = Block.Material.Iron;
 
                        Instance.AddComponent<UseEventTrigger>().Action = () =>
                        {
                            // nothing extra
                        };
                    }
                }
            );


            //TNT
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Dynamite"), // Searches the ingame catalog and returns with the inputted string if possible.
                    NameOverride = "TNT", // Name.
                    NameToOrderByOverride = "6 6", // How the item is ordered (I didn't want it in the 'G' section, so I renamed it to Brick 2, so it shows up next to the brick.)
                    DescriptionOverride = "",// Description
                    CategoryOverride = ModAPI.FindCategory("Angry Birds Blocks"),//Category
                    ThumbnailOverride = ModAPI.LoadSprite("yeettnt.png", 5f),
                    AfterSpawn = (Instance) =>
                    {
                        Instance.GetComponent<PhysicalBehaviour>().InitialMass = 8; //All of these are mass,
                        Instance.GetComponent<PhysicalBehaviour>().TrueInitialMass = 8; //I wish I could tell you why,
                        Instance.GetComponent<PhysicalBehaviour>().rigidbody.mass = 8; //Zooi's made a mistake.
                        Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("TNT.png", 1f);
                        Instance.AddComponent<BoxCollider2D>();
                        Instance.GetComponent<PhysicalBehaviour>().Properties = ModAPI.FindPhysicalProperties("Wood");

                        Instance.GetComponent<ExplosiveBehaviour>().Range = 18f;
                        Instance.GetComponent<ExplosiveBehaviour>().ImpactForceThreshold = 18f;
                    }
                }
            );
        }
    }
}
public class CategoryBuilder
{
    /// <summary>
    /// Use this method to create your own category.
    /// </summary>
    /// <param name="name">New category name</param>
    /// <param name="description">Description of the new category</param>
    /// <param name="icon">New category icon</param>
    public static void Create(string name, string description, Sprite icon)
    {
        CatalogBehaviour manager = UnityEngine.Object.FindObjectOfType<CatalogBehaviour>();
        if (manager.Catalog.Categories.FirstOrDefault((Category c) => c.name == name) == null)
        {
            Category category = ScriptableObject.CreateInstance<Category>();
            category.name = name;
            category.Description = description;
            category.Icon = icon;
            Category[] NewCategories = new Category[manager.Catalog.Categories.Length + 1];
            Category[] categories = manager.Catalog.Categories;
            for (int i = 0; i < categories.Length; i++)
            {
                NewCategories[i] = categories[i];
            }
            NewCategories[NewCategories.Length - 1] = category;
            manager.Catalog.Categories = NewCategories;
        }
    }

    //Made in USSR
    //AZULE
}
// Originally uploaded by 'Lee Myung-Bak'. Do not reupload without their explicit permission.
