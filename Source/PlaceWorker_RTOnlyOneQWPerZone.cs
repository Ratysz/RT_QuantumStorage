﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
    public class PlaceWorker_RTOnlyOneQWPerZone : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Thing thingToIgnore = null)
        {
            IEnumerable<IntVec3> cells = GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size);
            foreach (IntVec3 cell in cells)
            {
                Zone_Stockpile zoneStockpile = cell.GetZone(Map) as Zone_Stockpile;
				if (zoneStockpile != null && zoneStockpile.FindWarehouse() != null)
				{
					return "PlaceWorker_RTOnlyOneQWPerZone".Translate();
				}
            }
            return true;
        }
    }
}
