using System;
using UnityEngine;
using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace Table_Diner_Configurable
{
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

				float r = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				if (r < 1)
				{
					r = TableDiner.settings.tableDistance;
				}
				//Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(__instance.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)),bp ? TableDinerGlobal.circleMaterialBP : TableDinerGlobal.circleMaterial, 0);
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(__instance.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(r, r, r)), bp ? TableDinerGlobal.circleMaterialBP : TableDinerGlobal.circleMaterial, 0);
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
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(GenThing.TrueCenter(UI.MouseCell(), TableDiner.modInstance.lastRotation, td.size, 0) + Vector3.up * 10, Quaternion.identity, new Vector3(TableDiner.settings.tableDistance, TableDiner.settings.tableDistance, TableDiner.settings.tableDistance)), TableDinerGlobal.circleMaterialBP, 0);
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


	//Patch ExposeData to save individual table radius
	[HarmonyPatch(typeof(Verse.Thing), "ExposeData")]
	public static class ExposeDataThing
	{
		[HarmonyPostfix]
		public static void _Postfix(Thing __instance)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return;
			}
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
				float td = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				Scribe_Values.Look(ref td, "TableDiner_TableDistance", 0);
				TableDinerGlobal.tableRadii[__instance.ThingID] = td;
			}
		}
	}

	//Patch Pawn ExposeData to save Pawn table radius
	[HarmonyPatch(typeof(Verse.Pawn), "ExposeData")]
	public static class ExposeDataPawn
	{
		[HarmonyPostfix]
		public static void _Postfix(Pawn __instance)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return;
			}
			if (__instance.IsColonist)
			{
				float td = TableDinerGlobal.GetTableRadius(__instance.ThingID);
				Scribe_Values.Look(ref td, "TableDiner_TableDistance", 0);
				TableDinerGlobal.tableRadii[__instance.ThingID] = td;
			}
		}
	}

	//Patch ITab_Pawn_Needs to add table search distance
	[HarmonyPatch(typeof(RimWorld.ITab_Pawn_Needs), "FillTab")]
	public static class FillTab
	{
		public static bool mOver = false;

		[HarmonyPostfix]
		public static void _Postfix(ITab_Pawn_Needs __instance)
		{
			if (Find.CurrentMap == null) return;
			if (!TableDiner.settings.useExtraFeatures) return;
			Pawn SelPawn = Find.Selector.SingleSelectedThing as Pawn;
			if (SelPawn != null && SelPawn.IsColonist)
			{
				Vector2 size = NeedsCardUtility.GetSize(SelPawn);
				Rect tabRect = new Rect(20, size.y - (ITab_Table.WinSize.y) + 10, ITab_Table.WinSize.x - 40, ITab_Table.WinSize.y - 20);
				Rect tabRectBig = new Rect(10, size.y - (ITab_Table.WinSize.y) + 5, ITab_Table.WinSize.x - 20, ITab_Table.WinSize.y - 10);
				float tr = TableDinerGlobal.GetTableRadius(SelPawn.ThingID);
				GUI.color = Color.white;
				if (tr > TableDiner.settings.tableDistance)
				{
					GUI.color = Color.yellow;
				}
				if (Mouse.IsOver(tabRect))
				{
					Widgets.DrawHighlight(tabRectBig);
					mOver = true;
				}
				TableDinerGlobal.tableRadii[SelPawn.ThingID] = Mathf.Pow(Widgets.HorizontalSlider(tabRect, Mathf.Sqrt(tr), 0, 23, true, tr < 1 ? "TDiner.Ignored".Translate().ToString() : Mathf.Round(tr).ToString(), "TDiner.TRSlideLabel".Translate()), 2);
				GUI.color = Color.white;
			}
		}
	}


	//circle overlay for pawns
	[HarmonyPatch(typeof(Verse.InspectTabBase), "TabUpdate")]
	public static class TabUpdate
	{
		[HarmonyPostfix]
		public static void __Postfix(ITab_Pawn_Needs __instance)
		{
            if (!(__instance is RimWorld.ITab_Pawn_Needs)) return;
			if (Find.CurrentMap == null) return;
			if (!TableDiner.settings.displayRing) return;
			Pawn SelPawn = Find.Selector.SingleSelectedThing as Pawn;
			if (SelPawn != null && SelPawn.IsColonist && FillTab.mOver)
			{
				float r = TableDinerGlobal.GetTableRadius(SelPawn.ThingID);
				if (r < 1)
				{
					r = TableDiner.settings.tableDistance;
				}
				Graphics.DrawMesh(TableDiner.tableCircle, Matrix4x4.TRS(SelPawn.TrueCenter() + Vector3.up * 10, Quaternion.identity, new Vector3(r, r, r)), TableDinerGlobal.circleMaterial, 0);
				FillTab.mOver = false;
			}
		}
	}

	//add distance checks to CarryIngestible toil chairValidator
	[HarmonyPatch(typeof(RimWorld.Toils_Ingest), "CarryIngestibleToChewSpot")]
	public static class CarryIngestibleToChewSpot
	{
		[HarmonyPrefix]
		public static bool __Prefix(Pawn pawn, TargetIndex ingestibleInd, ref Toil __result)
		{
			if (!TableDiner.settings.useExtraFeatures)
			{
				return true;
			}
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                IntVec3 cell2 = IntVec3.Invalid;
                Thing thing = null;
                Thing thing2 = actor.CurJob.GetTarget(ingestibleInd).Thing;
                Predicate<Thing> baseChairValidator = delegate (Thing t)
                {
                    if (t.def.building == null || !t.def.building.isSittable)
                    {
                        return false;
                    }
                    if (!TryFindFreeSittingSpotOnThing(t, out var _))
                    {
                        return false;
                    }
                    if (t.IsForbidden(pawn))
                    {
                        return false;
                    }
                    if (!actor.CanReserve(t))
                    {
                        return false;
                    }
                    if (!t.IsSociallyProper(actor))
                    {
                        return false;
                    }
                    if (t.IsBurning())
                    {
                        return false;
                    }
                    if (t.HostileTo(pawn))
                    {
                        return false;
                    }
                    bool flag = false;
                    for (int i = 0; i < 4; i++)
                    {
                        IntVec3 c = t.Position + GenAdj.CardinalDirections[i];
                        Building edifice = c.GetEdifice(t.Map);
                        if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
                        {
                            float tr = TableDinerGlobal.GetTableRadius(edifice.ThingID);
                            float pr = TableDinerGlobal.GetTableRadius(actor.ThingID);

                            if (tr >= 1 || pr >= 1)
                            {
                                float r2 = (edifice.TrueCenter() - actor.TrueCenter()).sqrMagnitude;
                                if (tr < 1)
                                {
                                    if (r2 <= Mathf.Pow(pr, 2))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                                else if (pr < 1)
                                {
                                    if (r2 <= Mathf.Pow(tr, 2))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (r2 <= Mathf.Pow(Mathf.Min(tr, pr), 2))
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    return flag ? true : false;
                };
                if (thing2.def.ingestible.chairSearchRadius > 0f)
                {
                    thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), thing2.def.ingestible.chairSearchRadius, (Thing t) => baseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);
                }
                if (thing == null)
                {
                    cell2 = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing, (IntVec3 c) => actor.CanReserveSittableOrSpot(c));
                    Danger chewSpotDanger = cell2.GetDangerFor(pawn, actor.Map);
                    if (chewSpotDanger != Danger.None)
                    {
                        thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), thing2.def.ingestible.chairSearchRadius, (Thing t) => baseChairValidator(t) && (int)t.Position.GetDangerFor(pawn, t.Map) <= (int)chewSpotDanger);
                    }
                }
                if (thing != null && !TryFindFreeSittingSpotOnThing(thing, out cell2))
                {
                    Log.Error("Could not find sitting spot on chewing chair! This is not supposed to happen - we looked for a free spot in a previous check!");
                }
                actor.ReserveSittableOrSpot(cell2, actor.CurJob);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, cell2);
                actor.pather.StartPath(cell2, PathEndMode.OnCell);
                bool TryFindFreeSittingSpotOnThing(Thing t, out IntVec3 cell)
                {
                    foreach (IntVec3 item in t.OccupiedRect())
                    {
                        if (actor.CanReserveSittableOrSpot(item))
                        {
                            cell = item;
                            return true;
                        }
                    }
                    cell = default(IntVec3);
                    return false;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            __result = toil;
            return false;
		}
	}
}
