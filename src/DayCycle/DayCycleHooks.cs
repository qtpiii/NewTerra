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
		On.RoomCamera.ChangeRoom += RoomCameraOnChangeRoom;
		On.RoomCamera.ModifyEffectColorA += RoomCameraOnModifyEffectColorA;
		On.RoomCamera.ModifyEffectColorB += RoomCameraOnModifyEffectColorB;
		
		On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPageOnctor; // adds the DayCyclePanel to the room settings page
		
		// saving and loading of ext data
		On.RoomSettings.Save_string_bool += RoomSettingsOnSave_string_bool;
		On.RoomSettings.Load_Timeline += RoomSettingsOnLoad_Timeline;
	}

	private static Color[] RoomCameraOnModifyEffectColorA(On.RoomCamera.orig_ModifyEffectColorA orig, RoomCamera self, Color[] colors)
	{
		var settingsExt = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self.room.roomSettings);
		Color[] origReturn = orig(self, colors);
		
		float[] intensitiesAtTime;
		if (settingsExt.timeOverride)
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(settingsExt.time);
		}
		else
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(self.game.Time());
		}
		
		for (int i = 0; i < origReturn.Length; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				origReturn[i] = Color.Lerp(origReturn[i], settingsExt.effectColors[0, j], settingsExt.effectColorIntensities[0, j] * intensitiesAtTime[j]);
			}
		}
		return origReturn;
	}
	
	private static Color[] RoomCameraOnModifyEffectColorB(On.RoomCamera.orig_ModifyEffectColorB orig, RoomCamera self, Color[] colors)
	{
		var settingsExt = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self.room.roomSettings);
		Color[] origReturn = orig(self, colors);
		
		float[] intensitiesAtTime;
		if (settingsExt.timeOverride)
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(settingsExt.time);
		}
		else
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(self.game.Time());
		}
		
		for (int i = 0; i < origReturn.Length; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				origReturn[i] = Color.Lerp(origReturn[i], settingsExt.effectColors[1, j], settingsExt.effectColorIntensities[1, j] * intensitiesAtTime[j]);
			}
		}
		return origReturn;
	}

	#region room settings
	private static void RoomSettingsOnSave_string_bool(On.RoomSettings.orig_Save_string_bool orig, RoomSettings self, string path, bool saveAsTemplate)
	{
		orig(self, path, saveAsTemplate);
		
		var ext = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self);
		
		string originalText = File.ReadAllText(path);
		using StreamWriter writer = File.CreateText(path);
		writer.Write(originalText);
		
		if (ext.time != 0f)
		{
			writer.WriteLine($"Time: {ext.time}");
		}

		StringBuilder palettes = new();
		for (int i = 0; i < ext.palettes.GetLength(1); i++)
		{
			if (ext.palettes[0, i] != null)
			{
				palettes.Append($"main_{i},{ext.palettes[0, i]},{ext.paletteIntensities[0, i]};");
			}
		}
		for (int i = 0; i < ext.palettes.GetLength(1); i++)
		{
			if (ext.palettes[1, i] != null)
			{
				palettes.Append($"fade_{i},{ext.palettes[1, i]},{ext.paletteIntensities[1, i]};");
			}
		}
		if (palettes.ToString() != "")
		{
			writer.WriteLine($"DayCyclePalettes: {palettes}");
		}

		StringBuilder effectColors = new();
		for (int i = 0; i < 4; i++)
		{
			effectColors.Append($"a_{i},{ext.effectColors[0, i].r},{ext.effectColors[0, i].g},{ext.effectColors[0, i].b},{ext.effectColorIntensities[0, i]};");
		}
		for (int i = 0; i < 4; i++)
		{
			effectColors.Append($"b_{i},{ext.effectColors[1, i].r},{ext.effectColors[1, i].g},{ext.effectColors[1, i].b},{ext.effectColorIntensities[1, i]};");
		}
		writer.WriteLine($"DayCycleEffectColors: {effectColors}");
	}

	private static bool RoomSettingsOnLoad_Timeline(On.RoomSettings.orig_Load_Timeline orig, RoomSettings self, SlugcatStats.Timeline timelinepoint)
	{
		bool origReturn = orig(self, timelinepoint);

		if (!origReturn) // if the original method failed for whatever reason
		{
			return false;
		}

		var ext = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self);
		
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
					for (int j = 0; j < split.Length - 1; j++) // -1 because theres always a trailing ;
					{
						string[] keyValue = split[j].Split(','); // 0 is identifier, 1 is palette id, 2 is intensity
						if (keyValue.Length != 3) continue;
						if (keyValue[0].StartsWith("main_"))
						{
							// index 5 of the identifier should always be an integer (eg: fade_2)
							ext.palettes[0, int.Parse(keyValue[0][5].ToString())] = int.Parse(keyValue[1]);
							ext.paletteIntensities[0, int.Parse(keyValue[0][5].ToString())] = float.Parse(keyValue[2]);
						}
						if (keyValue[0].StartsWith("fade_"))
						{
							ext.palettes[1, int.Parse(keyValue[0][5].ToString())] = int.Parse(keyValue[1]);
							ext.paletteIntensities[1, int.Parse(keyValue[0][5].ToString())] = float.Parse(keyValue[2]);
						}
					}
					break;
				}
				case "DayCycleEffectColors":
				{
					string[] split = keyValuePairs[i][1].Split(';');
					for (int j = 0; j < split.Length - 1; j++) // -1 because theres always a trailing ;
					{
						string[] keyValue = split[j].Split(','); // 0 is identifier, 1 is palette id, 2 is intensity
						if (keyValue.Length != 5) continue;
						if (keyValue[0].StartsWith("a_"))
						{
							// index 2 of the identifier should always be an integer (eg: a_3)
							ext.effectColors[0, int.Parse(keyValue[0][2].ToString())] = new Color(float.Parse(keyValue[1]), float.Parse(keyValue[2]), float.Parse(keyValue[3]));
							ext.effectColorIntensities[0, int.Parse(keyValue[0][2].ToString())] = float.Parse(keyValue[4]);
						}
						if (keyValue[0].StartsWith("b_"))
						{
							ext.effectColors[1, int.Parse(keyValue[0][2].ToString())] = new Color(float.Parse(keyValue[1]), float.Parse(keyValue[2]), float.Parse(keyValue[3]));
							ext.effectColorIntensities[1, int.Parse(keyValue[0][2].ToString())] = float.Parse(keyValue[4]);
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
		
		self.subNodes.Add(new DayCyclePanel(owner, "DayCycle_Panel", self, new Vector2(1030f, 540f)));
	}
	#endregion
	
	#region palette manipulation
	private static void RoomCameraOnChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
	{
		orig(self, newRoom, cameraPosition);
		
		var settingsExt = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self.room.roomSettings);
		var cameraExt = DayCycleExtensions.cameraExtensionTable.GetOrCreateValue(self);
		cameraExt.ResetPaletteTextures(self, settingsExt);
	}

	private static void RoomCameraOnUpdate(On.RoomCamera.orig_Update orig, RoomCamera self)
	{
		orig(self);

		var settingsExt = DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(self.room.roomSettings);
		var cameraExt = DayCycleExtensions.cameraExtensionTable.GetOrCreateValue(self);
		float[] intensitiesAtTime;
		if (settingsExt.timeOverride)
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(settingsExt.time);
		}
		else
		{
			intensitiesAtTime = DayCycleExtensions.PaletteIntensityAtTime(self.game.clock * 0.0001f);
		}
		
		self.LoadPalette(self.paletteA, ref self.fadeTexA);
		self.LoadPalette(self.paletteB, ref self.fadeTexB);
		for (int i = 0; i < 4; i++)
		{
			for (int x = 0; x < 32; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					if (cameraExt.paletteTextures[0, i] != null)
					{
						self.fadeTexA.SetPixel(x, y, Color.Lerp(self.fadeTexA.GetPixel(x, y), cameraExt.paletteTextures[0, i].GetPixel(x, y), settingsExt.paletteIntensities[0, i] * intensitiesAtTime[i]));
					}

					if (cameraExt.paletteTextures[1, i] != null)
					{
						self.fadeTexB.SetPixel(x, y, Color.Lerp(self.fadeTexB.GetPixel(x, y), cameraExt.paletteTextures[1, i].GetPixel(x, y), settingsExt.paletteIntensities[1, i] * intensitiesAtTime[i]));
					}
				}
			}
		}
		self.fadeTexA.Apply();
		self.fadeTexB.Apply();
		self.ApplyEffectColorsToAllPaletteTextures(self.room.roomSettings.EffectColorA, self.room.roomSettings.EffectColorB);
	}
	#endregion
}
