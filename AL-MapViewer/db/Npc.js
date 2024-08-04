const { Position } = require("./Position");
const { Boundary } = require("./Boundary");
const { Item } = require("./Item");

class Npc
{
    Id = 0;
    Name = "";
    NpcId = "";
    /** @type Position[] */
    Positions = [];
    /** @type Boundary|null */
    Boundary = null;
    /** @type string[]|null */
    Says = [];
    QuestName = "";
    Width = 0;
    Height = 0;
    Moves = 0;
    SkinFilename = "";
    /** @type Item|null */
    TokenItem = null;
    /** @type Item[] */
    Items = [];
    MapId = 0;

}

module.exports = {Npc};