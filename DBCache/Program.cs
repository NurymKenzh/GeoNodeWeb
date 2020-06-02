using Dapper;
using Npgsql;
using System;

namespace DBCache
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} >> starting creating cache!");
            var connection = new NpgsqlConnection("Host=localhost;Database=analytics;Username=postgres;Password=postgres;Port=5432");
            connection.Open();
            string query = "SELECT id, name, dt, point, value FROM public.climate_et;" +
                "SELECT id, name, dt, point, value FROM public.climate_et_dlt;" +
                "SELECT id, name, dt, point, value FROM public.climate_gtk;" +
                "SELECT id, name, dt, point, value FROM public.climate_pr;" +
                "SELECT id, name, dt, point, value FROM public.climate_pr_dlt;" +
                "SELECT id, name, dt, point, value FROM public.climate_spei;" +
                "SELECT id, name, dt, point, value FROM public.climate_spi;" +
                "SELECT id, name, dt, point, value FROM public.climate_tas;" +
                "SELECT id, name, dt, point, value FROM public.climate_tas_dlt;" +
                "SELECT id, name, dt, point, value FROM public.climate_tasmax;" +
                "SELECT id, name, dt, point, value FROM public.climate_tasmax_dlt;" +
                "SELECT id, name, dt, point, value FROM public.climate_tasmin;" +
                "SELECT id, name, dt, point, value FROM public.climate_tasmin_dlt;";
            connection.Query(query);
            connection.Close();
            Console.WriteLine($"{DateTime.Now.ToString()} >> finished creating cache!");
        }
    }
}
