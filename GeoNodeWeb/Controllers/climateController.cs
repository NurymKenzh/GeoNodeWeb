using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace GeoNodeWeb.Controllers
{
    public class GSLayer
    {
        public int resourcebase_ptr_id;
        public string title_en;
        public string supplemental_information_en;
    }
    public class esnow_datasetcalculationlayer
    {
        public int id;
        public int layer_id;
    }

    public class layers_layer
    {
        public int resourcebase_ptr_id;
        public string name;
        public string supplemental_information_en;
    }

    public class climate_rasterstat
    {
        public DateTime date;
        public string data;
    }

    public class climate_mosaicinfo
    {
        public string name;
        public string title_en;
        public string title_ru;
        public string title_kk;
        public string description_en;
        public string description_ru;
        public string description_kk;
        public string extended_title_en;
        public string extended_title_kk;
        public string extended_title_ru;
    }

    public class climate_x
    {
        public string name;
        public DateTime dt;
        public string point;
        public decimal? value;
        public int year;
        public decimal lon;
        public decimal lat;
    }

    public class wm
    {
        public int? objectid;
        public int? codewmb;
        public string namewmb_ru;
        public string namewmb_en;
        public string namewmb_kk;
        public string codewma;
    }

    public class raster_table
    {
        public string column;
        public int column_index;
        public string row;
        public decimal? value;
        public string id;
    }

    public class raster_table_b
    {
        public string parameter;
        public decimal? year;
        public decimal? s1;
        public decimal? s2;
        public decimal? s3;
        public decimal? s4;
        public decimal? m1;
        public decimal? m2;
        public decimal? m3;
        public decimal? m4;
        public decimal? m5;
        public decimal? m6;
        public decimal? m7;
        public decimal? m8;
        public decimal? m9;
        public decimal? m10;
        public decimal? m11;
        public decimal? m12;
    }

    public class raster_table_for_chart
    {
        public string period;
        public decimal? year;
        public decimal? max;
        public decimal? min;
        public decimal? median;
        public string month;
        public string season;
    }

    public class climateController : Controller
    {
        private readonly HttpApiClientController _HttpApiClient;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IStringLocalizer<SharedResources> _sharedLocalizer;

        private static bool server = Convert.ToBoolean(Startup.Configuration["Server"]);
        private string geoserverConnection = server ? Startup.Configuration["geoserverConnectionServer"].ToString() : Startup.Configuration["geoserverConnectionDebug"].ToString(),
            postgresConnection = server ? Startup.Configuration["postgresConnectionServer"].ToString() : Startup.Configuration["postgresConnectionDebug"].ToString(),
            geoportalConnection = server ? Startup.Configuration["geoportalConnectionServer"].ToString() : Startup.Configuration["geoportalConnectionDebug"].ToString(),

            geodataProdConnection = server ? Startup.Configuration["geodataProdConnectionServer"].ToString() : Startup.Configuration["geodataProdConnectionDebug"].ToString(),
            geoportalProdConnection = server ? Startup.Configuration["geoportalProdConnectionServer"].ToString() : Startup.Configuration["geoportalProdConnectionDebug"].ToString(),

            geodataanalyticsProdConnection = server ? Startup.Configuration["geodataanalyticsProdConnectionServer"].ToString() : Startup.Configuration["geodataanalyticsProdConnectionDebug"].ToString();

        //[HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        public climateController(HttpApiClientController HttpApiClient,
            IHostingEnvironment hostingEnvironment,
            IStringLocalizer<SharedResources> sharedLocalizer)
        {
            _HttpApiClient = HttpApiClient;
            _hostingEnvironment = hostingEnvironment;
            _sharedLocalizer = sharedLocalizer;
        }

        public IActionResult Index()
        {
            //using (var connection = new NpgsqlConnection(geoserverConnection))
            //{
            //    connection.Open();

            //    var datetimepr_pd_avg_m_rcp45_10 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_10_2011_2090\"");
            //    ViewBag.DateTimepr_pd_avg_m_rcp45_10 = datetimepr_pd_avg_m_rcp45_10.OrderBy(d => d).ToArray();

            //    //var datetimepr_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimepr_pd_avg_m_rcp45_20_2011_2090 = datetimepr_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimepr_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_30_2011_2070\"");
            //    //ViewBag.DateTimepr_pd_avg_m_rcp45_30_2011_2070 = datetimepr_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_10_2011_2090 = datetimetas_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_20_2011_2090 = datetimetas_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_30_2011_2070\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_30_2011_2070 = datetimetas_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_10_2011_2090 = datetimetasmax_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_20_2011_2090 = datetimetasmax_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_30_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_30_2011_2090 = datetimetasmax_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_10_2011_2090 = datetimetasmin_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_20_2011_2090 = datetimetasmin_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_30_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_30_2011_2090 = datetimetasmin_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();
            //}
            //using (var connection = new NpgsqlConnection(geoportalConnection))
            //{
            //    connection.Open();
            //    var GSLayers = connection.Query<GSLayer>($"SELECT resourcebase_ptr_id, title_en, supplemental_information_en FROM public.layers_layer");
            //    using (var connection2 = new NpgsqlConnection(postgresConnection))
            //    {
            //        connection2.Open();
            //        var esnow_datasetcalculationlayers = connection2.Query<esnow_datasetcalculationlayer>($"SELECT id, layer_id FROM public.esnow_datasetcalculationlayer;");
            //        ViewBag.GSLayers = GSLayers
            //            .Where(g => esnow_datasetcalculationlayers.Select(l => l.layer_id).Contains(g.resourcebase_ptr_id))
            //            .OrderBy(l => l.resourcebase_ptr_id)
            //            .ToArray();
            //    }
            //}
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerProdUrlServer"].ToString() : Startup.Configuration["GeoServerProdUrlDebug"].ToString();
            return View();
        }

        public IActionResult Index20200505()
        {
            //using (var connection = new NpgsqlConnection(geoserverConnection))
            //{
            //    connection.Open();

            //    var datetimepr_pd_avg_m_rcp45_10 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_10_2011_2090\"");
            //    ViewBag.DateTimepr_pd_avg_m_rcp45_10 = datetimepr_pd_avg_m_rcp45_10.OrderBy(d => d).ToArray();

            //    //var datetimepr_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimepr_pd_avg_m_rcp45_20_2011_2090 = datetimepr_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimepr_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_30_2011_2070\"");
            //    //ViewBag.DateTimepr_pd_avg_m_rcp45_30_2011_2070 = datetimepr_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_10_2011_2090 = datetimetas_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_20_2011_2090 = datetimetas_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetas_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_30_2011_2070\"");
            //    //ViewBag.DateTimetas_pd_avg_m_rcp45_30_2011_2070 = datetimetas_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_10_2011_2090 = datetimetasmax_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_20_2011_2090 = datetimetasmax_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmax_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_30_2011_2090\"");
            //    //ViewBag.DateTimetasmax_pd_avg_m_rcp45_30_2011_2090 = datetimetasmax_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_10_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_10_2011_2090 = datetimetasmin_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_20_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_20_2011_2090 = datetimetasmin_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

            //    //var datetimetasmin_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_30_2011_2090\"");
            //    //ViewBag.DateTimetasmin_pd_avg_m_rcp45_30_2011_2090 = datetimetasmin_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();
            //}
            //using (var connection = new NpgsqlConnection(geoportalConnection))
            //{
            //    connection.Open();
            //    var GSLayers = connection.Query<GSLayer>($"SELECT resourcebase_ptr_id, title_en, supplemental_information_en FROM public.layers_layer");
            //    using (var connection2 = new NpgsqlConnection(postgresConnection))
            //    {
            //        connection2.Open();
            //        var esnow_datasetcalculationlayers = connection2.Query<esnow_datasetcalculationlayer>($"SELECT id, layer_id FROM public.esnow_datasetcalculationlayer;");
            //        ViewBag.GSLayers = GSLayers
            //            .Where(g => esnow_datasetcalculationlayers.Select(l => l.layer_id).Contains(g.resourcebase_ptr_id))
            //            .OrderBy(l => l.resourcebase_ptr_id)
            //            .ToArray();
            //    }
            //}
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerProdUrlServer"].ToString() : Startup.Configuration["GeoServerProdUrlDebug"].ToString();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetLayerDates(
            string workspace,
            string layer)
        {
            List<string> datesL = new List<string>();

            List<DateTime> datesD = new List<DateTime>();

            // with DB
            try
            {
                using (var connection = new NpgsqlConnection(geodataProdConnection))
                {
                    connection.Open();
                    var datetime = connection.Query<DateTime>($"SELECT ingestion FROM public.{layer}");
                    datesD = datetime.OrderBy(d => d).ToList();
                    connection.Close();
                }
            }
            catch { }

            //// withot DB
            //string[] dates = datesL.OrderBy(d => d).ToArray();
            // with DB
            string[] dates = datesD.Select(d => d.ToString("yyyyMMdd")).ToArray();

            if(datesD.Count() == 0)
            {
                if (layer.Contains("10"))
                {
                    datesL.Add("19510101");
                    datesL.Add("19610101");
                    datesL.Add("19710101");
                    datesL.Add("19810101");
                    datesL.Add("19910101");
                    datesL.Add("20110101");
                    datesL.Add("20210101");
                    datesL.Add("20310101");
                    datesL.Add("20410101");
                    datesL.Add("20510101");
                    datesL.Add("20610101");
                    datesL.Add("20710101");
                    datesL.Add("20810101");
                    datesL.Add("20910101");
                }
                if (layer.Contains("20"))
                {
                    datesL.Add("19510101");
                    datesL.Add("19610101");
                    datesL.Add("19710101");
                    datesL.Add("19810101");
                    datesL.Add("20110101");
                    datesL.Add("20210101");
                    datesL.Add("20310101");
                    datesL.Add("20410101");
                    datesL.Add("20510101");
                    datesL.Add("20610101");
                    datesL.Add("20710101");
                    datesL.Add("20810101");
                }
                if (layer.Contains("30"))
                {
                    datesL.Add("19510101");
                    datesL.Add("19610101");
                    datesL.Add("19710101");
                    datesL.Add("20110101");
                    datesL.Add("20210101");
                    datesL.Add("20310101");
                    datesL.Add("20410101");
                    datesL.Add("20510101");
                    datesL.Add("20610101");
                    datesL.Add("20710101");
                }
                dates = datesL.OrderBy(d => d).ToArray();
            }
            return Json(new
            {
                dates
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetLayerInfo(
            string workspace,
            string layer)
        {
            //// GeoServer
            //string GeoServerUrl = server ? Startup.Configuration["GeoServerProdUrlServer"].ToString() : Startup.Configuration["GeoServerProdUrlDebug"].ToString();
            //string title = "",
            //    abstr = "";
            //HttpResponseMessage response = await _HttpApiClient.GetAsync($"rest/workspaces/{workspace}/coveragestores/{layer}/coverages/{layer}.xml");
            //string xml = "";
            //if (response.IsSuccessStatusCode)
            //{
            //    xml = await response.Content.ReadAsStringAsync();
            //}
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(xml);
            //XmlNodeList ti = xmlDoc.GetElementsByTagName("title");
            //XmlNodeList ab = xmlDoc.GetElementsByTagName("abstract");
            //title = ti[0].InnerText;
            //abstr = ab[0].InnerText;

            // DB
            string title = "",
                description = "",
                extended_title = "";
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT name, title_en, title_ru, title_kk, description_en, description_ru, description_kk, extended_title_en, extended_title_kk, extended_title_ru" +
                    $" FROM public.climate_mosaicinfo" +
                    $" WHERE name LIKE '{layer}';";
                var climate_mosaicinfos = connection.Query<climate_mosaicinfo>(query);
                climate_mosaicinfo climate_Mosaicinfo = climate_mosaicinfos.FirstOrDefault();
                string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                if (language == "ru")
                {
                    title = climate_Mosaicinfo?.title_ru;
                    description = climate_Mosaicinfo?.description_ru;
                    extended_title = climate_Mosaicinfo?.extended_title_ru;
                }
                else if (language == "en")
                {
                    title = climate_Mosaicinfo?.title_en;
                    description = climate_Mosaicinfo?.description_en;
                    extended_title = climate_Mosaicinfo?.extended_title_en;
                }
                else
                {
                    title = climate_Mosaicinfo?.title_kk;
                    description = climate_Mosaicinfo?.description_kk;
                    extended_title = climate_Mosaicinfo?.extended_title_kk;
                }
                connection.Close();
            }

            return Json(new
            {
                title,
                description,
                extended_title
            });
        }

        public IActionResult Charts(int Id)
        {
            ViewBag.Id = Id;
            //List<layers_layer> layers_layers = new List<layers_layer>();
            //List<esnow_datasetcalculationlayer> esnow_datasetcalculationlayers = new List<esnow_datasetcalculationlayer>();
            //using (var connection = new NpgsqlConnection(geoportalConnection))
            //{
            //    connection.Open();
            //    var layers_layers_ = connection.Query<layers_layer>($"SELECT resourcebase_ptr_id, name, supplemental_information_en FROM public.layers_layer;");
            //    layers_layers = layers_layers_.ToList();
            //    ViewBag.ShapeName = layers_layers.FirstOrDefault(l => l.name == LayerName)?.supplemental_information_en;
            //}
            //using (var connection = new NpgsqlConnection(postgresConnection))
            //{
            //    connection.Open();
            //    var esnow_datasetcalculationlayers_ = connection.Query<esnow_datasetcalculationlayer>($"SELECT id, layer_id FROM public.esnow_datasetcalculationlayer;");
            //    esnow_datasetcalculationlayers = esnow_datasetcalculationlayers_.ToList();
            //}
            //layers_layer layers_layer = layers_layers.FirstOrDefault(l => l.name == LayerName);
            //esnow_datasetcalculationlayer esnow_datasetcalculationlayer = esnow_datasetcalculationlayers
            //    .FirstOrDefault(l => l.layer_id == layers_layer.resourcebase_ptr_id);

            //int layer_id = esnow_datasetcalculationlayer.id;
            //using (var connection = new NpgsqlConnection(geoserverConnection))
            //{
            //    connection.Open();
            //    var datetime = connection.Query<DateTime>($"SELECT datetime FROM public.\"SANMOST_MOD10A2006_MAXIMUM_SNOW_EXTENT\"");
            //    ViewBag.DateTime = datetime.OrderBy(d => d).ToArray();
            //    ViewBag.Mean = $"{datetime.Min().Year.ToString()} - {datetime.Max().Year.ToString()}";
            //}
            //ViewBag.LayerId = layer_id;
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerProdUrlServer"].ToString() : Startup.Configuration["GeoServerProdUrlDebug"].ToString();


            List<wm> wms = new List<wm>();
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var wmsDB = connection.Query<wm>($"SELECT \"OBJECTID\" as objectid, \"NameWMB_Ru\" as namewmb_ru, \"NameWMB_En\" as namewmb_en, \"NameWMB_Kz\" as namewmb_kk, \"CodeWMA\" as codewma, \"CodeWMB\" as codewmb FROM public.wma_polygon;");
                string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                if (language == "ru")
                {
                    wms = wmsDB.ToList().OrderBy(w => w.namewmb_ru).ThenBy(w => w.codewma).ToList();
                }
                else if (language == "en")
                {
                    wms = wmsDB.ToList().OrderBy(w => w.namewmb_en).ThenBy(w => w.codewma).ToList();
                }
                else
                {
                    wms = wmsDB.ToList().OrderBy(w => w.namewmb_kk).ThenBy(w => w.codewma).ToList();
                }
                connection.Close();
            }
            ViewBag.wms = wms;

            return View();
        }

        [HttpPost]
        public ActionResult ChartRasters(
            string rname,
            string seasonmonth,
            string pointxs,
            string pointys,
            int objectid)
        {
            decimal pointx = 0,
                pointy = 0;
            try
            {
                pointx = Convert.ToDecimal(pointxs);
                pointy = Convert.ToDecimal(pointys);
            }
            catch
            {
                pointx = Convert.ToDecimal(pointxs.Replace('.', ','));
                pointy = Convert.ToDecimal(pointys.Replace('.', ','));
            }
            string rnameDB = rname;
            if (!string.IsNullOrEmpty(seasonmonth))
            {
                rnameDB += "_" + seasonmonth.ToString();
            }
            List<climate_x> climate_xs = new List<climate_x>();
            using (var connection = new NpgsqlConnection(geodataanalyticsProdConnection))
            {
                string table = "";
                switch (rname.Split('_')[0] + "_" + rname.Split('_')[1])
                {
                    case "tasmax_pd":
                        table = "climate_tasmax";
                        break;
                    case "tasmax_dlt":
                        table = "climate_tasmax_dlt";
                        break;
                    case "tas_pd":
                        table = "climate_tas";
                        break;
                    case "tas_dlt":
                        table = "climate_tas_dlt";
                        break;
                    case "tasmin_pd":
                        table = "climate_tasmin";
                        break;
                    case "tasmin_dlt":
                        table = "climate_tasmin_dlt";
                        break;
                    case "pr_pd":
                        table = "climate_pr";
                        break;
                    case "pr_dlt":
                        table = "climate_pr_dlt";
                        break;
                    case "et_pd":
                        table = "climate_et";
                        break;
                    case "et_dlt":
                        table = "climate_et_dlt";
                        break;
                    case "gtk_pd":
                        table = "climate_gtk";
                        //rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_h_" + rname.Split('_')[4];
                        if (!string.IsNullOrEmpty(seasonmonth))
                        {
                            rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_" + seasonmonth + "_h_" + rname.Split('_')[4];
                        }
                        break;
                    case "spi_pd":
                        table = "climate_spi";
                        //rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_h_" + rname.Split('_')[4];
                        if (!string.IsNullOrEmpty(seasonmonth))
                        {
                            rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_" + seasonmonth + "_h_" + rname.Split('_')[4];
                        }
                        else
                        {
                            rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5]  + "_h_" + rname.Split('_')[4];
                        }
                        break;
                    case "spei_pd":
                        table = "climate_spei";
                        //rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_h_" + rname.Split('_')[4];
                        if (!string.IsNullOrEmpty(seasonmonth))
                        {
                            rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5] + "_" + seasonmonth + "_h_" + rname.Split('_')[4];
                        }
                        else
                        {
                            rnameDB = rname.Split('_')[0] + "_" + rname.Split('_')[3] + "_" + rname.Split('_')[2] + "_" + rname.Split('_')[5]  + "_h_" + rname.Split('_')[4];
                        }
                        break;
                }
                connection.Open();
                string query = $"SELECT name, dt, value" +
                    $" FROM public.{table}" +
                    $" WHERE name = '{rnameDB}'" +
                    $" AND point =" +
                    $" (SELECT point" +
                    $" FROM public.climate_coords" +
                    $" WHERE ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString().Replace(',','.')} {pointy.ToString().Replace(',', '.')})')) =" +
                    $" (SELECT MIN(ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString().Replace(',', '.')} {pointy.ToString().Replace(',', '.')})')))" +
                    $" FROM public.climate_coords) LIMIT 1);";
                var climate_xsQ = connection.Query<climate_x>(query, commandTimeout: 600);
                climate_xs = climate_xsQ.Where(c => c.name == rnameDB).OrderBy(c => c.dt).ToList();
                connection.Close();
            }
            for (int i = 0; i < climate_xs.Count(); i++)
            {
                climate_xs[i].year = climate_xs[i].dt.Year;
            }
            if (climate_xs.Count() > 0)
            {
                for (int y = climate_xs.Min(c => c.year); y <= climate_xs.Max(c => c.year); y += 10)
                {
                    if (climate_xs.Count(c => c.year == y) == 0)
                    {
                        climate_xs.Add(new climate_x()
                        {
                            value = null,
                            year = y
                        });
                    }
                }
            }
            climate_xs = climate_xs.OrderBy(c => c.year).ToList();

            string wmbname = "",
                wmacode = "";
            if(objectid>=0)
            {
                using (var connection = new NpgsqlConnection(geodataProdConnection))
                {
                    connection.Open();
                    var nameRu = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                        $"FROM public.wma_polygon " +
                        $"WHERE \"OBJECTID\" = {objectid.ToString()} " +
                        $"LIMIT 1;");
                    var nameEn = connection.Query<string>($"SELECT \"NameWMB_En\" " +
                        $"FROM public.wma_polygon " +
                        $"WHERE \"OBJECTID\" = {objectid.ToString()} " +
                        $"LIMIT 1;");
                    var nameKk = connection.Query<string>($"SELECT \"NameWMB_Kz\" " +
                        $"FROM public.wma_polygon " +
                        $"WHERE \"OBJECTID\" = {objectid.ToString()} " +
                        $"LIMIT 1;");
                    var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                        $"FROM public.wma_polygon " +
                        $"WHERE \"OBJECTID\" = {objectid.ToString()} " +
                        $"LIMIT 1;");
                    string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                    if (language == "ru")
                    {
                        wmbname = nameRu.FirstOrDefault();
                    }
                    else if (language == "en")
                    {
                        wmbname = nameEn.FirstOrDefault();
                    }
                    else
                    {
                        wmbname = nameKk.FirstOrDefault();
                    }
                    wmacode = code.FirstOrDefault();
                    connection.Close();
                }
            }

            return Json(new
            {
                climate_xs,
                wmbname,
                wmacode
            });
        }

        [HttpPost]
        public ActionResult TableRasters(
            string pointxs,
            string pointys,
            int year,
            string rcp,
            string decade)
        {
            if(year.ToString()[3] == '0')
            {
                year += 1;
            }
            string messsage = "TableRasters OK";
            List<raster_table> raster_table = new List<raster_table>();
            List<raster_table_b> raster_table_bs = new List<raster_table_b>();
            try
            {
                decimal pointx = 0,
                pointy = 0;
                try
                {
                    pointx = Convert.ToDecimal(pointxs);
                    pointy = Convert.ToDecimal(pointys);
                }
                catch
                {
                    pointx = Convert.ToDecimal(pointxs.Replace('.', ','));
                    pointy = Convert.ToDecimal(pointys.Replace('.', ','));
                }

                //if(period == "y")
                //{
                //    season = null;
                //    month = null;
                //}
                //if (period == "s")
                //{
                //    month = null;
                //}
                List<string> tables = new List<string>();
                tables.Add("climate_tasmax");
                tables.Add("climate_tas");
                tables.Add("climate_tasmin");
                tables.Add("climate_tasmax_dlt");
                tables.Add("climate_tas_dlt");
                tables.Add("climate_tasmin_dlt");
                tables.Add("climate_pr");
                tables.Add("climate_pr_dlt");
                tables.Add("climate_et");
                tables.Add("climate_et_dlt");
                tables.Add("climate_gtk");
                tables.Add("climate_spi");
                tables.Add("climate_spei");
                List<string> parameters = new List<string>();
                try
                {
                    parameters.Add("avg_m_rcp45_10_01");
                    parameters.Add("avg_m_rcp45_10_02");
                    parameters.Add("avg_m_rcp45_10_03");
                    parameters.Add("avg_m_rcp45_10_04");
                    parameters.Add("avg_m_rcp45_10_05");
                    parameters.Add("avg_m_rcp45_10_06");
                    parameters.Add("avg_m_rcp45_10_07");
                    parameters.Add("avg_m_rcp45_10_08");
                    parameters.Add("avg_m_rcp45_10_09");
                    parameters.Add("avg_m_rcp45_10_10");
                    parameters.Add("avg_m_rcp45_10_11");
                    parameters.Add("avg_m_rcp45_10_12");
                    parameters.Add("avg_m_rcp45_20_01");
                    parameters.Add("avg_m_rcp45_20_02");
                    parameters.Add("avg_m_rcp45_20_03");
                    parameters.Add("avg_m_rcp45_20_04");
                    parameters.Add("avg_m_rcp45_20_05");
                    parameters.Add("avg_m_rcp45_20_06");
                    parameters.Add("avg_m_rcp45_20_07");
                    parameters.Add("avg_m_rcp45_20_08");
                    parameters.Add("avg_m_rcp45_20_09");
                    parameters.Add("avg_m_rcp45_20_10");
                    parameters.Add("avg_m_rcp45_20_11");
                    parameters.Add("avg_m_rcp45_20_12");
                    parameters.Add("avg_m_rcp45_30_01");
                    parameters.Add("avg_m_rcp45_30_02");
                    parameters.Add("avg_m_rcp45_30_03");
                    parameters.Add("avg_m_rcp45_30_04");
                    parameters.Add("avg_m_rcp45_30_05");
                    parameters.Add("avg_m_rcp45_30_06");
                    parameters.Add("avg_m_rcp45_30_07");
                    parameters.Add("avg_m_rcp45_30_08");
                    parameters.Add("avg_m_rcp45_30_09");
                    parameters.Add("avg_m_rcp45_30_10");
                    parameters.Add("avg_m_rcp45_30_11");
                    parameters.Add("avg_m_rcp45_30_12");
                    parameters.Add("avg_m_rcp85_10_01");
                    parameters.Add("avg_m_rcp85_10_02");
                    parameters.Add("avg_m_rcp85_10_03");
                    parameters.Add("avg_m_rcp85_10_04");
                    parameters.Add("avg_m_rcp85_10_05");
                    parameters.Add("avg_m_rcp85_10_06");
                    parameters.Add("avg_m_rcp85_10_07");
                    parameters.Add("avg_m_rcp85_10_08");
                    parameters.Add("avg_m_rcp85_10_09");
                    parameters.Add("avg_m_rcp85_10_10");
                    parameters.Add("avg_m_rcp85_10_11");
                    parameters.Add("avg_m_rcp85_10_12");
                    parameters.Add("avg_m_rcp85_20_01");
                    parameters.Add("avg_m_rcp85_20_02");
                    parameters.Add("avg_m_rcp85_20_03");
                    parameters.Add("avg_m_rcp85_20_04");
                    parameters.Add("avg_m_rcp85_20_05");
                    parameters.Add("avg_m_rcp85_20_06");
                    parameters.Add("avg_m_rcp85_20_07");
                    parameters.Add("avg_m_rcp85_20_08");
                    parameters.Add("avg_m_rcp85_20_09");
                    parameters.Add("avg_m_rcp85_20_10");
                    parameters.Add("avg_m_rcp85_20_11");
                    parameters.Add("avg_m_rcp85_20_12");
                    parameters.Add("avg_m_rcp85_30_01");
                    parameters.Add("avg_m_rcp85_30_02");
                    parameters.Add("avg_m_rcp85_30_03");
                    parameters.Add("avg_m_rcp85_30_04");
                    parameters.Add("avg_m_rcp85_30_05");
                    parameters.Add("avg_m_rcp85_30_06");
                    parameters.Add("avg_m_rcp85_30_07");
                    parameters.Add("avg_m_rcp85_30_08");
                    parameters.Add("avg_m_rcp85_30_09");
                    parameters.Add("avg_m_rcp85_30_10");
                    parameters.Add("avg_m_rcp85_30_11");
                    parameters.Add("avg_m_rcp85_30_12");
                    parameters.Add("avg_s_rcp45_10_1");
                    parameters.Add("avg_s_rcp45_10_2");
                    parameters.Add("avg_s_rcp45_10_3");
                    parameters.Add("avg_s_rcp45_10_4");
                    parameters.Add("avg_s_rcp45_20_1");
                    parameters.Add("avg_s_rcp45_20_2");
                    parameters.Add("avg_s_rcp45_20_3");
                    parameters.Add("avg_s_rcp45_20_4");
                    parameters.Add("avg_s_rcp45_30_1");
                    parameters.Add("avg_s_rcp45_30_2");
                    parameters.Add("avg_s_rcp45_30_3");
                    parameters.Add("avg_s_rcp45_30_4");
                    parameters.Add("avg_s_rcp85_10_1");
                    parameters.Add("avg_s_rcp85_10_2");
                    parameters.Add("avg_s_rcp85_10_3");
                    parameters.Add("avg_s_rcp85_10_4");
                    parameters.Add("avg_s_rcp85_20_1");
                    parameters.Add("avg_s_rcp85_20_2");
                    parameters.Add("avg_s_rcp85_20_3");
                    parameters.Add("avg_s_rcp85_20_4");
                    parameters.Add("avg_s_rcp85_30_1");
                    parameters.Add("avg_s_rcp85_30_2");
                    parameters.Add("avg_s_rcp85_30_3");
                    parameters.Add("avg_s_rcp85_30_4");
                    parameters.Add("avg_y_rcp45_10");
                    parameters.Add("avg_y_rcp45_20");
                    parameters.Add("avg_y_rcp45_30");
                    parameters.Add("avg_y_rcp85_10");
                    parameters.Add("avg_y_rcp85_20");
                    parameters.Add("avg_y_rcp85_30");
                }
                catch { }
                parameters = parameters
                    .Where(p => p.Contains(rcp))
                    .Where(p => p.Split('_')[3] == decade)
                    .ToList();

                using (var connection = new NpgsqlConnection(geodataanalyticsProdConnection))
                {
                    connection.Open();
                    string query_point = $"SELECT ST_AsEWKT(point) as point" +
                        $" FROM public.climate_coords" +
                        $" WHERE ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString().Replace(',', '.')} {pointy.ToString().Replace(',', '.')})')) =" +
                        $" (SELECT MIN(ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString().Replace(',', '.')} {pointy.ToString().Replace(',', '.')})')))" +
                        $" FROM public.climate_coords) LIMIT 1;";
                    var points = connection.Query<string>(query_point, commandTimeout: 600);
                    string point = points.FirstOrDefault();
                    foreach (string table in tables)
                    {
                        if(table == "climate_gtk")
                        {

                        }
                        //string query = $"SELECT dt, name, value" +
                        //    $" FROM public.{table}" +
                        //    $" WHERE point =" +
                        //    $" (SELECT point" +
                        //    $" FROM public.climate_coords" +
                        //    $" WHERE ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString()} {pointy.ToString()})')) =" +
                        //    $" (SELECT MIN(ST_Distance(point, ST_GeomFromEWKT('SRID=4326;POINT({pointx.ToString()} {pointy.ToString()})')))" +
                        //    $" FROM public.climate_coords) LIMIT 1);";
                        string query = $"SELECT dt, name, value" +
                            $" FROM public.{table}" +
                            $" WHERE point = ST_GeomFromEWKT('{point}');";
                        var climate_xsQ = connection.Query<climate_x>(query, commandTimeout: 600);
                        foreach (string parameter in parameters)
                        {
                            string parameter_ = "";
                            if (table.Contains("dlt"))
                            {
                                parameter_ = $"{table.Replace("climate_", "")}_{parameter}";
                                if (table == "climate_pr_dlt")
                                {
                                    string[] ps = parameter.Split('_');
                                    parameter_ = $"{table.Replace("climate_", "")}_{ps[0]}_{ps[1]}_{ps[2]}_{ps[3]}_mm";
                                    if (ps.Length == 5)
                                    {
                                        parameter_ += $"_{ps[4]}";
                                    }
                                }
                            }
                            else if (table == "climate_gtk" || table == "climate_spi" || table == "climate_spei")
                            {
                                string[] ps = parameter.Split('_');
                                parameter_ = $"{table.Replace("climate_", "")}_{ps[1]}_{ps[0]}_{ps[3]}_h_{ps[2]}";
                                if (ps.Length == 5)
                                {
                                    parameter_ = $"{table.Replace("climate_", "")}_{ps[1]}_{ps[0]}_{ps[3]}_{ps[4]}_h_{ps[2]}";
                                }
                            }
                            else
                            {
                                parameter_ = $"{table.Replace("climate_", "")}_pd_{parameter}";
                            }

                            decimal? value = climate_xsQ.FirstOrDefault(c => c.dt.Year == year && c.name == parameter_)?.value;
                            raster_table.Add(new raster_table()
                            {
                                column = GetColumn(parameter),
                                column_index = GetColumnIndex(parameter),
                                row = GetRow(table, parameter),
                                value = value == null ? null : (decimal?)Math.Round((decimal)value, 2),
                                id = $"{table}_{parameter}"
                            });
                        }
                    }
                    connection.Close();
                }
                raster_table = raster_table.OrderBy(r => r.row).ThenBy(r => r.column_index).ToList();

                string[] rows = raster_table.Select(r => r.row).Distinct().ToArray();
                int[] columns = raster_table.Select(r => r.column_index).Distinct().ToArray();
                for (int i = 0; i < rows.Count(); i++)
                {
                    raster_table_b raster_table_b_new = new raster_table_b()
                    {
                        parameter = rows[i]
                    };
                    for (int j = 0; j < columns.Count(); j++)
                    {
                        switch (columns[j])
                        {
                            case 1:
                                raster_table_b_new.year = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 2:
                                raster_table_b_new.s1 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 3:
                                raster_table_b_new.s2 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 4:
                                raster_table_b_new.s3 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 5:
                                raster_table_b_new.s4 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 6:
                                raster_table_b_new.m1 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 7:
                                raster_table_b_new.m2 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 8:
                                raster_table_b_new.m3 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 9:
                                raster_table_b_new.m4 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 10:
                                raster_table_b_new.m5 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 11:
                                raster_table_b_new.m6 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 12:
                                raster_table_b_new.m7 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 13:
                                raster_table_b_new.m8 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 14:
                                raster_table_b_new.m9 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 15:
                                raster_table_b_new.m10 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 16:
                                raster_table_b_new.m11 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                            case 17:
                                raster_table_b_new.m12 = raster_table.FirstOrDefault(r => r.row == rows[i] && r.column_index == columns[j])?.value;
                                break;
                        }
                    }
                    raster_table_bs.Add(raster_table_b_new);
                }
            }
            catch(Exception ex)
            {
                messsage = ex.Message + ". " + ex.InnerException?.Message;
            }
            
            return Json(new
            {
                raster_table,
                raster_table_bs,
                messsage
            });
        }

        private string GetColumn(string parameter)
        {
            if(parameter.Contains("_y_"))
            {
                return _sharedLocalizer["Year"];
            }
            else if(parameter.Contains("_s_"))
            {
                switch (parameter.Split('_')[4])
                {
                    case "1":
                        return _sharedLocalizer["Spring"];
                    case "2":
                        return _sharedLocalizer["Summer"];
                    case "3":
                        return _sharedLocalizer["Autumn"];
                    case "4":
                        return _sharedLocalizer["Winter"];
                }
            }
            else if (parameter.Contains("_m_"))
            {
                switch (parameter.Split('_')[4])
                {
                    case "01":
                        return _sharedLocalizer["January"];
                    case "02":
                        return _sharedLocalizer["February"];
                    case "03":
                        return _sharedLocalizer["March"];
                    case "04":
                        return _sharedLocalizer["April"];
                    case "05":
                        return _sharedLocalizer["May"];
                    case "06":
                        return _sharedLocalizer["June"];
                    case "07":
                        return _sharedLocalizer["July"];
                    case "08":
                        return _sharedLocalizer["August"];
                    case "09":
                        return _sharedLocalizer["September"];
                    case "10":
                        return _sharedLocalizer["October"];
                    case "11":
                        return _sharedLocalizer["November"];
                    case "12":
                        return _sharedLocalizer["December"];
                }
            }
            return "";
        }

        private int GetColumnIndex(string parameter)
        {
            if(parameter.Contains("_y_"))
            {
                return 1;
            }
            else if(parameter.Contains("_s_"))
            {
                switch (parameter.Split('_')[4])
                {
                    case "1":
                        return 2;
                    case "2":
                        return 3;
                    case "3":
                        return 4;
                    case "4":
                        return 5;
                }
            }
            else if (parameter.Contains("_m_"))
            {
                switch (parameter.Split('_')[4])
                {
                    case "01":
                        return 6;
                    case "02":
                        return 7;
                    case "03":
                        return 8;
                    case "04":
                        return 9;
                    case "05":
                        return 10;
                    case "06":
                        return 11;
                    case "07":
                        return 12;
                    case "08":
                        return 13;
                    case "09":
                        return 14;
                    case "10":
                        return 15;
                    case "11":
                        return 16;
                    case "12":
                        return 17;
                }
            }
            return 0;
        }

        private string GetRow(string table, string parameter)
        {
            string r = "";
            switch (table)
            {
                case "climate_tasmax":
                    r = _sharedLocalizer["MaxTemperature"];
                    break;
                case "climate_tas":
                    r = _sharedLocalizer["AverageTemperature"];
                    break;
                case "climate_tasmin":
                    r = _sharedLocalizer["MinTemperature"];
                    break;
                case "climate_tasmax_dlt":
                    r = _sharedLocalizer["MaxTemperatureDev"];
                    break;
                case "climate_tas_dlt":
                    r = _sharedLocalizer["AverageTemperatureDev"];
                    break;
                case "climate_tasmin_dlt":
                    r = _sharedLocalizer["MinTemperatureDev"];
                    break;
                case "climate_pr":
                    r = _sharedLocalizer["Precipitation"];
                    break;
                case "climate_pr_dlt":
                    r = _sharedLocalizer["AveragePrecipitationDev"];
                    break;
                case "climate_et":
                    r = _sharedLocalizer["Evapotranspiration"];
                    break;
                case "climate_et_dlt":
                    r = _sharedLocalizer["EvapotranspirationDev"];
                    break;
                case "climate_gtk":
                    r = _sharedLocalizer["AridityIndices"];
                    break;
                case "climate_spi":
                    r = _sharedLocalizer["SPI"];
                    break;
                case "climate_spei":
                    r = _sharedLocalizer["SPEI"];
                    break;
            }
            //switch (parameter.Split('_')[3])
            //{
            //    case "10":
            //        r += ", 10 лет";
            //        break;
            //    case "20":
            //        r += ", 20 лет";
            //        break;
            //    case "30":
            //        r += ", 30 лет";
            //        break;
            //}
            //switch (parameter.Split('_')[2])
            //{
            //    case "rcp45":
            //        r += ", RCP 4.5";
            //        break;
            //    case "rcp85":
            //        r += ", RCP 8.5";
            //        break;
            //}
            //switch (parameter.Split('_')[1])
            //{
            //    case "y":
            //        r += ", год";
            //        break;
            //    case "s":
            //        switch (parameter.Split('_')[1])
            //        {
            //            case "1":
            //                r += ", весна";
            //                break;
            //            case "2":
            //                r += ", лето";
            //                break;
            //            case "3":
            //                r += ", осень";
            //                break;
            //            case "4":
            //                r += ", зима";
            //                break;
            //        }
            //        break;
            //    case "m":
            //        switch (parameter.Split('_')[1])
            //        {
            //            case "01":
            //                r += ", январь";
            //                break;
            //            case "02":
            //                r += ", февраль";
            //                break;
            //            case "03":
            //                r += ", март";
            //                break;
            //            case "04":
            //                r += ", апрель";
            //                break;
            //            case "05":
            //                r += ", май";
            //                break;
            //            case "06":
            //                r += ", июнь";
            //                break;
            //            case "07":
            //                r += ", июль";
            //                break;
            //            case "08":
            //                r += ", август";
            //                break;
            //            case "09":
            //                r += ", сентябрь";
            //                break;
            //            case "10":
            //                r += ", октябрь";
            //                break;
            //            case "11":
            //                r += ", ноябрь";
            //                break;
            //            case "12":
            //                r += ", декабрь";
            //                break;
            //        }
            //        break;
            //}

            return r;
        }

        [HttpPost]
        public ActionResult GetChart1(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_y_{RCP}_{Decade}";

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_y_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB.ToList();
                connection.Close();
            }

            string wmbname = "",
                wmacode = "";
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var nameRu = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameEn = connection.Query<string>($"SELECT \"NameWMB_En\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameKk = connection.Query<string>($"SELECT \"NameWMB_Kz\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                if (language == "ru")
                {
                    wmbname = nameRu.FirstOrDefault();
                }
                else if (language == "en")
                {
                    wmbname = nameEn.FirstOrDefault();
                }
                else
                {
                    wmbname = nameKk.FirstOrDefault();
                }
                wmacode = code.FirstOrDefault();
                connection.Close();
            }

            List<decimal?> max = new List<decimal?>(),
                min = new List<decimal?>(),
                median = new List<decimal?>();
            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = climate_rasterstats.Min(c => c.date.Year),
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            List<int> years = new List<int>();
            List<string> periods = new List<string>();
            for (int year = year_start; year <= year_finish; year += 10)
            {
                years.Add(year);
                periods.Add($"{year.ToString()} - {(year + 9).ToString()}");
                climate_rasterstat climate_rasterstat = climate_rasterstats.FirstOrDefault(c => c.date.Year == year);
                if (climate_rasterstat == null)
                {
                    max.Add(null);
                    min.Add(null);
                    median.Add(null);
                }
                else
                {
                    var data = JObject.Parse(climate_rasterstat.data);
                    foreach (JProperty property in data.Properties())
                    {
                        if (property.Name == "max")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            max.Add(d);
                        }
                        if (property.Name == "min")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            min.Add(d);
                        }
                        if (property.Name == "median")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            median.Add(d);
                        }
                    }
                }
            }

            return Json(new
            {
                years,
                periods,
                max,
                min,
                median,
                wmbname,
                wmacode
            });
        }

        [HttpPost]
        public ActionResult GetChart2(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Seasons,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_s_{RCP}_{Decade}";

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_s_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB
                    .Where(c => Seasons.Contains(c.date.Month))
                    .ToList();
                connection.Close();
            }

            string wmbname = "",
                wmacode = "";
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var nameRu = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameEn = connection.Query<string>($"SELECT \"NameWMB_En\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameKk = connection.Query<string>($"SELECT \"NameWMB_Kz\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                if (language == "ru")
                {
                    wmbname = nameRu.FirstOrDefault();
                }
                else if (language == "en")
                {
                    wmbname = nameEn.FirstOrDefault();
                }
                else
                {
                    wmbname = nameKk.FirstOrDefault();
                }
                wmacode = code.FirstOrDefault();
                connection.Close();
            }

            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = climate_rasterstats.Min(c => c.date.Year),
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            List<int> years = new List<int>();
            List<string> periods = new List<string>();
            int yearsCount = (year_finish - year_start) / 10 + 1;
            decimal?[,] max = new decimal?[4, yearsCount],
                min = new decimal?[4, yearsCount],
                median = new decimal?[4, yearsCount];
            for (int year = year_start, i = 0; year <= year_finish; year += 10, i++)
            {
                years.Add(year);
                periods.Add($"{year.ToString()} - {(year + 9).ToString()}");
                for (int season = 0; season <= 3; season++)
                {
                    climate_rasterstat climate_rasterstat = climate_rasterstats
                        .FirstOrDefault(c => c.date.Year == year && c.date.Month == season + 1);
                    if (climate_rasterstat == null)
                    {
                        max[season, i] = null;
                        min[season, i] = null;
                        median[season, i] = null;
                    }
                    else
                    {
                        var data = JObject.Parse(climate_rasterstat.data);
                        foreach (JProperty property in data.Properties())
                        {
                            if (property.Name == "max")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                max[season, i] = d;
                            }
                            if (property.Name == "min")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                min[season, i] = d;
                            }
                            if (property.Name == "median")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                median[season, i] = d;
                            }
                        }
                    }
                }
            }

            return Json(new
            {
                years,
                periods,
                max,
                min,
                median,
                wmbname,
                wmacode
            });
        }

        [HttpPost]
        public ActionResult GetChart3(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Months,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_m_{RCP}_{Decade}";

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_m_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB
                    .Where(c => Months.Contains(c.date.Month))
                    .ToList();
                connection.Close();
            }

            string wmbname = "",
                wmacode = "";
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var nameRu = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameEn = connection.Query<string>($"SELECT \"NameWMB_En\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var nameKk = connection.Query<string>($"SELECT \"NameWMB_Kz\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                string language = new RequestLocalizationOptions().DefaultRequestCulture.Culture.Name;
                if (language == "ru")
                {
                    wmbname = nameRu.FirstOrDefault();
                }
                else if (language == "en")
                {
                    wmbname = nameEn.FirstOrDefault();
                }
                else
                {
                    wmbname = nameKk.FirstOrDefault();
                }
                wmacode = code.FirstOrDefault();
                connection.Close();
            }

            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = 0,
                year_finish = 0;
            if(climate_rasterstats.Count() > 0)
            {
                year_start = climate_rasterstats.Min(c => c.date.Year);
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            }
            List<int> years = new List<int>();
            List<string> periods = new List<string>();
            int yearsCount = (year_finish - year_start) / 10 + 1;
            decimal?[,] max = new decimal?[12, yearsCount],
                min = new decimal?[12, yearsCount],
                median = new decimal?[12, yearsCount];
            for (int year = year_start, i = 0; year <= year_finish; year += 10, i++)
            {
                years.Add(year);
                periods.Add($"{year.ToString()} - {(year + 9).ToString()}");
                for (int month = 0; month <= 11; month++)
                {
                    climate_rasterstat climate_rasterstat = climate_rasterstats
                        .FirstOrDefault(c => c.date.Year == year && c.date.Month == month + 1);
                    if (climate_rasterstat == null)
                    {
                        max[month, i] = null;
                        min[month, i] = null;
                        median[month, i] = null;
                    }
                    else
                    {
                        var data = JObject.Parse(climate_rasterstat.data);
                        foreach (JProperty property in data.Properties())
                        {
                            if (property.Name == "max")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                max[month, i] = d;
                            }
                            if (property.Name == "min")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                min[month, i] = d;
                            }
                            if (property.Name == "median")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                median[month, i] = d;
                            }
                        }
                    }
                }
            }

            return Json(new
            {
                years,
                periods,
                max,
                min,
                median,
                wmbname,
                wmacode
            });
        }

        [HttpPost]
        public ActionResult GetTable1(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_y_{RCP}_{Decade}";
            List<raster_table_for_chart> raster_table_for_chart_bs = new List<raster_table_for_chart>();

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_y_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB.ToList();
                connection.Close();
            }
            
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                connection.Close();
            }

            List<decimal?> max = new List<decimal?>(),
                min = new List<decimal?>(),
                median = new List<decimal?>();
            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = climate_rasterstats.Min(c => c.date.Year),
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            List<int> years = new List<int>();
            for (int year = year_start; year <= year_finish; year += 10)
            {
                years.Add(year);
                climate_rasterstat climate_rasterstat = climate_rasterstats.FirstOrDefault(c => c.date.Year == year);
                if (climate_rasterstat == null)
                {
                    max.Add(null);
                    min.Add(null);
                    median.Add(null);
                }
                else
                {
                    var data = JObject.Parse(climate_rasterstat.data);
                    foreach (JProperty property in data.Properties())
                    {
                        if (property.Name == "max")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            max.Add(d);
                        }
                        if (property.Name == "min")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            min.Add(d);
                        }
                        if (property.Name == "median")
                        {
                            decimal? d = Convert.ToDecimal(property.Value);
                            median.Add(d);
                        }
                    }
                }
            }

            int decade = Convert.ToInt32(Decade);
            for (int i = 0; i < years.Count; i++)
            {
                raster_table_for_chart raster_table_for_chart_b_new = new raster_table_for_chart();

                raster_table_for_chart_b_new.year = years[i];
                raster_table_for_chart_b_new.min = min[i];
                raster_table_for_chart_b_new.max = max[i];
                raster_table_for_chart_b_new.median = median[i];

                raster_table_for_chart_b_new.period = raster_table_for_chart_b_new.year.ToString() + " - " + (raster_table_for_chart_b_new.year + decade - 1).ToString();

                raster_table_for_chart_bs.Add(raster_table_for_chart_b_new);
            }

            return Json(new
            {
                raster_table_for_chart_bs
            });
        }

        [HttpPost]
        public ActionResult GetTable2(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Seasons,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_s_{RCP}_{Decade}";
            List<raster_table_for_chart> raster_table_for_chart_bs = new List<raster_table_for_chart>();

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_s_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB
                    .Where(c => Seasons.Contains(c.date.Month))
                    .ToList();
                connection.Close();
            }
            
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                connection.Close();
            }

            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = climate_rasterstats.Min(c => c.date.Year),
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            List<int> years = new List<int>();
            int yearsCount = (year_finish - year_start) / 10 + 1;
            decimal?[,] max = new decimal?[4, yearsCount],
                min = new decimal?[4, yearsCount],
                median = new decimal?[4, yearsCount];
            for (int year = year_start, i = 0; year <= year_finish; year += 10, i++)
            {
                years.Add(year);
                for (int season = 0; season <= 3; season++)
                {
                    climate_rasterstat climate_rasterstat = climate_rasterstats
                        .FirstOrDefault(c => c.date.Year == year && c.date.Month == season + 1);
                    if (climate_rasterstat == null)
                    {
                        max[season, i] = null;
                        min[season, i] = null;
                        median[season, i] = null;
                    }
                    else
                    {
                        var data = JObject.Parse(climate_rasterstat.data);
                        foreach (JProperty property in data.Properties())
                        {
                            if (property.Name == "max")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                max[season, i] = d;
                            }
                            if (property.Name == "min")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                min[season, i] = d;
                            }
                            if (property.Name == "median")
                            {
                                decimal? d = Convert.ToDecimal(property.Value);
                                median[season, i] = d;
                            }
                        }
                    }
                }
            }

            int decade = Convert.ToInt32(Decade);
            for (int i = 0; i < 4; i++)
            {
                bool checkForNull = true;
                for (int j = 0; j < years.Count; j++)
                {
                    if (max[i, j] != null)
                    {
                        checkForNull = false;
                        break;
                    }
                }
                if (!checkForNull)
                {
                    for (int j = 0; j < years.Count; j++)
                    {
                        raster_table_for_chart raster_table_for_chart_b_new = new raster_table_for_chart();
                        if (i == 0)
                        {
                            raster_table_for_chart_b_new.season = _sharedLocalizer["Spring"];
                        }
                        if (i == 1)
                        {
                            raster_table_for_chart_b_new.season = _sharedLocalizer["Summer"];
                        }
                        if (i == 2)
                        {
                            raster_table_for_chart_b_new.season = _sharedLocalizer["Autumn"];
                        }
                        if (i == 3)
                        {
                            raster_table_for_chart_b_new.season = _sharedLocalizer["Winter"];
                        }
                        raster_table_for_chart_b_new.year = years[j];
                        raster_table_for_chart_b_new.min = min[i, j];
                        raster_table_for_chart_b_new.max = max[i, j];
                        raster_table_for_chart_b_new.median = median[i, j];

                        raster_table_for_chart_b_new.period = raster_table_for_chart_b_new.year.ToString() + " - " + (raster_table_for_chart_b_new.year + decade - 1).ToString();

                        raster_table_for_chart_bs.Add(raster_table_for_chart_b_new);
                    }
                }
            }

            return Json(new
            {
                raster_table_for_chart_bs
            });
        }

        [HttpPost]
        public ActionResult GetTable3(int Id,
            string SubParameter,
            string Decade,
            string RCP,
            int[] Months,
            int[] Dates)
        {
            string layer_name = $"{SubParameter}_m_{RCP}_{Decade}";
            List<raster_table_for_chart> raster_table_for_chart_bs = new List<raster_table_for_chart>();

            // Отклонения осадков в мм
            if (SubParameter == "pr_dlt_avg_mm")
            {
                string SubParameter_ = SubParameter.Substring(0, SubParameter.Length - 3);
                layer_name = $"{SubParameter_}_m_{RCP}_{Decade}_mm";
            }

            List<climate_rasterstat> climate_rasterstats = new List<climate_rasterstat>();
            using (var connection = new NpgsqlConnection(geoportalProdConnection))
            {
                connection.Open();
                string query = $"SELECT date, data" +
                    $" FROM public.climate_rasterstats" +
                    $" WHERE layer_name = '{layer_name}'" +
                    $" AND feature_id = {Id.ToString()}" +
                    $" ORDER BY date;";
                var climate_rasterstats_DB = connection.Query<climate_rasterstat>(query);
                climate_rasterstats = climate_rasterstats_DB
                    .Where(c => Months.Contains(c.date.Month))
                    .ToList();
                connection.Close();
            }
            
            using (var connection = new NpgsqlConnection(geodataProdConnection))
            {
                connection.Open();
                var name = connection.Query<string>($"SELECT \"NameWMB_Ru\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                var code = connection.Query<string>($"SELECT \"CodeWMA\" " +
                    $"FROM public.wma_polygon " +
                    $"WHERE \"OBJECTID\" = {Id.ToString()} " +
                    $"LIMIT 1;");
                connection.Close();
            }

            climate_rasterstats = climate_rasterstats.Where(c => Dates.Contains(c.date.Year)).ToList();
            int year_start = 0,
                year_finish = 0;
            if (climate_rasterstats.Count() > 0)
            {
                year_start = climate_rasterstats.Min(c => c.date.Year);
                year_finish = climate_rasterstats.Max(c => c.date.Year);
            }
            List<int> years = new List<int>();
            int yearsCount = (year_finish - year_start) / 10 + 1;
            decimal?[,] max = new decimal?[12, yearsCount],
                min = new decimal?[12, yearsCount],
                median = new decimal?[12, yearsCount];
            for (int year = year_start, i = 0; year <= year_finish; year += 10, i++)
            {
                years.Add(year);
                for (int month = 0; month <= 11; month++)
                {
                    climate_rasterstat climate_rasterstat = climate_rasterstats
                        .FirstOrDefault(c => c.date.Year == year && c.date.Month == month + 1);
                    if (climate_rasterstat == null)
                    {
                        max[month, i] = null;
                        min[month, i] = null;
                        median[month, i] = null;
                    }
                    else
                    {
                        var data = JObject.Parse(climate_rasterstat.data);
                        foreach (JProperty property in data.Properties())
                        {
                            if (property.Name == "max")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                max[month, i] = d;
                            }
                            if (property.Name == "min")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                min[month, i] = d;
                            }
                            if (property.Name == "median")
                            {
                                decimal? d = null;
                                try
                                {
                                    d = Convert.ToDecimal(property.Value);
                                }
                                catch { }
                                median[month, i] = d;
                            }
                        }
                    }
                }
            }

            int decade = Convert.ToInt32(Decade);
            List<string> months = new List<string>
            { _sharedLocalizer["January"], _sharedLocalizer["February"], _sharedLocalizer["March"], _sharedLocalizer["April"], _sharedLocalizer["May"], _sharedLocalizer["June"],
              _sharedLocalizer["July"], _sharedLocalizer["August"], _sharedLocalizer["September"], _sharedLocalizer["October"], _sharedLocalizer["November"], _sharedLocalizer["December"]};
            for (int i = 0; i < 12; i++)
            {
                bool checkForNull = true;
                for (int j = 0; j < years.Count; j++)
                {
                    if (max[i, j] != null)
                    {
                        checkForNull = false;
                        break;
                    }
                }
                if (!checkForNull)
                {
                    for (int j = 0; j < years.Count; j++)
                    {
                        raster_table_for_chart raster_table_for_chart_b_new = new raster_table_for_chart();
                        for (int k = 0; k < months.Count; k++)
                        {
                            if (i == k)
                            {
                                raster_table_for_chart_b_new.month = months[k];
                            }
                        }

                        raster_table_for_chart_b_new.year = years[j];
                        raster_table_for_chart_b_new.min = min[i, j];
                        raster_table_for_chart_b_new.max = max[i, j];
                        raster_table_for_chart_b_new.median = median[i, j];

                        raster_table_for_chart_b_new.period = raster_table_for_chart_b_new.year.ToString() + " - " + (raster_table_for_chart_b_new.year + decade - 1).ToString();

                        raster_table_for_chart_bs.Add(raster_table_for_chart_b_new);
                    }
                }
            }

            return Json(new
            {
                raster_table_for_chart_bs
            });
        }

        [HttpPost]
        public ActionResult Download(
            string lefts,
            string bottoms,
            string rights,
            string tops,
            string table,
            string email,
            //string parameter,
            int yearstart,
            int yearfinish
            )
        {
            string message = "email sent";
            decimal left = 0,
                bottom = 0,
                right = 0,
                top = 0;
            try
            {
                left = Convert.ToDecimal(lefts);
                bottom = Convert.ToDecimal(bottoms);
                right = Convert.ToDecimal(rights);
                top = Convert.ToDecimal(tops);
            }
            catch
            {
                left = Convert.ToDecimal(lefts.Replace('.', ','));
                bottom = Convert.ToDecimal(bottoms.Replace('.', ','));
                right = Convert.ToDecimal(rights.Replace('.', ','));
                top = Convert.ToDecimal(tops.Replace('.', ','));
            }
            try
            {
                string rname = table;
                //switch (rname.Split('_')[0] + "_" + rname.Split('_')[1])
                //{
                //    case "tasmax_pd":
                //        table = "climate_tasmax";
                //        break;
                //    case "tasmax_dlt":
                //        table = "climate_tasmax_dlt";
                //        break;
                //    case "tas_pd":
                //        table = "climate_tas";
                //        break;
                //    case "tas_dlt":
                //        table = "climate_tas_dlt";
                //        break;
                //    case "tasmin_pd":
                //        table = "climate_tasmin";
                //        break;
                //    case "tasmin_dlt":
                //        table = "climate_tasmin_dlt";
                //        break;
                //    case "pr_pd":
                //        table = "climate_pr";
                //        break;
                //    case "pr_dlt":
                //        table = "climate_pr_dlt";
                //        break;
                //}

                List<string> points = new List<string>();
                List<climate_x> climate_xs = new List<climate_x>();
                using (var connection = new NpgsqlConnection(geodataanalyticsProdConnection))
                {
                    connection.Open();
                    try
                    {
                        string query = $"SELECT ST_AsText(point)" +
                        $" FROM public.climate_coords" +
                        $" WHERE ST_Contains(ST_GeometryFromText('POLYGON(({left.ToString()} {bottom.ToString()},{right.ToString()} {bottom.ToString()},{right.ToString()} {top.ToString()},{left.ToString()} {top.ToString()},{left.ToString()} {bottom.ToString()}))')," +
                        $" ST_GeometryFromText(ST_AsText(point)));";
                        var pointsDB = connection.Query<string>(query, commandTimeout: 600);
                        points = pointsDB.ToList();
                    }
                    catch
                    {
                        string query = $"SELECT ST_AsText(point)" +
                        $" FROM public.climate_coords" +
                        $" WHERE ST_Contains(ST_GeometryFromText('POLYGON(({left.ToString().Replace(',', '.')} {bottom.ToString().Replace(',', '.')},{right.ToString().Replace(',', '.')} {bottom.ToString().Replace(',', '.')},{right.ToString().Replace(',', '.')} {top.ToString().Replace(',', '.')},{left.ToString().Replace(',', '.')} {top.ToString().Replace(',', '.')},{left.ToString().Replace(',', '.')} {bottom.ToString().Replace(',', '.')}))')," +
                        $" ST_GeometryFromText(ST_AsText(point)));";
                        var pointsDB = connection.Query<string>(query, commandTimeout: 600);
                        points = pointsDB.ToList();
                    }
                    foreach (string point in points)
                    {
                        var climate_xsDB = connection.Query<climate_x>($"SELECT name, dt, ST_AsText(point) as point, value" +
                            $" FROM public.{table}" +
                            $" WHERE point = ST_GeomFromEWKT('{point}')" +
                            $" AND date_part('year', dt) <= '{yearfinish.ToString()}'" +
                            $" AND date_part('year', dt) >= '{yearstart.ToString()}'" +
                            $" ORDER BY name, dt", commandTimeout: 600);
                        climate_xs.AddRange(climate_xsDB.ToList());
                    }
                    connection.Close();
                }

                // delete old files
                foreach (string file in Directory.EnumerateFiles(Path.Combine(_hostingEnvironment.WebRootPath, "Download")))
                {
                    try
                    {
                        string date = file.Split("__")[1];
                        int year = Convert.ToInt32(date.Substring(0, 4)),
                            month = Convert.ToInt32(date.Substring(4, 2)),
                            day = Convert.ToInt32(date.Substring(6, 2));
                        DateTime dt = new DateTime(year, month, day);
                        if (DateTime.Today - dt > new TimeSpan(2, 0, 0, 0))
                        {
                            try
                            {
                                System.IO.File.Delete(file);
                            }
                            catch
                            { }
                        }
                    }
                    catch
                    {

                    }
                }

                // create csv file
                string fileName = $"{table}__{DateTime.Now.ToString("yyyyMMdd__HHmmss")}.csv",
                    fileZipName = Path.ChangeExtension(fileName, "zip"),
                    filePath = Path.Combine(_hostingEnvironment.WebRootPath, "Download", fileName),
                    fileZipPath = Path.Combine(_hostingEnvironment.WebRootPath, "Download", fileZipName);
                using (var writer = new StreamWriter(filePath))
                {
                    //writer.WriteLine("name\tdate\tpoint\tvalue");
                    //foreach (climate_x climate_X in climate_xs)
                    //{
                    //    writer.WriteLine($"{climate_X.name}\t{climate_X.dt.ToString("yyyy.MM.dd")}\t{climate_X.point}\t{climate_X.value?.ToString()}");
                    //}
                    writer.WriteLine("Параметр\tПериод\tRCP\tПериодичность\tСезон/месяц\tДата\tШирота\tДолгота\tЗначение");
                    foreach (climate_x climate_X in climate_xs)
                    {
                        string[] ss = climate_X.name.Split('_');
                        string parameter_ = $"{ss[0]}_{ss[1]}_{ss[2]}",
                            period_ = $"{ss[3]}",
                            rcp_ = $"{ss[4]}",
                            periodiocity_ = $"{ss[5]}",
                            seasonmonth_ = ss.Length == 7 ? $"{ss[6]}" : "",
                            long_ = climate_X.point.Replace("POINT(", "").Split(' ')[0],
                            lat_ = climate_X.point.Replace("POINT(", "").Split(' ')[1].Replace(")", "");
                        writer.WriteLine($"{parameter_}\t{period_}\t{rcp_}\t{periodiocity_}\t{seasonmonth_}\t{climate_X.dt.ToString("yyyy.MM.dd")}\t{lat_}\t{long_}\t{climate_X.value?.ToString()}");
                    }
                }

                // zip file
                //ZipFile.CreateFromDirectory(filePath, fileZipPath);
                using (ZipArchive zip = ZipFile.Open(fileZipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(filePath, fileName);
                }

                //send email
                //MailMessage mail = new MailMessage();
                //SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                //mail.From = new MailAddress("ingeokz@gmail.com");
                //mail.To.Add(email);
                //mail.Subject = "Climate data";
                ////Attachment attachment;
                ////attachment = new System.Net.Mail.Attachment(fileZipPath);
                ////mail.Attachments.Add(attachment);
                //mail.IsBodyHtml = true;
                //mail.Body = $"Скачать данные <a href=\"{this.Request.Host}\\climate\\DownloadDataFile?file={fileZipName}\">здесь</a>";
                //SmtpServer.UseDefaultCredentials = false;
                //SmtpServer.Port = 587;
                //SmtpServer.Credentials = new System.Net.NetworkCredential("ingeokz@gmail.com", "Qwerty!@#");
                //SmtpServer.EnableSsl = true;
                //SmtpServer.Send(mail);

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.mail.ru");
                mail.From = new MailAddress("ingeokz@mail.ru");
                mail.To.Add(email);
                mail.Subject = "Climate data";
                Attachment attachment;
                attachment = new System.Net.Mail.Attachment(fileZipPath);
                mail.Attachments.Add(attachment);
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("ingeokz@mail.ru", "geoportal2020");
                SmtpServer.EnableSsl = true;
                SmtpServer.Timeout = int.MaxValue;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                message = ex.Message + ". " + ex.InnerException?.Message;
            }
            return Json(new
            {
                message
            });
        }

        public IActionResult DownloadDataFile(string file)
        {
            string filePath = filePath = Path.Combine(_hostingEnvironment.WebRootPath, "Download", file);
            string fileName = file;
            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/force-download", fileName);

        }

        public IActionResult NewTableAnalytics()
        {
            try
            {
                using (var connection = new NpgsqlConnection(geodataanalyticsProdConnection))
                {
                    connection.Open();
                    List<string> tables = new List<string>();
                    //tables.Add("climate_tasmax");
                    //tables.Add("climate_tas");
                    //tables.Add("climate_tasmin");
                    //tables.Add("climate_tasmax_dlt");
                    //tables.Add("climate_tas_dlt");
                    //tables.Add("climate_tasmin_dlt");
                    //tables.Add("climate_pr");
                    //tables.Add("climate_pr_dlt");
                    tables.Add("climate_et");
                    //tables.Add("climate_et_dlt");
                    foreach (string table in tables)
                    {
                        string query = $"SELECT name, dt, ST_AsEWKT(point) as point, value FROM public.{table};";
                        var climate_xsQ = connection.Query<climate_x>(query, commandTimeout: 6000);
                        string query_insert = $"INSERT INTO public.climate_et2(name, dt, value, lon, lat) VALUES";
                        // '2016-06-22 19:10:25-07'
                        List<climate_x> climate_xs = climate_xsQ.ToList();
                        List<string> list = new List<string>();
                        //foreach (climate_x climate_x in climate_xsQ)
                        for(int i=0;i< climate_xs.Count();i++)
                        {
                            // SRID=4326;POINT(45.125 34.125)
                            climate_xs[i].lon = Convert.ToDecimal(climate_xs[i].point.Split('(')[1].Split(' ')[0]);
                            climate_xs[i].lat = Convert.ToDecimal(climate_xs[i].point.Split('(')[1].Split(' ')[1].Split(')')[0]);
                            //query_insert += $" ('{climate_xs[i].name}'," +
                            //    $" '{climate_xs[i].dt.ToString("yyyy-MM-dd HH:mm:ss")}+06:30'," +
                            //    $" {climate_xs[i].value.ToString()}," +
                            //    $" {climate_xs[i].lon.ToString()}," +
                            //    $" {climate_xs[i].lat.ToString()})";
                            list.Add($"{climate_xs[i].name}\t" +
                                $"{climate_xs[i].dt.ToString("yyyy-MM-dd HH:mm:ss")}+06:30\t" +
                                $"{climate_xs[i].value.ToString()}\t" +
                                $"{climate_xs[i].lon.ToString()}\t" +
                                $"{climate_xs[i].lat.ToString()}");
                        }
                        System.IO.File.WriteAllLines("D:/SavedLists.txt", list);
                        //connection.Query(query_insert, commandTimeout: 6000);
                    }
                    connection.Close();
                }
            }
            catch(Exception ex)
            {

            }
            return View();
        }
    }
}