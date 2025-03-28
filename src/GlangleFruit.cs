using Fisobs.Core;
using RWCustom;
using UnityEngine;
using static Pom.Pom;

namespace NewTerra;

//TODO: graphics, sounds, stem

public sealed class GlangleFruit : PlayerCarryableItem, IDrawable, IPlayerEdible
{
	#region setup
	public static AbstractPhysicalObject.AbstractObjectType GLANGLEFRUIT_AOT = null!;
	public static void Apply()
	{
		GLANGLEFRUIT_AOT = new("Glangle", true);
		RegisterManagedObject<GSpawner, PomData, ManagedRepresentation>("GlangleFruit", Plugin.POM_CATEGORY);
		On.Player.Grabability += (orig, self, obj) => obj is GlangleFruit ? Player.ObjectGrabability.OneHand : orig(self, obj);
		On.Player.ThrowObject += (orig, self, grasp, eu) =>
		{
			if (self.grasps[grasp].grabbed is GlangleFruit gfruit) gfruit.Speeeen(45f);
			orig(self, grasp, eu);
		};
		Content.Register(new GFisob());
	}
	#endregion

	private const float MASS_BASE = 0.1f;
	private const float MASS_PER_BITE = 0.01f;
	private const float CHUNKRAD_BASE = 5f;
	private const float CHUNKRAD_PER_BITE = 0.1f;
	private const float VISUAL_PERPDISTANCE_PER_SEED = 15f;
	private const float VISUAL_DISTANCE_PER_SEED = 0.1f;
	private const float VISUAL_DISTANCE_INITIAL = 6f;
	private const float VISUAL_RANDOM_DISPLACE_ORBIT_MAX = 6f;
	private const float VISUAL_RANDOM_DISPLACE_RADIAL_MAX = 1f;
	private const float VISUAL_RANDOM_EXTRA_ROT_MAX = 10f;
	private const float PHYS_GRAVITY = 0.9f;
	private const float PHYS_BOUNCE = 0.2f;
	private const float PHYS_AIR_FRICTION = 0.97f;
	private const float PHYS_SURFACE_FRICTION = 0.5f;
	private const float PHYS_WATER_FRICTION = 0.95f;
	private const float PHYS_BUOYANCY = 1.1f;
	private readonly (float angle, float dst, float extraRot)[] seedTransforms;
	private float rot = 0f;
	private float lastRot = 0f;
	private float angVel = 0f;

	public GAbstract GAbs => (GAbstract)abstractPhysicalObject;

	public GlangleFruit(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
	{
		bodyChunks = [new BodyChunk(
			this,
			0,
			GAbs.Room.realizedRoom.MiddleOfTile(GAbs.pos),
			CHUNKRAD_BASE + GAbs.bitesLeft * CHUNKRAD_PER_BITE,
			MASS_BASE + GAbs.bitesLeft * MASS_PER_BITE)
			{
				collideWithObjects = true,

			}
		];
		bodyChunkConnections = [];
		airFriction = PHYS_AIR_FRICTION;
		gravity = PHYS_GRAVITY;
		bounce = PHYS_BOUNCE;
		surfaceFriction = PHYS_SURFACE_FRICTION;
		collisionLayer = 1;
		waterFriction = PHYS_WATER_FRICTION;
		buoyancy = PHYS_BUOYANCY;
		ChangeCollisionLayer(0);
		Random.State state = Random.state;
		Random.InitState(GAbs.ID.number);
		seedTransforms = new (float, float, float)[GAbs.maxBites];
		float
			angle = 0f,
			dst = VISUAL_DISTANCE_INITIAL;
		for (int i = 0; i < GAbs.maxBites; i++)
		{
			float perimeter = 2f * Mathf.PI * dst;
			float randRadialDisplace = VISUAL_RANDOM_DISPLACE_RADIAL_MAX * Random.Range(-1f, 1f);
			float randOrbitDisplace = VISUAL_RANDOM_DISPLACE_ORBIT_MAX * Random.Range(-1f, 1f);
			float randAngleIncrement = 360f * (randOrbitDisplace / perimeter);
			float randRot = VISUAL_RANDOM_EXTRA_ROT_MAX * Random.Range(-1f, 1f);
			float baseAngleIncrement = 360f * (VISUAL_PERPDISTANCE_PER_SEED / perimeter);
			seedTransforms[i] = (
				Mathf.LerpAngle(angle, angle + randAngleIncrement, 1f),
				dst + randRadialDisplace,
				randRot);
			angle = Mathf.LerpAngle(angle, angle + baseAngleIncrement, 1f);
			dst += VISUAL_DISTANCE_PER_SEED;
		}
		Random.state = state;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		lastRot = rot;
		if (grabbedBy.Count > 0)
		{
			if (!GAbs.isConsumed) GAbs.Consume();
			if (collisionLayer == 1) ChangeCollisionLayer(0);
			angVel = 0f;
			rot = Custom.AimFromOneVectorToAnother(
				firstChunk.pos,
				grabbedBy[0].grabber.bodyChunks[grabbedBy[0].graspUsed].pos);
		}
		else
		{
			angVel = (firstChunk.vel.magnitude, angVel, firstChunk.ContactPoint.x, firstChunk.contactPoint.y) switch
			{
				( <= 0.1f, _, _, _) => 0f, //coming to a stop
				( >= 0.1f, 0f, 0, 0) => Random.Range(5, 10f) * Mathf.Sign(firstChunk.vel.x), //going into freefall
				(_, _, 0, -1) => (float)(360f * (firstChunk.vel.x / (2f * Mathf.PI * firstChunk.rad))), //rolling on the ground
				_ => angVel, //neutral fall/flight
			};
			rot = Mathf.LerpAngle(rot, rot + angVel, 1f);
		}
	}

	public override void Grabbed(Creature.Grasp grasp)
	{
		base.Grabbed(grasp);
		ChangeCollisionLayer(1);
	}
	public override void HitByWeapon(Weapon weapon)
	{
		base.HitByWeapon(weapon);
		GAbs.Consume();
	}
	public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
	{
		base.TerrainImpact(chunk, direction, speed, firstContact);
		if (speed >= 2f && firstContact)
		{
			room.PlaySound(
				soundId: SoundID.Swollen_Water_Nut_Terrain_Impact,
				firstChunk,
				false,
				0.9f,
				0.7f);
		}
	}

	void Speeeen(float angVelMaxAbs)
	{
		angVel = angVelMaxAbs * Random.Range(-1f, 1f);
	}

	#region idrawable
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[GAbs.maxBites + 1];

		FSprite kernel = new("Circle20")
		{
			color = Color.Lerp(Color.red, Color.yellow, 0.3f),
			height = CHUNKRAD_BASE * 2f,
			width = CHUNKRAD_BASE * 2f
		};
		sLeaser.sprites[0] = kernel;

		for (int i = 0; i < GAbs.maxBites; i++)
		{
			FSprite seedSprite = new("DangleSeed1B")
			{
				isVisible = true,
				color = Color.Lerp(Color.red, Color.yellow, 0.7f),
			};
			sLeaser.sprites[i + 1] = seedSprite;
		}
		AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		Vector2 basePos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
		float startAngle = Mathf.LerpAngle(lastRot, rot, timeStacker);
		sLeaser.sprites[0].SetPosition(basePos);
		sLeaser.sprites[0].rotation = startAngle;
		for (int i = 0; i < GAbs.maxBites; i++)
		{
			FSprite seed = sLeaser.sprites[i + 1];
			(float angle, float dst, float extraRot) = seedTransforms[i];
			Vector2 displace = Custom.DegToVec(startAngle + angle).normalized * dst;
			seed.rotation = startAngle + angle + extraRot;
			seed.SetPosition(basePos + displace);
			seed.isVisible = BitesLeft >= i;
		}
		if (slatedForDeletetion || room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer = null)
	{
		newContainer ??= rCam.ReturnFContainer("Items");
		for (int i = 0; i < sLeaser.sprites.Length; i++)
		{
			sLeaser.sprites[i].RemoveFromContainer();
			newContainer.AddChild(sLeaser.sprites[i]);
			if (i != 0)
			{
				sLeaser.sprites[i].MoveBehindOtherNode(sLeaser.sprites[i - 1]);
				sLeaser.sprites[i].MoveInFrontOfOtherNode(sLeaser.sprites[0]);
			}
		}
	}

	#endregion
	#region iplayeredible

	public int BitesLeft => GAbs.bitesLeft;

	public int FoodPoints => 0;

	public bool Edible => true;

	public bool AutomaticPickUp => false;
	public void BitByPlayer(Creature.Grasp grasp, bool eu)
	{
		Player player = (Player)grasp.grabber;
		player.AddQuarterFood();
		player.AddQuarterFood();
		room.PlaySound(SoundID.Slugcat_Bite_Slime_Mold, player.firstChunk, false, 0.6f, 1.1f);
		room.PlaySound(SoundID.Swollen_Water_Nut_Terrain_Impact, player.firstChunk, false, 0.5f, 1.3f);
		GAbs.bitesLeft -= 1;
		firstChunk.mass = MASS_BASE + GAbs.bitesLeft * MASS_PER_BITE;
		firstChunk.rad = CHUNKRAD_BASE + GAbs.bitesLeft * CHUNKRAD_PER_BITE;
		if (GAbs.bitesLeft <= 0)
		{
			((Player)grasp.grabber).ObjectEaten(this);
			grasp.Release();
			Destroy();
		}
	}

	public void ThrowByPlayer()
	{

	}
	#endregion

	public sealed class GAbstract : AbstractConsumable
	{
		public int maxBites;
		public int bitesLeft;
		public GAbstract(
			World world,
			PhysicalObject? realizedObject,
			WorldCoordinate pos,
			EntityID ID,
			int originRoom,
			int placedObjectIndex,
			PlacedObject.ConsumableObjectData? consumableData,
			int maxBites) : base(
				world,
				GLANGLEFRUIT_AOT,
				realizedObject,
				pos,
				ID,
				originRoom,
				placedObjectIndex,
				consumableData)
		{
			this.maxBites = maxBites;
			bitesLeft = maxBites;
		}
		public override void Realize()
		{
			base.Realize();
			realizedObject ??= new GlangleFruit(this);
		}
		public override string ToString()
		{
			string text = $"{originRoom};{placedObjectIndex};{bitesLeft}";
			return this.SaveToString(text);
		}
	}

	public sealed class GFisob : Fisobs.Items.Fisob
	{
		public GFisob() : base(GLANGLEFRUIT_AOT)
		{
			Icon = new GIcon();
		}

		public override AbstractPhysicalObject Parse(
			World world,
			EntitySaveData entitySaveData,
			Fisobs.Sandbox.SandboxUnlock? unlock)
		{
			string[] spl = System.Text.RegularExpressions.Regex.Split(entitySaveData.CustomData, ";");
			int.TryParse(spl[0], out int origin);
			int.TryParse(spl[1], out int poindex);
			int.TryParse(spl[2], out int bitesLeft);
			return new GAbstract(world, null, entitySaveData.Pos, entitySaveData.ID, origin, poindex, null, bitesLeft);
		}

	}
	public class GIcon : Icon
	{
		public override int Data(AbstractPhysicalObject apo) => 0;
		public override Color SpriteColor(int data) => Color.white;
		public override string SpriteName(int data) => "Circle20";
	}

	public sealed class PomData : ManagedData
	{

		[IntegerField("00depleteMin", 0, 50, 3, ManagedFieldWithPanel.ControlType.slider, displayName: "deplete cycles min")]
		public int depleteCyclesMin;
		[IntegerField("01depleteMax", 0, 50, 3, ManagedFieldWithPanel.ControlType.slider, displayName: "deplete cycles max")]
		public int depleteCyclesMax;
		[IntegerField("02maxBites", 1, 100, 3, ManagedFieldWithPanel.ControlType.slider, displayName: "max bites")]
		public int maxBites;

		public PlacedObject.ConsumableObjectData GetVanillaConsumableData() => new(owner) { minRegen = depleteCyclesMin, maxRegen = depleteCyclesMax };
		public PomData(PlacedObject owner) : base(owner, null)
		{

		}
	}

	public sealed class GSpawner : UpdatableAndDeletable
	{
		private readonly PlacedObject _owner;
		private readonly bool _firstTimeRealized;
		public GSpawner(PlacedObject owner, Room room)
		{
			this.room = room;
			_owner = owner;
			_firstTimeRealized = room.abstractRoom.firstTimeRealized;
		}

		private PomData _data => (PomData)_owner.data;
		public override void Update(bool eu)
		{
			int roomIndex = room.abstractRoom.index;
			int placedObjectIndex = room.roomSettings.placedObjects.IndexOf(_owner);
			LogWarning($"{roomIndex} {placedObjectIndex} ");
			if (room.game.session switch
			{
				StoryGameSession story => !story.saveState.ItemConsumed(room.world, false, roomIndex, placedObjectIndex) && _firstTimeRealized,
				_ => true
			})
			{
				LogWarning("yummers");
				GAbstract absFruit = new(
					room.world,
					null,
					room.ToWorldCoordinate(_owner.pos),
					room.game.GetNewID(),
					roomIndex,
					placedObjectIndex,
					_data.GetVanillaConsumableData(),
					_data.maxBites)
				{
					isConsumed = false
				};
				room.abstractRoom.AddEntity(absFruit);
			}
			else
			{
				LogWarning($"sadly consumed");
			}
			Destroy();
		}
	}

	public sealed class GStem : UpdatableAndDeletable, IDrawable
	{
		private const float SEG_MIN_LENGTH = 5f;
		private const float SEG_MAX_LENGTH = 20f;
		private GlangleFruit? _fruit;
		private Vector2 _fruitRestPos;
		private Vector2 _stuckPos;
		private float _ropeLength;

		public GStem(Room room, GlangleFruit fruit)
		{
			_fruit = fruit;
			_fruit.firstChunk.HardSetPosition(_fruitRestPos);
			_stuckPos.x = _fruitRestPos.x;
			_ropeLength = -1f;
			int x = room.GetTilePosition(_fruitRestPos).x;
			for (int i = room.GetTilePosition(_fruitRestPos).y; i < room.TileHeight; i++)
			{
				if (room.GetTile(x, i).Solid)
				{
					_stuckPos.y = room.MiddleOfTile(x, i).y - 10f;
					_ropeLength = Mathf.Abs(_stuckPos.y - _fruitRestPos.y);
					break;
				}
			}
		}

		public override void Update(bool eu)
		{
			//todo: make it actually hold the thing up
			base.Update(eu);
			if (_fruit is not null)
			{
				if (_fruit.GAbs.isConsumed) _fruit = null;
				else
				{
					float fruitGrav = _fruit.gravity;

				}
			}
			else
			{
			}
		}


		#region stem idrawable
		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			AddToContainer(sLeaser, rCam);
			sLeaser.InitSprites(1);
			sLeaser.sprites[0] = new TriangleMesh("Futile_White", [new TriangleMesh.Triangle(1, 2, 3)], false);
		}
		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			TriangleMesh placeholder = (TriangleMesh)sLeaser.sprites[1];
			placeholder.MoveVertice(0, _stuckPos + new Vector2(5f, -5f));
			placeholder.MoveVertice(1, _stuckPos + new Vector2(-5f, -5f));
			placeholder.MoveVertice(1, _fruitRestPos + new Vector2(-5f, -5f));
			if (slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer = null)
		{
			newContainer ??= rCam.ReturnFContainer("Items");
			sLeaser.UntetherSprites();
			newContainer.AddChild(sLeaser.sprites[0]);

		}
		#endregion
	}
}
