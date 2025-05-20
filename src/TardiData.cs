namespace NewTerra
{
	public class TardiData
	{
		public int startOfSprites;

		public bool spritesInitialized = false;

		public int totalAddedSprites = 2;

		public bool CanGrabWithExtraHands => canGrabWithExtraHandsCounter <= 0;
		public int canGrabWithExtraHandsCounter = 0;
		public int switchGraspsCounter = 0;

		public float poisonStunAmount = 0.0f;
		public int poisonStunCounter = 0;
		public int poisonStunLimit = 0;

		public int ArmSprite(int i)
		{
			return startOfSprites + i;
		}
	}
}
