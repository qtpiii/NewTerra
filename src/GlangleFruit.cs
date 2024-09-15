using Fisobs.Core;
using UnityEngine;
using static Pom.Pom;

namespace NewTerra;

public sealed class GlangleFruit : PlayerCarryableItem, IDrawable, IPlayerEdible
{
	#region setup
	public static AbstractPhysicalObject.AbstractObjectType GLANGLEFRUIT_AOT = null!;
	public static void Apply()
	{
		GLANGLEFRUIT_AOT = new("Glangle", true);
		RegisterManagedObject<Spawner, PomData, ManagedRepresentation>("GlangleFruit", "NEW_TERRA");
		On.Player.Grabability += (orig, self, obj) => obj is GlangleFruit ? Player.ObjectGrabability.OneHand : orig(self, obj);
		Content.Register(new GFisob());
	}
	#endregion

	public const float MAX_MASS = 0.6f;
	public GAbstract GAbs => (GAbstract)abstractPhysicalObject;

	public GlangleFruit(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
	{
		bodyChunks = [new BodyChunk(this, 0, GAbs.Room.realizedRoom.MiddleOfTile(GAbs.pos), 10f, MAX_MASS) { collideWithObjects = true }];
		bodyChunkConnections = [];
		airFriction = 0.999f;
		gravity = 0.9f;
		bounce = 0.2f;
		surfaceFriction = 0.7f;
		collisionLayer = 1;
		waterFriction = 0.95f;
		buoyancy = 1.1f;
	}

	#region idrawable
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = [new FSprite("Circle20") { color = Color.red }];
		AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		sLeaser.sprites[0].SetPosition(Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos);
		sLeaser.sprites[0].color = Color.Lerp(Color.yellow, Color.red, (float)GAbs.bitesLeft / (float)GAbstract.MAX_BITES);
		if (slatedForDeletetion || room != rCam.room)
		{
			sLeaser.CleanSpritesAndRemove();
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		newContatiner ??= rCam.ReturnFContainer("Items");
		sLeaser.sprites[0].RemoveFromContainer();
		newContatiner.AddChild(sLeaser.sprites[0]);
	}

	#endregion
	#region yummers

	public int BitesLeft => GAbs.bitesLeft;

	public int FoodPoints => 0;

	public bool Edible => true;

	public bool AutomaticPickUp => false;
	public void BitByPlayer(Creature.Grasp grasp, bool eu)
	{
		((Player)grasp.grabber).AddQuarterFood();
		GAbs.bitesLeft -= 1;
		firstChunk.mass = MAX_MASS * ((float)GAbs.bitesLeft / (float)GAbstract.MAX_BITES);
		if (GAbs.bitesLeft <= 0) Destroy();
	}

	public void ThrowByPlayer() { }
	#endregion

	public sealed class GAbstract : AbstractConsumable
	{
		public const int MAX_BITES = 10;
		public int bitesLeft;
		public GAbstract(
			World world,
			PhysicalObject? realizedObject,
			WorldCoordinate pos,
			EntityID ID,
			int originRoom,
			int placedObjectIndex,
			PlacedObject.ConsumableObjectData? consumableData,
			int bitesLeft = MAX_BITES) : base(
				world,
				GLANGLEFRUIT_AOT,
				realizedObject,
				pos,
				ID,
				originRoom,
				placedObjectIndex,
				consumableData)
		{
			this.bitesLeft = bitesLeft;
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

		public override string SpriteName(int data) => "throw new System.NotImplementedException();";
	}

	public sealed class PomData : ManagedData
	{

		[IntegerField("00depleteMin", 0, 50, 3, ManagedFieldWithPanel.ControlType.slider, displayName: "deplete cycles min")]
		public int depleteCyclesMin;
		[IntegerField("00depleteMax", 0, 50, 3, ManagedFieldWithPanel.ControlType.slider, displayName: "deplete cycles max")]
		public int depleteCyclesMax;

		public PlacedObject.ConsumableObjectData GetVanillaConsumableData() => new(owner) { minRegen = depleteCyclesMin, maxRegen = depleteCyclesMax };
		public PomData(PlacedObject owner) : base(owner, null)
		{

		}
	}

	public sealed class Spawner(PlacedObject owner) : UpdatableAndDeletable
	{
		private PomData _data => (PomData)owner.data;
		public override void Update(bool eu)
		{
			int roomIndex = room.abstractRoom.index;
			int placedObjectIndex = room.roomSettings.placedObjects.IndexOf(owner);
			if (room.game.session switch
			{
				StoryGameSession story => !story.saveState.ItemConsumed(room.world, false, roomIndex, placedObjectIndex),
				_ => true
			})
			{
				LogWarning("yummers");
				GAbstract absFruit = new(
					room.world,
					null,
					room.ToWorldCoordinate(owner.pos),
					room.game.GetNewID(),
					roomIndex,
					placedObjectIndex,
					_data.GetVanillaConsumableData());
				room.abstractRoom.AddEntity(absFruit);
			}
			Destroy();
		}
	}
}
