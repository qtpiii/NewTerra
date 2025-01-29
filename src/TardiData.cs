namespace NewTerra
{
	public class TardiData
	{
		public int startOfSprites;

		public bool spritesInitialized = false;

		public int totalAddedSprites = 2;
		
		public int ArmSprite(int i)
		{
			return startOfSprites + i;
		}
	}
}
