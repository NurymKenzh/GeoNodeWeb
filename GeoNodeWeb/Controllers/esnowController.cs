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
            public DateTime date; // for Min, Max, Avg
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

        private static bool server = Convert.ToBoolean(Startup.Configuration["Server"]);
        private string geoserverConnection = server ? Startup.Configuration["geoserverConnectionServer"].ToString() : Startup.Configuration["geoserverConnectionDebug"].ToString(),
            postgresConnection = server ? Startup.Configuration["postgresConnectionServer"].ToString() : Startup.Configuration["postgresConnectionDebug"].ToString();

        public IActionResult Index()
        {
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var datetime = connection.Query<DateTime>($"SELECT datetime FROM public.\"SANMOST_MOD10A2006_MAXIMUM_SNOW_EXTENT\"");
                ViewBag.DateTime = datetime.OrderBy(d => d).ToArray();
            }
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerUrlServer"].ToString() : Startup.Configuration["GeoServerUrlDebug"].ToString();
            return View();
        }

        public IActionResult Charts(int Id)
        {
            ViewBag.Id = Id;
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var datetime = connection.Query<DateTime>($"SELECT datetime FROM public.\"SANMOST_MOD10A2006_MAXIMUM_SNOW_EXTENT\"");
                ViewBag.DateTime = datetime.OrderBy(d => d).ToArray();
            }
            return View();
        }

        [HttpPost]
        public ActionResult GetWMAInfo1(int Id,
            DateTime? DateTimeFrom,
            DateTime? DateTimeTo)
        {
            List<rasterstat> rasterstats = new List<rasterstat>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var rasterstatsC = connection.Query<rasterstat>($"SELECT date, data FROM public.esnow_rasterstats " +
                    $"WHERE \"feature_id\" = {Id.ToString()};");
                rasterstats = rasterstatsC.ToList();
            }
            rasterstats = rasterstats
                .Where(r => (r.date >= DateTimeFrom || DateTimeFrom == null) && (r.date <= DateTimeTo || DateTimeTo == null))
                .ToList();
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

            // Min, Max, Avg
            List<SnowData> min = new List<SnowData>(),
                max = new List<SnowData>(),
                avg = new List<SnowData>();
            for (int i = 0; i < rasterstats.Count(); i++)
            {
                for (int k = 0; k < rasterstats[i].SnowData.Count(); k++)
                {
                    bool exist = false;
                    for (int j = 0; j < min.Count(); j++)
                    {
                        if (rasterstats[i].date.Month == min[i].date.Month && rasterstats[i].date.Day == min[i].date.Day)
                        {
                            if (min[i].DataType == rasterstats[i].SnowData[k].DataType)
                            {
                                exist = true;
                                if (min[i].Area > rasterstats[i].SnowData[k].Area)
                                {
                                    min[i].Area = rasterstats[i].SnowData[k].Area - 10000;
                                    min[i].Percentage = rasterstats[i].SnowData[k].Percentage;
                                    min[i].PixelsCount = rasterstats[i].SnowData[k].PixelsCount;
                                }
                            }
                        }
                    }
                    for (int j = 0; j < max.Count(); j++)
                    {
                        if (rasterstats[i].date.Month == max[i].date.Month && rasterstats[i].date.Day == max[i].date.Day)
                        {
                            if (max[i].DataType == rasterstats[i].SnowData[k].DataType)
                            {
                                exist = true;
                                if (max[i].Area < rasterstats[i].SnowData[k].Area)
                                {
                                    max[i].Area = rasterstats[i].SnowData[k].Area + 10000;
                                    max[i].Percentage = rasterstats[i].SnowData[k].Percentage;
                                    max[i].PixelsCount = rasterstats[i].SnowData[k].PixelsCount;
                                }
                            }
                        }
                    }
                    if (!exist)
                    {
                        min.Add(new SnowData()
                        {
                            DataType = rasterstats[i].SnowData[k].DataType,
                            date = rasterstats[i].date,
                            Area = rasterstats[i].SnowData[k].Area - 10000,
                            Percentage = rasterstats[i].SnowData[k].Percentage,
                            PixelsCount = rasterstats[i].SnowData[k].PixelsCount
                        });
                        max.Add(new SnowData()
                        {
                            DataType = rasterstats[i].SnowData[k].DataType,
                            date = rasterstats[i].date,
                            Area = rasterstats[i].SnowData[k].Area + 10000,
                            Percentage = rasterstats[i].SnowData[k].Percentage,
                            PixelsCount = rasterstats[i].SnowData[k].PixelsCount
                        });
                    }
                }
            }
            for (int i = 0; i < rasterstats.Count(); i++)
            {
                for (int k = 0; k < rasterstats[i].SnowData.Count(); k++)
                {
                    decimal area = 0,
                        percentage = 0;
                    int pixelsCount = 0,
                        count = 0;
                    for (int j = 0; j < rasterstats.Count(); j++)
                    {
                        if(rasterstats[j].date.Month == rasterstats[i].date.Month && rasterstats[j].date.Day == rasterstats[i].date.Day)
                        {
                            for (int l = 0; l < rasterstats[j].SnowData.Count(); l++)
                            {
                                if(rasterstats[i].SnowData[k].DataType == rasterstats[j].SnowData[l].DataType)
                                {
                                    area += rasterstats[j].SnowData[l].Area;
                                    pixelsCount += rasterstats[j].SnowData[l].PixelsCount;
                                    percentage += rasterstats[j].SnowData[l].Percentage;
                                    count++;
                                }
                            }
                        }
                    }
                    avg.Add(new SnowData()
                    {
                        DataType = rasterstats[i].SnowData[k].DataType,
                        date = rasterstats[i].date,
                        Area = area / count + 3000,
                        Percentage = percentage / count,
                        PixelsCount = pixelsCount / count
                    });
                }
            }

            string wmaname = "";
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = 1 " +
                    $"LIMIT 1;");
                wmaname = name.FirstOrDefault();
            }
            return Json(new
            {
                wmaname,
                rasterstats,
                min,
                max,
                avg
            });
        }

        [HttpPost]
        public ActionResult GetWMAInfo2(int Id,
            int StartMonth,
            int MonthsCount,
            int[] Years)
        {
            List<rasterstat> rasterstats = new List<rasterstat>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var rasterstatsC = connection.Query<rasterstat>($"SELECT date, data FROM public.esnow_rasterstats " +
                    $"WHERE \"feature_id\" = {Id.ToString()};");
                rasterstats = rasterstatsC.OrderBy(r => r.date).ToList();
            }

            int[] months = new int[MonthsCount];
            months[0] = StartMonth;
            for (int i = 1; i < MonthsCount; i++)
            {
                months[i] = StartMonth + i;
                if(months[i]>12)
                {
                    months[i] -= 12;
                }
            }
            List<rasterstat> rasterstatsm = new List<rasterstat>();
            for (int i = 0; i < MonthsCount; i++)
            {
                foreach (rasterstat rasterstat in rasterstats.Where(r => r.date.Month == months[i]).OrderBy(r => r.date))
                {
                    rasterstatsm.Add(rasterstat);
                }
            }

            foreach (rasterstat rasterstat in rasterstatsm)
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

            rasterstatsm = rasterstatsm.OrderBy(r => r.date).ToList();

            // Min, Max, Avg, AvgYears
            List<SnowData> min = new List<SnowData>(),
                max = new List<SnowData>(),
                avg = new List<SnowData>(),
                avgyears = new List<SnowData>();
            for (int i = 0; i < rasterstatsm.Count(); i++)
            {
                for (int k = 0; k < rasterstatsm[i].SnowData.Count(); k++)
                {
                    bool exist = false;
                    for (int j = 0; j < min.Count(); j++)
                    {
                        if (rasterstatsm[i].date.Month == min[i].date.Month && rasterstatsm[i].date.Day == min[i].date.Day)
                        {
                            if (min[i].DataType == rasterstatsm[i].SnowData[k].DataType)
                            {
                                exist = true;
                                if (min[i].Area > rasterstatsm[i].SnowData[k].Area)
                                {
                                    min[i].Area = rasterstatsm[i].SnowData[k].Area - 10000;
                                    min[i].Percentage = rasterstatsm[i].SnowData[k].Percentage;
                                    min[i].PixelsCount = rasterstatsm[i].SnowData[k].PixelsCount;
                                }
                            }
                        }
                    }
                    for (int j = 0; j < max.Count(); j++)
                    {
                        if (rasterstatsm[i].date.Month == max[i].date.Month && rasterstatsm[i].date.Day == max[i].date.Day)
                        {
                            if (max[i].DataType == rasterstatsm[i].SnowData[k].DataType)
                            {
                                exist = true;
                                if (max[i].Area < rasterstatsm[i].SnowData[k].Area)
                                {
                                    max[i].Area = rasterstatsm[i].SnowData[k].Area + 10000;
                                    max[i].Percentage = rasterstatsm[i].SnowData[k].Percentage;
                                    max[i].PixelsCount = rasterstatsm[i].SnowData[k].PixelsCount;
                                }
                            }
                        }
                    }
                    if (!exist)
                    {
                        min.Add(new SnowData()
                        {
                            DataType = rasterstatsm[i].SnowData[k].DataType,
                            date = rasterstatsm[i].date,
                            Area = rasterstatsm[i].SnowData[k].Area - 10000,
                            Percentage = rasterstatsm[i].SnowData[k].Percentage,
                            PixelsCount = rasterstatsm[i].SnowData[k].PixelsCount
                        });
                        max.Add(new SnowData()
                        {
                            DataType = rasterstatsm[i].SnowData[k].DataType,
                            date = rasterstatsm[i].date,
                            Area = rasterstatsm[i].SnowData[k].Area + 10000,
                            Percentage = rasterstatsm[i].SnowData[k].Percentage,
                            PixelsCount = rasterstatsm[i].SnowData[k].PixelsCount
                        });
                    }
                }
            }
            for (int i = 0; i < rasterstatsm.Count(); i++)
            {
                for (int k = 0; k < rasterstatsm[i].SnowData.Count(); k++)
                {
                    decimal area = 0,
                        percentage = 0;
                    int pixelsCount = 0,
                        count = 0;
                    for (int j = 0; j < rasterstatsm.Count(); j++)
                    {
                        if (rasterstatsm[j].date.Month == rasterstatsm[i].date.Month && rasterstatsm[j].date.Day == rasterstatsm[i].date.Day)
                        {
                            for (int l = 0; l < rasterstatsm[j].SnowData.Count(); l++)
                            {
                                if (rasterstatsm[i].SnowData[k].DataType == rasterstatsm[j].SnowData[l].DataType)
                                {
                                    area += rasterstatsm[j].SnowData[l].Area;
                                    pixelsCount += rasterstatsm[j].SnowData[l].PixelsCount;
                                    percentage += rasterstatsm[j].SnowData[l].Percentage;
                                    count++;
                                }
                            }
                        }
                    }
                    avg.Add(new SnowData()
                    {
                        DataType = rasterstatsm[i].SnowData[k].DataType,
                        date = rasterstatsm[i].date,
                        Area = area / count + 3000,
                        Percentage = percentage / count,
                        PixelsCount = pixelsCount / count
                    });
                    if(Years.Contains(rasterstatsm[i].date.Year))
                    {
                        avgyears.Add(new SnowData()
                        {
                            DataType = rasterstatsm[i].SnowData[k].DataType,
                            date = rasterstatsm[i].date,
                            Area = area / count,
                            Percentage = percentage / count,
                            PixelsCount = pixelsCount / count
                        });
                    }
                }
            }

            string wmaname = "";
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = 1 " +
                    $"LIMIT 1;");
                wmaname = name.FirstOrDefault();
            }
            return Json(new
            {
                wmaname,
                rasterstats,
                min,
                max,
                avg,
                avgyears
            });
        }
    }
}