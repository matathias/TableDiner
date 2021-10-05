using UnityEngine;
using Verse;


namespace Table_Diner_Configurable
{
	public class TableDinerSettings : ModSettings
	{
		//mod settings
		public float tableDistance = 60;
		public bool overwriteNonDefault = false;
		public bool displayRing = true;
		public bool useExtraFeatures = true;
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
			list.CheckboxLabeled("TDiner.overwriteNonDefault".Translate(), ref overwriteNonDefaultUnapplied);
			list.Gap(30f);
			//only show button if changes are made
			if (tableDistanceUnapplied != tableDistance || overwriteNonDefaultUnapplied != overwriteNonDefault)
			{
				if (list.ButtonText("TDiner.ApplyChanges".Translate()))
				{
					//only applying when player presses the 'apply' button, because we hav to search through a bunch of defs when we do apply.
					tableDistance = tableDistanceUnapplied;
					overwriteNonDefault = overwriteNonDefaultUnapplied;
					TableDiner.modInstance.Apply();
				}
			}
			else
			{
				//empty space so alignment remains without the apply button
				list.Gap(30f);
			}
			list.CheckboxLabeled("TDiner.displayRing".Translate(), ref displayRing);
			list.CheckboxLabeled("TDiner.ExtraFeatures".Translate(), ref useExtraFeatures);
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
            Scribe_Values.Look(ref useExtraFeatures, "useExtraFeatures", true);
			tableDistanceUnapplied = tableDistance;
			overwriteNonDefaultUnapplied = overwriteNonDefault;
		}
	}
}
