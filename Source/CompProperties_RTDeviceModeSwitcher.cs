using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	class CompProperties_RTDeviceModeSwitcher : CompProperties
	{
		public bool canSwitchModes = false;

		public CompProperties_RTDeviceModeSwitcher()
        {
			compClass = typeof(CompRTDeviceModeSwitcher);
        }
    }
}
