using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Harmony;
using System.Reflection;

namespace Table_Diner_Configurable
{
    public class TableDiner : Mod
    {
		//Mod stuff
		public static TableDiner_Settings settings;
		public static TableDiner modInstance;

		//circle widget stuff
		public static Mesh tableCircle;
		public static Material circleMaterial;
		public static Material circleMaterialBP;
		private Color circleColour1;
		private Color circleColour2;
		public Rot4 lastRotation;

		//working vars
		public float chairSearchDefault;
		private List<ThingDef> defaultDefs;
		private List<Pair<ThingDef, float>> nonDefaultDefs;
		private bool init = false;


		public TableDiner(ModContentPack mcp) : base(mcp)
		{
			settings = GetSettings<TableDiner_Settings>();
			modInstance = this;
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			settings.DoSettingsWindow(inRect);
		}
		public override string SettingsCategory()
		{
			return "TDiner.TableDiner".Translate();
		}

		public void Apply()
		{
			//only apply if initialised
			if (!init)
			{
				return;
			}

			//apply to all which had default chairSearchRadii
			foreach (ThingDef def in defaultDefs)
			{
				def.ingestible.chairSearchRadius = settings.tableDistance;
			}
			//only apply to non-default if setting enabled.
			if (settings.overwriteNonDefault)
			{
				foreach (Pair<ThingDef, float> defpair in nonDefaultDefs)
				{
					defpair.First.ingestible.chairSearchRadius = settings.tableDistance;
				}
			}
			//reset non-default if setting disabled, incase it was enabled earlier.
			else
			{
				foreach (Pair<ThingDef, float> defpair in nonDefaultDefs)
				{
					defpair.First.ingestible.chairSearchRadius = defpair.Second;
				}
			}
		}

		public void Init()
		{
			//we have now initialisd
			init = true;

			//circle widget stuff
			circleMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.white, true);
			circleMaterialBP = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Color.white, Color.blue, 0.3f), true);
			circleColour1 = new Color(0.8f, 0.8f, 0.8f, 0.4f);
			circleColour2 = new Color(0.8f, 0.8f, 0.8f, 0f);
			CreateCircleMesh();
			lastRotation = new Rot4();

			//grab the default chairSearchRadius
			chairSearchDefault = ThingDefOf.MealSimple.ingestible.chairSearchRadius;
			//setup lists
			defaultDefs = new List<ThingDef>();
			nonDefaultDefs = new List<Pair<ThingDef, float>>();

			//find all ingestibles
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
			{
				//avoid duplicates incase init is called twice, somehow.
				if (!defaultDefs.Contains(def) && !nonDefaultDefs.Exists(delegate (Pair<ThingDef, float> p) { return p.First == def; }) && def.ingestible != null)
				{
					if (def.ingestible.chairSearchRadius == chairSearchDefault)
					{
						//chairSearch is default, add to default list
						defaultDefs.Add(def);
					}
					else
					{
						//chairSearch is non-default, add to non-default list
						nonDefaultDefs.Add(new Pair<ThingDef, float>(def, def.ingestible.chairSearchRadius));
					}
				}
			}
			//apply chairSearch values for the first time.
			Apply();
		}

		private void CreateCircleMesh()
		{
			//torus-making code.
			List<Vector3> v = new List<Vector3>();
			for (int i = 0; i < 359; i += 4)
			{
				v.Add(new Vector3(Mathf.Cos(Mathf.Deg2Rad * i), 0, Mathf.Sin(Mathf.Deg2Rad * i)));
			}

			List<Vector3> v2 = new List<Vector3>();
			for (int i = 0; i < 359; i += 4)
			{
				v2.Add(new Vector3(Mathf.Cos(Mathf.Deg2Rad * i) * 0.5f, 0, Mathf.Sin(Mathf.Deg2Rad * i) * 0.5f));
			}
			Vector3[] vArray = new Vector3[v.Count * 2];
			Color[] cArray = new Color[v.Count * 2];
			int[] tArray = new int[v.Count * 6];
			for (int i = 0; i < v.Count; i++)
			{
				vArray[i] = v[i];
				vArray[i + v.Count] = v2[i];
				cArray[i] = circleColour1;
				cArray[i + v.Count] = circleColour2;
				int n = i * 6;
				tArray[n] = i;
				tArray[n + 1] = i + v.Count;
				tArray[n + 2] = (i + 1) % v.Count;
				tArray[n + 3] = (i + 1) % v.Count;
				tArray[n + 4] = i + v.Count;
				tArray[n + 5] = ((i + 1) % v.Count) + v.Count;
			}
			tableCircle = new Mesh
			{
				name = "TableCircleMesh",
				vertices = vArray,
				triangles = tArray,
				colors = cArray
			};
		}
	}

	[StaticConstructorOnStartup]
	public static class TableDiner_Initializer
	{
		static TableDiner_Initializer()
		{
			//init once all defs are loaded.
			var harmony = HarmonyInstance.Create("TableDiner");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			TableDiner.modInstance.Init();
		}
	}

	public class TableDiner_Settings : ModSettings
	{
		//mod settings
		public float tableDistance = 60;
		public bool overwriteNonDefault = false;
		public bool displayRing = true;
		//temporary settings since settings aren't applied immediately.
		private float tableDistanceUnapplied = 60;
		private bool overwriteNonDefaultUnapplied = false;

		public void DoSettingsWindow(Rect inRect)
		{
			//wide panels look terrible
			Rect lR = new Rect(inRect.x + 0.25f * inRect.width, inRect.y, inRect.width * 0.5f, inRect.height);
			Listing_Standard list = new Listing_Standard();
			list.ColumnWidth = lR.width;
			list.Begin(lR);
			list.Gap();
			list.Label("TDiner.TableSearchDist".Translate());
			list.Label(tableDistanceUnapplied.ToString());
			tableDistanceUnapplied = (int)list.Slider(tableDistanceUnapplied, 0, 400);
			if (list.ButtonTextLabeled("TDiner.VDefault".Translate(), "TDiner.Set".Translate()))
			{
				tableDistanceUnapplied = TableDiner.modInstance.chairSearchDefault;
				overwriteNonDefaultUnapplied = false;
			}
			if (list.ButtonTextLabeled("TDiner.TDDefault".Translate(), "TDiner.Set".Translate()))
			{
				tableDistanceUnapplied = 60f;
				overwriteNonDefaultUnapplied = false;
			}
			list.Gap();
			list.CheckboxLabeled("TDiner.overwriteNonDefault".Translate(),ref overwriteNonDefaultUnapplied);
			list.Gap(30f);
			//only show button if changes are made
			if (tableDistanceUnapplied != tableDistance || overwriteNonDefaultUnapplied != overwriteNonDefault)
			{
				if (list.ButtonText("TDiner.ApplyChanges".Translate()))
				{
					//only applying when player presses the 'apply' button, because we hav to search through a bunch of defs when we do apply.
					TableDiner.modInstance.Apply();
					tableDistance = tableDistanceUnapplied;
					overwriteNonDefault = overwriteNonDefaultUnapplied;
				}
			}
			else
			{
				//empty space so alignment remains without the apply button
				list.Gap(30f);
			}
			list.CheckboxLabeled("TDiner.displayRing".Translate(), ref displayRing);
			list.Label("TDiner.RadiusNote".Translate());
			list.End();
		}
		
		public override void ExposeData()
		{
			//save/load stuff
			base.ExposeData();
			Scribe_Values.Look(ref tableDistance, "tableSearchDistance", 60);
			Scribe_Values.Look(ref overwriteNonDefault, "overwriteNonDefault", false);
			Scribe_Values.Look(ref displayRing, "displayRing", true);
			tableDistanceUnapplied = tableDistance;
			overwriteNonDefaultUnapplied = overwriteNonDefault;
		}
	}

	//extraSelectionOverlays applies to blueprints and building defs.
	[HarmonyPatch(typeof(Verse.Thing), "DrawExtraSelectionOverlays")]
	public static class DrawExtraSelectionOverlays
	{
		[HarmonyPostfix]
		public static void _Postfix(Thing __instance)
		{
			bool bp = false;
			//check if a blueprint
			if (__instance.def.entityDefToBuild != null)
			{
				ThingDef td = __instance.def.entityDefToBuild as ThingDef;
				bp = (td != null && td.surfaceType == SurfaceType.Eat);
			}
			//if eat surface / eat surface blueprint, draw circle.
			if (TableDiner.settings.displayRing && (__instance.def.surfaceType == SurfaceType.Eat || bp))
			{
				//we draw a custom circle, because GenDraw.DrawRadiusRing is limited in it's radius.
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(__instance.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)),bp ? TableDiner.circleMaterialBP : TableDiner.circleMaterial, 0);
			}
		}
	}

	//SelectedUpdate handles the building widget when placing a blueprint.
	[HarmonyPatch(typeof(RimWorld.Designator_Place), "SelectedUpdate")]
	public static class SelectedUpdate
	{
		[HarmonyPostfix]
		public static void _Postfix(Designator_Place __instance)
		{
			if (!TableDiner.settings.displayRing) return;

			//work our way through the tree to a thingDef, and check if it's an eat surface.
			ThingDef td = __instance.PlacingDef as ThingDef;
			if (td != null && td.surfaceType == SurfaceType.Eat)
			{
				//we draw a custom circle, because GenDraw.DrawRadiusRing is limited in it's radius.
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(Gen.TrueCenter(UI.MouseCell(), TableDiner.modInstance.lastRotation, td.size, 0) + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)), TableDiner.circleMaterialBP, 0);
			}
		}
	}

	//really hacky crap to get around a protected rotation variable.. ugh...
	[HarmonyPatch(typeof(Verse.GenDraw), "DrawInteractionCell")]
	public static class DrawInteractionCell
	{
		[HarmonyPostfix]
		public static void _Postfix(Rot4 placingRot)
		{
			TableDiner.modInstance.lastRotation = placingRot;
		}
	}
}
