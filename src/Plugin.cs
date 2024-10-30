using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using BepInEx;
using UnityEngine;

namespace NewTerra;

[BepInPlugin(MOD_ID, "New Terra", "0.1.0")]
[BepInDependency("thalber.blackglare", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("io.github.dual.fisobs")]
public class Plugin : BaseUnityPlugin
{
	internal const string MOD_ID = "qtpi.new-terra";
	internal const string POM_CATEGORY = "NEW_TERRA";
	internal const string TARDIGOATED_ID = "Tenacious";
	

	internal readonly Dictionary<string, string[]> DecalAutoplaceSets = new();
	internal string? currWeather;

	public void OnEnable()
	{
		
		On.PlayerGraphics.ctor += PlayerGraphics_ctor;
		On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
		On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
		On.Creature.Violence += Creature_Violence;
		On.Player.Update += Player_Update;
		On.Player.ctor += Player_ctor;
		On.Player.TerrainImpact += Player_TerrainImpact;
		On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
		On.World.ctor += World_ctor;
		On.RoomSettings.LoadEffects += RoomSettings_LoadEffects;
		On.RoomRain.Update += RoomRain_Update;
		try
		{
			GlangleFruit.Apply();
			__SwitchToBepinexLogger(Logger);
			_AddBGLabels();
		}
		catch (FileNotFoundException)
		{
			Logger.LogWarning("BlackGlare not loaded, labels inaccessible");
		}
		catch (Exception ex)
		{
			Logger.LogFatal(ex);
		}

	}

	private void _AddBGLabels()
	{
		BlackGlare.API.Labels.AddObjectLabel<Player>((p) => "Goated").AddCondition((p) => p.SlugCatClass.value == TARDIGOATED_ID);
		// BlackGlare.API.Labels.AddObjectLabel<GlangleFruit>((f) => $"yummers x{f.GAbs.bitesLeft}");
	}

	#region misc hooks

	private void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
	{
		orig(self, eu);
		if (self.room.roomSettings.name.StartsWith("RU_"))
		{
			self.floodingSound.Volume = 0;
		}
	}

	private void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
	{
		orig(room);
		if (room.abstractRoom.name == "AW_E01")
		{
			if (room.game.IsStorySession && room.game.GetStorySession.saveState.cycleNumber == 0 && room.abstractRoom.firstTimeRealized)
			{
				room.AddObject(new AW_TenaciousStart(room));
			}
		}
	}

	#endregion

	#region damage resistance
	private void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
	{
		if (self != null && self.SlugCatClass.value == TARDIGOATED_ID)
		{
			self.immuneToFallDamage = 1;
			if (speed > 60f)
			{
				self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
				self.Stun((int)Mathf.Lerp(40f, 120f, speed));
			}
		}
		orig(self, chunk, direction, speed, firstContact);
	}

	private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);
		if (self != null && self.SlugCatClass.value == TARDIGOATED_ID)
		{
			abstractCreature.lavaImmune = true;
		}
	}

	private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
		if (self != null && self.SlugCatClass.value == TARDIGOATED_ID)
		{
			self.Hypothermia -= 0.75f * self.HypothermiaGain;
		}
	}

	private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
	{
		if (self is Player player && player != null && player.SlugCatClass.value == TARDIGOATED_ID)
		{
			if (type == Creature.DamageType.Blunt)
			{
				damage *= 0.25f;
			}
			else if (type == Creature.DamageType.Electric)
			{
				damage *= 0.25f;
			}
			else if (type == Creature.DamageType.Explosion)
			{
				damage *= 0.75f;
			}
			else
			{
				damage *= 1.25f;
			}
		}
		orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
	}
	#endregion

	#region graphics
	private void LoadResources(RainWorld rainWorld)
	{
		DecalAutoplaceSets.Clear();
		foreach (string fname in AssetManager.ListDirectory("decalsets", false, true)) {
			System.IO.FileInfo file = new(fname);
			if (file.Extension is not ".txt") continue;
			DecalAutoplaceSets.Add(file.Name[..^4], System.IO.File.ReadAllLines(fname));
		}
		try
		{
			Futile.atlasManager.LoadAtlas("atlases/body");
			Futile.atlasManager.LoadAtlas("atlases/face");
			Futile.atlasManager.LoadAtlas("atlases/head");
			Futile.atlasManager.LoadAtlas("atlases/hips");
			Futile.atlasManager.LoadAtlas("atlases/legs");
			Futile.atlasManager.LoadAtlas("atlases/arm");
			Futile.atlasManager.LoadAtlas("atlases/NT_VultureMasks");
			Futile.atlasManager.LoadAtlas("atlases/DangleSeed");
		}
		catch (Exception ex)
		{
			Logger.LogError("Error on resource load");
			Logger.LogError(ex);
		}

	}

	private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self.player != null && self.player.SlugCatClass.value == TARDIGOATED_ID)
		{
			string? name = sLeaser.sprites[0]?.element?.name;
			if (name != null && name.StartsWith("BodyA") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[0].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[1]?.element?.name;
			if (name != null && name.StartsWith("HipsA") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[1].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[3]?.element?.name;
			if (name != null && name.StartsWith("HeadA") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[3].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[4]?.element?.name;
			if (name != null && name.StartsWith("LegsA") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[4].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[5]?.element?.name;
			if (name != null && name.StartsWith("PlayerArm") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[5].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[6]?.element?.name;
			if (name != null && name.StartsWith("PlayerArm") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[6].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[7]?.element?.name;
			if (name != null && name.StartsWith("OnTopOfTerrainHand") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[7].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[8]?.element?.name;
			if (name != null && name.StartsWith("OnTopOfTerrainHand") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[8].SetElementByName("Tardi" + name);
			}
			name = sLeaser.sprites[9]?.element?.name;
			if (name != null && name.StartsWith("Face") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
			{
				sLeaser.sprites[9].SetElementByName("Tardi" + name);
			}
		}
	}

	private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
	{
		orig(self, ow);
		if (self.player != null && self.player.SlugCatClass.value == TARDIGOATED_ID)
		{
			if (self.RenderAsPup)
			{
				self.tail[0] = new(self, 9f, 0.75f, null, 0.85f, 1.0f, 1.0f, true);
				self.tail[1] = new(self, 8f, 1.25f, self.tail[0], 0.85f, 1.0f, 0.5f, true);
				self.tail[2] = new(self, 6f, 1.25f, self.tail[1], 0.85f, 1.0f, 0.5f, true);
				self.tail[3] = new(self, 3f, 1f, self.tail[2], 0.85f, 1.0f, 0.5f, true);
			}
			else
			{
				self.tail[0] = new(self, 9f, 1.5f, null, 0.85f, 1.0f, 1.0f, true);
				self.tail[1] = new(self, 8f, 2.5f, self.tail[0], 0.85f, 1.0f, 0.5f, true);
				self.tail[2] = new(self, 6f, 2.5f, self.tail[1], 0.85f, 1.0f, 0.5f, true);
				self.tail[3] = new(self, 3f, 2f, self.tail[2], 0.85f, 1.0f, 0.5f, true);
			}

			var bp = self.bodyParts.ToList();
			bp.RemoveAll(x => x is TailSegment);
			bp.AddRange(self.tail);
			self.bodyParts = bp.ToArray();
		}

	}

	#endregion

	#region weather

        private void RoomSettings_LoadEffects(On.RoomSettings.orig_LoadEffects orig, RoomSettings self, string[] s)
        {
            orig(self, s);
            if (self.name.StartsWith("RU_") || self.name.StartsWith("AW_"))
            {
                switch (currWeather)
                {
                    case "1":
                        self.Clouds = 0.1f;
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.15f, false));
                        break;
                    case "2":
                        self.Clouds = 0.6f;
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Bloom, 0.2f, false));
                        break;
                    case "3":
                        self.Clouds = 0.9f;
                        self.CeilingDrips = 0.7f;
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 0.8f, false));
                        break;
                    case "4":
                        self.Clouds = 1f;
                        self.CeilingDrips = 1f;
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 1f, false));
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.HeavyRain, 0.05f, false));
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.BkgOnlyLightning, 1f, false));
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.ExtraLoudThunder, 1f, false));
                        self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0.2f, false));
                        break;
                }
            }
            //if (self.name.StartsWith("DB_"))
            //{
            //    if (ModManager.MSC && self.DangerType == RoomRain.DangerType.AerieBlizzard)
            //    {
            //        self.DangerType = MoreSlugcatsEnums.RoomRainDangerType.Blizzard;
            //    }
            //}

        }

	private void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
	{

		orig(self, game, region, name, singleRoomWorld);
		List<string> weatherpatterns = new List<string>
		{
			"1", "1", "2", "2", "3", "4"
		};

		System.Random rnd = new System.Random();

		if (game != null && game.IsStorySession)
		{
			if (game.GetStorySession.saveState.cycleNumber == 0)
			{
				currWeather = "2";
			}
			else
			{
				currWeather = weatherpatterns[rnd.Next(weatherpatterns.Count)];
			}
		}
	}

	#endregion

	#region music



	#endregion
}
