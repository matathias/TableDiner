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
			if (tr > TableDiner.settings.tableDistance)
			{
				GUI.color = Color.yellow;
			}
			if (Mouse.IsOver(tabRect))
			{
				Widgets.DrawHighlight(tabRectBig);
			}
			TableDinerGlobal.tableRadii[SelThing.ThingID] = Mathf.Pow(Widgets.HorizontalSlider(tabRect, Mathf.Sqrt(tr), 0, 23, true, tr < 1 ? "TDiner.Ignored".Translate().ToString() : Mathf.Round(tr).ToString(), "TDiner.TRSlideLabel".Translate()), 2);
			GUI.color = Color.white;
		}
	}
}
