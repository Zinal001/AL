const { Position } = require("./Position");

class Zone
{
    Id = 0;
    Name = "";
    Type = "";
    Drop = "";
    /** @type Position[] */
    Polygon = [];


    toSvgPoints()
    {
        if(this.Polygon == null)
            return "";

        let pos = [];
        for(let p of this.Polygon)
            pos.push(`${p.X.toString().replace(",", ".")},${p.Y.toString().replace(",", ".")}`);

        return pos.join(" ");
    }

}

module.exports = {Zone};