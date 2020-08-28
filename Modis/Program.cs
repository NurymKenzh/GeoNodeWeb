using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Modis
{
    class Program
    {
        class ModisProduct
        {
            public string Source;
            public string Product;
            public DateTime StartDate;
        }

        const string ModisUser = "sandugash_2004",
            ModisPassword = "Arina2009",
            ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04",
            DownloadingDir = @"C:\MODIS\Downloading",
            DownloadedDir = @"C:\MODIS\Downloaded",
            CMDPath = @"C:\Windows\system32\cmd.exe";
        //const string ModisUser = "caesarmod",
        //    ModisPassword = "caesar023Earthdata",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04",
        //    DownloadingDir = @"R:\MODISDownloading",
        //    DownloadedDir = @"D:\MODIS",
        //    CMDPath = @"C:\Windows\system32\cmd.exe";

        static ModisProduct[] modisProducts = new ModisProduct[4];

        static void Main(string[] args)
        {
            modisProducts[0] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A1.006",
                StartDate = new DateTime(2000, 2, 24)
            };
            modisProducts[1] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A2.006",
                StartDate = new DateTime(2000, 2, 24)
            };
            modisProducts[2] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A1.006",
                StartDate = new DateTime(2002, 7, 4)
            };
            modisProducts[3] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A2.006",
                StartDate = new DateTime(2002, 7, 4)
            };

            while (true)
            {
                DateTime startDate = modisProducts.Max(m => m.StartDate);
                foreach (ModisProduct modisProduct in modisProducts)
                {
                    DateTime startDateCurrent = ModisDownload(modisProduct);
                    if (startDate < startDateCurrent)
                    {
                        startDate = startDateCurrent;
                    }
                }
                if (startDate == DateTime.Today)
                {
                    Log("Sleep 1 hour");
                    Thread.Sleep(1000 * 60 * 60 * 1);
                }
            }
        }

        private static void GDALExecute(
            string ModisFileName,
            string FolderToNavigate,
            params string[] Parameters)
        {
            Process process = new Process();
            try
            {
                // run cmd.exe
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = CMDPath;
                process.Start();

                // move to folder
                if (!string.IsNullOrEmpty(FolderToNavigate))
                {
                    process.StandardInput.WriteLine($"{FolderToNavigate[0]}:");
                    process.StandardInput.WriteLine($"cd {FolderToNavigate}");
                }

                process.StandardInput.WriteLine(ModisFileName + " " + string.Join(" ", Parameters));
                process.StandardInput.WriteLine("exit");
                string output = process.StandardOutput.ReadToEnd();
                Log(output);
                string error = process.StandardError.ReadToEnd();
                Log(error);
                process.WaitForExit();
                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString(), exception?.InnerException);
            }
        }

        private static DateTime ModisDownload(
            ModisProduct ModisProduct)
        {
            EmptyDownloadingDir();
            DateTime DateStart = GetStartDate(ModisProduct),
                DateFinish = GetFinishDate(DateStart);
            try
            {
                string arguments = 
                    $"-U {ModisUser} -P {ModisPassword}" +
                    $" -r -u https://n5eil01u.ecs.nsidc.org" +
                    $" -p mail@pymodis.com" +
                    $" -s {ModisProduct.Source}" +
                    $" -p {ModisProduct.Product}" +
                    $" -t {ModisSpans}" +
                    $" -f {DateStart.ToString("yyyy-MM-dd")}" +
                    $" -e {DateFinish.ToString("yyyy-MM-dd")}" +
                    $" {DownloadingDir}";
                GDALExecute("modis_download.py", "", arguments);
                MoveDownloadedFiles();
            }
            catch (Exception exception)
            {
                Log(exception.Message + ": " + exception.InnerException?.Message);
            }
            EmptyDownloadingDir();
            return DateStart;
        }

        private static void MoveDownloadedFiles()
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(DownloadingDir, "*.hdf*"))
                {
                    File.Move(file, Path.Combine(DownloadedDir, Path.GetFileName(file)));
                }
            }
            catch (Exception exception)
            {
                Log(exception.Message + ": " + exception.InnerException?.Message);
            }
        }

        private static void EmptyDownloadingDir()
        {
            try
            {
                foreach (string folder in Directory.EnumerateDirectories(DownloadingDir))
                {
                    Directory.Delete(folder, true);
                }
                foreach (string file in Directory.EnumerateFiles(DownloadingDir))
                {
                    File.Delete(file);
                }
            }
            catch (Exception exception)
            {
                Log(exception.Message + ": " + exception.InnerException?.Message);
            }
        }

        private static DateTime GetStartDate(ModisProduct ModisProduct)
        {
            DateTime StartDate = ModisProduct.StartDate;
            foreach (string file in Directory.EnumerateFiles(DownloadedDir, "*.hdf"))
            {
                if (GetFileProduct(Path.GetFileName(file)) == ModisProduct.Product)
                {
                    if (GetFileDate(Path.GetFileName(file)) > StartDate)
                    {
                        StartDate = GetFileDate(Path.GetFileName(file));
                    }
                }
            }
            if (StartDate > ModisProduct.StartDate)
            {
                StartDate = StartDate.AddDays(1);
            }
            if (StartDate > DateTime.Today)
            {
                StartDate = DateTime.Today;
            }
            return StartDate;
        }

        private static string GetFileProduct(string File)
        {
            return $"{File.Split('.')[0]}.{File.Split('.')[3]}";
        }

        private static DateTime GetFinishDate(DateTime StartDate)
        {
            DateTime FinishDate = StartDate.AddDays(10);
            if (FinishDate > DateTime.Today)
            {
                FinishDate = DateTime.Today;
            }
            return FinishDate;
        }

        private static DateTime GetFileDate(string File)
        {
            string date = File.Split('.')[1].Replace("A", ""),
                s_year = date.Substring(0, 4),
                s_dayofyear = date.Substring(4, 3);
            int year = Convert.ToInt32(s_year),
                dayofyear = Convert.ToInt32(s_dayofyear);
            DateTime Date = new DateTime(year, 1, 1).AddDays(dayofyear - 1);
            return Date;
        }

        private static void Log(string log)
        {
            foreach (string line in log.Split("\r\n"))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} >> {line}");
            }
        }
    }
}
