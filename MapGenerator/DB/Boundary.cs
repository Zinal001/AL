using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Boundary
    {
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        [NotMapped]
        public int Width
        {
            get => X2 - X1;
        }

        [NotMapped]
        public int Height
        {
            get => Y2 - Y1;
        }
    }
}
