using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funbit.Ets.Telemetry.Server.Models
{
    public abstract class BaseModel
    {
        public string Id { get; set; }
        public DateTime DateCreated { get; set; }
        public BaseModel()
        {
            Id = Guid.NewGuid().ToString("N");
            DateCreated = DateTime.Now;
        }
    }
}