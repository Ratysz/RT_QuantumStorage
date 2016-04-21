using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace RT_QuantumStorage
{
	public static class Utilities
	{
		public static bool CanAbsorb(this Thing thing1, Thing thing2)
		{
			return (thing1 != null
				&& thing2 != null
				&& thing1 != thing2
				&& thing1.def == thing2.def
				&& thing1.stackCount > 0
				&& thing2.stackCount > 0
				&& thing1.stackCount < thing1.def.stackLimit);
		}

		public static CompRTQuantumWarehouse FindWarehouse(this Thing thing)
		{
			if (thing != null)
			{
				Zone_Stockpile zoneStockpile = null;
				foreach (IntVec3 cell in thing.OccupiedRect().Cells)
				{
					Zone zone = cell.GetZone();
					if (zone != null && zone.GetType().ToString().Equals("RimWorld.Zone_Stockpile"))
					{
						zoneStockpile = zone as Zone_Stockpile;
						break;
					}
				}
				if (zoneStockpile != null)
				{
					return zoneStockpile.FindWarehouse();
				}
			}
			return null;
		}

		public static CompRTQuantumWarehouse FindWarehouse(this Zone_Stockpile zoneStockpile)
		{
			if (zoneStockpile != null)
			{
				CompRTQuantumWarehouse compWarehouse = null;
				foreach (Thing thingWithComps in zoneStockpile.AllContainedThings.ToList<Thing>())
				{
					compWarehouse = thingWithComps.TryGetComp<CompRTQuantumWarehouse>();
					if (compWarehouse != null)
					{
						return compWarehouse;
					}
				}
			}
			return null;
		}

		public static List<Thing> GetItemList(this IntVec3 cell, bool includeChunks = false)
		{
			List<Thing> list = new List<Thing>();
			foreach (Thing thing in cell.GetThingList())
			{
				if (thing.def.category == ThingCategory.Item)
				{
					list.Add(thing);
				}
			}
			if (!includeChunks)
			{
				list.RemoveAll(x => x.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("StoneChunks")));
				list.RemoveAll(x => x.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("Chunks")));
			}
			return list;
		}

		public static bool IsChunk(this Thing thing)
		{
			return (thing != null
				&& (thing.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("StoneChunks"))
				|| thing.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("Chunks"))));
		}

		public static bool AllowedToAccept(this IntVec3 cell, Thing thing)
		{
			if (cell.GetZone() != null && cell.GetZone().GetType().ToString().Equals("RimWorld.Zone_Stockpile"))
			{
				return ((Zone_Stockpile)cell.GetZone()).GetStoreSettings().AllowedToAccept(thing);
			}
			Building_Storage buildingStorage = (Building_Storage)cell.GetThingList().Find(x => x.GetType().ToString().Equals("RimWorld.Building_Storage"));
			if (buildingStorage != null)
			{
				return buildingStorage.GetStoreSettings().AllowedToAccept(thing);
			}
			return false;
		}

		public static bool AllowedToAccept(this IntVec3 cell, ThingDef thingDef)
		{
			Thing thing = ThingMaker.MakeThing(thingDef);
			return cell.AllowedToAccept(thing);
		}

		public static StoragePriority Priority(this IntVec3 cell)
		{
			if (cell.GetZone() != null && cell.GetZone().GetType().ToString().Equals("RimWorld.Zone_Stockpile"))
			{
				return ((Zone_Stockpile)cell.GetZone()).GetStoreSettings().Priority;
			}
			Building_Storage buildingStorage = (Building_Storage)cell.GetThingList().Find(x => x.GetType().ToString().Equals("RimWorld.Building_Storage"));
			if (buildingStorage != null)
			{
				return buildingStorage.GetStoreSettings().Priority;
			}
			return StoragePriority.Unstored;
		}

		public static void ThrowSparkle(this IntVec3 cell)
		{
			MoteThrower.ThrowLightningGlow(cell.ToVector3() + new Vector3(0.5f, 0.0f, 0.5f), 0.05f);
		}

		public static void ThrowDustPuff(this IntVec3 cell)
		{
			MoteThrower.ThrowDustPuff(cell.ToVector3() + new Vector3(0.5f, 0.0f, 0.5f), 0.5f);
		}

		public static void DropSound(this IntVec3 cell, ThingDef thingDef)
		{
			if (thingDef.soundDrop != null)
			{
				thingDef.soundDrop.PlayOneShot(cell);
			}
		}

		public static void Shuffle<T>(this IList<T> list)
		{
			System.Random random = new System.Random();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
