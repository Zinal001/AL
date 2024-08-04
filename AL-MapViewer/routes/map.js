const db = require("../db");


module.exports = async (req, res) => {

    let serverId = req.params.serverId ?? "Default";
    let mapName = req.params.mapName ?? "main";
    let mapX = req.params.mapX ?? 0;
    let mapY = req.params.mapY ?? 0;

    let setShowDefault = (name, def) => {
        if(req.query[name] == undefined)
            return def;

        return req.query[name] == 1;
    };

    let show = {
        bounds: setShowDefault("b", true),
        spawns: setShowDefault("s", true),
        quirks: setShowDefault("q", false),
        doors: setShowDefault("d", false),
        npcs: setShowDefault("n", true),
        zones: setShowDefault("z", false),
        monsters: req.query.m ?? []
    };

    let server = await db.server(serverId);

    let allServers = await db.allServers();
    let allMaps = [];

    /** @type Map|null */
    let map = null;
    let quirks = [];
    let doors = [];
    let npcs = [];
    let spawns = [];
    let zones = [];
    let monsters = [];
    let uniqueMonsters = [];

    if(server != null)
    {
        map = await db.map(mapName, server.Id);
        allMaps = (await db.mapsByServerId(server.Id)).sort((a, b) => a.DisplayName.localeCompare(b.DisplayName));
    }

    if(map != null)
    {
        quirks = await db.quirks(map.Id);
        doors = await db.doors(map.Id);
        npcs = await db.npcs(map.Id);
        spawns = await db.spawns(map.Id);
        zones = await db.zones(map.Id);
        monsters = await db.monsters(map.Id);

        for(let monster of monsters)
        {
            if(!uniqueMonsters[monster.Name])
            {
                uniqueMonsters[monster.Name] = {
                    Name: monster.Name,
                    Types: [monster.Type],
                };
            }
            else if(!uniqueMonsters[monster.Name].Types.includes(monster.Type))
                uniqueMonsters[monster.Name].Types.push(monster.Type);
        }


        let qm = [];
        for(let k in uniqueMonsters)
        {
            if(uniqueMonsters[k].Types.length > 1)
            {
                for(let t of uniqueMonsters[k].Types)
                {
                    qm.push({
                        Name: `${uniqueMonsters[k].Name} (${t})`,
                        Type: t
                    });
                }
            }
            else
            {
                qm.push({
                    Name: uniqueMonsters[k].Name,
                    Type: uniqueMonsters[k].Types[0]
                });
            }
        }
        uniqueMonsters = qm;
        uniqueMonsters.sort((a, b) => a.Name.localeCompare(b.Name) );
    }

    let baseUrl = `${req.protocol}://${req.get("host")}/`;

    if(process.env.BASE_URL)
        baseUrl = process.env.BASE_URL;

    res.render("mapPage", {
        baseUrl: baseUrl,
        server: server,
        map: map,
        mapX: mapX,
        mapY: mapY,
        quirks: quirks,
        doors: doors,
        spawns: spawns,
        zones: zones,
        monsters: monsters,
        uniqueMonsters: uniqueMonsters,
        npcs: npcs,
        show: show,
        allServers: allServers,
        allMaps: allMaps
    });



};