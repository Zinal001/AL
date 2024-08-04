class Boundary
{
    X1 = 0;
    Y1 = 0;
    X2 = 0;
    Y2 = 0;
    Width = 0;
    Height = 0;

    get [Symbol.toStringTag]() {
        return `(${this.X1}, ${this.Y1}, ${this.Width}, ${this.Height})`;
    }

}

module.exports = {Boundary};