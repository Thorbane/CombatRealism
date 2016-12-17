﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Reflection;
using System;
using Combat_Realism;

namespace AchtungModCR
{

    // install job defs
    //
    public class RootMap_Patch : RootMap
    {
        internal void RootMap_Start()
        {
            Controller.getInstance().InstallJobDefs();
            Start();
        }
    }

    // start of game/map
    //
    public static class MapIniterUtility_Patch
    {
        internal static void MapIniterUtility_FinalizeMapInit()
        {
            MapIniterUtility.FinalizeMapInit();
            Settings.Load();
            Controller.getInstance().Initialize();
        }
    }

    // handle events early
    //
    public class MainTabsRoot_Patch : MainTabsRoot
    {
        internal void MainTabsRoot_HandleLowPriorityShortcuts()
        {
            Controller.getInstance().HandleEvents();
            HandleLowPriorityShortcuts();
        }
    }

    // handle drawing
    //
    public static class SelectionDrawer_Patch
    {
        internal static void SelectionDrawer_DrawSelectionOverlays()
        {
            Controller.getInstance().HandleDrawing();
            SelectionDrawer.DrawSelectionOverlays();
        }
    }

    // handle gui
    //
    public class ThingOverlays_Patch : ThingOverlays
    {
        public void ThingOverlays_ThingOverlaysOnGUI()
        {
            ThingOverlaysOnGUI();
            Controller.getInstance().HandleDrawingOnGUI();
        }
    }

    // turn reservation error into warning inject
    //
    public class ReservationManager_Patch
    {
        internal void ReservationManager_LogCouldNotReserveError(Pawn claimant, TargetInfo target, int maxPawns)
        {
            Job curJob = claimant.CurJob;
            string str = "null";
            int curToilIndex = -1;
            if (curJob != null)
            {
                str = curJob.ToString();
                if (claimant.jobs.curDriver != null)
                {
                    curToilIndex = claimant.jobs.curDriver.CurToilIndex;
                }
            }

            Pawn pawn = Find.Reservations.FirstReserverOf(target, claimant.Faction, true);
            string str2 = "null";
            int num2 = -1;
            if (pawn != null)
            {
                Job job2 = pawn.CurJob;
                if (job2 != null)
                {
                    str2 = job2.ToString();
                    if (pawn.jobs.curDriver != null)
                    {
                        num2 = pawn.jobs.curDriver.CurToilIndex;
                    }
                }
            }

            Log.Warning(string.Concat(new object[] {
                     "Could not reserve ", target, " for ", claimant.NameStringShort, " doing job ", str, "(curToil=", curToilIndex, ") for maxPawns ", maxPawns
                }));
            Log.Warning(string.Concat(new object[] {
                     "Existing reserver: ", pawn.NameStringShort, " doing job ", str2, "(curToil=", num2, ")"
                }));

        }
    }

    // custom context menu
    //
    public static class FloatMenuMakerMap_Patch
    {
        public static List<FloatMenuOption> FloatMenuMakerMap_ChoicesAtFor(Vector3 clickPos, Pawn pawn)
        {
            List<FloatMenuOption> options = FloatMenuMakerMap.ChoicesAtFor(clickPos, pawn);
            options.AddRange(Controller.getInstance().AchtungChoicesAtFor(clickPos, pawn));
            return options;
        }
    }

    // bug removed for now
    //// track projectiles
    ////
    //public abstract class Projectile_Patch : ProjectileCR
    //{
    //	public void Projectile_Launch(Thing launcher, Vector3 origin, TargetInfo targ, Thing equipment = null)
    //	{
    //		Controller.getInstance().AddProjectile(this, launcher, origin, targ, equipment);
    //		Launch(launcher, origin, targ, equipment);
    //	}
    //}

    [StaticConstructorOnStartup]
    static class Main
    {
        static Main()
        {
            HookInjector injector = new HookInjector();
            injector.Inject(typeof(RootMap), "Start", typeof(RootMap_Patch));
            injector.Inject(typeof(MapIniterUtility), "FinalizeMapInit", typeof(MapIniterUtility_Patch));
            injector.Inject(typeof(MainTabsRoot), "HandleLowPriorityShortcuts", typeof(MainTabsRoot_Patch));
            injector.Inject(typeof(SelectionDrawer), "DrawSelectionOverlays", typeof(SelectionDrawer_Patch));
            injector.Inject(typeof(ThingOverlays), "ThingOverlaysOnGUI", typeof(ThingOverlays_Patch));
            injector.Inject(typeof(ReservationManager), "LogCouldNotReserveError", typeof(ReservationManager_Patch));
            injector.Inject(typeof(FloatMenuMakerMap), "ChoicesAtFor", typeof(FloatMenuMakerMap_Patch));

            //MethodInfo method = typeof(Projectile).GetMethod("Launch", new Type[] { typeof(Thing), typeof(Vector3), typeof(TargetInfo), typeof(Thing) });
            //injector.Inject(typeof(Projectile), method, typeof(Projectile_Patch));
        }
    }
}