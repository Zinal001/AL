const { Boundary } = require("./Boundary");

class MonsterZone
{
    Id = 0;
    Name = "";
    /** @type string|null */
    SType = "";
    Type = "";
    Count = 0;
    /** @type Boundary[] */
    Boundaries = [];
    Roams = 0;
    Grow = 0;
    SkinFilename = "";
    SkinWidth = 0;
    SkinHeight = 0;
    MapId = 0;
}
module.exports = {MonsterZone};