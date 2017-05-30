using System;

using Verse;

namespace RT_QuantumStorage
{
	public class ResearchMod_IncreaseCapacity : ResearchMod
	{
		public int stockpilesExtraStacksPerCell = 0;
		public int chunkSilosExtraChunksPerCell = 0;

		public override void Apply()
		{
			Mod.resModCapacity = this;
			stockpilesExtraStacksPerCell++;
			chunkSilosExtraChunksPerCell++;
			chunkSilosExtraChunksPerCell++;
		}
	}

	public class ResearchMod_EnableModeSwitch : ResearchMod
	{
		public bool modeSwitchEnabled = false;

		public override void Apply()
		{
			Mod.resModSwitch = this;
			modeSwitchEnabled = true;
		}
	}
}
