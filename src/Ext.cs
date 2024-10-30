using System;

namespace NewTerra;

internal static class Ext
{

	public static void InitSprites(this RoomCamera.SpriteLeaser leaser, int count)
	{
		if (leaser.sprites is null) leaser.sprites = new FSprite[count];
		else
		{
			Array.Resize(ref leaser.sprites, count);
		}
	}
	public static void UntetherSprites(this RoomCamera.SpriteLeaser leaser)
	{
		foreach (FSprite sprite in leaser?.sprites ?? []) sprite.RemoveFromContainer();
	}
}