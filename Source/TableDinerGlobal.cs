using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using HarmonyLib;
using System.Reflection;

namespace Table_Diner_Configurable
{
	[StaticConstructorOnStartup]
	public static class TableDinerGlobal
	{
		//put materials in a class that has StaticConstructorOnStartup to shut the stupid warning up.
		public static Material circleMaterial;
		public static Material circleMaterialBP;

		public static Dictionary<string, float> tableRadii;

		private static System.Random localRandom;

		static TableDinerGlobal()
		{
			var harmony = new Harmony("TableDiner");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            localRandom = new System.Random();

			circleMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.white, true);
			circleMaterialBP = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Color.white, Color.blue, 0.3f), true);

			tableRadii = new Dictionary<string, float>();

			PatchCommonSense(harmony);

			TableDiner.modInstance.Init();
		}

		public static float GetTableRadius(string thingID)
		{
			if (!tableRadii.ContainsKey(thingID))
			{
				return 0;
			}
			return tableRadii[thingID];
		}

		private static void PatchCommonSense(Harmony harmony)
        {
			try
            {
				((Action)(() =>
				{
					harmony.Patch(typeof(CommonSense.JobDriver_PrepareToIngestToils_ToolUser_CommonSensePatch).GetMethod("reserveChewSpot"), new HarmonyMethod(typeof(ReserveChewSpot).GetMethod("_Prefix")));
					Log.Message("Table Diner patched Common Sense");
				}))();
            }
			catch (Exception)
            {
				Log.Message("Table Diner did not find Common Sense.");
            }
        }
	}
}
