using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	class CompProperties_RTQuantumStockpile : CompProperties
    {
        public int maxStacks = 4;

		public CompProperties_RTQuantumStockpile()
        {
			compClass = typeof(CompRTQuantumStockpile);
        }
	}
}
