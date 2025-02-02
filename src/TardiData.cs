namespace NewTerra
{
	public class TardiData
	{
		public int startOfSprites;

		public bool spritesInitialized = false;

		public int totalAddedSprites = 2;

		public bool CanGrabWithExtraHands => canGrabWithExtraHandsCounter <= 0;
		public int canGrabWithExtraHandsCounter = 0;
		
		public int ArmSprite(int i)
		{
			return startOfSprites + i;
		}
	}
}
