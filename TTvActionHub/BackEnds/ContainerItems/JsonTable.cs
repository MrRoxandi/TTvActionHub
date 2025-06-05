using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.BackEnds.ContainerItems
{
    public class JsonTable
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(1500)]
        public string JsonData { get; set; } = string.Empty;

    }
}
