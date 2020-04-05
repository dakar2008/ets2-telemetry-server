using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funbit.Ets.Telemetry.Server.Models
{
    public class JobStatus : BaseModel
    {
        public bool JobStarted { get; set; }
        public bool JobDelivered { get; set; }
    }
}