using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace NewTerra
{
	internal class PlayerHooks
	{
		public void OnEnable()
		{
			try
			{
				On.PlayerGraphics.ctor += PlayerGraphics_ctor;
				On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
				On.PlayerGraphics.Update += PlayerGraphicsOnUpdate;

				On.Creature.Violence += Creature_Violence;
				On.Creature.ctor += CreatureOnctor;
				On.Player.Update += Player_Update;
				On.Player.ctor += Player_ctor;
				On.Player.TerrainImpact += Player_TerrainImpact;
				IL.Player.GraphicsModuleUpdated += PlayerOnGraphicsModuleUpdated;
			}
			catch (Exception ex)
			{
				Plugin.logger.LogFatal(ex);
			}
		}

		private void PlayerGraphicsOnUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
		{
			orig(self);

			if (self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				for (int i = 0; i < 2; i++)
				{
					self.hands[i].retract = false;//self.player.grasps[i + 2] == null || self.player.grasps[i] == null;
					self.hands[i].retractCounter = 0;
					self.hands[i].mode = Limb.Mode.HuntRelativePosition;
					self.hands[i + 2].pos = self.hands[i].pos;
					self.hands[i + 2].pos -= new Vector2(0, 10);
				}
			}
		}

		private void PlayerOnGraphicsModuleUpdated(ILContext il)
		{
			ILCursor c = new(il);
			
			/*
			for (int i = 0; i < 2; i++)
			{
				c.GotoNext(
					x => x.MatchLdfld<PlayerGraphics>(nameof(PlayerGraphics.hands)),
					x => x.MatchLdloc(0),
					x => x.MatchLdelemRef()
				);
				Debug.Log(c);
				c.Index += 2;
				Debug.Log(c);
				c.EmitDelegate((int j) =>
				{
					Debug.Log("TEST: " + j);
					Debug.Log(Mathf.Clamp(j, 0, 1));
					return Mathf.Clamp(j, 0, 1);
				});
				Debug.Log(c);
			}
			*/
			
			c.GotoNext(
				x => x.MatchLdloc(0),
				x => x.MatchLdcI4(2),
				x => x.MatchBlt(out _)
			);
			c.Index += 2;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((int grasps, Player player) =>
			{
				return player.grasps.Length;
			});
		}

		#region graphics

		private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (self.player != null && self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
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
			if (self.player != null && self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
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

				self.hands = new SlugcatHand[4];
				var list = self.bodyParts.ToList();
				for (int j = 0; j < 4; j++)
				{
					self.hands[j] = new SlugcatHand(self, self.owner.bodyChunks[0], j, 3f, 0.8f, 1f);
					list.Add(self.hands[j]);
				}
				self.bodyParts = list.ToArray();
			}
		}

		#endregion

		#region gameplay

		private void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
		{
			if (self != null && self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
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
			if (self != null && self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				abstractCreature.lavaImmune = true;
			}
		}

		private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			orig(self, eu);
			if (self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				self.Hypothermia -= 0.75f * self.HypothermiaGain;
			}
			
			/*
			if (self.grasps[0] != null && self.grasps[1] != null && (self.grasps[2] == null || self.grasps[3] == null))
			{
				foreach (var physObj in self.room.physicalObjects[2])
				{
					if (self.grasps[2] == null)
					{
						self.SlugcatGrab(physObj, 2);
					}
					else if (self.grasps[3] == null)
					{
						self.SlugcatGrab(physObj, 3);
					}
				}
			}
			*/

			if (self.grasps[0] != null && self.grasps[1] != null && self.input[0].pckp != self.input[1].pckp)
			{
				foreach (var physObj in self.room.physicalObjects[2])
				{
					if (self.grasps[2] == null && Vector2.Distance(physObj.firstChunk.pos, self.firstChunk.pos) < 30)
					{
						self.SlugcatGrab(physObj, 2);
					}
				}
			}

			if (self.grasps[2] != null)
			{
				self.grasps[2].grabbedChunk.MoveFromOutsideMyUpdate(eu, self.firstChunk.pos + new Vector2(-10, 0));

				if (self.grasps[2].grabbed is Weapon weapon)
				{
					//weapon.ChangeMode(Weapon.Mode.Carried);
				}
				
				if (self.grasps[2].grabbed is Spear spear)
				{
					//spear.setRotation = Vector2.up;
				}
			}
		}

		private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
		{
			if (self is Player player && player != null && player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
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
		
		// for changing the length of the grasps array
		private void CreatureOnctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractcreature, World world)
		{
			orig(self, abstractcreature, world);
			
			if (self is Player player)
			{
				player.GetInitialSlugcatClass();
				if (player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
				{
					self.grasps = new Creature.Grasp[4];
				}
			}
		}

		#endregion
	}
}
