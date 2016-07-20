using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class CompRTQuantumStockpile : ThingComp
	{
		private CompProperties_RTQuantumStockpile properties
		{
			get
			{
				return (CompProperties_RTQuantumStockpile)props;
			}
		}
		public int maxStacks
		{
			get
			{
				return properties.maxStacks + ResearchModsSpecial.stockpilesExtraStacksPerCell;
			}
		}

		private CompPowerTrader compPowerTrader;
		private List<IntVec3> cells = new List<IntVec3>();
		private int cellIndex = 0;
		private CompRTQuantumWarehouse compWarehouse = null;
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
		}

		public override void PostDeSpawn()
		{
			if (compWarehouse != null) compWarehouse.DeRegisterStockpile(this);
		}

		public override void CompTick()
		{
			QuantumStockpileTick(5);
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("CompRTQuantumStockpile_MaxStacks".Translate());
			stringBuilder.Append(" ");
			stringBuilder.Append(maxStacks.ToString("F0"));
			if (compWarehouse != null)
			{
				if (compWarehouse.parent != parent)
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("CompRTQuantumStockpile_QWConnect".Translate());
				}
			}
			return stringBuilder.ToString();
		}

		public override IEnumerable<Command> CompGetGizmosExtra()
		{
			Command_Toggle command = new Command_Toggle();
			command.isActive = () => sparklesEnabled;
			command.toggleAction = () => sparklesEnabled = !sparklesEnabled;
			command.groupKey = 02134657;
			command.icon = Resources.sparklesButtonTexture;
			command.defaultLabel = "CompRTQuantumStockpile_SparklesToggle".Translate();
			if (sparklesEnabled)
			{
				command.defaultDesc = "CompRTQuantumStockpile_SparklesAreOn".Translate();
			}
			else
			{
				command.defaultDesc = "CompRTQuantumStockpile_SparklesAreOff".Translate();
			}
			yield return command;
		}

		public override void PostExposeData()
		{
			Scribe_Values.LookValue(ref sparklesEnabled, "sparklesEnabled", false);
		}

		private void QuantumStockpileTick(int tickAmount)
		{
			if ((Find.TickManager.TicksGame + tickStagger) % tickAmount == 0)
			{
				if ((compPowerTrader == null || compPowerTrader.PowerOn)
					&& parent.OccupiedRect().Center.Priority() != StoragePriority.Unstored)
				{
					if (compWarehouse == null)
					{
						compWarehouse = parent.FindWarehouse();
						if (compWarehouse != null) compWarehouse.RegisterStockpile(this);
					}
					else
					{
						if (compWarehouse.parent == null || compWarehouse.parent.Destroyed || !compWarehouse.IsValid())
						{
							compWarehouse.DeRegisterStockpile(this);
							compWarehouse = null;
						}
					}
					if (cellIndex >= cells.Count)
					{
						cellIndex = 0;
						if (compWarehouse == null)
						{
							DefragStacks();
						}
						else
						{
							ProcessCell(cells[cellIndex]);
							cellIndex++;
						}
					}
					else
					{
						ProcessCell(cells[cellIndex]);
						cellIndex++;
					}
				}
				else
				{
					if (compWarehouse != null)
					{
						compWarehouse.DeRegisterStockpile(this);
						compWarehouse = null;
					}
				}
			}
		}

		public void ProcessCell(IntVec3 cell)
		{
			if (sparklesEnabled) cell.ThrowSparkle();
			if (cell.Priority() != StoragePriority.Unstored)
			{
				List<Thing> cellThings = cell.GetItemList(true);
				for (int i = 0; i < cellThings.Count; i++)
				{
					Thing cellThing = cellThings[i];
					if (!ReceiveThing(cellThing) && compWarehouse != null)
					{
						compWarehouse.QueueThing(cellThing);
					}
					else
					{
						break;
					}
				}
			}
		}

		public bool ReceiveThing(Thing thingToReceive)
		{
			if (thingToReceive != null && !thingToReceive.IsChunk())
			{
				if (compWarehouse != null) compWarehouse.buffer.RemoveAll(x => x == thingToReceive);
				foreach (IntVec3 cellReceiving in parent.OccupiedRect().Cells)
				{
					if (cellReceiving.AllowedToAccept(thingToReceive)
						&& cellReceiving.Priority() >= thingToReceive.Position.Priority())
					{
						IntVec3 thingToReceiveCell = thingToReceive.Position;
						List<Thing> thingsReceiving = cellReceiving.GetItemList();
						if (thingsReceiving.Count < maxStacks)
						{
							thingToReceiveCell.ThrowDustPuff();
							Thing thing = GenSpawn.Spawn(thingToReceive.SplitOff(thingToReceive.stackCount), cellReceiving);
							cellReceiving.DropSound(thing.def);
							SlotGroup slotGroup = cellReceiving.GetSlotGroup();
							if (slotGroup != null && slotGroup.parent != null)
							{
								slotGroup.parent.Notify_ReceivedThing(thing);
							}
							return true;
						}
						else
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
			return false;
		}

		public void DefragStacks()
		{
			foreach (IntVec3 targetCell in GenAdj.CellsOccupiedBy(parent))
			{
				if (sparklesEnabled) targetCell.ThrowSparkle();
				foreach (IntVec3 sourceCell in GenAdj.CellsOccupiedBy(parent))
				{
					List<Thing> targetThings = targetCell.GetItemList(true);
					for (int i = 0; i < targetThings.Count(); i++)
					{
						bool itemWasMoved = false;
						Thing targetThing = targetThings[i];
						if (targetThing != null)
						{
							if (targetThing.IsChunk())
							{
								if (compWarehouse != null) compWarehouse.QueueThing(targetThing);
								itemWasMoved = true;
								break;
							}
							else
							{
								if (targetCell.AllowedToAccept(targetThing))
								{
									List<Thing> sourceThings = sourceCell.GetItemList();
									for (int j = 0; j < sourceThings.Count(); j++)
									{
										Thing sourceThing = sourceThings[j];
										if (targetThing.CanAbsorb(sourceThing)
											&& sourceThing.stackCount < sourceThing.def.stackLimit)
										{
											int targetStackCount = targetThing.stackCount;
											if (targetThing.TryAbsorbStack(sourceThing, true))
											{
												sourceCell.ThrowDustPuff();
											}
											if (targetStackCount != targetThing.stackCount)
											{
												ForbidUtility.SetForbidden(targetThing, false, false);
												targetCell.DropSound(targetThing.def);
											}
											itemWasMoved = true;
											break;
										}
									}
								}
							}
						}
						if (itemWasMoved) break;
					}
				}
			}
		}
	}
}
