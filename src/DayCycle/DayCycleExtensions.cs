using System.Runtime.CompilerServices;

namespace NewTerra.DayCycle;

public static class DayCycleExtensions
{
	public class RoomSettingsExtension
	{
		public int?[,] dayCyclePalettes = new int?[2, 4]; // first dimension: 0, main; 1, fade. second dimension: 0, dawn; 1, day; 2, dusk; 3, night.
		public float[,] dayCyclePaletteIntensities = new float[2, 4]; // same layout as the one above
		public float time;
	}
	public static ConditionalWeakTable<RoomSettings, RoomSettingsExtension> extensionTable = new();
}
