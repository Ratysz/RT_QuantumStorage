using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class PlaceWorker_RTNoQSOverlap : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot)
		{
			IEnumerable<IntVec3> cells = GenAdj.CellsAdjacent8Way(loc, rot, checkingDef.Size).Union<IntVec3>(GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size));
			foreach (IntVec3 cell in cells)
			{
				List<Thing> things = Find.ThingGrid.ThingsListAt(cell);
				foreach (Thing thing in things)
				{
					if (thing.TryGetComp<CompRTQuantumStockpile>() != null
						|| thing.TryGetComp<CompRTQuantumChunkSilo>() != null)
					{
						return "PlaceWorker_RTNoQSOverlap".Translate();
					}
					else if (thing.def.entityDefToBuild != null)
					{
						ThingDef thingDef = thing.def.entityDefToBuild as ThingDef;
						Thing simThing = ThingMaker.MakeThing(thingDef);
						if (simThing != null
							&& (simThing.TryGetComp<CompRTQuantumStockpile>() != null
							|| simThing.TryGetComp<CompRTQuantumStockpile>() != null))
						{
							return "PlaceWorker_RTNoQSOverlap".Translate();
						}
					}
				}
			}
			return true;
		}
	}
}
