using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	class CompProperties_RTQuantumChunkSilo : CompProperties
    {
		public int maxChunks = 4;

		public CompProperties_RTQuantumChunkSilo()
        {
			compClass = typeof(CompRTQuantumChunkSilo);
        }
    }
}
