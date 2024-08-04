
class Position
{
    X = 0;
    Y = 0;

    get [Symbol.toStringTag]() {
        return `(${this.X}, ${this.Y})`;
    }
}

module.exports = {Position};