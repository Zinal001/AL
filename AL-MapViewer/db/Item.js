
class Item
{
    Id = 0;
    ItemId = "";
    Name = "";
    Cost = 0;
    CurrencyType = "";
    X = 0;
    Y = 0;
    Width = 0;
    Height = 0;
    Size = 0;
    ImageSetName = "";


    get [Symbol.toStringTag]() {
        return this.ItemId;
    }
}

module.exports = {Item};