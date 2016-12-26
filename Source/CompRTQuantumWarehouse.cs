using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class CompRTQuantumWarehouse : ThingComp
	{
		private CompProperties_RTQuantumWarehouse properties
		{
			get
			{
				return (CompProperties_RTQuantumWarehouse)props;
			}
		}

		private CompPowerTrader compPowerTrader;
		private bool valid;
		private List<CompRTQuantumStockpile> compStockpiles;
		private List<CompRTQuantumChunkSilo> compChunkSilos;
		private List<CompRTQuantumRelay> compRelays;
		private int qsTargetIndex = 0;
		private int qsSourceIndex = 1;
		private int qcsTargetIndex = 0;
		private int qcsSourceIndex = 1;
		private int qrIndex = 0;
		public List<Thing> buffer = new List<Thing>();
		private readonly int bufferSize = 32;
		private readonly int bufferLifespan = 64;
		private int bufferTicker = 0;
		private int tickStagger;
		private static int lastTickStagger;

		private bool sparklesEnabled = false;

		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();
			compPowerTrader = parent.TryGetComp<CompPowerTrader>();

			lastTickStagger++;
			tickStagger = lastTickStagger;

			compStockpiles = new List<CompRTQuantumStockpile>();
			compChunkSilos = new List<CompRTQuantumChunkSilo>();
			compRelays = new List<CompRTQuantumRelay>();

			valid = false;
		}

		public override void CompTick()
		{
			QuantumWarehouseTick(5);
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (valid)
			{
				bool newLine = false;
				if (compChunkSilos.Count != 0 || compStockpiles.Count != 0)
				{
					stringBuilder.Append("CompRTQuantumWarehouse_ConnectedQS".Translate());
					stringBuilder.Append(" ");
					stringBuilder.Append((compStockpiles.Count + compChunkSilos.Count).ToString("F0"));
					newLine = true;
				}
				if (compRelays.Count != 0)
				{
					if (newLine) stringBuilder.AppendLine();
					stringBuilder.Append("CompRTQuantumWarehouse_ConnectedQR".Translate());
					stringBuilder.Append(" ");
					stringBuilder.Append(compRelays.Count.ToString("F0"));
				}
			}
			else
			{
				stringBuilder.Append("CompRTQuantumWarehouse_NotValid".Translate());
			}
			return stringBuilder.ToString();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Command_Toggle commandSparkles = new Command_Toggle();
			commandSparkles.isActive = () => sparklesEnabled;
			commandSparkles.toggleAction = () => sparklesEnabled = !sparklesEnabled;
			commandSparkles.groupKey = 296571;
			commandSparkles.icon = Resources.sparklesButtonTexture;
			commandSparkles.defaultLabel = "CompRTQuantumWarehouse_SparklesToggle".Translate();
			if (sparklesEnabled)
			{
				commandSparkles.defaultDesc = "CompRTQuantumWarehouse_SparklesAreOn".Translate();
			}
			else
			{
				commandSparkles.defaultDesc = "CompRTQuantumWarehouse_SparklesAreOff".Translate();
			}
			yield return commandSparkles;
		}

		public override void PostExposeData()
		{
			Scribe_Values.LookValue(ref sparklesEnabled, "sparklesEnabledQW", false);
		}

		private void QuantumWarehouseTick(int tickAmount)
		{
			if ((Find.TickManager.TicksGame + tickStagger) % tickAmount == 0)
			{
				if (compPowerTrader == null || compPowerTrader.PowerOn)
				{
					if (parent.OccupiedRect().CenterCell.Priority(parent.Map) == StoragePriority.Unstored)
					{
						valid = false;
					}
					if (valid)
					{
						if (compStockpiles.Count > 1)
						{
							qsSourceIndex++;
							if (qsSourceIndex >= compStockpiles.Count)
							{
								qsTargetIndex++;
								qsSourceIndex = qsTargetIndex + 1;
							}
							if (qsTargetIndex >= compStockpiles.Count - 1)
							{
								compStockpiles.Shuffle();
								qsTargetIndex = 0;
								qsSourceIndex = 1;
							}
							DefragStockpileStacks();
							DefragStockpileCells();
						}
						else if (compStockpiles.Count == 1)
						{
							CompRTQuantumStockpile compStockpile = compStockpiles.First();
							if (sparklesEnabled)
							{
								foreach (IntVec3 stockpileCell in compStockpile.parent.OccupiedRect().Cells)
								{
									stockpileCell.ThrowSparkle(parent.Map);
								}
							}
							compStockpile.DefragStacks();
						}

						if (compChunkSilos.Count > 1)
						{
							qcsSourceIndex++;
							if (qcsSourceIndex >= compChunkSilos.Count)
							{
								qcsTargetIndex++;
								qcsSourceIndex = qcsTargetIndex + 1;
							}
							if (qcsTargetIndex >= compChunkSilos.Count - 1)
							{
								compChunkSilos.Shuffle();
								qcsTargetIndex = 0;
								qcsSourceIndex = 1;
							}
							DefragChunkSilos();
						}
						else if (compChunkSilos.Count == 1)
						{
							CompRTQuantumChunkSilo compChunkSilo = compChunkSilos.First();
							foreach (IntVec3 chunkSiloCell in compChunkSilo.parent.OccupiedRect().Cells)
							{
								if (sparklesEnabled) chunkSiloCell.ThrowSparkle(parent.Map);
								List<Thing> cellThings = chunkSiloCell.GetItemList(parent.Map);
								if (cellThings.Count != 0)
								{
									QueueThing(cellThings.RandomElement());
								}
							}
						}

						if (compRelays.Count != 0)
						{
							if (qrIndex >= compRelays.Count)
							{
								qrIndex = 0;
							}
							SendItemsToRelay();
							qrIndex++;
						}

						if (buffer.Count != 0)
						{
							Thing thing = buffer.RandomElement();
							ReceiveThing(thing);
							buffer.Remove(thing);
							if (buffer.Count != 0)
							{
								if (bufferTicker == bufferLifespan)
								{
									bufferTicker = 0;
									buffer.Clear();
								}
								bufferTicker++;
							}
						}
					}
					else
					{
						CompRTQuantumWarehouse compWarehouse = parent.FindWarehouse();
						valid = (compWarehouse != null && compWarehouse == this
							&& parent.OccupiedRect().CenterCell.Priority(parent.Map) != StoragePriority.Unstored);
					}
				}
				else
				{
					valid = false;
				}
			}
		}

		public void QueueThing(Thing thing)
		{
			if (buffer.Count <= bufferSize && !buffer.Contains(thing))
			{
				buffer.Add(thing);
			}
		}

		public bool ReceiveThing(Thing thingToReceive)
		{
			if (compStockpiles.Count == 0 || !compStockpiles.RandomElement().ReceiveThing(thingToReceive))
			{
				if (compChunkSilos.Count == 0 || !compChunkSilos.RandomElement().ReceiveThing(thingToReceive))
				{
					return false;
				}
			}
			return true;
		}

		private void DefragStockpileStacks()
		{
			CompRTQuantumStockpile sourceStockpile = compStockpiles[qsSourceIndex];
			CompRTQuantumStockpile targetStockpile = compStockpiles[qsTargetIndex];
			foreach (IntVec3 targetCell in targetStockpile.parent.OccupiedRect().Cells)
			{
				if (sparklesEnabled) targetCell.ThrowSparkle(parent.Map);
				foreach (IntVec3 sourceCell in sourceStockpile.parent.OccupiedRect().Cells)
				{
					List<Thing> targetThings = targetCell.GetItemList(parent.Map);
					for (int i = 0; i < targetThings.Count(); i++)
					{
						bool itemWasMoved = false;
						Thing targetThing = targetThings[i];
						if (targetThing != null && targetCell.AllowedToAccept(parent.Map, targetThing))
						{
							List<Thing> sourceThings = sourceCell.GetItemList(parent.Map);
							for (int j = 0; j < sourceThings.Count(); j++)
							{
								Thing sourceThing = sourceThings[j];
								if (targetThing.CanAbsorb(sourceThing)
									&& sourceThing.stackCount < sourceThing.def.stackLimit)
								{
									int targetStackCount = targetThing.stackCount;
									if (targetThing.TryAbsorbStack(sourceThing, true))
									{
										sourceCell.ThrowDustPuff(parent.Map);
									}
									if (targetStackCount != targetThing.stackCount)
									{
										ForbidUtility.SetForbidden(targetThing, false, false);
										targetCell.DropSound(parent.Map, targetThing.def);
									}
									itemWasMoved = true;
									break;
								}
							}
						}
						if (itemWasMoved) break;
					}
				}
			}
		}

		private void DefragStockpileCells()
		{
			CompRTQuantumStockpile sourceStockpile = compStockpiles[qsSourceIndex];
			CompRTQuantumStockpile targetStockpile = compStockpiles[qsTargetIndex];
			foreach (IntVec3 sourceCell in sourceStockpile.parent.OccupiedRect().Cells)
			{
				if (sparklesEnabled) sourceCell.ThrowSparkle(parent.Map);
				List<Thing> sourceThingsWithChunks = sourceCell.GetItemList(parent.Map, true);
				foreach (Thing thing in sourceThingsWithChunks)
				{
					if (thing.IsChunk())
					{
						QueueThing(thing);
						return;
					}
				}
				foreach (IntVec3 targetCell in targetStockpile.parent.OccupiedRect().Cells)
				{
					List<Thing> targetThings = targetCell.GetItemList(parent.Map);
					List<Thing> sourceThings = sourceCell.GetItemList(parent.Map);
					Thing targetThing = (targetThings.Count == 0) ? (null) : (targetThings[0]);
					Thing sourceThing = (sourceThings.Count == 0) ? (null) : (sourceThings[0]);
					if (sourceThing != null && targetThings.Count < sourceThings.Count - 1
						&& targetCell.AllowedToAccept(parent.Map, sourceThing)
						&& sourceThing.stackCount > 0)
					{
						sourceCell.ThrowDustPuff(parent.Map);
						Thing thing = GenSpawn.Spawn(sourceThing.SplitOff(sourceThing.stackCount), targetCell, parent.Map);
						targetCell.DropSound(parent.Map, thing.def);
						SlotGroup slotGroup = targetCell.GetSlotGroup(parent.Map);
						if (slotGroup != null && slotGroup.parent != null)
						{
							slotGroup.parent.Notify_ReceivedThing(thing);
						}
					}
					else if (targetThing != null && sourceThings.Count < targetThings.Count - 1
						&& sourceCell.AllowedToAccept(parent.Map, targetThing)
						&& targetThing.stackCount > 0)
					{
						targetCell.ThrowDustPuff(parent.Map);
						Thing thing = GenSpawn.Spawn(targetThing.SplitOff(targetThing.stackCount), sourceCell, parent.Map);
						sourceCell.DropSound(parent.Map, thing.def);
						SlotGroup slotGroup = sourceCell.GetSlotGroup(parent.Map);
						if (slotGroup != null && slotGroup.parent != null)
						{
							slotGroup.parent.Notify_ReceivedThing(thing);
						}
					}
				}
			}
		}

		private void DefragChunkSilos()
		{
			CompRTQuantumChunkSilo sourceChunkSilo = compChunkSilos[qcsSourceIndex];
			CompRTQuantumChunkSilo targetChunkSilo = compChunkSilos[qcsTargetIndex];
			if (sourceChunkSilo.ChunkTotalFast() > targetChunkSilo.ChunkTotalFast() + 1)
			{
				bool chunkMoved = false;
				foreach (IntVec3 sourceCell in sourceChunkSilo.parent.OccupiedRect().Cells)
				{
					if (sparklesEnabled) sourceCell.ThrowSparkle(parent.Map);
					List<Thing> sourceThings = sourceCell.GetItemList(parent.Map, true);
					foreach (Thing sourceThing in sourceThings)
					{
						if (sourceThing.IsChunk())
						{
							if (!chunkMoved)
							{
								targetChunkSilo.ReceiveThing(sourceThing);
								chunkMoved = true;
								break;
							}
						}
						else
						{
							QueueThing(sourceThing);
						}
					}
				}
				if (sparklesEnabled)
				{
					foreach (IntVec3 targetCell in targetChunkSilo.parent.OccupiedRect().Cells)
					{
						targetCell.ThrowSparkle(parent.Map);
					}
				}
			}
			else
			{
				foreach (IntVec3 sourceCell in sourceChunkSilo.parent.OccupiedRect().Cells)
				{
					if (sparklesEnabled) sourceCell.ThrowSparkle(parent.Map);
					List<Thing> sourceThings = sourceCell.GetItemList(parent.Map);
					if (sourceThings.Count != 0)
					{
						QueueThing(sourceThings.RandomElement());
					}
				}
				if (sparklesEnabled)
				{
					foreach (IntVec3 targetCell in targetChunkSilo.parent.OccupiedRect().Cells)
					{
						targetCell.ThrowSparkle(parent.Map);
					}
				}
			}
		}

		private void SendItemsToRelay()
		{
			CompRTQuantumRelay compRelay = compRelays[qrIndex];
			if (compRelay != null)
			{
				bool itemSent = false;
				if (compStockpiles.Count != 0)
				{
					CompRTQuantumStockpile compStockpile = compStockpiles.RandomElement();
					foreach (IntVec3 stockpileCell in compStockpile.parent.OccupiedRect().Cells)
					{
						List<Thing> stockpileThings = stockpileCell.GetItemList(parent.Map);
						for (int i = 0; i < stockpileThings.Count; i++)
						{
							Thing stockpileThing = stockpileThings[i];
							if (stockpileCell.AllowedToAccept(parent.Map, stockpileThing)
								&& compRelay.ReceiveThing(stockpileThing))
							{
								itemSent = true;
								break;
							}
						}
						if (itemSent) break;
					}
				}
				if (!itemSent && compChunkSilos.Count != 0)
				{
					CompRTQuantumChunkSilo compChunkSilo = compChunkSilos.RandomElement();
					foreach (IntVec3 chunkSiloCell in compChunkSilo.parent.OccupiedRect().Cells)
					{
						List<Thing> chunkSiloThings = chunkSiloCell.GetItemList(parent.Map, true);
						for (int i = 0; i < chunkSiloThings.Count; i++)
						{
							Thing chunkSiloThing = chunkSiloThings[i];
							if (chunkSiloCell.AllowedToAccept(parent.Map, chunkSiloThing)
								&& compRelay.ReceiveThing(chunkSiloThing))
							{
								itemSent = true;
								break;
							}
						}
						if (itemSent) break;
					}
				}
			}
		}

		public bool IsValid()
		{
			return valid;
		}

		#region Registering/de-registering
		public void RegisterStockpile(CompRTQuantumStockpile compStockpile)
		{
			if (!compStockpiles.Contains(compStockpile))
			{
				compStockpiles.Add(compStockpile);
			}
		}

		public void DeRegisterStockpile(CompRTQuantumStockpile compStockpile)
		{
			compStockpiles.Remove(compStockpile);
		}

		public void RegisterRelay(CompRTQuantumRelay compRelay)
		{
			if (!compRelays.Contains(compRelay))
			{
				compRelays.Add(compRelay);
			}
		}

		public void DeRegisterRelay(CompRTQuantumRelay compRelay)
		{
			compRelays.Remove(compRelay);
		}

		public void RegisterChunkSilo(CompRTQuantumChunkSilo compChunkSilo)
		{
			if (!compChunkSilos.Contains(compChunkSilo))
			{
				compChunkSilos.Add(compChunkSilo);
			}
		}

		public void DeRegisterChunkSilo(CompRTQuantumChunkSilo compChunkSilo)
		{
			compChunkSilos.Remove(compChunkSilo);
		}
		#endregion
	}
}
