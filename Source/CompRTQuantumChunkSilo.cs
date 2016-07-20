using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class CompRTQuantumChunkSilo : ThingComp
	{
		private CompProperties_RTQuantumChunkSilo properties
		{
			get
			{
				return (CompProperties_RTQuantumChunkSilo)props;
			}
		}
		public int maxChunks
		{
			get
			{
				return (properties.maxChunks + ResearchModsSpecial.chunkSilosExtraChunksPerCell - 1)
					* parent.OccupiedRect().Cells.Count();
			}
		}

		private CompPowerTrader compPowerTrader;
		private List<ChunkRecord> chunkRecords = new List<ChunkRecord>();
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
			if (compWarehouse != null) compWarehouse.DeRegisterChunkSilo(this);
			if (parent == null || parent.Destroyed) EmptyOut();
		}

		public override void CompTick()
		{
			QuantumChunkSiloTick(5);
		}

		public override string CompInspectStringExtra()
		{
			IntVec3 totals = ChunkTotals();
			string inspectStringExtra = "CompRTQuantumChunkSilo_ChunksContained".Translate()
				+ " " + totals.x + "/" + totals.y + "/" + totals.z + "/" + maxChunks;
			if (compWarehouse != null && compWarehouse.parent != parent)
			{
				inspectStringExtra = inspectStringExtra + System.Environment.NewLine
					+ "CompRTQuantumChunkSilo_QWConnect".Translate();
			}
			return inspectStringExtra;
		}

		public override IEnumerable<Command> CompGetGizmosExtra()
		{
			Command_Toggle commandSparkles = new Command_Toggle();
			commandSparkles.isActive = () => sparklesEnabled;
			commandSparkles.toggleAction = () => sparklesEnabled = !sparklesEnabled;
			commandSparkles.groupKey = 4719905;
			commandSparkles.icon = Resources.sparklesButtonTexture;
			commandSparkles.defaultLabel = "CompRTQuantumChunkSilo_SparklesToggle".Translate();
			if (sparklesEnabled)
			{
				commandSparkles.defaultDesc = "CompRTQuantumChunkSilo_SparklesAreOn".Translate();
			}
			else
			{
				commandSparkles.defaultDesc = "CompRTQuantumChunkSilo_SparklesAreOff".Translate();
			}
			yield return commandSparkles;
		}

		public override void PostExposeData()
		{
			Scribe_Values.LookValue(ref sparklesEnabled, "sparklesEnabled", false);
			Scribe_Collections.LookList(ref chunkRecords, "chunkRecords", LookMode.Deep);
		}

		private void QuantumChunkSiloTick(int tickAmount)
		{
			if ((Find.TickManager.TicksGame + tickStagger) % tickAmount == 0)
			{
				if ((compPowerTrader == null || compPowerTrader.PowerOn)
					&& parent.OccupiedRect().Center.Priority() != StoragePriority.Unstored)
				{
					if (compWarehouse == null)
					{
						compWarehouse = parent.FindWarehouse();
						if (compWarehouse != null) compWarehouse.RegisterChunkSilo(this);
					}
					else
					{
						if (compWarehouse.parent == null || compWarehouse.parent.Destroyed || !compWarehouse.IsValid())
						{
							compWarehouse.DeRegisterChunkSilo(this);
							compWarehouse = null;
						}
					}
					if (cellIndex >= cells.Count)
					{
						cellIndex = 0;
					}
					ProcessCell(cells[cellIndex]);
					PopulateCells();
					cellIndex++;
				}
				else
				{
					if (compWarehouse != null)
					{
						compWarehouse.DeRegisterChunkSilo(this);
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
			IntVec3 cellReceiving = parent.OccupiedRect().Center;
			if (thingToReceive != null
				&& thingToReceive.Spawned
				&& !thingToReceive.Destroyed
				&& thingToReceive.IsChunk()
				&& ChunkTotalFast() < maxChunks
				&& cellReceiving.AllowedToAccept(thingToReceive)
				&& cellReceiving.Priority() >= thingToReceive.Position.Priority())
			{
				if (compWarehouse != null) compWarehouse.buffer.RemoveAll(x => x == thingToReceive);
				ChunkRecord chunkRecord = chunkRecords.Find(x => x.chunkDef == thingToReceive.def);
				if (chunkRecord != null)
				{
					chunkRecord.amount++;
				}
				else
				{
					chunkRecord = new ChunkRecord();
					chunkRecord.amount = 1;
					chunkRecord.chunkDef = thingToReceive.def;
					chunkRecords.Add(chunkRecord);
				}
				thingToReceive.Position.ThrowDustPuff();
				thingToReceive.Position.DropSound(thingToReceive.def);
				thingToReceive.DeSpawn();
				return true;
			}
			return false;
		}

		private Thing ProduceThing()
		{
			if (chunkRecords.Count != 0)
			{
				ChunkRecord chunkRecord = chunkRecords.RandomElement();
				chunkRecord.amount--;
				if (chunkRecord.amount <= 0)
				{
					chunkRecords.Remove(chunkRecord);
				}
				return ThingMaker.MakeThing(chunkRecord.chunkDef);
			}
			return null;
		}

		private void UnProduceThing(Thing thing)
		{
			if (thing != null)
			{
				ChunkRecord chunkRecord = chunkRecords.Find(x => x.chunkDef == thing.def);
				if (chunkRecord != null)
				{
					chunkRecord.amount++;
				}
				else
				{
					chunkRecord = new ChunkRecord();
					chunkRecord.amount = 1;
					chunkRecord.chunkDef = thing.def;
					chunkRecords.Add(chunkRecord);
				}
			}
		}

		public int ChunkTotalFast()
		{
			int num = 0;
			foreach (ChunkRecord chunkRecord in chunkRecords)
			{
				num = num + chunkRecord.amount;
			}
			return num;
		}

		public IntVec3 ChunkTotals()
		{
			int total = 0;
			int metal = 0;
			int stone = 0;
			foreach (ChunkRecord chunkRecord in chunkRecords)
			{
				total += chunkRecord.amount;
				if (chunkRecord.chunkDef.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("Chunks")))
				{
					metal += chunkRecord.amount;
				}
				else
				{
					stone += chunkRecord.amount;
				}
			}
			return new IntVec3(metal, stone, total);
		}

		private void PopulateCells()
		{
			foreach (IntVec3 cell in parent.OccupiedRect().Cells)
			{
				if (cell.GetItemList(true).Count == 0)
				{
					Thing thing = ProduceThing();
					if (thing != null)
					{
						if (cell.AllowedToAccept(thing) && GenPlace.TryPlaceThing(thing, cell, ThingPlaceMode.Direct))
						{
							cell.DropSound(thing.def);
						}
						else
						{
							UnProduceThing(thing);
						}
					}
				}
			}
		}

		public void EmptyOut()
		{
			while (chunkRecords.Count != 0)
			{
				Thing thing = ProduceThing();
				IntVec3 cell = parent.OccupiedRect().RandomCell;
				if (GenPlace.TryPlaceThing(thing, cell, ThingPlaceMode.Near))
				{
					cell.DropSound(thing.def);
				}
			}
		}
	}

	public class ChunkRecord : IExposable
	{
		public ThingDef chunkDef;
		public int amount;

		public void ExposeData()
		{
			Scribe_Defs.LookDef(ref chunkDef, "chunkDef");
			Scribe_Values.LookValue(ref amount, "amount", 0);
		}
	}
}
