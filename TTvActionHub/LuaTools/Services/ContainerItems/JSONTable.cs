using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTvActionHub.LuaTools.Services.ContainerItems
{
    public class JSONTable
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JSONData { get; set; } = string.Empty;

    }
}
