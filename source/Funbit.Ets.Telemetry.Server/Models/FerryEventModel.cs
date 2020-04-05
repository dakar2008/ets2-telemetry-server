using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funbit.Ets.Telemetry.Server.Models
{
    public class FerryEventModel : BaseModel
    {
        public long PayAmount { get; set; }
        public string SourceId { get; set; }
        public string SourceName { get; set; }
        public string TargetId { get; set; }
        public string TargetName { get; set; }
    }
}