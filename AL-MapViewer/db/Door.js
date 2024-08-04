const { Map } = require("./Map");

class Door
{
    Id = 0;
    X = 0;
    Y = 0;
    Width = 0;
    Height = 0;
    ToMapName = "";
    FromSpawnId = 0;
    ToSpawnId = 0;
    ToX = 0;
    ToY = 0;
    /** @type ObjectId|null */
    ToMapId = null;
    MapId = 0;
    /** @type Map|null */
    ToMap = null;
}

module.exports = {Door};