using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Combat_Realism
{
    public class Building_Reloadable : Building_Turret
    {
        private const int minTicksBeforeAutoReload = 1800;              // This much time must pass before haulers will try to automatically reload an auto-turret

        #region Fields

        protected int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft;                                // Need this public so aim mode can modify it
        protected TargetInfo currentTargetInt = TargetInfo.Invalid;
        public Thing gun;
        protected bool loaded = true;
        protected CompMannable mannableComp;

        // New fields
        private CompAmmoUser _compAmmo = null;
        public bool isReloading = false;
        protected int ticksSinceLastBurst = minTicksBeforeAutoReload;
        protected CompFireModes _compFireModes = null;
        // bool for other mods to forbid current reloading
        public bool dontReload;

        #endregion

        #region Properties

        public override Verb AttackVerb
        {
            get
            {
                if (gun == null)
                {
                    return null;
                }
                return GunCompEq.verbTracker.PrimaryVerb;
            }
        }

        public override TargetInfo CurrentTarget
        {
            get
            {
                return currentTargetInt;
            }
        }

        public CompEquippable GunCompEq
        {
            get
            {
                return gun.TryGetComp<CompEquippable>();
            }
        }

        protected bool WarmingUp
        {
            get
            {
                return burstWarmupTicksLeft > 0;
            }
        }

        // New properties
        public CompAmmoUser compAmmo
        {
            get
            {
                if (_compAmmo == null && gun != null) _compAmmo = gun.TryGetComp<CompAmmoUser>();
                return _compAmmo;
            }
        }

        public CompFireModes compFireModes
        {
            get
            {
                if (_compFireModes == null && gun != null) _compFireModes = gun.TryGetComp<CompFireModes>();
                return _compFireModes;
            }
        }

        public bool needsReload
        {
            get
            {
                return mannableComp == null
                    && compAmmo != null
                    && (compAmmo.curMagCount < compAmmo.Props.magazineSize || compAmmo.selectedAmmo != compAmmo.currentAmmo);
            }
        }

        public bool allowAutomaticReload
        {
            get
            {
                return mannableComp == null && compAmmo != null
                    && (ticksSinceLastBurst >= minTicksBeforeAutoReload || compAmmo.curMagCount <= Mathf.CeilToInt(compAmmo.Props.magazineSize / 6));
            }
        }

        #endregion

        #region Methods

        protected void BeginBurst()
        {
            ticksSinceLastBurst = 0;
            GunCompEq.PrimaryVerb.TryStartCastOn(CurrentTarget, false);
        }

        protected void BurstComplete()
        {
            if (def.building.turretBurstCooldownTicks >= 0)
            {
                burstCooldownTicksLeft = def.building.turretBurstCooldownTicks;
            }
            else
            {
                burstCooldownTicksLeft = GunCompEq.PrimaryVerb.verbProps.defaultCooldownTicks;
            }
            if (compAmmo != null && compAmmo.curMagCount <= 0)
            {
                OrderReload();
            }
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                {
                    RoofDef roofDef = Find.RoofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }
                if (mannableComp == null)
                {
                    return !GenAI.MachinesLike(Faction, pawn);
                }
            }
            return true;
        }

        protected TargetInfo TryFindNewTarget()
        {
            Thing searcher;
            Faction faction;
            if (mannableComp != null && mannableComp.MannedNow)
            {
                searcher = mannableComp.ManningPawn;
                faction = mannableComp.ManningPawn.Faction;
            }
            else
            {
                searcher = this;
                faction = Faction;
            }
            if (GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && faction.HostileTo(Faction.OfPlayer) && Rand.Value < 0.5f && Find.ListerBuildings.allBuildingsColonist.Count > 0)
            {
                return Find.ListerBuildings.allBuildingsColonist.RandomElement<Building>();
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
            }
            if (GunCompEq.PrimaryVerb.verbProps.ai_IsIncendiary)
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, new Predicate<Thing>(IsValidTarget), GunCompEq.PrimaryVerb.verbProps.range, GunCompEq.PrimaryVerb.verbProps.minRange, targetScanFlags);
        }

        public override void DrawExtraSelectionOverlays()
        {
            float range = GunCompEq.PrimaryVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(Position, range);
            }
            float minRange = GunCompEq.PrimaryVerb.verbProps.minRange;
            if (minRange < 90f && minRange > 0.1f)
            {
                GenDraw.DrawRadiusRing(Position, minRange);
            }
            if (burstWarmupTicksLeft > 0)
            {
                int degreesWide = (int)((float)burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, CurrentTarget, degreesWide, (float)def.size.x * 0.5f);
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.LookValue<bool>(ref loaded, "loaded", false, false);

            // Look new variables
            Scribe_Values.LookValue(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.LookValue(ref isReloading, "isReloading", false);
            Scribe_Deep.LookDeep(ref gun, "gun");

            Scribe_Values.LookValue<bool>(ref dontReload, "dontReload", false, false);
        }

        public override void OrderAttack(TargetInfo targ)
        {
            if ((targ.Cell - Position).LengthHorizontal < GunCompEq.PrimaryVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if ((targ.Cell - Position).LengthHorizontal > GunCompEq.PrimaryVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            forcedTarget = targ;
        }

        protected void TryStartShootSomething()
        {
            // Check for ammo first
            if (compAmmo != null && (isReloading || (mannableComp == null && compAmmo.curMagCount <= 0))) return;

            if (forcedTarget.ThingDestroyed)
            {
                forcedTarget = null;
            }
            if (GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && Find.RoofGrid.Roofed(Position))
            {
                return;
            }
            bool isValid = currentTargetInt.IsValid;
            if (forcedTarget.IsValid)
            {
                currentTargetInt = forcedTarget;
            }
            else
            {
                currentTargetInt = TryFindNewTarget();
            }
            if (!isValid && currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(Position);
            }
            if (currentTargetInt.IsValid)
            {
                if (AttackVerb.verbProps.warmupTicks > 0)
                {
                    burstWarmupTicksLeft = AttackVerb.verbProps.warmupTicks;
                }
                else
                {
                    BeginBurst();
                }
            }
        }


        // New methods

        public CompMannable GetMannableComp()
        {
            return mannableComp;
        }

        public void OrderReload()
        {
            if (mannableComp == null
                || !mannableComp.MannedNow
                || (compAmmo.currentAmmo == compAmmo.selectedAmmo && compAmmo.curMagCount == compAmmo.Props.magazineSize)) return;

            Job reloadJob = null;
            CompInventory inventory = mannableComp.ManningPawn.TryGetComp<CompInventory>();
            if (inventory != null)
            {
                Thing ammo = inventory.container.FirstOrDefault(x => x.def == compAmmo.selectedAmmo);
                if (ammo != null)
                {
                    Thing droppedAmmo;
                    int amount = compAmmo.Props.magazineSize;
                    if (compAmmo.currentAmmo == compAmmo.selectedAmmo) amount -= compAmmo.curMagCount;
                    if (inventory.container.TryDrop(ammo, Position, ThingPlaceMode.Direct, Mathf.Min(ammo.stackCount, amount), out droppedAmmo))
                    {
                        reloadJob = new Job(CR_JobDefOf.ReloadTurret, this, droppedAmmo) { maxNumToCarry = droppedAmmo.stackCount };
                    }
                }
            }
            if (reloadJob == null)
            {
                reloadJob = new WorkGiver_ReloadTurret().JobOnThing(mannableComp.ManningPawn, this);
            }
            if (reloadJob != null)
            {
                mannableComp.ManningPawn.jobs.StartJob(reloadJob, JobCondition.Ongoing, null, true);
            }
        }

        #endregion

    }
}
