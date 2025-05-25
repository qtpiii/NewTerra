using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace NewTerra
{
	public static class PlayerHooks
	{
		public static void Apply()
		{
			try
			{
				
				On.PlayerGraphics.ctor += PlayerGraphics_ctor;
				On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
				On.PlayerGraphics.InitiateSprites += PlayerGraphicsOnInitiateSprites;
				On.PlayerGraphics.AddToContainer += PlayerGraphicsOnAddToContainer; // PROBLEM HERE
				On.PlayerGraphics.Update += PlayerGraphicsOnUpdate;
				On.PlayerGraphics.Reset += PlayerGraphicsOnReset;
				
				IL.SlugcatHand.Update += SlugcatHandUnhardcode;
				IL.SlugcatHand.EngageInMovement += SlugcatHandUnhardcode;

				IL.SlugcatHand.Update += SlugcatHandOnUpdate; // changes "* i"s to "* (i % 2)"

				IL.Player.GrabUpdate += PlayerOnGrabUpdate; // for grasp cycling

				IL.Creature.Update += IL_Creature_Update;

				On.Creature.Update += Creature_Update;
				On.Creature.InjectPoison += Creature_InjectPoison;
				On.Creature.Violence += Creature_Violence;
				On.Creature.ctor += CreatureOnctor;
				On.Player.Update += Player_Update;
				On.Player.ctor += Player_ctor;
				On.Player.TerrainImpact += Player_TerrainImpact;
				On.Player.SlugcatGrab += PlayerOnSlugcatGrab;
				//IL.Player.GraphicsModuleUpdated += PlayerOnGraphicsModuleUpdated; // stupid joar. this unhardcodes how many times a for loop repeats (from 2 to the actual amount of grasps)
			}
			catch (Exception ex)
			{
				Plugin.logger.LogFatal(ex);
			}
		}

		private static void PlayerOnGrabUpdate(ILContext il)
		{
			ILCursor c = new(il);

			c.GotoNext(
				x => x.MatchCallOrCallvirt(typeof(Creature).GetMethod(nameof(Creature.SwitchGrasps), BindingFlags.Instance | BindingFlags.Public))
			);
			c.Remove();
			c.EmitDelegate((Player self, int _, int _) =>
			{
				if (self.SlugCatClass.value != Plugin.TARDIGOATED_ID)
				{
					self.SwitchGrasps(0, 1);
				}
				else
				{
					//RotateGrasps(self);
				}
			});
		}

		public static void RotateGrasps(Player self)
		{
			// moves all grasps clockwise
			PhysicalObject?[] objects = self.grasps.Select(x => x is null ? null : x.grabbed).ToArray();
			self.LoseAllGrasps();
			if (objects[0] is not null) self.Grab(objects[0], 1, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
			if (objects[1] is not null) self.Grab(objects[1], 3, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
			if (objects[3] is not null) self.Grab(objects[3], 2, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
			if (objects[2] is not null) self.Grab(objects[2], 0, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
		}

		private static void SlugcatHandOnUpdate(ILContext il)
		{
			ILCursor c = new(il);

			while (true)
			{
				if (c.TryGotoNext(
					    x => x.MatchLdarg(0),
					    x => x.MatchLdfld<Limb>(nameof(Limb.limbNumber)),
					    x => x.MatchConvR4(),
					    x => x.MatchMul()
				    ))
				{
					c.Index += 2;
					c.EmitDelegate((int i) =>
					{
						return i % 2;
					});
				}
				else
				{
					break;
				}
			}
		}

		private static void SlugcatHandUnhardcode(ILContext il)
		{
			ILCursor c = new(il);

			// goes to every "limbNumber == 0 ?" in the method and replaces it with "limbNumber % 2 != 0 ?"
			while (true)
			{
				if (c.TryGotoNext(
					    x => x.MatchLdarg(0),
					    x => x.MatchLdfld<Limb>("limbNumber"),
					    x => x.MatchBr(out _)
				    ))
				{
					goto InjectDelegate;
				}
				else if (c.TryGotoNext(
					         x => x.MatchLdarg(0),
					         x => x.MatchLdfld<Limb>("limbNumber"),
					         x => x.MatchBrfalse(out _)
				         ))
				{
					goto InjectDelegate;
				}
				else if (c.TryGotoNext(
					         x => x.MatchLdarg(0),
					         x => x.MatchLdfld<Limb>("limbNumber"),
					         x => x.MatchBrtrue(out _)
				         ))
				{
					goto InjectDelegate;
				}
				else
				{
					break;
				}

				InjectDelegate:
				{
					c.Index += 2;
					c.EmitDelegate<Func<int, int>>((i) =>
					{
						return i % 2 != 0 ? 1 : 0;
					});
				}
			}
		}

		private static void PlayerOnGraphicsModuleUpdated(ILContext il)
		{
			ILCursor c = new(il);

			c.GotoNext(
				x => x.MatchLdloc(0),
				x => x.MatchBrfalse(out _)
			);
			c.Index += 1;
			c.EmitDelegate((int i) =>
			{
				return i % 2 != 0 ? 1 : 0;
			});

			c.GotoNext(
				x => x.MatchLdloc(0),
				x => x.MatchLdcI4(2),
				x => x.MatchBlt(out _)
			);
			c.Index += 2;
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, Player, int>>((int grasps, Player player) =>
			{
				return player.grasps.Length;
			});
		}

		#region graphics

		private static void PlayerGraphicsOnReset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
		{
			orig(self);

			if (self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				self.hands[2].Reset(self.player.bodyChunks[0].pos);
				self.hands[3].Reset(self.player.bodyChunks[0].pos);
				Plugin.tardiCWT.TryGetValue(self.player, out var data);
				if (data == null) return;
				data.spritesInitialized = false;
			}
		}

		private static void PlayerGraphicsOnUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
		{
			orig(self);

			if (self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				for (int i = 0; i < 2; i++)
				{
					self.hands[i + 2].Update();
					self.hands[i + 2].relativeHuntPos.y -= 15;
				}

				if ((self.player.animation == Player.AnimationIndex.BeamTip || self.player.animation == Player.AnimationIndex.StandOnBeam) && self.disbalanceAmount > 0)
				{
					self.hands[2].relativeHuntPos.x *= -1;
				}
			}
		}

		private static void PlayerGraphicsOnInitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);

			if (self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				Plugin.tardiCWT.TryGetValue(self.player, out var data);
				if (data == null) return;

				if (data.spritesInitialized == false) // trust nojoardy. (sprites might be initiated twice :gunchie: and this is used in addtocontainer :gunched:)
				{
					data.spritesInitialized = true;
				}
				else
				{
					return;
				}

				data.startOfSprites = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + data.totalAddedSprites);

				for (int i = 0; i < 2; i++)
				{
					sLeaser.sprites[data.ArmSprite(i)] = new FSprite("TardiPlayerArm10");
					sLeaser.sprites[data.ArmSprite(i)].anchorX = 0.9f;
				}
				self.AddToContainer(sLeaser, rCam, null);
			}
		}

		private static void PlayerGraphicsOnAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (self.player.SlugCatClass.value != Plugin.TARDIGOATED_ID)
			{
				orig(self, sLeaser, rCam, newContatiner);
			}
			else
			{
				Plugin.tardiCWT.TryGetValue(self.player, out var data);
				if (data == null) return;
				if (data.startOfSprites == 0) return;
				newContatiner ??= rCam.ReturnFContainer("Midground");
				foreach (int spriteIndex in (int[])[data.startOfSprites, data.startOfSprites + 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11])
				{
					if (!data.spritesInitialized || spriteIndex > sLeaser.sprites.Length - 1) continue;
					//UnityEngine.Debug.Log(spriteIndex);

					sLeaser.sprites[spriteIndex].RemoveFromContainer();

					if (spriteIndex is > 6 and < 9 or > 9)
					{
						rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[spriteIndex]);
					}
					else
					{
						newContatiner.AddChild(sLeaser.sprites[spriteIndex]);
					}
				}
			}
		}

		private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (self.player != null && self.player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				foreach (var index in (int[])[0, 1, 3, 4, 5, 6, 7, 8, 9])
				{
					string? name = sLeaser.sprites[index]?.element?.name;
					if (name != null && !name.StartsWith("Tardi") && Futile.atlasManager.DoesContainElementWithName("Tardi" + name))
					{
						sLeaser.sprites[index].SetElementByName("Tardi" + name);
					}
				}
				if (!self.player.dead && self.player.Stunned && self.player.injectedPoison > 0f)
				{
					sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("TardiPoisonFace");
				}

				Plugin.tardiCWT.TryGetValue(self.player, out var data);
				if (data == null || data.startOfSprites > sLeaser.sprites.Length - 1)
				{
					data.startOfSprites = sLeaser.sprites.Length - 2;
					return;
				}

				// 90% of the code in this for loop and "vector" and "vector2" are decompiled code :grape:
				Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
				Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
				for (int j = 0; j < 2; j++)
				{
					Vector2 vector10 = Vector2.Lerp(self.hands[j + 2].lastPos, self.hands[j + 2].pos, timeStacker);
					if (self.hands[j + 2].mode != Limb.Mode.Retracted)
					{
						sLeaser.sprites[data.ArmSprite(j)].x = vector10.x - camPos.x;
						sLeaser.sprites[data.ArmSprite(j)].y = vector10.y - camPos.y;
						float num9 = 4.5f / ((float)self.hands[j + 2].retractCounter + 1f);
						//float num9 = 4.5f;
						if ((self.player.animation == Player.AnimationIndex.StandOnBeam || self.player.animation == Player.AnimationIndex.BeamTip) && self.disbalanceAmount <= 40f && self.hands[j + 2].mode == Limb.Mode.HuntRelativePosition)
						{
							num9 *= self.disbalanceAmount / 40f;
						}
						if (self.player.animation == Player.AnimationIndex.HangFromBeam)
						{
							num9 *= 0.5f;
						}
						num9 *= Mathf.Abs(Mathf.Cos(Custom.AimFromOneVectorToAnother(vector2, vector) / 360f * 3.1415927f * 2f));
						Vector2 vector11 = vector + Custom.RotateAroundOrigo(new Vector2((-1f + 2f * (float)(j % 2f)) * num9, -3.5f), Custom.AimFromOneVectorToAnother(vector2, vector));
						sLeaser.sprites[data.ArmSprite(j)].element = Futile.atlasManager.GetElementWithName("Tardi" + self._cachedPlayerArms[Mathf.RoundToInt(Mathf.Clamp(Vector2.Distance(vector10, vector11) / 2f, 0f, 12f))]);
						sLeaser.sprites[data.ArmSprite(j)].rotation = Custom.AimFromOneVectorToAnother(vector10, vector11) + 90f;
						if (self.player.bodyMode == Player.BodyModeIndex.Crawl)
						{
							sLeaser.sprites[data.ArmSprite(j)].scaleY = ((vector.x < vector2.x) ? (-1f) : 1f);
						}
						else if (self.player.bodyMode == Player.BodyModeIndex.WallClimb)
						{
							sLeaser.sprites[data.ArmSprite(j)].scaleY = ((self.player.flipDirection == -1) ? (-1f) : 1f);
						}
						else
						{
							sLeaser.sprites[data.ArmSprite(j)].scaleY = Mathf.Sign(Custom.DistanceToLine(vector10, vector, vector2));
						}
						if (self.player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
						{
							sLeaser.sprites[data.ArmSprite(j)].scaleY = ((j % 2 != 0) ? 1f : (-1f));
						}
					}
					sLeaser.sprites[data.ArmSprite(j)].isVisible = self.hands[j + 2].mode != Limb.Mode.Retracted && ((self.player.animation != Player.AnimationIndex.ClimbOnBeam && self.player.animation != Player.AnimationIndex.ZeroGPoleGrab) || !self.hands[j + 2].reachedSnapPosition);
					/* // climbing on pole sprites, may or may not add
					if ((self.player.animation == Player.AnimationIndex.ClimbOnBeam || self.player.animation == Player.AnimationIndex.HangFromBeam || self.player.animation == Player.AnimationIndex.GetUpOnBeam || self.player.animation == Player.AnimationIndex.ZeroGPoleGrab) && self.hands[j + 2].reachedSnapPosition)
					{
						sLeaser.sprites[7 + j].x = vector10.x - camPos.x;
						sLeaser.sprites[7 + j].y = vector10.y - camPos.y + ((self.player.animation != Player.AnimationIndex.ClimbOnBeam && self.player.animation != Player.AnimationIndex.ZeroGPoleGrab) ? 3f : 0f);
						sLeaser.sprites[7 + j].isVisible = true;
					}
					else
					{
						sLeaser.sprites[7 + j].isVisible = false;
					}*/
				}
			}
		}

		private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
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

		private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
		{
			orig(self, eu);
			if (self is Player player && player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				if (player.injectedPoison > 0f)
				{
					player.slowMovementStun = Math.Max(player.slowMovementStun, Mathf.RoundToInt(player.injectedPoison * 8f));
					player.drown = Mathf.Max(player.drown, player.injectedPoison * 0.25f);
					player.aerobicLevel = Mathf.Max(player.aerobicLevel, player.injectedPoison * 1.5f);
					if (player.graphicsModule != null)
					{
						PlayerGraphics playerGraphics = player.graphicsModule as PlayerGraphics;
						playerGraphics.malnourished = Mathf.Max(playerGraphics.malnourished, player.injectedPoison);
					}
					player.injectedPoison -= 0.0005f;
					if (player.injectedPoison >= 1.5f && !player.dead)
					{
						player.Die();
					}
				}

				Plugin.tardiCWT.TryGetValue(player, out var data);
				if (data == null) return;
				if (data.poisonStunCounter >= 10 || data.poisonStunLimit >= 200)
				{
					if (data.poisonStunAmount > 0f)
					{
						player.Stun(Mathf.RoundToInt(Mathf.Lerp(20, 200, data.poisonStunAmount * 1.5f)));
						player.aerobicLevel = Mathf.Max(player.aerobicLevel, player.injectedPoison * 2f);
						data.poisonStunAmount = 0f;
						data.poisonStunLimit = -1600;
					}
				}
			}
		}

		private static void IL_Creature_Update(ILContext il)
		{
			ILCursor c = new(il);

			ILLabel ilLabel = il.DefineLabel();

			c.GotoNext(
				MoveType.After,
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<Creature>(nameof(Creature.injectedPoison)),
			 	x => x.MatchLdcR4(0),
				x => x.MatchBleUn(out ilLabel)
				);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<Creature, bool>>((Creature creature) =>
			{
				if (creature is Player player)
				{
					if (player.SlugCatClass.value == Plugin.TARDIGOATED_ID) return true;
				}
				return false;
			});
			c.Emit(OpCodes.Brtrue, ilLabel);
		}

		private static void Creature_InjectPoison(On.Creature.orig_InjectPoison orig, Creature self, float amount, Color poisonColor)
		{
			orig(self, amount, poisonColor);
			if (self is Player player && player.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				Plugin.tardiCWT.TryGetValue(player, out var data);
				if (data == null) return;
				data.poisonStunAmount += amount * 2;
				if (data.poisonStunCounter >= 20)
				{
					if (data.poisonStunLimit < 0) data.poisonStunLimit = 0;
				}
				data.poisonStunCounter = 0;
				data.poisonStunLimit += 1;
			}
		}

		private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
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

		private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);
			if (self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				abstractCreature.lavaImmune = true;
				
				try
				{
					UnityEngine.Debug.Log("women penis and boobs");
					Plugin.tardiCWT.Add(self, new TardiData()); // attach cwt
				}
				catch (ArgumentException)
				{
					Custom.Log("Tenacious CWT already attached!");
				}
			}
		}

		private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			PhysicalObject grasp2PreUpdate = null;
			PhysicalObject grasp3PreUpdate = null;
			if (self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				if (self.grasps[2] is not null) grasp2PreUpdate = self.grasps[2].grabbed;
				if (self.grasps[3] is not null) grasp3PreUpdate = self.grasps[3].grabbed;
			}
			orig(self, eu);
			if (self.SlugCatClass.value == Plugin.TARDIGOATED_ID)
			{
				self.Hypothermia -= 0.75f * self.HypothermiaGain;

				Plugin.tardiCWT.TryGetValue(self, out var data);
				if (data == null) return;

				if (data.poisonStunCounter < 30)
				{
					data.poisonStunCounter += 1;
				}

				self.switchHandsCounter = 0;
				
				if (self.grasps[0] != null && self.grasps[1] != null && (self.input[0].pckp == true && self.input[1].pckp == false))
				{
					foreach (var physObj in self.room.physicalObjects[2])
					{
						if (self.grasps[2] == null && Vector2.Distance(physObj.firstChunk.pos, self.firstChunk.pos) < 30)
						{
							if ((self.grasps[0].grabbed != null && self.grasps[0].grabbed == physObj) || (self.grasps[1].grabbed != null && self.grasps[1].grabbed == physObj))
							{
								continue;
							}
							self.SlugcatGrab(physObj, 2);
						}
					}
				}
				if (self.grasps[0] != null && self.grasps[1] != null && self.grasps[2] != null && (self.input[0].pckp == true && self.input[1].pckp == false))
				{
					foreach (var physObj in self.room.physicalObjects[2])
					{
						if (self.grasps[3] == null && Vector2.Distance(physObj.firstChunk.pos, self.firstChunk.pos) < 30)
						{
							if ((self.grasps[0].grabbed != null && self.grasps[0].grabbed == physObj) || (self.grasps[1].grabbed != null && self.grasps[1].grabbed == physObj))
							{
								continue;
							}
							self.SlugcatGrab(physObj, 3);
						}
					}
				}

				if (false)//if (self.switchHandsProcess <= 0f)
				{
					for (int i = 0; i < 2; i++)
					{
						if (self.grasps[i] is null && self.grasps[i + 2] is not null)
						{
							PhysicalObject obj = self.grasps[i + 2].grabbed;
							self.ReleaseGrasp(i + 2);
							self.SlugcatGrab(obj, i);
						}
					}
				}

				if (data.canGrabWithExtraHandsCounter > 0)
				{
					data.canGrabWithExtraHandsCounter--;
				}

				if (data.switchGraspsCounter > 0)
				{
					data.switchGraspsCounter--;
				}
				if (self.input[0].pckp && self.input[1].pckp == false)
				{
					if (data.switchGraspsCounter > 0)
					{
						RotateGrasps(self);
						data.switchGraspsCounter = 0;
					}
					data.switchGraspsCounter = 10;
				}
				
				for (int i = 0; i < 2; i++)
				{
					if (grasp2PreUpdate != null && self.grasps[i] is not null && self.grasps[i].grabbed == grasp2PreUpdate)
					{
						self.grasps[i].Release();
						self.Grab(grasp2PreUpdate, 3, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
					}
					if (grasp3PreUpdate != null && self.grasps[i] is not null && self.grasps[i].grabbed == grasp3PreUpdate)
					{
						self.grasps[i].Release();
						self.Grab(grasp3PreUpdate, 3, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, false, true);
					}
				}
			}
		}
		
		private static void PlayerOnSlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspused)
		{
			if (self.SlugCatClass.value != Plugin.TARDIGOATED_ID)
			{
				orig(self, obj, graspused);
			}
			else
			{
				int countOfObjTypeHeld = self.grasps.Select(x => x is null ? null : x.grabbed).Count(x => x?.GetType() == obj.GetType());
				if (obj is Spear && countOfObjTypeHeld >= 2) return;
				
				if (graspused is 2 or 3 && (obj == self.grasps[0].grabbed || obj == self.grasps[1].grabbed)) return;
				
				Plugin.tardiCWT.TryGetValue(self, out var data);
				if (data == null) return;
				if (graspused is 0 or 1)
				{
					if (self.grasps[0] is null || self.grasps[1] is null)
					{
						data.canGrabWithExtraHandsCounter = 5;
					}
					orig(self, obj, graspused);
					return;
				}

				if (graspused == 2 && data.CanGrabWithExtraHands)
				{
					orig(self, obj, graspused);
					data.canGrabWithExtraHandsCounter = 5;
					return;
				}

				if (graspused == 3 && data.CanGrabWithExtraHands)
				{
					orig(self, obj, graspused);
					return;
				}
			}
		}

		private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
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
		private static void CreatureOnctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractcreature, World world)
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
