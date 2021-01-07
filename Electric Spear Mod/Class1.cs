using Partiality.Modloader;
using UnityEngine;
using RWCustom;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Electric_Spear_Mod
{
    [MonoModPatch("")]
    class Empty { }
    [MonoModPatch("global::AbstractPhysicalObject")]
    public class Patch_AbstractPhysicalObject : AbstractPhysicalObject
    {
        [MonoMod.MonoModIgnore]
        public Patch_AbstractPhysicalObject(World world, AbstractPhysicalObject.AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
            this.type = type;
            this.realizedObject = realizedObject;
            this.stuckObjects = new List<AbstractPhysicalObject.AbstractObjectStick>();
        }
        public extern void orig_Realize();
        public virtual void Realize()
        {
            if (this.realizedObject != null)
            {
                Debug.Log("realized object is null");
                return;
            }
            switch (this.type)
            {

                case (AbstractPhysicalObject.AbstractObjectType)0x0000001F://electric spear object
                    {
                        Debug.Log("realizedObject defined");
                        /*if ((this as AbstractElectricSpear).charge != 1 || (this as AbstractElectricSpear).charge != 2 || (this as AbstractElectricSpear).charge != 3 || (this as AbstractElectricSpear).charge != 0)
                        {
                            this.realizedObject = new ElectricSpear(this, this.world, 3);
                        }
                        else
                        {
                            this.realizedObject = new ElectricSpear(this, this.world, (this as AbstractElectricSpear).charge);
                        }*/
                        this.realizedObject = new ElectricSpear(this, this.world);
                        //(this as AbstractElectricSpear).realizedElectricSpear = (this.realizedObject as ElectricSpear);                      
                    }
                    return;
            }
            orig_Realize();
        }
        public enum AbstractObjectType
        {
            Creature,
            Rock,
            Spear,
            FlareBomb,
            VultureMask,
            PuffBall,
            DangleFruit,
            Oracle,
            PebblesPearl,
            SLOracleSwarmer,
            SSOracleSwarmer,
            DataPearl,
            SeedCob,
            WaterNut,
            JellyFish,
            Lantern,
            KarmaFlower,
            Mushroom,
            VoidSpawn,
            FirecrackerPlant,
            SlimeMold,
            FlyLure,
            ScavengerBomb,
            SporePlant,
            AttachedBee,
            EggBugEgg,
            NeedleEgg,
            DartMaggot,
            BubbleGrass,
            NSHSwarmer,
            OverseerCarcass,
            ElectricSpear
        }
        [MonoMod.MonoModIgnore]
        public override void Update(int time)
        {
            base.Update(time);
        }
        [MonoMod.MonoModIgnore]
        public virtual void Move(WorldCoordinate newCoord)
        {
            if (!newCoord.CompareDisregardingTile(this.pos))
            {
                this.timeSpentHere = 0;
                if (newCoord.room != this.pos.room)
                {
                    this.ChangeRooms(newCoord);
                }
                if (!newCoord.TileDefined && this.pos.room == newCoord.room)
                {
                    newCoord.Tile = this.pos.Tile;
                }
                this.pos = newCoord;
                for (int i = 0; i < this.stuckObjects.Count; i++)
                {
                    AbstractPhysicalObject.AbstractObjectStick abstractObjectStick = this.stuckObjects[i];
                    abstractObjectStick.A.Move(newCoord);
                    abstractObjectStick.B.Move(newCoord);
                }
            }
        }
        [MonoMod.MonoModIgnore]
        public virtual void ChangeRooms(WorldCoordinate newCoord)
        {
            this.world.GetAbstractRoom(this.pos).RemoveEntity(this);
            this.world.GetAbstractRoom(newCoord).AddEntity(this);
        }
        [MonoMod.MonoModIgnore]
        public virtual void RealizeInRoom()
        {
            if (this.InDen)
            {
                return;
            }
            this.Realize();
            if (this.world.GetAbstractRoom(this.pos).realizedRoom == null)
            {
                Debug.Log("TRYING TO REALIZE IN NON REALIZED ROOM! " + this.type);
                if (this is AbstractCreature)
                {
                    // Debug.Log("creature type: " + (this as AbstractCreature).creatureTemplate.type);
                }
                return;
            }
            if (!this.pos.TileDefined)
            {
                this.pos.Tile = base.Room.realizedRoom.LocalCoordinateOfNode(this.pos.abstractNode).Tile;
            }
            List<AbstractPhysicalObject> allConnectedObjects = this.GetAllConnectedObjects();
            for (int i = 0; i < allConnectedObjects.Count; i++)
            {
                allConnectedObjects[i].pos = this.pos;
                if (allConnectedObjects[i].realizedObject != null)
                {
                    allConnectedObjects[i].realizedObject.PlaceInRoom(base.Room.realizedRoom);
                }
            }
        }
        //(this as AbstractElectricSpear).charge = (this as AbstractElectricSpear).realizedElectricSpear.charge;
        [MonoMod.MonoModIgnore]
        public override void Abstractize(WorldCoordinate coord)
        {
            base.Abstractize(coord);
            this.timeSpentHere = 0;
            this.Move(coord);
            if (this.realizedObject != null && this.realizedObject.room != null)
            {
                this.realizedObject.room.RemoveObject(this.realizedObject);
            }
            this.realizedObject = null;
            if (this.destroyOnAbstraction)
            {
                this.Destroy();
            }
        }
        [MonoMod.MonoModIgnore]
        public override void IsEnteringDen(WorldCoordinate den)
        {
            if (den.room != this.pos.room || den.abstractNode != this.pos.abstractNode)
            {
                this.Move(den);
            }
            base.IsEnteringDen(den);
            for (int i = this.stuckObjects.Count - 1; i >= 0; i--)
            {
                if (i < this.stuckObjects.Count)
                {
                    AbstractPhysicalObject.AbstractObjectStick abstractObjectStick = this.stuckObjects[i];
                    if (den.room != abstractObjectStick.A.pos.room || den.abstractNode != abstractObjectStick.A.pos.abstractNode)
                    {
                        abstractObjectStick.A.Move(den);
                    }
                    if (den.room != abstractObjectStick.B.pos.room || den.abstractNode != abstractObjectStick.B.pos.abstractNode)
                    {
                        abstractObjectStick.B.Move(den);
                    }
                    if (!abstractObjectStick.A.InDen)
                    {
                        base.Room.MoveEntityToDen(abstractObjectStick.A);
                    }
                    if (!abstractObjectStick.B.InDen)
                    {
                        base.Room.MoveEntityToDen(abstractObjectStick.B);
                    }
                }
            }
        }
        [MonoMod.MonoModIgnore]
        public override void IsExitingDen()
        {
            base.IsExitingDen();
            for (int i = 0; i < this.stuckObjects.Count; i++)
            {
                if (this.stuckObjects[i].A.InDen)
                {
                    base.Room.MoveEntityOutOfDen(this.stuckObjects[i].A);
                }
                if (this.stuckObjects[i].B.InDen)
                {
                    base.Room.MoveEntityOutOfDen(this.stuckObjects[i].B);
                }
            }
        }
        [MonoMod.MonoModIgnore]
        public override void Destroy()
        {
            this.LoseAllStuckObjects();
            base.Destroy();
        }
        [MonoMod.MonoModIgnore]
        public override string ToString()
        {
            return string.Concat(new object[]
            {
            this.ID.ToString(),
            "<oA>",
            this.type.ToString(),
            "<oA>",
            this.pos.room,
            ".",
            this.pos.x,
            ".",
            this.pos.y,
            ".",
            this.pos.abstractNode
            });
        }
        [MonoMod.MonoModIgnore]
        public List<AbstractPhysicalObject> GetAllConnectedObjects()
        {
            List<AbstractPhysicalObject> result = new List<AbstractPhysicalObject>
        {
            this
        };
            this.AddConnected(ref result);
            return result;
        }
        [MonoMod.MonoModIgnore]
        private void AddConnected(ref List<AbstractPhysicalObject> l)
        {
            for (int i = 0; i < this.stuckObjects.Count; i++)
            {
                if (!l.Contains(this.stuckObjects[i].A))
                {
                    l.Add(this.stuckObjects[i].A);
                    this.stuckObjects[i].A.AddConnected(ref l);
                }
                if (!l.Contains(this.stuckObjects[i].B))
                {
                    l.Add(this.stuckObjects[i].B);
                    this.stuckObjects[i].B.AddConnected(ref l);
                }
            }
        }
        [MonoMod.MonoModIgnore]
        public void LoseAllStuckObjects()
        {
            for (int i = this.stuckObjects.Count - 1; i >= 0; i--)
            {
                this.stuckObjects[i].Deactivate();
            }
        }
        [MonoMod.MonoModIgnore]
        public PhysicalObject realizedObject;
        [MonoMod.MonoModIgnore]
        public List<AbstractPhysicalObject.AbstractObjectStick> stuckObjects;
        [MonoMod.MonoModIgnore]
        public bool destroyOnAbstraction;
        [MonoMod.MonoModIgnore]
        public AbstractPhysicalObject.AbstractObjectType type;
        [MonoMod.MonoModIgnore]
        public abstract class AbstractObjectStick { }

    }
    [MonoModPatch("global::ScavengerTreasury")]
    public class Patch_ScavengerTreasury : ScavengerTreasury
    {
        [MonoMod.MonoModIgnore]
        public Patch_ScavengerTreasury(Room room, PlacedObject placedObj) : base(room, placedObj) { }

        public extern void orig_ctor(Room room, PlacedObject placedObj);
        [MonoModConstructor]
        public void ctor(Room room, PlacedObject placedObj)
        {
            orig_ctor(room, placedObj);
            this.room = room;
            this.placedObj = placedObj;
            this.property = new List<AbstractPhysicalObject>();
            this.tiles = new List<IntVector2>();
            IntVector2 tilePosition = room.GetTilePosition(placedObj.pos);
            int num = (int)(this.Rad / 20f);
            for (int i = tilePosition.x - num; i <= tilePosition.x + num; i++)
            {
                for (int j = tilePosition.y - num; j <= tilePosition.y + num; j++)
                {
                    if (Custom.DistLess(room.MiddleOfTile(i, j), placedObj.pos, this.Rad) && !room.GetTile(i, j).Solid && !room.GetTile(i, j + 1).Solid && room.GetTile(i, j - 1).Solid)
                    {
                        this.tiles.Add(new IntVector2(i, j));
                    }
                }
            }
            //Debug.Log("PRE IF ROOM FIRST TIME");
            if (room.abstractRoom.firstTimeRealized)
            {
                bool eflag = true;
                bool flag = room.world.region.name == "SH" || room.world.region.name == "SB";
                for (int k = 0; k < this.tiles.Count; k++)
                {
                    if (UnityEngine.Random.value < Mathf.InverseLerp(this.Rad, this.Rad / 5f, Vector2.Distance(room.MiddleOfTile(this.tiles[k]), placedObj.pos)))
                    {
                        //Debug.Log("ENTERED THE FOR");
                        AbstractPhysicalObject abstractPhysicalObject;
                        if (eflag)
                        {
                            //Debug.Log("ABSTRACT OBJECT ELECTRIFIED");
                            eflag = false;
                            abstractPhysicalObject = new AbstractElectricSpear(room.world, null, room.GetWorldCoordinate(this.tiles[k]), room.game.GetNewID(), 3);
                        }
                        else if (UnityEngine.Random.value < 0.1f)
                        {
                            abstractPhysicalObject = new DataPearl.AbstractDataPearl(room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, room.GetWorldCoordinate(this.tiles[k]), room.game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc);
                        }
                        else if (UnityEngine.Random.value < 0.142857149f)
                        {
                            abstractPhysicalObject = new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, room.GetWorldCoordinate(this.tiles[k]), room.game.GetNewID());
                        }
                        else if (UnityEngine.Random.value < 1f / ((!flag) ? 20f : 5f))
                        {
                            abstractPhysicalObject = new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, room.GetWorldCoordinate(this.tiles[k]), room.game.GetNewID());
                        }
                        else
                        {
                            abstractPhysicalObject = new AbstractSpear(room.world, null, room.GetWorldCoordinate(this.tiles[k]), room.game.GetNewID(), UnityEngine.Random.value < 0.75f);
                        }
                        this.property.Add(abstractPhysicalObject);
                        if (abstractPhysicalObject != null)
                        {
                            //Debug.Log("ABSTRACT OBJECT ADDED TO ENTITIES");
                            room.abstractRoom.entities.Add(abstractPhysicalObject);
                        }
                    }
                }

            }
        }
        [MonoMod.MonoModIgnore]
        public float Rad
        {
            get
            {
                return (this.placedObj.data as PlacedObject.ResizableObjectData).handlePos.magnitude;
            }
        }
        [MonoMod.MonoModIgnore]
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.property.Count < 1)
            {
                return;
            }
            AbstractPhysicalObject abstractPhysicalObject = this.property[UnityEngine.Random.Range(0, this.property.Count)];
            if (abstractPhysicalObject.slatedForDeletion)
            {
                this.property.Remove(abstractPhysicalObject);
                return;
            }
            if (abstractPhysicalObject.realizedObject == null)
            {
                return;
            }
            if (abstractPhysicalObject.realizedObject.room != this.room)
            {
                this.property.Remove(abstractPhysicalObject);
                return;
            }
            if (abstractPhysicalObject.realizedObject.grabbedBy.Count > 0)
            {
                if (abstractPhysicalObject.realizedObject.grabbedBy[0].grabber is Player)
                {
                    this.room.socialEventRecognizer.AddStolenProperty(abstractPhysicalObject.ID);
                    for (int i = 0; i < this.room.abstractRoom.creatures.Count; i++)
                    {
                        if (this.room.abstractRoom.creatures[i].creatureTemplate.type == CreatureTemplate.Type.Scavenger && this.room.abstractRoom.creatures[i].realizedCreature != null && this.room.abstractRoom.creatures[i].realizedCreature.Consious)
                        {
                            float num = this.room.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, this.room.game.world.RegionNumber, (abstractPhysicalObject.realizedObject.grabbedBy[0].grabber as Player).playerState.playerNumber);
                            if (num < 0.9f)
                            {
                                this.room.game.session.creatureCommunities.InfluenceLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, this.room.game.world.RegionNumber, (abstractPhysicalObject.realizedObject.grabbedBy[0].grabber as Player).playerState.playerNumber, Custom.LerpMap(num, -0.5f, 0.9f, -0.3f, 0f), 0.5f, 0f);
                                Debug.Log("treasury theft noticed!");
                            }
                        }
                    }
                }
                this.property.Remove(abstractPhysicalObject);
                return;
            }
        }
        [MonoMod.MonoModIgnore]
        public PlacedObject placedObj;
        [MonoMod.MonoModIgnore]
        public List<AbstractPhysicalObject> property;
        [MonoMod.MonoModIgnore]
        public List<IntVector2> tiles;
    }

    [MonoModPatch("global::SaveState")]
    public class Patch_SaveState : SaveState
    {
        [MonoModIgnore]
        Patch_SaveState(int saveStateNumber, PlayerProgression progression) : base(saveStateNumber, progression) { }
        [MonoModIgnore]
        public extern void orig_ctor(int saveStateNumber, PlayerProgression progression);

        public extern static AbstractPhysicalObject orig_AbstractPhysicalObjectFromString(World world, string objString);
        public static AbstractPhysicalObject AbstractPhysicalObjectFromString(World world, string objString)
        {
            orig_AbstractPhysicalObjectFromString(world, objString);
            AbstractPhysicalObject result;
            try
            {
                //ElectricSpear<oA>109.23.11.0<oA>0<oA>2<rgC><rgA>
                string[] array = Regex.Split(objString, "<oA>");
                EntityID id = EntityID.FromString(array[0]);
                AbstractPhysicalObject.AbstractObjectType abstractObjectType = Custom.ParseEnum<AbstractPhysicalObject.AbstractObjectType>(array[1]);
                WorldCoordinate pos = new WorldCoordinate(int.Parse(array[2].Split(new char[]
                {
            '.'
                })[0]), int.Parse(array[2].Split(new char[]
                {
            '.'
                })[1]), int.Parse(array[2].Split(new char[]
                {
            '.'
                })[2]), int.Parse(array[2].Split(new char[]
                {
            '.'
                })[3]));
                if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.Spear)
                {
                    result = new AbstractSpear(world, null, pos, id, array[4] == "1")
                    {
                        stuckInWallCycles = int.Parse(array[3])
                    };
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.WaterNut)
                {
                    result = new WaterNut.AbstractWaterNut(world, null, pos, id, int.Parse(array[3]), int.Parse(array[4]), null, array[5] == "1");
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.PebblesPearl)
                {
                    result = new PebblesPearl.AbstractPebblesPearl(world, null, pos, id, int.Parse(array[3]), int.Parse(array[4]), null, int.Parse(array[6]), int.Parse(array[7]));
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.DataPearl)
                {
                    result = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, pos, id, int.Parse(array[3]), int.Parse(array[4]), null, (DataPearl.AbstractDataPearl.DataPearlType)int.Parse(array[5]));
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.SporePlant)
                {
                    result = new SporePlant.AbstractSporePlant(world, null, pos, id, int.Parse(array[3]), int.Parse(array[4]), null, array[5] == "1", array[6] == "1");
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.EggBugEgg)
                {
                    result = new EggBugEgg.AbstractBugEgg(world, null, pos, id, float.Parse(array[3]));
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.BubbleGrass)
                {
                    result = new BubbleGrass.AbstractBubbleGrass(world, null, pos, id, float.Parse(array[5]), int.Parse(array[3]), int.Parse(array[4]), null);
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.VultureMask)
                {
                    if (array.Length < 5)
                    {
                        result = new VultureMask.AbstractVultureMask(world, null, pos, id, id.RandomSeed, false);
                    }
                    else
                    {
                        result = new VultureMask.AbstractVultureMask(world, null, pos, id, int.Parse(array[3]), array[4] == "1");
                    }
                }
                else if (abstractObjectType == AbstractPhysicalObject.AbstractObjectType.OverseerCarcass)
                {
                    result = new OverseerCarcass.AbstractOverseerCarcass(world, null, pos, id, new Color(float.Parse(array[3]), float.Parse(array[4]), float.Parse(array[5])), int.Parse(array[6]));
                }
                else if (AbstractConsumable.IsTypeConsumable(abstractObjectType))
                {
                    result = new AbstractConsumable(world, abstractObjectType, null, pos, id, int.Parse(array[3]), int.Parse(array[4]), null);
                }
                else if (abstractObjectType == (AbstractPhysicalObject.AbstractObjectType)0x0000001F)
                {
                    result = new AbstractElectricSpear(world, null, pos, id, int.Parse(array[4]));
                    Debug.Log("-----------Abstract Electric Spear Created From txt file-----------");
                }
                else
                {
                    result = new AbstractPhysicalObject(world, abstractObjectType, null, pos, id);
                }
            }
            catch
            {
                result = null;
            }
            return result;
        }

    }
    public class AbstractElectricSpear : AbstractPhysicalObject
    {
        public AbstractElectricSpear(World world, ElectricSpear realizedObject, WorldCoordinate pos, EntityID ID, int charge) : base(world, (AbstractPhysicalObject.AbstractObjectType)0x000001F, realizedObject, pos, ID)
        {
            
            this.charge = charge;
            //this.realizedElectricSpear = realizedObject;
        }
        public bool stuckInWall
        {
            get
            {
                return this.stuckInWallCycles != 0;
            }
        }
        public void StuckInWallTick(int ticks)
        {
            if (this.stuckInWallCycles > 0)
            {
                this.stuckInWallCycles = Math.Max(0, this.stuckInWallCycles - ticks);
            }
            else if (this.stuckInWallCycles < 0)
            {
                this.stuckInWallCycles = Math.Min(0, this.stuckInWallCycles + ticks);
            }
        }
        public override string ToString()
        {
            return string.Concat(new object[]
            {
            this.ID.ToString(),
            "<oA>",
            this.type.ToString(),
            "<oA>",
            this.pos.room,
            ".",
            this.pos.x,
            ".",
            this.pos.y,
            ".",
            this.pos.abstractNode,
            "<oA>",
            this.stuckInWallCycles.ToString(),
            "<oA>",
            this.charge
            });
        }
        public int stuckInWallCycles;
        public bool stuckVertically;
        public int charge;
        //public ElectricSpear realizedElectricSpear;

    }
    public class AbstractAppendageStick : AbstractPhysicalObject.AbstractObjectStick
    {
        public AbstractAppendageStick(AbstractPhysicalObject electricSpear2, AbstractPhysicalObject stuckIn, int appendage, int prevSeg, float distanceToNext, float angle) : base(electricSpear2, stuckIn)
        {
            this.appendage = appendage;
            this.prevSeg = prevSeg;
            this.distanceToNext = distanceToNext;
            this.angle = angle;
        }
        public AbstractPhysicalObject ElectricSpear2
        {
            get
            {
                return this.A;
            }
        }
        public AbstractPhysicalObject LodgedIn
        {
            get
            {
                return this.B;
            }
        }
        public override string SaveToString(int roomIndex)
        {
            return string.Concat(new string[]
            {
            roomIndex.ToString(),
            "<stkA>sprLdgAppStk<stkA>",
            this.A.ID.ToString(),
            "<stkA>",
            this.B.ID.ToString(),
            "<stkA>",
            this.appendage.ToString(),
            "<stkA>",
            this.prevSeg.ToString(),
            "<stkA>",
            this.distanceToNext.ToString(),
            "<stkA>",
            this.angle.ToString()
            });
        }
        public int appendage;
        public int prevSeg;
        public float distanceToNext;
        public float angle;
    }
    public class AbstractSpearStick : AbstractPhysicalObject.AbstractObjectStick
    {
        public AbstractSpearStick(AbstractPhysicalObject spear, AbstractPhysicalObject stuckIn, int chunk, int bodyPart, float angle) : base(spear, stuckIn)
        {
            this.chunk = chunk;
            this.bodyPart = bodyPart;
            this.angle = angle;
        }
        public AbstractPhysicalObject Spear
        {
            get
            {
                return this.A;
            }
        }
        public AbstractPhysicalObject LodgedIn
        {
            get
            {
                return this.B;
            }
        }
        public override string SaveToString(int roomIndex)
        {
            return string.Concat(new string[]
            {
            roomIndex.ToString(),
            "<stkA>sprLdgStk<stkA>",
            this.A.ID.ToString(),
            "<stkA>",
            this.B.ID.ToString(),
            "<stkA>",
            this.chunk.ToString(),
            "<stkA>",
            this.bodyPart.ToString(),
            "<stkA>",
            this.angle.ToString()
            });
        }
        public int chunk;
        public int bodyPart;
        public float angle;
    }
    public class CreatureGripStick : AbstractPhysicalObject.AbstractObjectStick
    {
        public CreatureGripStick(AbstractCreature creature, AbstractPhysicalObject carried, int grasp, bool carry) : base(creature, carried)
        {
            this.grasp = grasp;
            this.carry = carry;
        }
        public override string SaveToString(int roomIndex)
        {
            return string.Concat(new string[]
            {
            roomIndex.ToString(),
            "<stkA>gripStk<stkA>",
            this.A.ID.ToString(),
            "<stkA>",
            this.B.ID.ToString(),
            "<stkA>",
            this.grasp.ToString(),
            "<stkA>",
            (!this.carry) ? "0" : "1"
            });
        }
        public int grasp;
        public bool carry;
    }
    public class ElectricSpear : Spear
    {
        //ABSTRACT PSYSICAL OBJECT
        public ElectricSpear(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.4f;
            this.collisionLayer = 2;
            base.waterFriction = 0.98f;
            base.buoyancy = 0.4f;
            this.pivotAtTip = false;
            this.lastPivotAtTip = false;
            this.stuckBodyPart = -1;
            base.firstChunk.loudness = 7f;
            this.tailPos = base.firstChunk.pos;
            this.soundLoop = new ChunkDynamicSoundLoop(base.firstChunk);
            //this.charge = charge;
        }
        public override bool HeavyWeapon
        {
            get
            {
                return true;
            }
        }
        public AbstractElectricSpear abstractElectricSpear
        {
            get
            {
                return this.abstractPhysicalObject as AbstractElectricSpear;
            }
        }
        public BodyChunk stuckInChunk
        {
            get
            {
                return this.stuckInObject.bodyChunks[this.stuckInChunkIndex];
            }
        }
        public float gravity
        {
            get
            {
                return this.g * this.room.gravity;
            }
            protected set
            {
                this.g = value;
            }
        }
        //INITIATE SPRITES
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[4];
            sLeaser.sprites[3] = new FSprite("SmallSpear", true);
            sLeaser.sprites[1] = new FSprite("pixel", true);
            sLeaser.sprites[1].scaleX = 2.3f;
            sLeaser.sprites[2] = new FSprite("pixel", true);
            sLeaser.sprites[2].scaleX = 2.3f;
            sLeaser.sprites[0] = new FSprite("pixel", true);
            sLeaser.sprites[0].scaleX = 2.3f;
            this.AddToContainer(sLeaser, rCam, null);
        }
        //APPLY PALETTE
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[3].color = this.color;
            sLeaser.sprites[1].color = whiteColor;
            sLeaser.sprites[1].scale = 2;
            sLeaser.sprites[2].color = whiteColor;
            sLeaser.sprites[2].scale = 2;
            sLeaser.sprites[0].color = redColor;
            sLeaser.sprites[0].scale = 2;
        }
        //DRAW SPRITES
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 a = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
            if (this.vibrate > 0)
            {
                a += Custom.DegToVec(UnityEngine.Random.value * 360f) * 2f * UnityEngine.Random.value;
            }
            Vector3 v = Vector3.Slerp(this.lastRotation, this.rotation, timeStacker);
            for (int i = 3; i >= 0; i--)
            {

                sLeaser.sprites[i].x = a.x - camPos.x;
                sLeaser.sprites[i].y = a.y - camPos.y;
                sLeaser.sprites[i].anchorY = Mathf.Lerp((!this.lastPivotAtTip) ? 0.5f : 0.85f, (!this.pivotAtTip) ? 0.5f : 0.85f, timeStacker);
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), v);

            }
            sLeaser.sprites[1].anchorY += 10f;//49
            sLeaser.sprites[2].anchorY += 8f;//47
            sLeaser.sprites[0].anchorY += 6f;//45
            if (this.blink > 0 && UnityEngine.Random.value < 0.5f)
            {
                sLeaser.sprites[0].color = base.blinkColor;
                sLeaser.sprites[1].color = base.blinkColor;
                sLeaser.sprites[2].color = base.blinkColor;
                sLeaser.sprites[3].color = base.blinkColor;
            }
            else
            {
                switch (charge)
                {
                    case 0:
                        sLeaser.sprites[0].color = redColor;
                        sLeaser.sprites[1].color = redColor;
                        sLeaser.sprites[2].color = redColor;
                        sLeaser.sprites[3].color = this.color;
                        break;
                    case 1:
                        sLeaser.sprites[0].color = redColor;
                        sLeaser.sprites[1].color = whiteColor;
                        sLeaser.sprites[2].color = redColor;
                        sLeaser.sprites[3].color = this.color;
                        break;
                    case 2:
                        sLeaser.sprites[0].color = redColor;
                        sLeaser.sprites[1].color = whiteColor;
                        sLeaser.sprites[2].color = whiteColor;
                        sLeaser.sprites[3].color = this.color;
                        break;
                    case 3:
                        sLeaser.sprites[0].color = whiteColor;
                        sLeaser.sprites[1].color = whiteColor;
                        sLeaser.sprites[2].color = whiteColor;
                        sLeaser.sprites[3].color = this.color;
                        break;
                }

            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
        //  HIT SOMETHING
        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj == null)
            {
                return false;
            }
            bool flag = false;
            if (this.abstractPhysicalObject.world.game.IsArenaSession && this.abstractPhysicalObject.world.game.GetArenaGameSession.GameTypeSetup.spearHitScore != 0 && this.thrownBy != null && this.thrownBy is Player && result.obj is Creature)
            {
                flag = true;
                if ((result.obj as Creature).State is HealthState && ((result.obj as Creature).State as HealthState).health <= 0f)
                {
                    flag = false;
                }
                else if (!((result.obj as Creature).State is HealthState) && (result.obj as Creature).State.dead)
                {
                    flag = false;
                }
            }
            if (result.obj is Creature)
            {
                if (result.obj is Centipede)
                {
                    if (charge != 3)
                    {
                        (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, this.spearDamageBonus, 20f);
                        charged = true;
                        depleted = false;
                    }
                    else
                    {
                        (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, this.spearDamageBonus, 20f);
                        charged = false;
                        depleted = false;
                    }
                }
                else
                {
                    if (charge != 0)
                    {
                        (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, this.spearDamageBonus, 20f);
                        (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Electric, 6f, 20f);
                        charged = false;
                        depleted = true;
                    }
                    else
                    {
                        (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, this.spearDamageBonus, 20f);
                        charged = false;
                        depleted = false;
                    }

                }
            }
            else if (result.chunk != null)
            {
                result.chunk.vel += base.firstChunk.vel * base.firstChunk.mass / result.chunk.mass;
            }
            else if (result.onAppendagePos != null)
            {
                (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, base.firstChunk.vel * base.firstChunk.mass);
            }
            if (result.obj is Creature && (result.obj as Creature).SpearStick(this, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, base.firstChunk.vel))
            {
                if (depleted)
                {
                    this.room.PlaySound(SoundID.Centipede_Shock, this.firstChunk);
                    charge--;

                }
                if (charged)
                {
                    this.room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, this.firstChunk);
                    charge++;
                    charged = false;
                }
                /*if(this != null)
                    {
                      this.abstractelectricSpear.charge = this.charge;
                    }*/
                this.room.PlaySound(SoundID.Spear_Stick_In_Creature, base.firstChunk);
                this.LodgeInCreature(result, eu);
                lightFlash = 1.3f;
                if (flag)
                {
                    this.abstractPhysicalObject.world.game.GetArenaGameSession.PlayerLandSpear(this.thrownBy as Player, this.stuckInObject as Creature);
                }
                return true;
            }
            this.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, base.firstChunk);
            this.vibrate = 20;
            this.ChangeMode(Weapon.Mode.Free);
            base.firstChunk.vel = base.firstChunk.vel * -0.5f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, UnityEngine.Random.value) * base.firstChunk.vel.magnitude;
            this.SetRandomSpin();
            return false;
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            this.soundLoop.sound = SoundID.None;
            if (base.firstChunk.vel.magnitude > 5f)
            {
                if (base.mode == Weapon.Mode.Thrown)
                {
                    this.soundLoop.sound = SoundID.Spear_Thrown_Through_Air_LOOP;
                }
                else if (base.mode == Weapon.Mode.Free)
                {
                    this.soundLoop.sound = SoundID.Spear_Spinning_Through_Air_LOOP;
                }
                this.soundLoop.Volume = Mathf.InverseLerp(5f, 15f, base.firstChunk.vel.magnitude);
            }
            this.soundLoop.Update();
            this.lastPivotAtTip = this.pivotAtTip;
            this.pivotAtTip = (base.mode == Weapon.Mode.Thrown || base.mode == Weapon.Mode.StuckInCreature);
            if (this.addPoles && this.room.readyForAI)
            {
                if (this.abstractSpear.stuckInWallCycles >= 0)
                {
                    this.room.GetTile(this.stuckInWall.Value).horizontalBeam = true;
                    for (int i = -1; i < 2; i += 2)
                    {
                        if (!this.room.GetTile(this.stuckInWall.Value + new Vector2(20f * (float)i, 0f)).Solid)
                        {
                            this.room.GetTile(this.stuckInWall.Value + new Vector2(20f * (float)i, 0f)).horizontalBeam = true;
                        }
                    }
                }
                else
                {
                    this.room.GetTile(this.stuckInWall.Value).verticalBeam = true;
                    for (int j = -1; j < 2; j += 2)
                    {
                        if (!this.room.GetTile(this.stuckInWall.Value + new Vector2(0f, 20f * (float)j)).Solid)
                        {
                            this.room.GetTile(this.stuckInWall.Value + new Vector2(0f, 20f * (float)j)).verticalBeam = true;
                        }
                    }
                }
                this.addPoles = false;
            }
            switch (base.mode)
            {
                case Weapon.Mode.Free:

                    if (this.spinning)
                    {
                        if (Custom.DistLess(base.firstChunk.pos, base.firstChunk.lastPos, 4f * this.room.gravity))
                        {
                            this.stillCounter++;
                        }
                        else
                        {
                            this.stillCounter = 0;
                        }
                        if (base.firstChunk.ContactPoint.y < 0 || this.stillCounter > 20)
                        {
                            this.spinning = false;
                            this.rotationSpeed = 0f;
                            this.rotation = Custom.DegToVec(Mathf.Lerp(-50f, 50f, UnityEngine.Random.value) + 180f);
                            base.firstChunk.vel *= 0f;
                            this.room.PlaySound(SoundID.Spear_Stick_In_Ground, base.firstChunk);
                        }
                    }
                    else if (!Custom.DistLess(base.firstChunk.lastPos, base.firstChunk.pos, 6f))
                    {
                        this.SetRandomSpin();
                    }
                    break;
                case Weapon.Mode.Thrown:
                    {
                        if (Custom.DistLess(this.thrownPos, base.firstChunk.pos, 560f * Mathf.Max(1f, this.spearDamageBonus)) && base.firstChunk.ContactPoint == this.throwDir && this.room.GetTile(base.firstChunk.pos).Terrain == Room.Tile.TerrainType.Air && this.room.GetTile(base.firstChunk.pos + this.throwDir.ToVector2() * 20f).Terrain == Room.Tile.TerrainType.Solid && (UnityEngine.Random.value < ((!(this is ExplosiveSpear)) ? 0.33f : 0.8f) || Custom.DistLess(this.thrownPos, base.firstChunk.pos, 140f) || this.alwaysStickInWalls))
                        {
                            bool flag = true;
                            foreach (AbstractWorldEntity abstractWorldEntity in this.room.abstractRoom.entities)
                            {
                                if (abstractWorldEntity is AbstractSpear && (abstractWorldEntity as AbstractSpear).realizedObject != null && ((abstractWorldEntity as AbstractSpear).realizedObject as Weapon).mode == Weapon.Mode.StuckInWall && abstractWorldEntity.pos.Tile == this.abstractPhysicalObject.pos.Tile)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag && !(this is ExplosiveSpear))
                            {
                                for (int k = 0; k < this.room.roomSettings.placedObjects.Count; k++)
                                {
                                    if (this.room.roomSettings.placedObjects[k].type == PlacedObject.Type.NoSpearStickZone && Custom.DistLess(this.room.MiddleOfTile(base.firstChunk.pos), this.room.roomSettings.placedObjects[k].pos, (this.room.roomSettings.placedObjects[k].data as PlacedObject.ResizableObjectData).Rad))
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                            }
                            if (flag)
                            {
                                this.stuckInWall = new Vector2?(this.room.MiddleOfTile(base.firstChunk.pos));
                                this.vibrate = 10;
                                this.ChangeMode(Weapon.Mode.StuckInWall);
                                this.room.PlaySound(SoundID.Spear_Stick_In_Wall, base.firstChunk);
                                base.firstChunk.collideWithTerrain = false;
                            }
                        }
                        break;
                    }
                case Weapon.Mode.StuckInCreature:
                    if (this.stuckInWall == null)
                    {
                        if (this.stuckInAppendage != null)
                        {
                            this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.VecToDeg(this.stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage))));
                            base.firstChunk.pos = this.stuckInAppendage.appendage.OnAppendagePosition(this.stuckInAppendage);
                        }
                        else
                        {
                            if (depleted)
                            {
                                //Debug.Log("STUCK IN CHUNK");
                                if (this.lightSource != null)
                                {
                                    //Debug.Log("LIGHT SOURCE NOT NULL");
                                    this.lightSource.stayAlive = true;
                                    this.lightSource.setPos = new Vector2?(this.firstChunk.pos);
                                    this.lightSource.setRad = new float?((300f * Mathf.Pow(this.lightFlash * UnityEngine.Random.value, 0.01f) * Mathf.Lerp(0.5f, 2f, 0.8f)) - 1.3f);//THE LAST FLOAT IN MATHF.LERP IS A MIGUE
                                    this.lightSource.setAlpha = new float?((Mathf.Pow(this.lightFlash * UnityEngine.Random.value, 0.01f)) - 0.8f);
                                    float num5 = this.lightFlash * UnityEngine.Random.value;
                                    num5 = Mathf.Lerp(num5, 1f, 0.5f * (1f - this.room.Darkness(this.firstChunk.pos)));
                                    this.lightSource.color = new Color(num5, num5, 1.5f);
                                    if (this.lightFlash <= 0f)
                                    {
                                        //Debug.Log("LIGHT SOURCE DESTROY");
                                        this.lightSource.Destroy();
                                    }
                                    if (this.lightSource.slatedForDeletetion)
                                    {
                                        if (depleted)
                                        {
                                            depleted = false;
                                        }
                                        //Debug.Log("LIGHT SOURCE SLATED FOR DELETETION");
                                        this.lightSource = null;
                                    }
                                }
                                else if (this.lightFlash > 0f)
                                {
                                    //Debug.Log("LIGHT SOURCE NULL AND LIGHT FLASH > 0");
                                    this.lightSource = new LightSource(this.firstChunk.pos, false, new Color(1f, 1f, 1f), this);
                                    this.lightSource.affectedByPaletteDarkness = 0f;
                                    this.lightSource.requireUpKeep = true;
                                    this.room.AddObject(this.lightSource);
                                }
                                if (this.lightFlash > 0f)
                                {
                                    //Debug.Log("LIGHT SOURCE NULL LIGHT SOURCE DEPLETION");
                                    this.lightFlash = Mathf.Max(0f, this.lightFlash - 0.0333933351f);
                                }
                            }
                            base.firstChunk.vel = this.stuckInChunk.vel;
                            if (this.stuckBodyPart == -1 || !this.room.BeingViewed || (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart) == null)
                            {
                                this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.VecToDeg(this.stuckInChunk.Rotation)));
                                base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, new Vector2(0f, 0f));
                            }
                            else
                            {
                                this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.AimFromOneVectorToAnother(this.stuckInChunk.pos, (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart).pos)));
                                base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, Vector2.Lerp(this.stuckInChunk.pos, (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart).pos, 0.5f) - this.stuckInChunk.pos);
                            }
                        }
                    }
                    else
                    {
                        if (this.pinToWallCounter > 0)
                        {
                            this.pinToWallCounter--;
                        }
                        if (this.stuckInChunk.vel.magnitude * this.stuckInChunk.mass > Custom.LerpMap((float)this.pinToWallCounter, 160f, 0f, 7f, 2f))
                        {
                            this.setRotation = new Vector2?((Custom.DegToVec(this.stuckRotation) + Vector2.ClampMagnitude(this.stuckInChunk.vel * this.stuckInChunk.mass * 0.005f, 0.1f)).normalized);
                        }
                        else
                        {
                            this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation));
                        }
                        base.firstChunk.vel *= 0f;
                        base.firstChunk.pos = this.stuckInWall.Value;
                        if ((this.stuckInChunk.owner is Creature && (this.stuckInChunk.owner as Creature).enteringShortCut != null) || (this.pinToWallCounter < 160 && UnityEngine.Random.value < 0.025f && this.stuckInChunk.vel.magnitude > Custom.LerpMap((float)this.pinToWallCounter, 160f, 0f, 140f, 30f / (1f + this.stuckInChunk.owner.TotalMass * 0.2f))))
                        {
                            this.stuckRotation = Custom.Angle(this.setRotation.Value, this.stuckInChunk.Rotation);
                            this.stuckInWall = null;
                        }
                        else
                        {
                            this.stuckInChunk.MoveFromOutsideMyUpdate(eu, this.stuckInWall.Value);
                            this.stuckInChunk.vel *= 0f;
                        }
                    }
                    if (this.stuckInChunk.owner.slatedForDeletetion)
                    {
                        this.ChangeMode(Weapon.Mode.Free);
                    }
                    break;
                case Weapon.Mode.StuckInWall:
                    base.firstChunk.pos = this.stuckInWall.Value;
                    base.firstChunk.vel *= 0f;
                    break;
            }
            for (int l = this.abstractPhysicalObject.stuckObjects.Count - 1; l >= 0; l--)
            {
                if (this.abstractPhysicalObject.stuckObjects[l] is AbstractPhysicalObject.ImpaledOnSpearStick)
                {
                    if (this.abstractPhysicalObject.stuckObjects[l].B.realizedObject != null && (this.abstractPhysicalObject.stuckObjects[l].B.realizedObject.slatedForDeletetion || this.abstractPhysicalObject.stuckObjects[l].B.realizedObject.grabbedBy.Count > 0))
                    {
                        this.abstractPhysicalObject.stuckObjects[l].Deactivate();
                    }
                    else if (this.abstractPhysicalObject.stuckObjects[l].B.realizedObject != null && this.abstractPhysicalObject.stuckObjects[l].B.realizedObject.room == this.room)
                    {
                        this.abstractPhysicalObject.stuckObjects[l].B.realizedObject.firstChunk.MoveFromOutsideMyUpdate(eu, base.firstChunk.pos + this.rotation * Custom.LerpMap((float)(this.abstractPhysicalObject.stuckObjects[l] as AbstractPhysicalObject.ImpaledOnSpearStick).onSpearPosition, 0f, 4f, 15f, -15f));
                        this.abstractPhysicalObject.stuckObjects[l].B.realizedObject.firstChunk.vel *= 0f;
                    }
                }
            }
        }
        //OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            if (this.abstractSpear.stuckInWall)
            {
                this.stuckInWall = new Vector2?(placeRoom.MiddleOfTile(this.abstractPhysicalObject.pos.Tile));
                this.ChangeMode(Weapon.Mode.StuckInWall);
            }
        }
        //CHANGE MODE
        public override void ChangeMode(Weapon.Mode newMode)
        {
            if (base.mode == Weapon.Mode.StuckInCreature)
            {
                if (this.room != null)
                {
                    this.room.PlaySound(SoundID.Spear_Dislodged_From_Creature, base.firstChunk);
                }
                this.PulledOutOfStuckObject();
                base.ChangeOverlap(true);
            }
            else if (newMode == Weapon.Mode.StuckInCreature)
            {
                base.ChangeOverlap(false);
            }
            if (newMode != Weapon.Mode.Thrown)
            {
                this.spearDamageBonus = 1f;
            }
            if (newMode == Weapon.Mode.StuckInWall)
            {
                if (this.abstractSpear.stuckInWallCycles == 0)
                {
                    this.abstractSpear.stuckInWallCycles = UnityEngine.Random.Range(3, 7) * ((this.throwDir.y == 0) ? 1 : -1);
                }
                for (int i = -1; i < 2; i += 2)
                {
                    if ((this.abstractSpear.stuckInWallCycles >= 0 && !this.room.GetTile(this.stuckInWall.Value + new Vector2(20f * (float)i, 0f)).Solid) || (this.abstractSpear.stuckInWallCycles < 0 && !this.room.GetTile(this.stuckInWall.Value + new Vector2(0f, 20f * (float)i)).Solid))
                    {
                        this.setRotation = new Vector2?((this.abstractSpear.stuckInWallCycles < 0) ? new Vector2(0f, (float)(-(float)i)) : new Vector2((float)(-(float)i), 0f));
                        break;
                    }
                }
                if (this.setRotation != null)
                {
                    this.stuckInWall = new Vector2?(this.room.MiddleOfTile(this.stuckInWall.Value) - this.setRotation.Value * 5f);
                }
                this.rotationSpeed = 0f;
            }
            if (newMode != Weapon.Mode.Free)
            {
                this.spinning = false;
            }
            if (newMode != Weapon.Mode.StuckInWall && newMode != Weapon.Mode.StuckInCreature)
            {
                this.stuckInWall = null;
            }
            base.ChangeMode(newMode);
        }
        public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            this.room.PlaySound(SoundID.Slugcat_Throw_Spear, base.firstChunk);
            this.alwaysStickInWalls = false;
        }
        private void LodgeInCreature(SharedPhysics.CollisionResult result, bool eu)
        {
            this.stuckInObject = result.obj;
            this.ChangeMode(Weapon.Mode.StuckInCreature);
            if (result.chunk != null)
            {
                this.stuckInChunkIndex = result.chunk.index;
                if (this.spearDamageBonus > 0.9f && this.room.GetTile(this.room.GetTilePosition(this.stuckInChunk.pos) + this.throwDir).Terrain == Room.Tile.TerrainType.Solid && this.room.GetTile(this.stuckInChunk.pos).Terrain == Room.Tile.TerrainType.Air)
                {
                    this.stuckInWall = new Vector2?(this.room.MiddleOfTile(this.stuckInChunk.pos) + this.throwDir.ToVector2() * (10f - this.stuckInChunk.rad));
                    this.stuckInChunk.MoveFromOutsideMyUpdate(eu, this.stuckInWall.Value);
                    this.stuckRotation = Custom.VecToDeg(this.rotation);
                    this.stuckBodyPart = -1;
                    this.pinToWallCounter = 300;
                }
                else if (this.stuckBodyPart == -1)
                {
                    this.stuckRotation = Custom.Angle(this.throwDir.ToVector2(), this.stuckInChunk.Rotation);
                }
                base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, new Vector2(0f, 0f));
                Debug.Log("Add spear to creature chunk " + this.stuckInChunk.index);
                new AbstractPhysicalObject.AbstractSpearStick(this.abstractPhysicalObject, (result.obj as Creature).abstractCreature, this.stuckInChunkIndex, this.stuckBodyPart, this.stuckRotation);
            }
            else if (result.onAppendagePos != null)
            {
                this.stuckInChunkIndex = 0;
                this.stuckInAppendage = result.onAppendagePos;
                this.stuckRotation = Custom.VecToDeg(this.rotation) - Custom.VecToDeg(this.stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage));
                Debug.Log("Add spear to creature Appendage");
                new AbstractPhysicalObject.AbstractSpearAppendageStick(this.abstractPhysicalObject, (result.obj as Creature).abstractCreature, result.onAppendagePos.appendage.appIndex, result.onAppendagePos.prevSegment, result.onAppendagePos.distanceToNext, this.stuckRotation);
            }
            if (this.room.BeingViewed)
            {
                for (int i = 0; i < 8; i++)
                {
                    this.room.AddObject(new WaterDrip(result.collisionPoint, -base.firstChunk.vel * UnityEngine.Random.value * 0.5f + Custom.DegToVec(360f * UnityEngine.Random.value) * base.firstChunk.vel.magnitude * UnityEngine.Random.value * 0.5f, false));
                }
            }
        }
        public virtual void TryImpaleSmallCreature(Creature smallCrit)
        {
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < this.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.ImpaledOnSpearStick)
                {
                    if ((this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.ImpaledOnSpearStick).onSpearPosition == num2)
                    {
                        num2++;
                    }
                    num++;
                }
            }
            if (num > 5 || num2 >= 5)
            {
                return;
            }
            new AbstractPhysicalObject.ImpaledOnSpearStick(this.abstractPhysicalObject, smallCrit.abstractCreature, 0, num2);
        }
        public override void SetRandomSpin()
        {
            if (this.room != null)
            {
                this.rotationSpeed = ((UnityEngine.Random.value >= 0.5f) ? 1f : -1f) * Mathf.Lerp(50f, 150f, UnityEngine.Random.value) * Mathf.Lerp(0.05f, 1f, this.room.gravity);
            }
            this.spinning = true;
        }
        public void ProvideRotationBodyPart(BodyChunk chunk, BodyPart bodyPart)
        {
            this.stuckBodyPart = bodyPart.bodyPartArrayIndex;
            this.stuckRotation = Custom.Angle(base.firstChunk.vel, (bodyPart.pos - chunk.pos).normalized);
            bodyPart.vel += base.firstChunk.vel;
        }
        public override void HitSomethingWithoutStopping(PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            base.HitSomethingWithoutStopping(obj, chunk, appendage);
            if (obj is Fly)
            {
                this.TryImpaleSmallCreature(obj as Creature);
            }
        }
        public void PulledOutOfStuckObject()
        {
            for (int i = 0; i < this.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).Spear == this.abstractPhysicalObject)
                {
                    this.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearAppendageStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).Spear == this.abstractPhysicalObject)
                {
                    this.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
            }
            this.stuckInObject = null;
            this.stuckInAppendage = null;
            this.stuckInChunkIndex = 0;
        }
        public override void RecreateSticksFromAbstract()
        {
            for (int i = 0; i < this.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).Spear == this.abstractPhysicalObject && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).LodgedIn.realizedObject != null)
                {
                    AbstractPhysicalObject.AbstractSpearStick abstractSpearStick = this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick;
                    this.stuckInObject = abstractSpearStick.LodgedIn.realizedObject;
                    this.stuckInChunkIndex = abstractSpearStick.chunk;
                    this.stuckBodyPart = abstractSpearStick.bodyPart;
                    this.stuckRotation = abstractSpearStick.angle;
                    this.ChangeMode(Weapon.Mode.StuckInCreature);
                }
                else if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearAppendageStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).Spear == this.abstractPhysicalObject && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).LodgedIn.realizedObject != null)
                {
                    AbstractPhysicalObject.AbstractSpearAppendageStick abstractSpearAppendageStick = this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick;
                    this.stuckInObject = abstractSpearAppendageStick.LodgedIn.realizedObject;
                    this.stuckInAppendage = new PhysicalObject.Appendage.Pos(this.stuckInObject.appendages[abstractSpearAppendageStick.appendage], abstractSpearAppendageStick.prevSeg, abstractSpearAppendageStick.distanceToNext);
                    this.stuckRotation = abstractSpearAppendageStick.angle;
                    this.ChangeMode(Weapon.Mode.StuckInCreature);
                }
            }
        }

        public Color redColor = Color.red;
        public Color whiteColor = Color.white;
        public World world;
        private int stuckInChunkIndex;
        private bool charged;
        private bool depleted;
        private float conRad = 7f;
        private int stuckBodyPart;
        private bool spinning;
        protected bool pivotAtTip;
        public PhysicalObject.Appendage.Pos stuckInAppendage;
        public float stuckRotation;
        public Vector2? stuckInWall;
        public bool alwaysStickInWalls;
        public int pinToWallCounter;
        private bool addPoles;
        public float spearDamageBonus;
        private int stillCounter;
        public LightSource lightSource;
        public float lightFlash;
        //public int charge => (this.abstractPhysicalObject as AbstractElectricSpear).charge;
        //public int charge;
        public int charge
        {
            get { return (this.abstractPhysicalObject as AbstractElectricSpear).charge; }
            set { (this.abstractPhysicalObject as AbstractElectricSpear).charge = value; }
        }
    }
}