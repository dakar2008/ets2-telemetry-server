using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funbit.Ets.Telemetry.Server.Models
{
    public class FineEventModel : BaseModel
    {
        public long Amount { get; set; }
        public string Offence { get; set; }
    }
}