using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace GeoNodeWeb.Controllers
{
    public class climateController : Controller
    {
        private static bool server = Convert.ToBoolean(Startup.Configuration["Server"]);
        private string geoserverConnection = server ? Startup.Configuration["geoserverConnectionServer"].ToString() : Startup.Configuration["geoserverConnectionDebug"].ToString(),
            postgresConnection = server ? Startup.Configuration["postgresConnectionServer"].ToString() : Startup.Configuration["postgresConnectionDebug"].ToString();
        public IActionResult Index()
        {
            using (var connection = new NpgsqlConnection(geoserverConnection))
            {
                connection.Open();

                var datetimepr_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_10_2011_2090\"");
                ViewBag.DateTimepr_pd_avg_m_rcp45_10_2011_2090 = datetimepr_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

                var datetimepr_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_20_2011_2090\"");
                ViewBag.DateTimepr_pd_avg_m_rcp45_20_2011_2090 = datetimepr_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

                var datetimepr_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"pr_pd_avg_m_rcp45_30_2011_2070\"");
                ViewBag.DateTimepr_pd_avg_m_rcp45_30_2011_2070 = datetimepr_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

                var datetimetas_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_10_2011_2090\"");
                ViewBag.DateTimetas_pd_avg_m_rcp45_10_2011_2090 = datetimetas_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

                var datetimetas_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_20_2011_2090\"");
                ViewBag.DateTimetas_pd_avg_m_rcp45_20_2011_2090 = datetimetas_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

                var datetimetas_pd_avg_m_rcp45_30_2011_2070 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tas_pd_avg_m_rcp45_30_2011_2070\"");
                ViewBag.DateTimetas_pd_avg_m_rcp45_30_2011_2070 = datetimetas_pd_avg_m_rcp45_30_2011_2070.OrderBy(d => d).ToArray();

                var datetimetasmax_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_10_2011_2090\"");
                ViewBag.DateTimetasmax_pd_avg_m_rcp45_10_2011_2090 = datetimetasmax_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

                var datetimetasmax_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_20_2011_2090\"");
                ViewBag.DateTimetasmax_pd_avg_m_rcp45_20_2011_2090 = datetimetasmax_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

                var datetimetasmax_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmax_pd_avg_m_rcp45_30_2011_2090\"");
                ViewBag.DateTimetasmax_pd_avg_m_rcp45_30_2011_2090 = datetimetasmax_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();

                var datetimetasmin_pd_avg_m_rcp45_10_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_10_2011_2090\"");
                ViewBag.DateTimetasmin_pd_avg_m_rcp45_10_2011_2090 = datetimetasmin_pd_avg_m_rcp45_10_2011_2090.OrderBy(d => d).ToArray();

                var datetimetasmin_pd_avg_m_rcp45_20_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_20_2011_2090\"");
                ViewBag.DateTimetasmin_pd_avg_m_rcp45_20_2011_2090 = datetimetasmin_pd_avg_m_rcp45_20_2011_2090.OrderBy(d => d).ToArray();

                var datetimetasmin_pd_avg_m_rcp45_30_2011_2090 = connection.Query<DateTime>($"SELECT ingestion FROM public.\"tasmin_pd_avg_m_rcp45_30_2011_2090\"");
                ViewBag.DateTimetasmin_pd_avg_m_rcp45_30_2011_2090 = datetimetasmin_pd_avg_m_rcp45_30_2011_2090.OrderBy(d => d).ToArray();
            }
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerUrlServer"].ToString() : Startup.Configuration["GeoServerUrlDebug"].ToString();
            return View();
        }
    }
}