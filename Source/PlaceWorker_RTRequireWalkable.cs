using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class PlaceWorker_RTRequireWalkable : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot)
		{
			IEnumerable<IntVec3> cells = GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size);
			foreach (IntVec3 cell in cells)
			{
				if (!cell.Walkable())
				{
					return "PlaceWorker_RTRequireWalkable".Translate();
				}
			}
			return true;
		}
	}
}
