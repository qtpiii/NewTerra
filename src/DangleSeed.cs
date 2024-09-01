using System;
using RWCustom;
using Fisobs;
using Fisobs.Items;
using UnityEngine;
using Fisobs.Core;
using Fisobs.Properties;
using Fisobs.Sandbox;
using static Pom.Pom;

namespace NewTerra
{
    public class DangleSeed : DangleFruit
    {
        public DangleSeed(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            this.bites = 2;
        }

        public new void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("DangleSeed0A", true);
            sLeaser.sprites[1] = new FSprite("DangleSeed0B", true);
            this.AddToContainer(sLeaser, rCam, null);
        }
    }

    sealed class DangleSeedAbstract(World world, WorldCoordinate pos, EntityID ID) : AbstractPhysicalObject(world, DangleSeedFisob.AbstractDangleSeed, null, pos, ID)
    {
        public override void Realize()
        {
            base.Realize();
            realizedObject ??= new DangleSeed(this);
        }
    }

    sealed class DangleSeedFisob : Fisob
    {
        public static readonly AbstractPhysicalObject.AbstractObjectType AbstractDangleSeed = new("DangleSeed", true);
        public static readonly MultiplayerUnlocks.SandboxUnlockID DangleSeed = new("DangleSeed", true);
        
        public DangleSeedFisob() : base(AbstractDangleSeed)
        {
            Icon = new SimpleIcon("Symbol_DangleFruit", new Color(0f, 0f, 1f));

            SandboxPerformanceCost = new(linear: 0.2f, 0f);

            RegisterUnlock(DangleSeed, parent: MultiplayerUnlocks.SandboxUnlockID.DangleFruit, data: 0);
        }

        private static readonly DangleSeedProperties properties = new();

        public override ItemProperties Properties(PhysicalObject forObject)
        {
            return properties;
        }

        public override AbstractPhysicalObject Parse(World world, EntitySaveData entitySaveData, SandboxUnlock unlock)
        {
            DangleSeedAbstract result = new(world, entitySaveData.Pos, entitySaveData.ID);
            return result;
        }
    }

    public class DangleSeedProperties : ItemProperties
    {
        public override void Throwable(Player player, ref bool throwable) => throwable = true;
        public override void Grabability(Player player, ref Player.ObjectGrabability grabability) => grabability = Player.ObjectGrabability.OneHand;
    }

    internal class DangleSeedData(PlacedObject owner) : ManagedData(owner, null)
    {
        [IntegerField(nameof(MinCycles), 0, 20, 0, ManagedFieldWithPanel.ControlType.slider, "Min Cycles")]
        public int MinCycles;

        [IntegerField(nameof(MaxCycles), 0, 20, 0, ManagedFieldWithPanel.ControlType.slider, "Max Cycles")]
        public int MaxCycles;
    }

    internal class DangleSeedObject : UpdatableAndDeletable
    {
        public DangleSeedObject(Room room, PlacedObject placedObject)
        {
            if (room.abstractRoom.firstTimeRealized)
            {
                int objIndex = room.roomSettings.placedObjects.IndexOf(placedObject);
                DangleSeedData data = (DangleSeedData)placedObject.data;

                if (room.game.session is not StoryGameSession session || session.saveState.ItemConsumed(room.world, false, room.abstractRoom.index, objIndex))
                {
                    AbstractConsumable obj = new(room.world, DangleSeedFisob.AbstractDangleSeed, null, room.GetWorldCoordinate(placedObject.pos), room.game.GetNewID(), room.abstractRoom.index, objIndex, new PlacedObject.ConsumableObjectData(placedObject))
                    {
                        isConsumed = false,
                        minCycles = data.MinCycles,
                        maxCycles = data.MaxCycles
                    };

                    obj = 
                }
            }
        }
    }
}
