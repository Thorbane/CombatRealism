using System;
using Verse;
using UnityEngine;
using RimWorld;

namespace Combat_Realism
{
    public class ProjectileCR_Explosive_RL : ProjectileCR
    {
        private int ticksToDetonation;
        private int Burnticks = 3;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (--Burnticks == 0)
            {
                ExhaustFlames.ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 1f);
                ExhaustFlames.ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 2f);
                Burnticks = 3;
            }
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            ExhaustFlames.ThrowSmokeForRocketsandMortars(base.Position.ToVector3Shifted(), 4f);
            ExhaustFlames.ThrowRocketExhaustFlame(base.Position.ToVector3Shifted(), 1f);
        }

        protected override void Impact(Thing hitThing)
        {
            if (this.def.projectile.explosionDelay == 0)
            {
                this.Explode();
                return;
            }
            this.landed = true;
            this.ticksToDetonation = this.def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher.Faction);
        }
        protected virtual void Explode()
        {
            this.Destroy(DestroyMode.Vanish);
            ProjectilePropertiesCR propsCR = def.projectile as ProjectilePropertiesCR;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            float explosionSpawnChance = this.def.projectile.explosionSpawnChance;
            GenExplosion.DoExplosion(base.Position,
                this.def.projectile.explosionRadius,
                this.def.projectile.damageDef,
                this.launcher,
                this.def.projectile.soundExplode,
                this.def,
                this.equipmentDef,
                this.def.projectile.postExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1,
                propsCR == null ? false : propsCR.damageAdjacentTiles,
                preExplosionSpawnThingDef,
                this.def.projectile.explosionSpawnChance,
                1);
            ThrowBigExplode(base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(def.projectile.explosionRadius * 0.7f), def.projectile.explosionRadius * 0.6f);
            CompExplosiveCR comp = this.TryGetComp<CompExplosiveCR>();
            if (comp != null)
            {
                comp.Explode(launcher, this.Position);
            }
        }

        public static void ThrowBigExplode(Vector3 loc, float size)
        {
            if (!loc.ShouldSpawnMotesAt())
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_BigExplode"), null);
            moteThrown.Scale = Rand.Range(5f, 6f) * size;
            moteThrown.exactRotation = Rand.Range(0f, 0f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(6, 8), Rand.Range(0.002f, 0.003f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
    }

    public class ExhaustFlames
    {
        public static void ThrowRocketExhaustFlame(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds())
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_ShotFlash, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
        public static void ThrowSmokeForRocketsandMortars(Vector3 loc, float size)
        {
            IntVec3 intVec = loc.ToIntVec3();
            if (!intVec.InBounds())
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke, null);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.exactRotation = Rand.Range(-0.5f, 0.5f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity((float)Rand.Range(30, 40), Rand.Range(0.008f, 0.012f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
        }
    }
}
