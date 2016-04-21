using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	class CompProperties_RTQuantumWarehouse : CompProperties
	{
		public CompProperties_RTQuantumWarehouse()
        {
            compClass = typeof(CompRTQuantumWarehouse);
        }
	}
}
