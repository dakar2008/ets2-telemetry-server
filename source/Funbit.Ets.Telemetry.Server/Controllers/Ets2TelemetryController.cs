using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Funbit.Ets.Telemetry.Server.Data;
using Funbit.Ets.Telemetry.Server.Helpers;
using Newtonsoft.Json;
using Funbit.Ets.Telemetry.Server.Models;
using SCSSdkClient;
using System.Collections.Generic;
using System.Linq;
using Z.EntityFramework.Plus;

namespace Funbit.Ets.Telemetry.Server.Controllers
{
    [RoutePrefix("api")]
    public class Ets2TelemetryController : ApiController
    {
        public const string TelemetryApiUriPath = "/api/ets2/telemetry";
        public const string TelemetryEventApiUriPath = "/api/ets2/telemetryevents";
        const string TestTelemetryJsonFileName = "Ets2TestTelemetry.json";

        static readonly bool UseTestTelemetryData = Convert.ToBoolean(
            ConfigurationManager.AppSettings["UseEts2TestTelemetryData"]);

        public static string GetEts2TelemetryJson()
        {
            return JsonConvert.SerializeObject(MainForm.data, JsonHelper.RestSettings);
        }

        public static string GetEts2Events(bool clearevents = false)
        {
            DBContext db = new DBContext();

            if (clearevents)
            {
                db.FerryEventModels.Delete();
                db.FineEventModels.Delete();
                db.TollgateEventModels.Delete();
                db.TrainEventModels.Delete();
                JobStatus js = db.JobStatuses.FirstOrDefault();
                js.JobStarted = false;
                js.JobDelivered = false;
                db.SaveChanges();
                string data = "No Data";
                return JsonConvert.SerializeObject(data, JsonHelper.RestSettings);
            }
            else
            {
                var data = new { FerryEvents = db.FerryEventModels.ToList(), FineEvents = db.FineEventModels.ToList(), TollgateEvents = db.TollgateEventModels.ToList(), TrainEvents = db.TrainEventModels.ToList(), JobStatus = db.JobStatuses.FirstOrDefault() };
                return JsonConvert.SerializeObject(data, JsonHelper.RestSettings);
            }
        }

        [HttpGet]
        [HttpPost]
        [Route("ets2/telemetry", Name = "Get")]
        public HttpResponseMessage Get()
        {
            var telemetryJson = GetEts2TelemetryJson();
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(telemetryJson, Encoding.UTF8, "application/json");
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }

        [HttpGet]
        [HttpPost]
        [Route("ets2/telemetryevents", Name = "GetEvents")]
        public HttpResponseMessage GetFerry(bool clearevents = false)
        {
            var telemetryJson = GetEts2Events(clearevents);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(telemetryJson, Encoding.UTF8, "application/json");
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            return response;
        }
    }
}