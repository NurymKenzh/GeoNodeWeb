using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.IO;

namespace GeoNodeWeb.Controllers
{
    public class datasetcalculationlayer
    {
        public int layer_id;
        public string layer_alias;
    }
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
            public int feature_id;
            public decimal Area;
            public int calculation_layer_id;
            public rasterstat()
            {
                SnowData = new List<SnowData>();
            }
        }

        private class dataset
        {
            public int calculation_layer_id;
            public int feature_id;
            public int DataType; // 0, 1, 11, 25, 37 ...
            public DateTime Date;
            public decimal area;
            public decimal percentage;
            public decimal area_full;

            public decimal area_avg;
            public decimal area_min;
            public decimal area_max;

            public string MonthDay
            {
                get
                {
                    return Date.ToString("MM-dd");
                }
            }

            public int dayofyear
            {
                get
                {
                    return Date.DayOfYear;
                }
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
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var datasetcalculationlayer = connection.Query<datasetcalculationlayer>($"SELECT layer_id, layer_alias FROM public.\"esnow_datasetcalculationlayer\"");
                ViewBag.CalculationLayers = datasetcalculationlayer.OrderBy(l => l.layer_id).ToArray();
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
            List<dataset> datasets = new List<dataset>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var datasetsC = connection.Query<dataset>($"SELECT feature_id, \"DataType\", \"Date\", area, percentage, area_full, area_avg, area_min, area_max, calculation_layer_id " +
                    $"FROM public.esnow_datasets " +
                    $"WHERE calculation_layer_id = 1 " +
                    $"AND feature_id = {Id.ToString()};");
                datasets = datasetsC.OrderBy(d => d.Date).ToList();
                if (DateTimeFrom != null)
                {
                    datasets = datasets.Where(d => d.Date >= DateTimeFrom).ToList();
                }
                if (DateTimeTo != null)
                {
                    datasets = datasets.Where(d => d.Date <= DateTimeTo).ToList();
                }
                var DataTypeValues = Enum.GetValues(typeof(DataType));
                for (int i = 0; i < DataTypeValues.Length; i++)
                {
                    if (datasets.Count(d => d.DataType == Convert.ToInt32(DataTypeValues.GetValue(i)) && d.area > 0) == 0)
                    {
                        datasets = datasets.Where(d => d.DataType != Convert.ToInt32(DataTypeValues.GetValue(i))).ToList();
                    }
                }
            }

            string wmainfo = "";
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                var area = connection.Query<string>($"SELECT \"AreaInSkm\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                wmainfo = $"{name.FirstOrDefault()}, {code.FirstOrDefault()} ({Convert.ToDecimal(area.FirstOrDefault()).ToString("0.00")}, м²)";
            }
            return Json(new
            {
                wmainfo,
                datasets
            });
        }

        [HttpPost]
        public ActionResult GetWMAInfo2(int Id,
            int DataType,
            int StartMonth,
            int MonthsCount,
            int[] Years)
        {
            const int startYear = 2000;
            List<dataset> datasets = new List<dataset>(),
                datasetsamm = new List<dataset>(),
                datasetsyears = new List<dataset>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var datasetsC = connection.Query<dataset>($"SELECT feature_id, \"DataType\", \"Date\", area, percentage, area_full, area_avg, area_min, area_max, calculation_layer_id " +
                    $"FROM public.esnow_datasets " +
                    $"WHERE calculation_layer_id = 1 " +
                    $"AND feature_id = {Id.ToString()} " +
                    $"AND \"DataType\" = {DataType.ToString()};");
                datasets = datasetsC.OrderBy(d => d.Date).ToList();

                // average, min, max
                for (int month = StartMonth, monthsCount = 0; monthsCount < 12; monthsCount++, month++)
                {
                    int monthR = month,
                        year = startYear;
                    if (monthR > 12)
                    {
                        monthR -= 12;
                        year = startYear + 1;
                    }
                    for (int day = 1; day < 32; day++)
                    {
                        dataset dataset = datasets.FirstOrDefault(d => d.Date.Month == monthR && d.Date.Day == day);
                        if (dataset != null)
                        {
                            //dataset.Date = new DateTime(year, dataset.Date.Month, dataset.Date.Day);
                            dataset datasetamm = new dataset()
                            {
                                calculation_layer_id = dataset.calculation_layer_id,
                                feature_id = dataset.feature_id,
                                DataType = dataset.DataType,
                                Date = new DateTime(year, dataset.Date.Month, dataset.Date.Day),
                                area = dataset.area,
                                percentage = dataset.percentage,
                                area_full = dataset.area_full,
                                area_avg = dataset.area_avg,
                                area_min = dataset.area_min,
                                area_max = dataset.area_max
                            };
                            datasetsamm.Add(datasetamm);
                        }
                    }
                }

                // years average
                foreach (dataset dataset in datasetsamm)
                {
                    List<dataset> datasets_avg = datasets
                        .Where(d => d.MonthDay == dataset.MonthDay)
                        .Where(d => Years.Contains(d.Date.Year))
                        .ToList();
                    if (datasets_avg.Count() > 0)
                    {
                        decimal area_avg = datasets_avg.Average(d => d.area);
                        dataset datasetyear = new dataset()
                        {
                            calculation_layer_id = dataset.calculation_layer_id,
                            feature_id = dataset.feature_id,
                            DataType = dataset.DataType,
                            Date = dataset.Date,
                            area = dataset.area,
                            percentage = dataset.percentage,
                            area_full = dataset.area_full,
                            area_avg = area_avg,
                            area_min = dataset.area_min,
                            area_max = dataset.area_max
                        };
                        datasetsyears.Add(datasetyear);
                    }
                }
            }

            string wmainfo = "";
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                var area = connection.Query<string>($"SELECT \"AreaInSkm\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE fid = {Id.ToString()} " +
                    $"LIMIT 1;");
                wmainfo = $"{name.FirstOrDefault()}, {code.FirstOrDefault()} ({Convert.ToDecimal(area.FirstOrDefault()).ToString("0.00")}, м²)";
            }
            return Json(new
            {
                wmainfo,
                datasetsamm,
                datasetsyears
            });
        }

        [HttpPost]
        public ActionResult GetWMAInfo1Old(int Id,
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
                rasterstats
            });
        }

        [HttpPost]
        public ActionResult GetWMAInfo2Old(int Id,
            int StartMonth,
            int MonthsCount,
            int[] Years)
        {
            //List<stat> statsd = TranslateStat();
            //List<stat> stats = new List<stat>();
            //foreach(stat stat in statsd)
            //{
            //    if(stats.Count(s => s.feature_id == stat.feature_id && s.DataType == stat.DataType && 
            //        s.Date.Month == stat.Date.Month && s.Date.Day == stat.Date.Day) == 0)
            //    {
            //        List<stat> statsSame = statsd
            //            .Where(s => s.feature_id == stat.feature_id && s.DataType == stat.DataType &&
            //                s.Date.Month == stat.Date.Month && s.Date.Day == stat.Date.Day)
            //            .ToList();
            //        stats.Add(new stat()
            //        {
            //            feature_id = stat.feature_id,
            //            DataType = stat.DataType,
            //            Date = new DateTime(1, stat.Date.Month, stat.Date.Day),
            //            area = statsSame.Where(s => Years.Contains(s.Date.Year)).Average(s => s.area),
            //            percentage = statsSame.Where(s => Years.Contains(s.Date.Year)).Average(s => s.percentage),
            //            area_full = stat.area_full,
            //            area_avg = stat.area_avg,
            //            area_min = stat.area_min,
            //            area_max = stat.area_max
            //        });
            //    }
            //}
            //stats = stats.Where(s => s.feature_id == Id).ToList();

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

        private List<dataset> TranslateStat()
        {
            List<dataset> stats = new List<dataset>();
            List<rasterstat> rasterstats = new List<rasterstat>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var rasterstatsC = connection.Query<rasterstat>($"SELECT feature_id, date, data FROM public.esnow_rasterstats");
                rasterstats = rasterstatsC.ToList();
            }
            rasterstats = rasterstats.ToList();
            // формирование rasterstats (SnowData)
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
            foreach (rasterstat rasterstat in rasterstats)
            {
                foreach(SnowData snowData in rasterstat.SnowData)
                {
                    stats.Add(new dataset()
                    {
                        feature_id = rasterstat.feature_id,
                        DataType = (int) snowData.DataType,
                        Date = rasterstat.date,
                        area = snowData.Area,
                        percentage = snowData.Percentage,
                        area_full = rasterstat.Area
                    });
                }
            }
            for (int i = 0; i < stats.Count(); i++)
            {
                List<dataset> statsSame = stats
                    .Where(s => s.feature_id == stats[i].feature_id && s.DataType == stats[i].DataType &&
                        s.Date.Month == stats[i].Date.Month && s.Date.Day == stats[i].Date.Day)
                    .ToList();
                stats[i].area_avg = statsSame.Average(s => s.area);
                stats[i].area_min = statsSame.Min(s => s.area);
                stats[i].area_max = statsSame.Max(s => s.area);
                if(stats[i].area_min != stats[i].area_max)
                {

                }
            }
            return stats;
        }

        public IActionResult Calc()
        {
            //// erase table esnow_datasets
            //using (var connection = new NpgsqlConnection(postgresConnection))
            //{
            //    connection.Open();
            //    connection.Execute($"DELETE FROM public.esnow_datasets;");
            //}
            // get data from esnow_rasterstats
            List<rasterstat> rasterstats = new List<rasterstat>();
            using (var connection = new NpgsqlConnection(postgresConnection))
            {
                connection.Open();
                var rasterstatsC = connection.Query<rasterstat>($"SELECT feature_id, date, data, calculation_layer_id FROM public.esnow_rasterstats");
                rasterstats = rasterstatsC.ToList();
            }
            rasterstats = rasterstats.ToList();
            // calculate rasterstats: Area, Percentage
            foreach(rasterstat rasterstat in rasterstats)
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
            // rasterstats to datasets
            List<dataset> datasets = new List<dataset>();
            foreach(rasterstat rasterstat in rasterstats)
            {
                foreach(SnowData snowData in rasterstat.SnowData)
                {
                    datasets.Add(new dataset()
                    {
                        calculation_layer_id = rasterstat.calculation_layer_id,
                        feature_id = rasterstat.feature_id,
                        DataType = (int) snowData.DataType,
                        Date = rasterstat.date,
                        area = snowData.Area,
                        percentage = snowData.Percentage,
                        area_full = rasterstat.Area,
                        area_avg = 0,
                        area_min = 0,
                        area_max = 0
                    });
                }
            }
            // fill the blanks (DataType, not dates)
            List<dataset> datasets0 = new List<dataset>();
            var DataTypeValues = Enum.GetValues(typeof(DataType));
            foreach (DateTime dateTime in datasets.Select(d => d.Date).Distinct())
            {
                List<dataset> datasets1 = datasets.Where(d => d.Date == dateTime).ToList();
                foreach (int calculation_layer_id in datasets1.Select(d => d.calculation_layer_id).Distinct())
                {
                    List<dataset> datasets2 = datasets1.Where(d => d.calculation_layer_id == calculation_layer_id).ToList();
                    foreach (int feature_id in datasets2.Select(d => d.feature_id).Distinct())
                    {
                        List<dataset> datasets3 = datasets2.Where(d => d.feature_id == feature_id).ToList();
                        dataset dataset = datasets3.FirstOrDefault();
                        for (int i = 0; i < DataTypeValues.Length; i++)
                        {
                            if (datasets3.Count(d => d.DataType == Convert.ToInt32(DataTypeValues.GetValue(i))) == 0)
                            {
                                datasets0.Add(new dataset()
                                {
                                    calculation_layer_id = calculation_layer_id,
                                    feature_id = feature_id,
                                    DataType = Convert.ToInt32(DataTypeValues.GetValue(i)),
                                    Date = dateTime,
                                    area = 0,
                                    percentage = 0,
                                    area_full = (decimal)dataset?.area_full,
                                    area_avg = 0,
                                    area_min = 0,
                                    area_max = 0
                                });
                            }
                        }
                    }
                }
            }
            datasets.AddRange(datasets0);
            // calculate atasets: area_avg, area_min, area_max
            List<dataset> datasetsAM = new List<dataset>();
            foreach (int calculation_layer_id in datasets.Select(d => d.calculation_layer_id).Distinct())
            {
                List<dataset> datasets1 = datasets.Where(d => d.calculation_layer_id == calculation_layer_id).ToList();
                foreach (int feature_id in datasets1.Select(d => d.feature_id).Distinct())
                {
                    List<dataset> datasets2 = datasets1.Where(d => d.feature_id == feature_id).ToList();
                    for (int i = 0; i < DataTypeValues.Length; i++)
                    {
                        List<dataset> datasets3 = datasets2.Where(d => d.DataType == Convert.ToInt32(DataTypeValues.GetValue(i))).ToList();
                        //foreach (string monthday in datasets3.Select(d => d.MonthDay).Distinct())
                        foreach (string MonthDay in datasets3.Select(d => d.MonthDay).Distinct())
                        {
                            List<dataset> datasets4 = datasets3.Where(d => d.MonthDay == MonthDay).ToList();
                            decimal area_avg = datasets4.Average(d => d.area),
                                area_min = datasets4.Min(d => d.area),
                                area_max = datasets4.Max(d => d.area);
                            foreach(dataset dataset in datasets4)
                            {
                                dataset datasetAM = new dataset()
                                {
                                    calculation_layer_id = dataset.calculation_layer_id,
                                    feature_id = dataset.feature_id,
                                    DataType = dataset.DataType,
                                    Date = dataset.Date,
                                    area = dataset.area,
                                    percentage = dataset.percentage,
                                    area_full = dataset.area_full,
                                    area_avg = area_avg,
                                    area_min = area_min,
                                    area_max = area_max
                                };
                                datasetsAM.Add(datasetAM);
                            }
                        }
                    }
                }
            }
            datasets.Clear();
            datasets.AddRange(datasetsAM);
            // insert datasets into table
            //using (var connection = new NpgsqlConnection(postgresConnection))
            //{
            //    connection.Open();
            //    for (int i = 0; i < datasets.Count(); i++)
            //    {
            //        string execute = $"INSERT INTO public.esnow_datasets" +
            //            $"(feature_id, \"DataType\", \"Date\", area, percentage, area_full, area_avg, area_min, area_max, calculation_layer_id)" +
            //            $"VALUES (" +
            //            $"{datasets[i].feature_id.ToString()}," +
            //            $"{Convert.ToInt32(datasets[i].DataType).ToString()}," +
            //            $"'{datasets[i].Date.Year.ToString()}-{datasets[i].Date.Month.ToString("00")}-{datasets[i].Date.Day.ToString("00")}'," +
            //            $"{datasets[i].area.ToString()}," +
            //            $"{datasets[i].percentage.ToString()}," +
            //            $"{datasets[i].area_full.ToString()}," +
            //            $"{datasets[i].area_avg.ToString()}," +
            //            $"{datasets[i].area_min.ToString()}," +
            //            $"{datasets[i].area_max.ToString()}," +
            //            $"{datasets[i].calculation_layer_id.ToString()});";
            //        connection.Execute(execute);
            //    }
            //    connection.Close();
            //}
            using (TextWriter tw = new StreamWriter(@"E:\Documents\Google Drive\Share\Backups\esnow_datasets.csv"))
            {
                tw.WriteLine("feature_id\tDataType\tDate\tarea\tpercentage\tarea_full\tarea_avg\tarea_min\tarea_max\tcalculation_layer_id");
                for (int i = 0; i < datasets.Count(); i++)
                {
                    string line = $"{datasets[i].feature_id.ToString()}\t" +
                        $"{datasets[i].DataType.ToString()}\t" +
                        $"'{datasets[i].Date.Year.ToString()}-{datasets[i].Date.Month.ToString("00")}-{datasets[i].Date.Day.ToString("00")}'\t" +
                        $"{datasets[i].area.ToString()}\t" +
                        $"{datasets[i].percentage.ToString()}\t" +
                        $"{datasets[i].area_full.ToString()}\t" +
                        $"{datasets[i].area_avg.ToString()}\t" +
                        $"{datasets[i].area_min.ToString()}\t" +
                        $"{datasets[i].area_max.ToString()}\t" +
                        $"{datasets[i].calculation_layer_id.ToString()}";
                    tw.WriteLine(line);
                }
            }
            return View("Index");
        }
    }
}