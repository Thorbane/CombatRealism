using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Combat_Realism
{
    public class WorkGiver_ReloadTurret : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
                ;
            }
        }

        public override bool HasJobOnThingForced(Pawn pawn, Thing t)
        {
            Building_Reloadable turret = t as Building_Reloadable;

            if (turret == null || !turret.needsReload ||
                !pawn.CanReserveAndReach(turret, PathEndMode.ClosestTouch, Danger.Deadly) ||
                turret.IsForbidden(pawn.Faction) || turret.Faction != pawn.Faction) return false;

            Thing ammo = GenClosest.ClosestThingReachable(pawn.Position,
                ThingRequest.ForDef(turret.compAmmo.selectedAmmo),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                80,
                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
            return ammo != null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            Building_Reloadable turret = t as Building_Reloadable;
            if (turret == null || !turret.allowAutomaticReload ||  turret.dontReload) return false;
            return HasJobOnThingForced(pawn, t);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Building_Reloadable turret = t as Building_Reloadable;
            if (turret == null) return null;

            Thing ammo = GenClosest.ClosestThingReachable(pawn.Position,
                ThingRequest.ForDef(turret.compAmmo.selectedAmmo),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                80,
                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));

            if (ammo == null) return null;
            int amountNeeded = turret.compAmmo.Props.magazineSize;
            if (turret.compAmmo.currentAmmo == turret.compAmmo.selectedAmmo)
                amountNeeded -= turret.compAmmo.curMagCount;
            return new Job(CR_JobDefOf.ReloadTurret, t, ammo) {maxNumToCarry = Mathf.Min(amountNeeded, ammo.stackCount)};
        }
    }
}