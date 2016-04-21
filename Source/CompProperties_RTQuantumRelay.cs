using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	class CompProperties_RTQuantumRelay : CompProperties
	{
		public CompProperties_RTQuantumRelay()
		{
			compClass = typeof(CompRTQuantumRelay);
		}
	}
}