using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class CompRTQuantumRelay : ThingComp
	{
		private CompProperties_RTQuantumRelay properties
		{
			get
			{
				return (CompProperties_RTQuantumRelay)props;
			}
		}

		private CompPowerTrader compPowerTrader;
		private List<IntVec3> cells = new List<IntVec3>();
		private int cellIndex = 0;
		private Thing quantumWarehouse;
		private CompRTQuantumWarehouse compWarehouse;
		private bool registered = false;
		private int tickStagger;
		private static int lastTickStagger;

		private bool sparklesEnabled = false;
		
		public override void PostSpawnSetup()
		{
			compPowerTrader = parent.TryGetComp<CompPowerTrader>();

			lastTickStagger++;
			tickStagger = lastTickStagger;

			cellIndex = tickStagger;
			cells = GenAdj.CellsAdjacent8Way(parent).ToList();
			while (cellIndex > cells.Count)
			{
				cellIndex -= cells.Count;
			}

			if (quantumWarehouse != null)
			{
				compWarehouse = quantumWarehouse.TryGetComp<CompRTQuantumWarehouse>();
			}
		}

		public override void PostDeSpawn()
		{
			if (compWarehouse != null)
			{
				compWarehouse.DeRegisterRelay(this);
				quantumWarehouse = null;
			}
		}

		public override void CompTick()
		{
			QuantumRelayTick(5);
		}

		public override string CompInspectStringExtra()
		{
			if (compWarehouse != null)
			{
				if (compWarehouse.parent != parent)
				{
					Zone zone = compWarehouse.parent.OccupiedRect().Center.GetZone();
					if (zone != null)
					{
						return "CompRTQuantumRelay_QWConnected".Translate() + " " + zone.label;
					}
				}
				else
				{
					return "CompRTQuantumRelay_QWIsSelf".Translate();
				}
			}
			else
			{
				return "CompRTQuantumRelay_QWIsNull".Translate();
			}
			return null;
		}

		public override IEnumerable<Command> CompGetGizmosExtra()
		{
			Command_Toggle commandSparkles = new Command_Toggle();
			commandSparkles.isActive = () => sparklesEnabled;
			commandSparkles.toggleAction = () => sparklesEnabled = !sparklesEnabled;
			commandSparkles.groupKey = 95918723;
			commandSparkles.icon = Resources.sparklesButtonTexture;
			commandSparkles.defaultLabel = "CompRTQuantumRelay_SparklesToggle".Translate();
			if (sparklesEnabled)
			{
				commandSparkles.defaultDesc = "CompRTQuantumRelay_SparklesAreOn".Translate();
			}
			else
			{
				commandSparkles.defaultDesc = "CompRTQuantumRelay_SparklesAreOff".Translate();
			}
			yield return commandSparkles;

			Command_Action commandWarehousePrev = new Command_Action();
			commandWarehousePrev.icon = Resources.leftArrowTexture;
			commandWarehousePrev.groupKey = 56182375;
			commandWarehousePrev.defaultLabel = "CompRTQuantumRelay_WarehousePrevLabel".Translate();
			commandWarehousePrev.defaultDesc = "CompRTQuantumRelay_WarehousePrevDesc".Translate();
			commandWarehousePrev.action = delegate
			{
				registered = false;
				if (compWarehouse != null) compWarehouse.DeRegisterRelay(this);
				List<Zone> zones = Find.ZoneManager.AllZones;
				if (zones != null && zones.Count != 0)
				{
					List<Zone_Stockpile> stockpileZones =
						(from zone in zones
						 where zone.GetType() == typeof(Zone_Stockpile)
						 select zone as Zone_Stockpile).ToList();
					if (stockpileZones != null && stockpileZones.Count != 0)
					{
						List<CompRTQuantumWarehouse> warehouses =
							(from zone in stockpileZones
							 where zone.FindWarehouse() != null
							 select zone.FindWarehouse()).ToList();
						if (warehouses != null && warehouses.Count != 0)
						{
							if (compWarehouse != null)
							{
								CompRTQuantumWarehouse warehouse = warehouses.Find((CompRTQuantumWarehouse t) => t == compWarehouse);
								if (warehouse != null)
								{
									int warehouseIndex = warehouses.FindIndex((CompRTQuantumWarehouse t) => t == compWarehouse);
									if (warehouseIndex == 0)
									{
										compWarehouse = warehouses[warehouses.Count - 1];
									}
									else
									{
										compWarehouse = warehouses[warehouseIndex - 1];
									}
								}
								else
								{
									compWarehouse = warehouses.First();
								}
							}
							else
							{
								compWarehouse = warehouses.First();
							}
						}
					}
				}
			};
			yield return commandWarehousePrev;

			Command_Action commandWarehouseNext = new Command_Action();
			commandWarehouseNext.icon = Resources.rightArrowTexture;
			commandWarehouseNext.groupKey = 91915621;
			commandWarehouseNext.defaultLabel = "CompRTQuantumRelay_WarehouseNextLabel".Translate();
			commandWarehouseNext.defaultDesc = "CompRTQuantumRelay_WarehouseNextDesc".Translate();
			commandWarehouseNext.action = delegate
			{
				registered = false;
				if (compWarehouse != null) compWarehouse.DeRegisterRelay(this);
				List<Zone> zones = Find.ZoneManager.AllZones;
				if (zones != null && zones.Count != 0)
				{
					List<Zone_Stockpile> stockpileZones =
						(from zone in zones
						 where zone.GetType() == typeof(Zone_Stockpile)
						 select zone as Zone_Stockpile).ToList();
					if (stockpileZones != null && stockpileZones.Count != 0)
					{
						List<CompRTQuantumWarehouse> warehouses =
							(from zone in stockpileZones
							 where zone.FindWarehouse() != null
							 select zone.FindWarehouse()).ToList();
						if (warehouses != null && warehouses.Count != 0)
						{
							if (compWarehouse != null)
							{
								CompRTQuantumWarehouse warehouse = warehouses.Find((CompRTQuantumWarehouse t) => t == compWarehouse);
								if (warehouse != null)
								{
									int warehouseIndex = warehouses.FindIndex((CompRTQuantumWarehouse t) => t == compWarehouse);
									if (warehouseIndex == warehouses.Count - 1)
									{
										compWarehouse = warehouses.First();
									}
									else
									{
										compWarehouse = warehouses[warehouseIndex + 1];
									}
								}
								else
								{
									compWarehouse = warehouses.First();
								}
							}
							else
							{
								compWarehouse = warehouses.First();
							}
						}
					}
				}
			};
			yield return commandWarehouseNext;
		}

		public override void PostExposeData()
		{
			if (compWarehouse != null) quantumWarehouse = compWarehouse.parent;
			Scribe_Values.LookValue(ref sparklesEnabled, "sparklesEnabledQR", false);
			Scribe_References.LookReference<Thing>(ref quantumWarehouse, "quantumWarehouse");
		}

		private void QuantumRelayTick(int tickAmount)
		{
			if ((Find.TickManager.TicksGame + tickStagger) % tickAmount == 0)
			{
				if (compPowerTrader == null || compPowerTrader.PowerOn)
				{
					if (compWarehouse != null
						&& compWarehouse.parent != null
						&& !compWarehouse.parent.Destroyed
						&& compWarehouse.IsValid())
					{
						if (!registered)
						{
							compWarehouse.RegisterRelay(this);
							registered = true;
						}
						else
						{
							if (cellIndex >= cells.Count)
							{
								cellIndex = 0;
							}
							ProcessCell(cells[cellIndex]);
							cellIndex++;
						}
					}
					else
					{
						registered = false;
						if (compWarehouse != null) compWarehouse.DeRegisterRelay(this);
					}
				}
				else
				{
					registered = false;
					if (compWarehouse != null) compWarehouse.DeRegisterRelay(this);
				}
			}
		}

		public void ProcessCell(IntVec3 cell)
		{
			if (sparklesEnabled) cell.ThrowSparkle();
			if (cell.Priority() != StoragePriority.Unstored && compWarehouse != null)
			{
				List<Thing> cellThings = cell.GetItemList(true);
				for (int i = 0; i < cellThings.Count; i++)
				{
					Thing cellThing = cellThings[i];
					compWarehouse.QueueThing(cellThing);
				}
			}
		}

		public bool ReceiveThing(Thing thingToReceive)
		{
			if (thingToReceive != null)
			{
				if (compWarehouse != null) compWarehouse.buffer.RemoveAll(x => x == thingToReceive);
				foreach (IntVec3 cellReceiving in parent.OccupiedRect().Cells)
				{
					if (cellReceiving.AllowedToAccept(thingToReceive)
						&& cellReceiving.Priority() >= thingToReceive.Position.Priority())
					{
						IntVec3 thingToReceiveCell = thingToReceive.Position;
						List<Thing> thingsReceiving = cellReceiving.GetItemList(true);
						if (thingsReceiving.Count == 0)
						{
							if (thingToReceive.IsChunk())
							{
								Thing chunk = ThingMaker.MakeThing(thingToReceive.def);
								if (GenPlace.TryPlaceThing(chunk, cellReceiving, ThingPlaceMode.Direct))
								{
									cellReceiving.DropSound(chunk.def);
									thingToReceive.Position.ThrowDustPuff();
									thingToReceive.DeSpawn();
								}
							}
							else
							{
								thingToReceiveCell.ThrowDustPuff();
								Thing thing = GenSpawn.Spawn(thingToReceive.SplitOff(thingToReceive.stackCount), cellReceiving);
								cellReceiving.DropSound(thing.def);
								SlotGroup slotGroup = cellReceiving.GetSlotGroup();
								if (slotGroup != null && slotGroup.parent != null)
								{
									slotGroup.parent.Notify_ReceivedThing(thing);
								}
							}
							return true;
						}
						else
						{
							if (!thingToReceive.IsChunk())
							{
								foreach (Thing thingReceiving in thingsReceiving)
								{
									if (thingReceiving.CanAbsorb(thingToReceive))
									{
										int thingReceivingStackCount = thingReceiving.stackCount;
										if (thingReceiving.TryAbsorbStack(thingToReceive, true))
										{
											thingToReceiveCell.ThrowDustPuff();
										}
										if (thingReceivingStackCount != thingReceiving.stackCount)
										{
											ForbidUtility.SetForbidden(thingReceiving, false, false);
											cellReceiving.DropSound(thingReceiving.def);
										}
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
	}
}
