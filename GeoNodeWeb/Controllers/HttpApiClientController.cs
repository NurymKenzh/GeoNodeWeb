using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoNodeWeb.Controllers
{
    public class HttpApiClientController : HttpClient
    {
        private readonly IHttpContextAccessor _HttpContextAccessor;
        public HttpApiClientController(IHttpContextAccessor HttpContextAccessor)
        {
            string APIUrl = Startup.Configuration["GeoServerProdUrlServer"];//"https://geoportal.ingeo.kz/geoserver/rest/workspaces/climate/coveragestores/pr_pd_avg_m_rcp45_10/coverages/pr_pd_avg_m_rcp45_10.xml";
            BaseAddress = new Uri(APIUrl);
            DefaultRequestHeaders.Accept.Clear();
            //DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            _HttpContextAccessor = HttpContextAccessor;
            //string token = _HttpContextAccessor.HttpContext.Session.GetString("Token");

            //DefaultRequestHeaders.Add("admin", "HdpjwZjfL7MnrK-Kcp!@uaZY");
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes($"admin:{Startup.Configuration["GeoServerProdPassword"]}")));
        }
    }
}