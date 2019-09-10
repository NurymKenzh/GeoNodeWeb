using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.SqlClient;

namespace GeoNodeWeb.Controllers
{
    public class esnowController : Controller
    {
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
    }
}