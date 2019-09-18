using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace GeoNodeWeb.Controllers
{
    public class esnowController : Controller
    {
        enum DataType
        {
            MissingData = 0,
            NoDecision = 1,
            Night = 11,
            NoSnow = 25,
            Lake = 37,
            Sea = 39,
            Cloud = 50,
            LakeIce = 100,
            Snow = 200,
            DetectorSaturated = 254,
            Fill = 255
        }

        private class SnowData
        {
            public DataType DataType;
            public int PixelsCount;
            public decimal Area;
            public decimal Percentage;
        }

        private class rasterstat
        {
            public DateTime date;
            public string data;
            public List<SnowData> SnowData;
            public int PixelsCount;
            public decimal Area;
            public rasterstat()
            {
                SnowData = new List<SnowData>();
            }
        }

        public IActionResult Index()
        {
            using (var connection = new NpgsqlConnection("Host=db-geodata.test.geoportal.ingeo.kz;Database=geoserver;Username=postgres;Password=;Port=15433"))
            {
                connection.Open();
                var datetime = connection.Query<DateTime>($"SELECT datetime FROM public.\"Maximum_Snow_Extent_MOD10A2006\"");
                ViewBag.DateTime = datetime.OrderBy(d => d).ToArray();
            }
            return View();
        }

        public IActionResult Charts(int Id)
        {
            ViewBag.Id = Id;
            return View();
        }

        [HttpPost]
        public ActionResult GetWMAInfo(int Id)
        {
            List<rasterstat> rasterstats = new List<rasterstat>();
            using (var connection = new NpgsqlConnection("Host=db-geodata.test.geoportal.ingeo.kz;Database=postgres;Username=postgres;Password=;Port=15433"))
            {
                connection.Open();
                var rasterstatsC = connection.Query<rasterstat>($"SELECT date, data FROM public.esnow_rasterstats " +
                    $"WHERE \"feature_id\" = {Id.ToString()};");
                rasterstats = rasterstatsC.ToList();
            }
            foreach (rasterstat rasterstat in rasterstats)
            {
                var data = JObject.Parse(rasterstat.data);
                foreach (JProperty property in data.Properties())
                {
                    if (int.TryParse(property.Name, out int n))
                    {
                        rasterstat.SnowData.Add(new SnowData()
                        {
                            DataType = (DataType)Convert.ToInt32(property.Name),
                            PixelsCount = Convert.ToInt32(property.Value)
                        });
                    }
                    if (property.Name == "count")
                    {
                        rasterstat.PixelsCount = Convert.ToInt32(property.Value);
                    }

                    if (property.Name == "AreaInSkm")
                    {
                        rasterstat.Area = Convert.ToDecimal(property.Value);
                    }
                }
                for (int i = 0; i < rasterstat.SnowData.Count(); i++)
                {
                    rasterstat.SnowData[i].Area = rasterstat.Area * rasterstat.SnowData[i].PixelsCount / rasterstat.PixelsCount;
                    rasterstat.SnowData[i].Percentage = (decimal)rasterstat.SnowData[i].PixelsCount / rasterstat.PixelsCount * 100;
                }
            }

            rasterstats = rasterstats.OrderBy(r => r.date).ToList();

            string wmaname = "";
            using (var connection = new NpgsqlConnection("Host=db-geodata.test.geoportal.ingeo.kz;Database=geoserver;Username=postgres;Password=;Port=15433"))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon_1 " +
                    $"WHERE fid = 1 " +
                    $"LIMIT 1;");
                wmaname = name.FirstOrDefault();
            }
            return Json(new
            {
                wmaname,
                rasterstats
            });
        }
    }
}