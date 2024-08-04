const db = require("../db");


module.exports = async (req, res) => {

    let id = req.params.id ?? null;
    let mapName = req.params.mapName ?? "main";
    let serverId = req.params.serverId ?? "Default";

    if(id == null || id === "")
    {
        res.send("No NPC id specified");
        return;
    }

    let server = await db.server(serverId);

    let map;
    let npc = null;

    map = await db.map(mapName, server.Id);

    if(map != null)
    {
        npc = await db.npc(id, map.Id);
    }

    res.render("npcPage", {
        baseUrl: `${req.protocol}://${req.get("host")}/`,
        server: server,
        map: map,
        npc: npc
    });
};