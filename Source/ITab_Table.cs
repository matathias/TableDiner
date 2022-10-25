using UnityEngine;
using RimWorld;
using Verse;

namespace Table_Diner_Configurable
{
	public class ITab_Table : ITab
	{
		public static readonly Vector2 WinSize = new Vector2(400f, 40f);

		public ITab_Table()
		{
			this.size = ITab_Table.WinSize;
			this.labelKey = "TDiner.TabTable";
			this.tutorTag = "Table";
		}

		protected override void FillTab()
		{
			Rect tabRect = new Rect(20, 10, this.size.x - 40, this.size.y - 20);
			Rect tabRectBig = new Rect(10, 5, this.size.x - 20, this.size.y - 10);
			if (!TableDiner.settings.useExtraFeatures)
			{
				Widgets.Label(tabRect, "TDiner.ExtraDisabled".Translate());
				return;
			}
			
			float tr = TableDinerGlobal.GetTableRadius(SelThing.ThingID);
			GUI.color = Color.white;
			
			if (Mouse.IsOver(tabRect))
			{
				Widgets.DrawHighlight(tabRectBig);
			}

			if (tr == 0) GUI.color = new Color(0.7f, 1, 0.7f);
			if (tr == -1) GUI.color = Color.red;
			if (tr > TableDiner.settings.tableDistance) GUI.color = Color.yellow;

			float trw = tr;
			if (tr >= 1)
			{
				trw = Mathf.Sqrt(tr);
			}
			float trs = Widgets.HorizontalSlider(tabRect, trw, -1, 23, true, tr < 0 ? "TDiner.Disabled".Translate().ToString() : (tr < 1 ? "TDiner.Ignored".Translate().ToString() : Mathf.Round(tr).ToString()), "TDiner.TRSlideLabel".Translate());
			if (trs >= 1)
            {
				TableDinerGlobal.tableRadii[SelThing.ThingID] = Mathf.Pow(trs, 2);
			}
			else
            {
				if (trs < 0)
                {
					TableDinerGlobal.tableRadii[SelThing.ThingID] = -1;
				}
				else
                {
					TableDinerGlobal.tableRadii[SelThing.ThingID] = 0;
				}
			}
			
			GUI.color = Color.white;
		}
	}
}
