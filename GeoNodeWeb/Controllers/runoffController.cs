using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GeoNodeWeb.Controllers
{
    public class runoffController : Controller
    {
        public IActionResult Index()
        {
            bool server = Convert.ToBoolean(Startup.Configuration["Server"]);
            ViewBag.GeoServerUrl = server ? Startup.Configuration["GeoServerProdUrlServer"].ToString() : Startup.Configuration["GeoServerProdUrlDebug"].ToString();
            return View();
        }
    }
}
