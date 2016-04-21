using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
    public static class ResearchModsSpecial
    {
		public static int stockpilesExtraStacksPerCell = 0;
		public static int chunkSilosExtraChunksPerCell = 0;
		public static bool modeSwitchEnabled = false;

		public static void EnableModeSwitch()
		{
			modeSwitchEnabled = true;
		}

		public static void ResetCapacity()
		{
			stockpilesExtraStacksPerCell = 0;
			chunkSilosExtraChunksPerCell = 0;
		}

		public static void IncreaseCapacity()
		{
			stockpilesExtraStacksPerCell++;
			chunkSilosExtraChunksPerCell++;
			chunkSilosExtraChunksPerCell++;
		}
    }
}
