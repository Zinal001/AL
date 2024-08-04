const { makeDb } = require("mysql-async-simple");
const mysql = require("mysql");
const { MongoClient } = require("mongodb");
const { Server } = require("./Server");
const { Map } = require("./Map");
const { Quirk } = require("./Quirk");
const { Door } = require("./Door");
const { Spawn } = require("./Spawn");
const { Zone } = require("./Zone");
const { MonsterZone } = require("./MonsterZone");
const { Npc } = require("./Npc");
const { Position } = require("./Position");
const { Boundary } = require("./Boundary");
const { Item } = require("./Item");

/**
 * @type MongoClient
 */
let client;
/**
 * @type Db
 */
let db;

const dbClasses = {
    "Quirk": Quirk,
    "Door": Door,
    "Spawn": Spawn,
    "Zone": Zone,
    "MonsterZone": MonsterZone,
    "Npc": Npc
};

async function init()
{
    client = new MongoClient("mongodb://root:root@db:27017");

    try {
        db = client.db("almapper");
        return true;
    }
    catch(ex)
    {
        return ex;
    }
}

/**
 *
 * @param {string} serverId
 * @returns {Promise<null|Server|>}
 */
async function server(serverId)
{
    const dbS = await db.collection("Servers").findOne({ ServerKey: serverId });
    if(dbS != null)
    {
        let server = new Server();
        populate(dbS, server);
        return server;
    }

    return null;
}

/**
 *
 * @returns {Promise<Server[]>}
 */
async function allServers()
{
    const cursor = db.collection("Servers").find();

    let servers = [];
    for await (const dbS of cursor)
    {
        let server = new Server();
        populate(dbS, server);
        servers.push(server);
    }

    return servers;
}

/**
 * @param {string} name
 * @param {number} serverId
 * @returns {Promise<null|Map>}
 */
async function map(name, serverId)
{
    let dbMap = await db.collection("Maps").findOne({ ServerId: serverId, Name: name });
    if(dbMap != null)
    {
        let map = new Map();
        populate(dbMap, map);

        return map;
    }

    return null;
}

/**
 *
 * @param {ObjectId} id
 * @returns {Promise<null|Map|>}
 */
async function mapById(id)
{
    let dbMap = await db.collection("Maps").findOne({ _id: id });

    if(dbMap != null)
    {
        let map = new Map();
        populate(dbMap, map);

        return map;
    }

    return null;
}

/**
 *
 * @param {number} serverId
 * @returns {Promise<Map[]>}
 */
async function mapsByServerId(serverId)
{
    let dbMaps = db.collection("Maps").find({ ServerId: serverId });
    let maps = [];
    for await (let dbM of dbMaps)
    {
        let m = new Map();
        populate(dbM, m);
        maps.push(m);
    }

    return maps;
}

/**
 * @param {number} mapId
 * @returns {Promise<Quirk[]>}
 */
async function quirks(mapId)
{
    return getMapItems(mapId, "Quirk");
}

/**
 * @param {number} mapId
 * @returns {Promise<Door[]>}
 */
async function doors(mapId)
{
    /** @type Door[] */
    let doors = await getMapItems(mapId, "Door");

    for(let d of doors)
    {
        if(d.ToMapId != null)
        {
            d.ToMap = await mapById(d.ToMapId);
        }
    }

    return doors;
}

/**
 * @param {number} mapId
 * @returns {Promise<Spawn[]>}
 */
async function spawns(mapId)
{
    return getMapItems(mapId, "Spawn");
}

/**
 * @param {number} mapId
 * @returns {Promise<Zone[]>}
 */
async function zones(mapId)
{
    return getMapItems(mapId, "Zone");
}

/**
 * @param {number} mapId
 * @returns {Promise<MonsterZone[]>}
 */
async function monsters(mapId)
{
    return getMapItems(mapId, "MonsterZone");
}

/**
 * @param {number} mapId
 * @returns {Promise<Npc[]>}
 */
async function npcs(mapId)
{
    return getMapItems(mapId, "Npc");
}

/**
 *
 * @param {string} npcId
 * @param {number} mapId
 * @returns {Promise<null|Npc>}
 */
async function npc(npcId, mapId)
{
    let dbNpc = await db.collection("Npcs").findOne({ NpcId: npcId, MapId: mapId });

    if(dbNpc != null)
    {
        let npc = new Npc();
        populate(dbNpc, npc);
        fixObjJson(npc);
        return npc;
    }

    return null;
}

/**
 *
 * @param {number} mapId
 * @param {string} itemType
 * @returns {Promise<[]>}
 */
async function getMapItems(mapId, itemType)
{
    let tableName = `${itemType}s`;
    let dbObjs = db.collection(tableName).find({ MapId: mapId });
    let objs = [];
    for await(let dbO of dbObjs)
    {
        let o = new dbClasses[itemType]();
        populate(dbO, o);
        fixObjJson(o);
        objs.push(o);
    }

    return objs;
}

function fixObjJson(obj)
{
    if("Boundary" in obj && obj.Boundary != null)
    {
        if(obj.Boundary === 'null')
            obj.Boundary = null;
        else
        {
            let dbB = JSON.parse(obj.Boundary);
            obj.Boundary = new Boundary();
            populate(dbB, obj.Boundary);
        }
    }

    if("Boundaries" in obj && obj.Boundaries != null)
    {
        if(obj.Boundaries === "null")
            obj.Boundaries = null;
        else
        {
            let dbBounds = JSON.parse(obj.Boundaries);
            let bounds = [];
            for(let dbB of dbBounds)
            {
                let b = new Boundary();
                populate(dbB, b);
                bounds.push(b);
            }
            obj.Boundaries = bounds;
        }
    }

    if("Position" in obj && obj.Position != null)
    {
        if(obj.Position === "null")
            obj.Position = null;
        else
        {
            let dbP = JSON.parse(obj.Position);
            obj.Position = new Position();
            populate(dbP, obj.Position);
        }
    }

    if("Positions" in obj && obj.Positions != null)
    {
        if(obj.Positions === "null")
            obj.Positions = null;
        else
        {
            let dbPos = JSON.parse(obj.Positions);
            let pos = [];
            for(let dbP of dbPos)
            {
                let p = new Position();
                populate(dbP, p);
                pos.push(p);
            }
            obj.Positions = pos;
        }
    }

    if("TokenItem" in obj && obj.TokenItem != null)
    {
        if(obj.TokenItem === "null")
            obj.TokenItem = null;
        else
        {
            let dbI = JSON.parse(obj.TokenItem);
            obj.TokenItem = new Item();
            populate(dbI, obj.TokenItem);
        }
    }

    if("Items" in obj && obj.Items != null)
    {
        if(obj.Items === "null")
            obj.Items = null;
        else
        {
            let dbItems = JSON.parse(obj.Items);
            let items = [];
            for(let dbI of dbItems)
            {
                let i = new Item();
                populate(dbI, i);
                items.push(i);
            }
            obj.Items = items;
        }
    }

    if("Polygon" in obj && obj.Polygon != null)
    {
        if(obj.Polygon === "null")
            obj.Polygon = null;
        else
        {
            let dbPolys = JSON.parse(obj.Polygon);
            let polys = [];
            for(let dbPos of dbPolys)
            {
                let pos = new Position();
                populate(dbPos, pos);
                polys.push(pos);
            }
            obj.Polygon = polys;
        }
    }

    if("Connections" in obj && obj.Connections != null)
        obj.Connections = JSON.parse(obj.Connections);

    if("Says" in obj && obj.Says != null)
        obj.Says = JSON.parse(obj.Says);
}

function populate(dbObj, obj)
{
    let names = Object.getOwnPropertyNames(obj);
    for(let name of names)
    {
        if(dbObj[name] !== undefined)
            obj[name] = dbObj[name];
        else if(name === "Id" && dbObj["_id"] !== undefined)
            obj["Id"] = dbObj["_id"];
    }
}

function shutdown()
{
    client.close().then(() => {}).catch((err) => {});
}

module.exports = {
    init,
    shutdown,
    server,
    map,
    quirks,
    doors,
    spawns,
    zones,
    monsters,
    npcs,
    npc,
    allServers,
    mapsByServerId
};