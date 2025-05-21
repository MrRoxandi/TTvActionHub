using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.LuaTools.Services.ContainerItems
{
    public class JsonTable
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JsonData { get; set; } = string.Empty;

    }
}
