using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NewTerra.Enums;
using MoreSlugcats;
using Mono.Cecil.Cil;
using MonoMod.Cil;

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

				On.RoomRain.Update += RoomRain_Update;

				On.Room.Loaded += Room_Loaded;

				On.Music.ProceduralMusic.ProceduralMusicInstruction.Track.AllowedInSubRegion += Track_AllowedInSubRegion;

				//IL.ScavengerTradeSpot.Update += ScavengerTradeSpot_Update;
			}
			catch(Exception ex)
			{
				Plugin.logger.LogFatal(ex);
			}
		}

		//private void ScavengerTradeSpot_Update(MonoMod.Cil.ILContext il)
		//{
		//	ILCursor c = new(il);

		//	while (true)
		//	{
		//		if (c.TryGotoNext(
		//				x => x.MatchLdfld<Room>(nameof(UpdatableAndDeletable.room)),
		//				x => x.MatchLdfld<RainWorldGame>(nameof(Room.game)),
		//				x => x.MatchLdfld<RainWorld>(nameof(RainWorldGame.rainWorld)),
		//				x => x.MatchLdfld<InGameTranslator>(nameof(RainWorld.inGameTranslator)),
		//				x => x.MatchLdstr("Scavenger Merchant")
		//			))
		//		{
		//			c.Index += 4;
		//			c.EmitDelegate((ScavengerTradeSpot self, string s) =>
		//			{
		//				if (self.room.abstractRoom.name.StartsWith("RU_"))
		//				{
		//					return "Scrounger Merchant";
		//				}
		//				else return s;
		//			});
		//		}
		//		else
		//		{
		//			break;
		//		}
		//	}
		//}

		private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);
			//if (self.roomSettings.name.StartsWith("RU_") || self.roomSettings.name.StartsWith("AW_"))
			//{
			//	switch (currWeather)
			//	{
			//	case CurrWeather.Sunny:
			//		self.roomSettings.Clouds = 0.1f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.15f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Brightness, 0.05f, false));
			//		break;
			//	case CurrWeather.Cloudy:
			//		self.roomSettings.Clouds = 0.3f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Bloom, 0.15f, false));
			//		break;
			//	case CurrWeather.Rainy:
			//		self.roomSettings.Clouds = 0.7f;
			//		self.roomSettings.CeilingDrips = 0.7f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 0.8f, false));
			//		break;
			//	case CurrWeather.Stormy:
			//		self.roomSettings.Clouds = 1f;
			//		self.roomSettings.CeilingDrips = 1f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightRain, 1f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.HeavyRain, 0.05f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.BkgOnlyLightning, 1f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.ExtraLoudThunder, 1f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0.2f, false));
			//		break;
			//	}
			//	Debug.Log("Effects loaded in " + self.roomSettings.name + " for " + currWeather + " weather");
			//}
			//if (self.roomSettings.name.StartsWith("DB_"))
			//{
			//	switch (currWeather)
			//	{
			//	case CurrWeather.Sunny:
			//		self.roomSettings.RainIntensity = 0.1f;
			//		self.roomSettings.Clouds = 0.1f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.1f, false));
			//		break;
			//	case CurrWeather.Cloudy:
			//		self.roomSettings.RainIntensity = 0.2f;
			//		self.roomSettings.Clouds = 0.3f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.05f, false));
			//		break;
			//	case CurrWeather.Rainy:
			//		self.roomSettings.RainIntensity = 0.6f;
			//		self.roomSettings.Clouds = 0.7f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Desaturation, 0.1f, false));
			//		break;
			//	case CurrWeather.Stormy:
			//		self.roomSettings.RainIntensity = 1f;
			//		self.roomSettings.Clouds = 1f;
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Darkness, 0.2f, false));
			//		self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Desaturation, 0.4f, false));
			//		break;
			//	}
			//	Debug.Log("Effects loaded in " + self.roomSettings.name + " for " + currWeather + " weather");
			//}
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
			if (self.room.roomSettings.name.StartsWith("RU_") || self.room.roomSettings.name.StartsWith("AW_"))
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

	}
}
