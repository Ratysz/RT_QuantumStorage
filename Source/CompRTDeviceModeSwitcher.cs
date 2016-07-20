using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_QuantumStorage
{
	public class CompRTDeviceModeSwitcher : ThingComp
	{
		private CompProperties_RTDeviceModeSwitcher properties
		{
			get
			{
				return (CompProperties_RTDeviceModeSwitcher)props;
			}
		}
		public bool canSwitchModes
		{
			get
			{
				return properties.canSwitchModes || ResearchModsSpecial.modeSwitchEnabled;
			}
		}

		private bool switchedToChunkSilo = false;

		public override void PostSpawnSetup()
		{
			base.PostSpawnSetup();

			SortOutComps();
		}

		public override IEnumerable<Command> CompGetGizmosExtra()
		{
			if (canSwitchModes)
			{
				Command_Action commandModeSwitch = new Command_Action();
				commandModeSwitch.groupKey = 676192;
				if (switchedToChunkSilo)
				{
					commandModeSwitch.defaultLabel = "CompRTDeviceModeSwitcher_StockpileModeLabel".Translate();
					commandModeSwitch.defaultDesc = "CompRTDeviceModeSwitcher_StockpileModeDesc".Translate();
					commandModeSwitch.icon = Resources.stockpileTexture;
				}
				else
				{
					commandModeSwitch.defaultLabel = "CompRTDeviceModeSwitcher_ChunkSiloModeLabel".Translate();
					commandModeSwitch.defaultDesc = "CompRTDeviceModeSwitcher_ChunkSiloModeDesc".Translate();
					commandModeSwitch.icon = Resources.chunkSiloTexture;
				}
				commandModeSwitch.action = () =>
				{
					CompFlickable compFlickable = parent.TryGetComp<CompFlickable>();
					if (compFlickable != null)
					{
						compFlickable.ResetToOn();
						compFlickable.DoFlick();
						FlickUtility.UpdateFlickDesignation(parent);
					}
					if (switchedToChunkSilo)
					{
						CompRTQuantumChunkSilo compChunkSilo = parent.TryGetComp<CompRTQuantumChunkSilo>();
						if (compChunkSilo != null)
						{
							compChunkSilo.EmptyOut();
						}
					}
					switchedToChunkSilo = !switchedToChunkSilo;
					SortOutComps();
				};
				yield return commandModeSwitch;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.LookValue(ref switchedToChunkSilo, "switchedToChunkSilo", false);
		}

		private void SortOutComps()
		{
			if (!switchedToChunkSilo)
			{
				CompRTQuantumChunkSilo compChunkSilo = parent.TryGetComp<CompRTQuantumChunkSilo>();
				if (compChunkSilo != null)
				{
					foreach (ThingComp comp in parent.AllComps)
					{
						if (comp is CompRTQuantumChunkSilo)
						{
							comp.PostDeSpawn();
						}
					}
					parent.AllComps.RemoveAll(x => x is CompRTQuantumChunkSilo);
					parent.def.comps.RemoveAll(x => x.compClass.ToString().Equals("RT_QuantumStorage.CompRTQuantumChunkSilo"));
				}
				if (parent.TryGetComp<CompRTQuantumStockpile>() == null)
				{
					CompProperties_RTQuantumStockpile compPropsStockpile = new CompProperties_RTQuantumStockpile();
					compPropsStockpile.compClass = typeof(CompRTQuantumStockpile);
					List<CompProperties> componentsBackup = new List<CompProperties>();
					foreach (CompProperties compProperties in parent.def.comps)
					{
						componentsBackup.Add(compProperties);
					}
					parent.def.comps.Clear();
					parent.def.comps.Add(compPropsStockpile);
					parent.InitializeComps();
					foreach (CompProperties compProperties in componentsBackup)
					{
						parent.def.comps.Add(compProperties);
					}

					CompRTQuantumStockpile compStockpile = parent.TryGetComp<CompRTQuantumStockpile>();
					compStockpile.PostSpawnSetup();
					compStockpile.PostExposeData();
				}
			}
			else
			{
				CompRTQuantumStockpile compStockpile = parent.TryGetComp<CompRTQuantumStockpile>();
				if (compStockpile != null)
				{
					foreach (ThingComp comp in parent.AllComps)
					{
						if (comp is CompRTQuantumStockpile)
						{
							comp.PostDeSpawn();
						}
					}
					parent.AllComps.RemoveAll(x => x is CompRTQuantumStockpile);
					parent.def.comps.RemoveAll(x => x.compClass.ToString().Equals("RT_QuantumStorage.CompRTQuantumStockpile"));
				}
				if (parent.TryGetComp<CompRTQuantumChunkSilo>() == null)
				{
					CompProperties_RTQuantumChunkSilo compPropsChunkSilo = new CompProperties_RTQuantumChunkSilo();
					compPropsChunkSilo.compClass = typeof(CompRTQuantumChunkSilo);
					List<CompProperties> componentsBackup = new List<CompProperties>();
					foreach (CompProperties compProperties in parent.def.comps)
					{
						componentsBackup.Add(compProperties);
					}
					parent.def.comps.Clear();
					parent.def.comps.Add(compPropsChunkSilo);
					parent.InitializeComps();
					foreach (CompProperties compProperties in componentsBackup)
					{
						parent.def.comps.Add(compProperties);
					}

					CompRTQuantumChunkSilo compChunkSilo = parent.TryGetComp<CompRTQuantumChunkSilo>();
					compChunkSilo.PostSpawnSetup();
					compChunkSilo.PostExposeData();
				}
			}
		}
	}
}
