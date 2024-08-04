class Boundary
{
    X1 = 0;
    Y1 = 0;
    X2 = 0;
    Y2 = 0;

    get [Symbol.toStringTag]() {
        return `(${this.X1}, ${this.Y1}, ${this.Width}, ${this.Height})`;
    }

    get Width()
    {
        return this.X2 - this.X1;
    }

    get Height()
    {
        return this.Y2 - this.Y1;
    }

}

module.exports = {Boundary};