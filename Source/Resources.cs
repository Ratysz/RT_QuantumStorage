using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
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
