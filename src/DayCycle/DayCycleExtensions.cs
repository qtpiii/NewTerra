using System.Runtime.CompilerServices;
using UnityEngine;

namespace NewTerra.DayCycle;

public static class DayCycleExtensions
{
	public class RoomSettingsExtension
	{
		public int?[,] palettes = new int?[2, 4]; // first dimension: 0, main; 1, fade. second dimension: 0, dawn; 1, day; 2, dusk; 3, night.
		public float[,] paletteIntensities = new float[2, 4]; // same layout as the one above
		public float time;
		public bool timeOverride;
	}
	public static ConditionalWeakTable<RoomSettings, RoomSettingsExtension> settingsExtensionTable = new();

	public class RoomCameraExtension
	{
		public Texture2D[,] paletteTextures = new Texture2D[2, 4];

		public void ResetPaletteTextures(RoomCamera self, RoomSettingsExtension settingsExt)
		{
			for (int i = 0; i < 4; i++)
			{
				if (settingsExt.palettes[0, i] != null)
				{
					self.LoadPalette(settingsExt.palettes[0, i].Value, ref paletteTextures[0, i]);
				}
				if (settingsExt.palettes[1, i] != null)
				{
					self.LoadPalette(settingsExt.palettes[1, i].Value, ref paletteTextures[1, i]);
				}
			}
		}
		
		// https://www.desmos.com/calculator/ppal3bkmri
		public float[] PaletteIntensityAtTime(float t)
		{
			t %= 1f;
			float[] ret = new float[4];

			ret[0] = 1f - Mathf.Pow(7f * t - 1.75f, 2f); // dawn
			ret[1] = 1f - Mathf.Pow(4f * t - 2f, 4f); // day
			ret[2] = 1f - Mathf.Pow(7f * t - (7f - 1.75f), 2f); // dusk
			ret[3] = Mathf.Max(1f - Mathf.Pow(4f * t, 4f), 1f - Mathf.Pow(4f * t - 4f, 4f)); // night

			for (int i = 0; i < 4; i++)
			{
				ret[i] = Mathf.Max(ret[i], 0f);
			}
			
			return ret;
		}
	}
	public static ConditionalWeakTable<RoomCamera, RoomCameraExtension> cameraExtensionTable = new();
}
