using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using BepInEx;
using BepInEx.Logging;
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

	internal static ManualLogSource logger;

	public static ConditionalWeakTable<Player, TardiData> tardiCWT = new();

	public void OnEnable()
	{
		logger = Logger;
		On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
		On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
		PlayerHooks playerHooks = new();
		WorldHooks worldHooks = new();
		try
		{
			playerHooks.Apply();
			worldHooks.Apply();
			GlangleFruit.Apply();
			__SwitchToBepinexLogger(Logger);
			_AddBGLabels();
		}
		catch (FileNotFoundException)
		{
			logger.LogWarning("BlackGlare not loaded, labels inaccessible");
		}
		catch (Exception ex)
		{
			logger.LogFatal(ex);
		}
		logger.LogWarning("boy pussy and boobs");
	}

	private void _AddBGLabels()
	{
		//BlackGlare.API.Labels.AddObjectLabel<Player>((p) => "Goated").AddCondition((p) => p.SlugCatClass.value == TARDIGOATED_ID);
		// BlackGlare.API.Labels.AddObjectLabel<GlangleFruit>((f) => $"yummers x{f.GAbs.bitesLeft}");
	}

	#region misc hooks

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

	#region graphics
	private void LoadResources(RainWorld rainWorld)
	{
		try
		{
			Futile.atlasManager.LoadAtlas("atlases/NT_VultureMasks");
			Futile.atlasManager.LoadAtlas("atlases/DangleSeed");
			Futile.atlasManager.LoadAtlas("atlases/body");
			Futile.atlasManager.LoadAtlas("atlases/face");
			Futile.atlasManager.LoadAtlas("atlases/head");
			Futile.atlasManager.LoadAtlas("atlases/hips");
			Futile.atlasManager.LoadAtlas("atlases/legs");
			Futile.atlasManager.LoadAtlas("atlases/arm");
		}
		catch (Exception ex)
		{
			Logger.LogError("Error on resource load");
			Logger.LogError(ex);
		}

	}

	#endregion
}
