using System;
using On.DevInterface;
using UnityEngine;
using DevUI = DevInterface.DevUI;
using DevUINode = DevInterface.DevUINode;

namespace NewTerra.DayCycle;

public class DayCycleHooks
{
	public static void Apply()
	{
		On.RoomCamera.Update += RoomCameraOnUpdate;
		
		On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPageOnctor; // adds the DayCyclePanel to the room settings page
		
		// AAAUGH
		void AttachCWT(RoomSettings self)
		{
			try
			{
				DayCycleExtensions.extensionTable.Add(self, new());
			}
			catch (ArgumentException) {}
		}
		On.RoomSettings.ctor_string_Region_bool_bool_Name += (orig, self, name, region, template, firstTemplate, playerChar) => { orig(self, name, region, template, firstTemplate, playerChar); AttachCWT(self); };
		On.RoomSettings.ctor_string_Region_bool_bool_Timeline_RainWorldGame += (orig, self, name, region, template, firstTemplate, point, game) => { orig(self, name, region, template, firstTemplate, point, game); AttachCWT(self); };
		On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += (orig, self, room, name, region, template, firstTemplate, point, game) => { orig(self, room, name, region, template, firstTemplate, point, game); AttachCWT(self); };
	}

	private static void RoomSettingsPageOnctor(RoomSettingsPage.orig_ctor orig, DevInterface.RoomSettingsPage self, DevUI owner, string id, DevUINode parentNode, string name)
	{
		orig(self, owner, id, parentNode, name);
		
		self.subNodes.Add(new DayCyclePanel(owner, "DayCycle_Panel", self, new Vector2(1030f, 600f)));
	}

	private static void RoomCameraOnUpdate(On.RoomCamera.orig_Update orig, RoomCamera self)
	{
		orig(self);

		var ext = DayCycleExtensions.extensionTable.GetOrCreateValue(self.room.roomSettings);
		
		/*
		for (int x = 0; x < 32; x++)
		{
			for (int y = 0; y < 16; y++)
			{
				self.paletteTexture.SetPixel(x, y, self.paletteTexture.GetPixel(x, y) * 0.999f);
			}
		}
		self.paletteTexture.Apply();
		*/
	}
}
