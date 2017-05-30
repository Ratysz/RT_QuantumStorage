using Verse;
using UnityEngine;

namespace RT_QuantumStorage
{
	class Mod : Verse.Mod
	{
		public static ResearchMod_IncreaseCapacity resModCapacity;
		public static ResearchMod_EnableModeSwitch resModSwitch;

		public Mod(ModContentPack content) : base(content)
		{

		}
	}

	[StaticConstructorOnStartup]
	public static class Resources
	{
		public static Texture2D sparklesButtonTexture = ContentFinder<Texture2D>.Get("RT_UI/Sparkles", true);
		public static Texture2D leftArrowTexture = ContentFinder<Texture2D>.Get("RT_UI/Left", true);
		public static Texture2D rightArrowTexture = ContentFinder<Texture2D>.Get("RT_UI/Right", true);
		public static Texture2D chunkSiloTexture = ContentFinder<Texture2D>.Get("RT_UI/RockLowA", true);
		public static Texture2D stockpileTexture = ContentFinder<Texture2D>.Get("RT_UI/MetalA", true);
	}
}
