using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NewTerra.Enums;

namespace NewTerra
{
	public class WorldHooks
	{

		public CurrWeather currWeather;

		public void OnEnable()
		{
			try
			{
				On.World.ctor += World_ctor;
				On.RoomSettings.LoadEffects += RoomSettings_LoadEffects;
				On.RoomRain.Update += RoomRain_Update;

				On.Music.ProceduralMusic.ProceduralMusicInstruction.Track.AllowedInSubRegion += Track_AllowedInSubRegion;
			}
			catch(Exception ex)
			{
				Plugin.logger.LogFatal(ex);
			}
		}

		private bool Track_AllowedInSubRegion(On.Music.ProceduralMusic.ProceduralMusicInstruction.Track.orig_AllowedInSubRegion orig, Music.ProceduralMusic.ProceduralMusicInstruction.Track self, string subRegion)
		{
			if (self.subRegions == null)
			{
				return true;
			}
			bool? regiontruth = null;
			bool? weathertruth = null;
			for (int i = 0; i < self.subRegions.Length; i++)
			{
				if (Enum.TryParse(self.subRegions[i], out CurrWeather allowedweather))
				{
					if (weathertruth != true) weathertruth = allowedweather == currWeather;
					continue;
				} //it's not a weather
				if (regiontruth != true) regiontruth = subRegion == self.subRegions[i];
			}
			return (weathertruth != false) && (regiontruth != false);
		}

		private void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
		{
			orig(self, eu);
			if (self.room.roomSettings.name.StartsWith("RU_"))
			{
				self.floodingSound.Volume = 0;
			}
		}

		private void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
		{
			orig(self, game, region, name, singleRoomWorld);
			List<CurrWeather> weatherpatterns = new List<CurrWeather>
		{
			CurrWeather.Sunny,
			CurrWeather.Sunny,
			CurrWeather.Cloudy, 
			CurrWeather.Cloudy, 
			CurrWeather.Rainy, 
			CurrWeather.Stormy
		};

			System.Random rnd = new System.Random();

			if (game != null && game.IsStorySession)
			{
				if (game.GetStorySession.saveState.cycleNumber == 0)
				{
					currWeather = CurrWeather.Cloudy;
				}
				else
				{
					currWeather = weatherpatterns[rnd.Next(weatherpatterns.Count)];
				}
				Debug.Log("Weather set to " + currWeather);
			}
		}

		private void RoomSettings_LoadEffects(On.RoomSettings.orig_LoadEffects orig, RoomSettings self, string[] s)
		{
			orig(self, s);
			if (!self.isTemplate)
			{
				if (self.name.StartsWith("RU_") || self.name.StartsWith("AW_"))
				{
					switch (currWeather)
					{
					case CurrWeather.Sunny:
						self.Clouds = 0.1f;
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.15f, false));
						break;
					case CurrWeather.Cloudy:
						self.Clouds = 0.4f;
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Bloom, 0.2f, false));
						break;
					case CurrWeather.Rainy:
						self.Clouds = 0.8f;
						self.CeilingDrips = 0.7f;
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 0.8f, false));
						break;
					case CurrWeather.Stormy:
						self.Clouds = 1f;
						self.CeilingDrips = 1f;
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 1f, false));
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.HeavyRain, 0.05f, false));
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.BkgOnlyLightning, 1f, false));
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.ExtraLoudThunder, 1f, false));
						self.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0.2f, false));
						break;
					}
					Debug.Log("Effects loaded in " + self.name + " for " + currWeather + " weather");
				}
				//if (self.name.StartsWith("DB_"))
				//{
				//    if (ModManager.MSC && self.DangerType == RoomRain.DangerType.AerieBlizzard)
				//    {
				//        self.DangerType = MoreSlugcatsEnums.RoomRainDangerType.Blizzard;
				//    }
				//}
			}
		}
	}
}
