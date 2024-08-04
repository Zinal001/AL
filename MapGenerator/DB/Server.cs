using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace MapGenerator.DB
{
    public class Server
    {
        [Key]
        public ObjectId Id { get; set; }

        [Required]
        public String ServerKey { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 3)]
        public String Name { get; set; }

        [Required]
        [StringLength(255)]
        public String Url { get; set; }

        public DateTime? LastGenerated { get; set; } = null;
    }
}
