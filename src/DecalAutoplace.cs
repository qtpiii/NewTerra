
using UnityEngine;

namespace NewTerra;

public class DecalAutoplace(EffExt.EffectExtraData data) : UpdatableAndDeletable, IDrawable
{
	#region shared
	internal static void Apply() {
		EffExt.EffectDefinitionBuilder effect = new("DecalAutoplace");
		effect
			.AddStringField("decalset", "default", "Decal set")
			.SetCategory(Plugin.POM_CATEGORY)
			.SetUADFactory((room, data, _) => new DecalAutoplace(data))
			.AddFloatField("density", 0f, 1f, 0.05f, 0.5f, "Density")
			.Register();
	}
	#endregion

	private string[]? set;

	#region idrawable
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		throw new System.NotImplementedException();
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		throw new System.NotImplementedException();
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		throw new System.NotImplementedException();
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		throw new System.NotImplementedException();
	}
	#endregion
}