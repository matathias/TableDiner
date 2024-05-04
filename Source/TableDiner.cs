using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace Table_Diner_Configurable
{
	public class TableDiner : Mod
	{
		//Mod stuff
		public static TableDinerSettings settings;
		public static TableDiner modInstance;

		//circle widget stuff
		public static Mesh tableCircle;
		private Color circleColour1;
		private Color circleColour2;
		public Rot4 lastRotation;

		//working vars
		public float chairSearchDefault;
		private List<ThingDef> defaultDefs;
		private List<Pair<ThingDef, float>> nonDefaultDefs;
		private bool init = false;

		private bool useFeatApplied = false;

		public TableDiner(ModContentPack mcp) : base(mcp)
		{
			settings = GetSettings<TableDinerSettings>();
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


			if (settings.useExtraFeatures && !useFeatApplied)
			{
				useFeatApplied = true;
				foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
				{
					if (def.surfaceType == SurfaceType.Eat)
					{
						if (def.inspectorTabs == null)
						{
							def.inspectorTabs = new List<Type>();
						}
						def.inspectorTabs.Add(typeof(ITab_Table));
						if (def.inspectorTabsResolved == null)
						{
							def.inspectorTabsResolved = new List<InspectTabBase>();
						}
						try
						{
							def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Table)));
						}
						catch (Exception ex)
						{
							Log.Error(string.Concat(new object[]
							{
							"Could not instantiate inspector tab of type ",
							typeof(ITab_Table),
							": ",
							ex
							}));
						}
					}
				}
			}

		}
		public void Init()
		{
			//we have now initialisd
			init = true;

			//circle widget stuff
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
}
