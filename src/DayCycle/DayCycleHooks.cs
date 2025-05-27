using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
		
		// saving and loading of ext data
		On.RoomSettings.Save_string_bool += RoomSettingsOnSave_string_bool;
		On.RoomSettings.Load_Timeline += RoomSettingsOnLoad_Timeline;
	}
	
	private static void RoomSettingsOnSave_string_bool(On.RoomSettings.orig_Save_string_bool orig, RoomSettings self, string path, bool saveAsTemplate)
	{
		orig(self, path, saveAsTemplate);
		
		var ext = DayCycleExtensions.extensionTable.GetOrCreateValue(self);
		
		string originalText = File.ReadAllText(path);
		using StreamWriter writer = File.CreateText(path);
		writer.Write(originalText);
		
		if (ext.time != 0f)
		{
			writer.WriteLine($"Time: {ext.time}");
		}

		StringBuilder palettes = new();
		for (int i = 0; i < ext.dayCyclePalettes.GetLength(1); i++)
		{
			if (ext.dayCyclePalettes[0, i] != null)
			{
				palettes.Append($"main_{i},{ext.dayCyclePalettes[0, i]};");
			}
		}
		for (int i = 0; i < ext.dayCyclePalettes.GetLength(1); i++)
		{
			if (ext.dayCyclePalettes[0, i] != null)
			{
				palettes.Append($"fade_{i},{ext.dayCyclePalettes[0, i]};");
			}
		}
		writer.WriteLine($"DayCyclePalettes: {palettes}");
	}

	private static bool RoomSettingsOnLoad_Timeline(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline timelinepoint)
	{
		bool origReturn = orig(self, timelinepoint);

		if (!origReturn) // if the original method failed for whatever reason
		{
			return false;
		}

		var ext = DayCycleExtensions.extensionTable.GetOrCreateValue(self);
		
		string[] rawFile = File.ReadAllLines(self.filePath);
		List<string[]> keyValuePairs = new List<string[]>();
		for (int i = 0; i < rawFile.Length; i++)
		{
			string[] keyValue = Regex.Split(rawFile[i], ": ");
			if (keyValue.Length == 2)
			{
				keyValuePairs.Add(keyValue);
			}
		}
		for (int i = 0; i < keyValuePairs.Count; i++)
		{
			switch (keyValuePairs[i][0])
			{
				case "Time":
				{
					ext.time = float.Parse(keyValuePairs[i][1]);
					break;
				}
				case "DayCyclePalettes":
				{
					string[] split = keyValuePairs[i][1].Split(';');
					for (int j = 0; j < split.Length; j++)
					{
						string[] keyValue = split[j].Split(',');
						if (keyValue[0].StartsWith("main_"))
						{
							ext.dayCyclePalettes[0, int.Parse(keyValue[0][5].ToString())] = int.Parse(keyValue[1]);
						}
					}
					break;
				}
			}
		}

		return true;
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
		{t
			for (int y = 0; y < 16; y++)
			{
				self.paletteTexture.SetPixel(x, y, self.paletteTexture.GetPixel(x, y) * 0.999f);
			}
		}
		self.paletteTexture.Apply();
		*/
	}
}
