using Dapper;
using Microsoft.VisualBasic.CompilerServices;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Modis
{
    class Program
    {
        class ModisProduct
        {
            public string Source;
            public string Product;
            public DateTime StartDate;
            public string[] DataSets;
            public int[] ExtractDataSetIndexes;
            public bool Spans;
            public bool Mosaic;
            public bool ConvertHdf;
            public bool Norm = false;
            public bool Publish;
            public int? AnomalyStartYear;
            public int? AnomalyEndYear;
            public bool Analize = false;
            public bool ZonalStatRaster = false;
            public int[] DayDividedDataSetIndexes;
            public int Period;
        }

        class PointData
        {
            public int pointid;
            public string product;
            public string dataset;
            public DateTime date;
            public int value;
            public int MOD10A1006_NDSISnowCover;
            public int MYD10A1006_NDSISnowCover;
            public int MOD10A2006_MaxSnowExtent;
            public int MOD10A2006_SnowCover;
            public int MYD10A2006_MaxSnowExtent;
            public int MYD10A2006_SnowCover;
            public int MOD10C2006_NDSI;
            public bool snow;
        }

        class ZonalStat
        {
            public string shp;
            public int gridid;
            public DateTime date;
            public decimal area;
            public int snow;
            public int nosnow;
            public int count;
            public decimal snowperc
            {
                get
                {
                    return count > 0 ? (decimal) snow / count : 0;
                }
            }
            public decimal nosnowperc
            {
                get
                {
                    return count > 0 ? (decimal) nosnow / count : 0;
                }
            }
        }

        class Exclusion
        {
            public string product;
            public DateTime date;
            public int count;
        }

        const int threadsCount = 4;

        //const string ModisUser = "sandugash_2004",
        //    ModisPassword = "Arina2009",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
        //    DownloadingDir = @"E:\MODIS\Downloading",
        //    DownloadedDir = @"E:\MODIS\Downloaded",
        //    Exclusions = @"E:\MODIS\exclusions.txt",
        //    CMDPath = @"C:\Windows\system32\cmd.exe",
        //    LastDateFile = "!last_date.txt",
        //    MosaicDir = @"E:\MODIS\Mosaic",
        //    ConvertDir = @"E:\MODIS\Convert",
        //    ArchiveDir = @"C:\MODIS\Archive",
        //    ModisProjection = "4326",
        //    GeoServerDir = @"E:\GeoServer\data_dir\data\MODIS",
        //    GeoServerWorkspace = "MODIS",
        //    GeoServerUser = "admin",
        //    GeoServerPassword = "geoserver",
        //    GeoServerURL = "http://localhost:8080/geoserver/",
        //    AnalizeShp = @"E:\MODIS\shp\WatershedsIleBasinPnt20201230.shp",
        //    ExtractRasterValueByPoint = @"E:\MODIS\Python\ExtractRasterValueByPoint.py",
        //    CloudMask = @"E:\MODIS\Python\CloudMask_v03.py",
        //    ZonalStatRaster = @"E:\MODIS\Python\ZonalStatRaster_v20210318v01.py",
        //    runpsqlPath = @"C:\Program Files\PostgreSQL\10\scripts\runpsql.bat",
        //    postgresPassword = "postgres",
        //    db = "GeoNodeWebModis",
        //    BuferFolder = @"E:\MODIS";

        const string ModisUser = "hvreren",
            ModisPassword = "Querty123",
            ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
            DownloadingDir = @"E:\MODIS\Downloading",
            DownloadedDir = @"E:\MODIS\Downloaded",
            Exclusions = @"E:\MODIS\exclusions.txt",
            CMDPath = @"C:\Windows\system32\cmd.exe",
            LastDateFile = "!last_date.txt",
            MosaicDir = @"E:\MODIS\Mosaic",
            ConvertDir = @"E:\MODIS\Convert",
            ArchiveDir = @"E:\MODIS\Archive",
            ModisProjection = "4326",
            GeoServerDir = @"E:\GeoServer\data_dir\data\MODIS",
            GeoServerWorkspace = "MODIS",
            GeoServerUser = "admin",
            GeoServerPassword = "geoserver",
            GeoServerURL = "http://localhost:8080/geoserver/",
            AnalizeShp = @"E:\MODIS\shp\WatershedsIleBasinPnt20201230.shp",
            ExtractRasterValueByPoint = @"E:\MODIS\Python\ExtractRasterValueByPoint.py",
            CloudMask = @"E:\MODIS\Python\CloudMask_v03.py",
            ZonalStatRaster = @"E:\MODIS\Python\ZonalStatRaster_v20210318v01.py",
            runpsqlPath = @"C:\Program Files\PostgreSQL\10\scripts\runpsql.bat",
            postgresPassword = "postgres",
            db = "GeoNodeWebModis",
            BuferFolder = @"E:\MODIS";

        static readonly string[] ZonalShps = {
            @"E:\MODIS\shp\WatershedsIleBasinOrder0.shp",
            @"E:\MODIS\shp\WatershedsIleBasinOrder1.shp",
            @"E:\MODIS\shp\WatershedsIleBasinOrder2.shp" };

        const string cloudsMaskSourceName = "CLOU",
            cloudsMaskSourceFinalName = "CLMA"; // CLOUD MASK

        static List<PointData> pointDatas = new List<PointData>();
        static BlockingCollection<PointData> pointDatas2 = new BlockingCollection<PointData>();
        static List<SnowData> pointSnows = new List<SnowData>();
        static BlockingCollection<SnowData> pointSnowsBlocking = new BlockingCollection<SnowData>();
        static BlockingCollection<Period> periods = new BlockingCollection<Period>();
        static List<ZonalStat> zonalStats = new List<ZonalStat>();

        static ModisProduct[] modisProducts = new ModisProduct[5];

        static List<Exclusion> exclusions = new List<Exclusion>();

        static void Main(string[] args)
        {
            Console.WriteLine("Press ESC to stop!");
            
            exclusions = new List<Exclusion>();
            List<string> exclusionsS = File.ReadAllLines(Exclusions).ToList();
            foreach (string exclusionS in exclusionsS)
            {
                string[] exclusionSArray = exclusionS.Split('\t');
                string product = exclusionSArray[0];
                DateTime date = new DateTime(Convert.ToInt32(exclusionSArray[1].Split('-')[0]),
                    Convert.ToInt32(exclusionSArray[1].Split('-')[1]),
                    Convert.ToInt32(exclusionSArray[1].Split('-')[2]));
                int count = Convert.ToInt32(exclusionSArray[3]);
                exclusions.Add(new Exclusion()
                {
                    product = product,
                    date = date,
                    count = count
                });
            }

            modisProducts[0] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A1.006",
                StartDate = new DateTime(2000, 2, 24),
                Period = 1,
                DataSets = new string[7]
                {
                "NDSISnowCover",
                "NDSISnowCoverBasic",
                "NDSISnowCoverAlgorithm",
                "NDSI",
                "SnowAlbedo",
                "orbitpnt",
                "granulepnt"
                },
                ExtractDataSetIndexes = new int[1] { 0 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Publish = false,
                Analize = true,
                ZonalStatRaster = true,
                DayDividedDataSetIndexes = new int[] { },
                Norm = false,
                AnomalyStartYear = null,
                AnomalyEndYear = null
            };
            modisProducts[1] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A1.006",
                StartDate = new DateTime(2002, 7, 4),
                Period = 1,
                DataSets = new string[7]
                {
                "NDSISnowCover",
                "NDSISnowCoverBasic",
                "NDSISnowCoverAlgorithm",
                "NDSI",
                "SnowAlbedo",
                "orbitpnt",
                "granulepnt"
                },
                ExtractDataSetIndexes = new int[1] { 0 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Publish = false,
                Analize = true,
                ZonalStatRaster = true,
                DayDividedDataSetIndexes = new int[] { },
                Norm = false,
                AnomalyStartYear = null,
                AnomalyEndYear = null
            };
            modisProducts[2] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A2.006",
                StartDate = new DateTime(2000, 2, 18),
                Period = 8,
                DataSets = new string[2]
                {
                "MaxSnowExtent",
                "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Publish = true,
                Analize = true,
                ZonalStatRaster = true,
                DayDividedDataSetIndexes = new int[1] { 1 },
                Norm = false,
                AnomalyStartYear = null,
                AnomalyEndYear = null
            };
            modisProducts[3] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A2.006",
                StartDate = new DateTime(2002, 7, 4),
                Period = 8,
                DataSets = new string[2]
                {
                "MaxSnowExtent",
                "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Publish = true,
                Analize = true,
                ZonalStatRaster = true,
                DayDividedDataSetIndexes = new int[1] { 1 },
                Norm = false,
                AnomalyStartYear = null,
                AnomalyEndYear = null
            };
            modisProducts[4] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10C2.006",
                StartDate = new DateTime(2000, 2, 24),
                Period = 8,
                DataSets = new string[1]
                {
                "NDSI"
                },
                ExtractDataSetIndexes = new int[1] { 0 },
                Spans = false,
                Mosaic = false,
                ConvertHdf = true,
                Publish = true,
                Analize = true,
                ZonalStatRaster = false,
                DayDividedDataSetIndexes = new int[] { },
                Norm = true,
                AnomalyStartYear = 2001,
                AnomalyEndYear = 2019
            };

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                DateTime start = DateTime.Now;

                DateTime dateNext = GetNextDate();
                foreach (ModisProduct modisProduct in modisProducts)
                {
                    ModisDownload(modisProduct, dateNext);
                }
                SaveNextDate();

                ModisMosaic();
                ModisConvertTif();
                ModisConvertHdf();
                ModisCrop();
                ModisNorm();
                ModisPublish();
                Anomaly();
                Clouds();
                Console.WriteLine("Press ESC to stop!");
                AnalizeV2();
                Console.WriteLine("Press ESC to stop!");
                SnowPeriods();
                Console.WriteLine("Press ESC to stop!");
                AnalizeZonalStatRaster();
                Console.WriteLine("Press ESC to stop!");

                if (dateNext == DateTime.Today)
                {
                    Log("Sleep 4 hour");
                    Thread.Sleep(1000 * 60 * 60 * 4);
                }
                EmptyDownloadedDir();

                DateTime finish = DateTime.Now;
                TimeSpan duration = finish - start;
                File.AppendAllText(@"E:\MODIS\time.txt", $"{start}\t{finish}\t{duration}{Environment.NewLine}");
            }
        }

        private static string GDALExecute(
            string ModisFileName,
            string FolderToNavigate,
            params string[] Parameters)
        {
            Process process = new Process();
            string output = "";
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
                output = process.StandardOutput.ReadToEnd();
                Log(output);
                string error = process.StandardError.ReadToEnd();
                Log(error);
                process.WaitForExit();
                if (!string.IsNullOrEmpty(error))
                {
                    //throw new Exception(error);
                }
            }
            catch (Exception exception)
            {
                Log($"{exception.ToString()}: {exception?.InnerException}");
                throw new Exception(exception.ToString(), exception?.InnerException);
            }
            return output;
        }

        private static void CurlExecute(
            string Parameters)
        {
            Process process = new Process();
            try
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = CMDPath;
                process.Start();

                process.StandardInput.WriteLine($"curl {Parameters}");
                process.StandardInput.WriteLine("exit");

                string output = process.StandardOutput.ReadToEnd();
                Log(output);
                string error = process.StandardError.ReadToEnd();
                Log(error);
                process.WaitForExit();
                if (error.ToLower().Contains("error"))
                {
                    throw new Exception(error);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString(), exception?.InnerException);
            }
        }

        private static void CurlBatExecute(string Parameter)
        {
            try
            {
                string bat = Path.ChangeExtension(Path.GetRandomFileName(), ".bat");
                File.WriteAllText(Path.Combine(DownloadingDir, bat), "curl" + Parameter);
                Process.Start(Path.Combine(DownloadingDir, bat)).WaitForExit();
                File.Delete(Path.Combine(DownloadingDir, bat));
            }
            catch (Exception exception)
            {
                Log($"{exception.ToString()}: {exception?.InnerException}");
            }
        }

        private static void CopyToDb(string Command)
        {
            Process process = new Process();
            try
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.EnvironmentVariables["PGPASSWORD"] = postgresPassword;
                process.StartInfo.FileName = runpsqlPath;
                process.Start();
                
                process.StandardInput.WriteLine("");
                Thread.Sleep(1000);
                process.StandardInput.WriteLine(db);
                Thread.Sleep(1000);
                process.StandardInput.WriteLine("");
                Thread.Sleep(1000);
                process.StandardInput.WriteLine("");
                Thread.Sleep(1000);
                //process.StandardInput.WriteLine($"COPY public.modispoints (pointid, product, dataset, date, value) FROM 'E:/MODIS/modispoints.txt' DELIMITER E'\\t';" + Environment.NewLine + "\\q");
                process.StandardInput.WriteLine(Command + Environment.NewLine + "\\q");
                Thread.Sleep(1000);
                //process.StandardInput.WriteLine("\\q");
                process.StandardInput.WriteLine("");
                Thread.Sleep(1000);

                string output = process.StandardOutput.ReadToEnd();
                Log(output);
                string error = process.StandardError.ReadToEnd();
                Log(error);
                process.WaitForExit();
                if (error.ToLower().Contains("error"))
                {
                    throw new Exception(error);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.ToString(), exception?.InnerException);
            }
        }

        private static void ModisDownload(
            ModisProduct ModisProduct,
            DateTime date)
        {
            if (ModisProduct.StartDate > date)
            {
                return;
            }
            if (!MustExists(ModisProduct, date))
            {
                return;
            }
            EmptyDownloadingDir();
            if (!CopyFromArchiveToDownloading(ModisProduct, date))
            {
                try
                {
                    string arguments =
                        $"-U {ModisUser} -P {ModisPassword}" +
                        $" -r -u https://n5eil01u.ecs.nsidc.org" +
                        $" -p mail@pymodis.com" +
                        $" -s {ModisProduct.Source}" +
                        $" -p {ModisProduct.Product}" +
                        (ModisProduct.Spans ? $" -t {ModisSpans}" : "") +
                        $" -f {date.ToString("yyyy-MM-dd")}" +
                        $" -e {date.ToString("yyyy-MM-dd")}" +
                        $" {DownloadingDir}";
                    GDALExecute("modis_download.py", "", arguments);
                    CopyFromDownloadingToArchive();

                    //int filesCount = Directory.GetFiles(DownloadingDir, "*hdf*").Count();
                    //string day = "-";
                    //if (filesCount > 0)
                    //{
                    //    day = Path.GetFileName(Directory.GetFiles(DownloadingDir, "*hdf*")?.FirstOrDefault()).Split('.')[1].Substring(5, 3);
                    //}
                    //string str = $"{ModisProduct.Product}\t{date.ToString("yyyy-MM-dd")}\t{day}\t{filesCount.ToString()}" + Environment.NewLine;
                    //File.AppendAllText(@"D:\MODIS\exclusions.txt", str);
                }
                catch (Exception exception)
                {
                    Log(exception.Message + ": " + exception.InnerException?.Message);
                }
            }
            MoveDownloadedFiles();
            EmptyDownloadingDir();
        }

        private static bool MustExists(
            ModisProduct ModisProduct,
            DateTime date)
        {
            DateTime date1January = new DateTime(date.Year, 1, 1);
            int days = (int)(date - date1January).TotalDays;
            if (days % ModisProduct.Period == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CopyFromArchiveToDownloading(
            ModisProduct ModisProduct,
            DateTime date)
        {
            string folder = Path.Combine(ArchiveDir, $"{date.Year.ToString()}.{ModisProduct.Product.Split('.')[0]}"),
                fileNameDate = $"*A{date.Year}{date.DayOfYear.ToString("D3")}*";
            List<string> files = Directory.EnumerateFiles(folder, fileNameDate).ToList();
            int mustFilesCount = 0;
            if (ModisProduct.Spans)
            {
                mustFilesCount = 2 * ModisSpans.Split(',').Count();
            }
            else
            {
                mustFilesCount = 2;
            }
            // exclusions
            Exclusion exclusion = exclusions.FirstOrDefault(e => e.product == ModisProduct.Product && e.date == date);
            if (exclusion != null)
            {
                mustFilesCount = exclusion.count;
            }

            if (files.Count() >= mustFilesCount)
            {
                foreach (string file in files)
                {
                    File.Copy(file, Path.Combine(DownloadingDir, Path.GetFileName(file)));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void CopyFromDownloadingToArchive()
        {
            foreach (string file in Directory.EnumerateFiles(DownloadingDir, "*.hdf*"))
            {
                try
                {
                    string[] fileNameArray = Path.GetFileNameWithoutExtension(file).Split('.');
                    string product = fileNameArray[0],
                        year = fileNameArray[1].Substring(1, 4),
                        archiveFolder = Path.Combine(ArchiveDir, $"{year}.{product}");
                    File.Copy(file, Path.Combine(archiveFolder, Path.GetFileName(file)), true);
                }
                catch (Exception exception)
                {
                    Log(exception.Message + ": " + exception.InnerException?.Message);
                }
            }
        }

        private static void MoveDownloadedFiles()
        {
            foreach (string file in Directory.EnumerateFiles(DownloadingDir, "*.hdf*"))
            {
                try
                {
                    File.Move(file, Path.Combine(DownloadedDir, Path.GetFileName(file)));
                }
                catch (Exception exception)
                {
                    Log(exception.Message + ": " + exception.InnerException?.Message);
                }
            }
        }

        private static void EmptyDownloadedDir()
        {
            try
            {
                foreach (string folder in Directory.EnumerateDirectories(DownloadedDir))
                {
                    Directory.Delete(folder, true);
                }
                foreach (string file in Directory.EnumerateFiles(DownloadedDir, "*.hdf*"))
                {
                    File.Delete(file);
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

        private static DateTime GetNextDate()
        {
            DateTime dateLast = modisProducts.Min(m => m.StartDate);
            string lastDateFile = Path.Combine(DownloadedDir, LastDateFile);
            if (File.Exists(lastDateFile))
            {
                string s_lastDate = File.ReadAllText(lastDateFile),
                    s_year = s_lastDate.Split('-')[0],
                    s_month = s_lastDate.Split('-')[1],
                    s_day = s_lastDate.Split('-')[2];
                int year = Convert.ToInt32(s_year),
                    month = Convert.ToInt32(s_month),
                    day = Convert.ToInt32(s_day);
                DateTime lastDateFromFile = new DateTime(year, month, day);
                if (lastDateFromFile >= dateLast)
                {
                    dateLast = lastDateFromFile.AddDays(1);
                }
            }
            return dateLast;
        }

        private static void SaveNextDate()
        {
            DateTime dateLast = GetNextDate();
            if(dateLast == DateTime.Today)
            {
                dateLast = dateLast.AddDays(-30);
            }
            string lastDateFile = Path.Combine(DownloadedDir, LastDateFile);
            File.WriteAllText(lastDateFile, dateLast.ToString("yyyy-MM-dd"));
        }

        private static void CreateListFiles()
        {
            string[] GeoServerFiles = Directory.EnumerateFiles(GeoServerDir, "*.tif*").ToArray();
            foreach (string file in Directory.EnumerateFiles(DownloadedDir, "*.hdf"))
            {
                string fileDate = GetHDFDate(file),
                    fileProduct = GetHDFProduct(file);
                if (GeoServerFiles.Count(g => g.Contains(fileDate) && g.Contains(fileProduct.Replace(".", ""))) > 0)
                {
                    continue;
                }
                string product = Path.GetFileName(file).Split('.')[0],
                    date = Path.GetFileName(file).Split('.')[1],
                    listFile = Path.Combine(DownloadedDir, $"{product}.{date}.txt");
                ModisProduct modisProduct = modisProducts.FirstOrDefault(m => m.Product.Split('.')[0] == product);
                if (!File.Exists(listFile) && modisProduct.Mosaic)
                {
                    File.WriteAllLines(listFile, Directory.EnumerateFiles(DownloadedDir, $"{product}.{date}*.hdf*").Select(f => Path.GetFileName(f)));
                }
            }
        }

        private static void ModisMosaic()
        {
            try
            {
                CreateListFiles();
                List<Task> taskList = new List<Task>();
                foreach (string listFile in Directory.EnumerateFiles(DownloadedDir, "*.txt"))
                {
                    if (listFile.Contains(LastDateFile))
                    {
                        continue;
                    }
                    taskList.Add(Task.Factory.StartNew(() => ModisMosaicTask(listFile)));
                    if (taskList.Count == threadsCount)
                    {
                        Task.WaitAll(taskList.ToArray());
                        taskList = new List<Task>();
                    }
                    //ModisMosaicTask(listFile);
                }
                Task.WaitAll(taskList.ToArray());
            }
            catch { }
        }

        private static void ModisMosaicTask(string ListFile)
        {
            try
            {
                string s_productShort = Path.GetFileName(ListFile).Split('.')[0];
                ModisProduct modisProduct = new ModisProduct();
                foreach (ModisProduct modisProductCurrent in modisProducts)
                {
                    if (modisProductCurrent.Product.Contains(s_productShort))
                    {
                        modisProduct = modisProductCurrent;
                        break;
                    }
                }
                for (int i = 0; i < modisProduct.DataSets.Count(); i++)
                {
                    if (!modisProduct.ExtractDataSetIndexes.Contains(i))
                    {
                        continue;
                    }
                    string indexes = "";
                    for (int j = 0; j < modisProduct.DataSets.Count(); j++)
                    {
                        if (j == i)
                        {
                            indexes += "1 ";
                        }
                        else
                        {
                            indexes += "0 ";
                        }
                    }
                    string index = (i).ToString().PadLeft(2, '0'),
                        tif = $"{modisProduct.Source.Split('/')[1]}_{modisProduct.Product.Replace(".", "")}_B{index}_{modisProduct.DataSets[i]}.tif",
                        arguments = $"-o {tif}" +
                            $" -s \"{indexes.Trim()}\"" +
                            $" \"{ListFile}\"";
                    GDALExecute(
                        "modis_mosaic.py",
                        MosaicDir,
                        arguments);
                }
                File.Delete(ListFile);
            }
            catch { }
        }

        private static void ModisConvertTif()
        {
            List<Task> taskList = new List<Task>();
            foreach (string file in Directory.EnumerateFiles(MosaicDir, "*.tif"))
            {
                taskList.Add(Task.Factory.StartNew(() => ModisConvertTask(file)));
                if (taskList.Count == threadsCount)
                {
                    Task.WaitAll(taskList.ToArray());
                    taskList = new List<Task>();
                }
                //ModisConvertTask(file);
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void ModisConvertTask(string TifFile)
        {
            string xml = TifFile + ".xml",
                    tifReprojected = $"{Path.GetFileNameWithoutExtension(TifFile)}_{ModisProjection}",
                    arguments = $"-v -s \"( 1 )\" -o {tifReprojected} -e {ModisProjection} \"{TifFile}\"";
            GDALExecute(
                "modis_convert.py",
                ConvertDir,
                arguments);
            File.Delete(TifFile);
            File.Delete(xml);
        }

        private static void ModisConvertHdf()
        {
            List<Task> taskList = new List<Task>();
            string[] GeoServerFiles = Directory.EnumerateFiles(GeoServerDir, "*.tif*").ToArray();
            foreach (string file in Directory.EnumerateFiles(DownloadedDir, "*.hdf"))
            {
                ModisProduct modisProduct = modisProducts.FirstOrDefault(m => m.Product.Contains(Path.GetFileNameWithoutExtension(file).Split('.')[0]));
                if (modisProduct.ConvertHdf)
                {
                    string[] fileArray = Path.GetFileNameWithoutExtension(file).Split('.');
                    string fileDate = GetHDFDate(file),
                        fileProduct = $"{fileArray[0]}.{fileArray[2]}";
                    if (GeoServerFiles.Count(g => g.Contains(fileDate) && g.Contains(fileProduct.Replace(".", ""))) > 0)
                    {
                        continue;
                    }
                    for (int i = 0; i < modisProduct.DataSets.Count(); i++)
                    {
                        int DataSetIndex = i;
                        string indexes = "";
                        if (!modisProduct.ExtractDataSetIndexes.Contains(i))
                        {
                            continue;
                        }
                        for (int j = 0; j < modisProduct.DataSets.Count(); j++)
                        {
                            if (j == i)
                            {
                                indexes += "1 ";
                            }
                            else
                            {
                                indexes += "0 ";
                            }
                        }
                        taskList.Add(Task.Factory.StartNew(() => ModisConvertHdfTask(modisProduct, file, indexes.Trim(), DataSetIndex)));
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void ModisConvertHdfTask(ModisProduct ModisProduct, string HdfFile, string indexes, int DataSetIndex)
        {
            string file = Path.GetFileNameWithoutExtension(HdfFile);
            string[] filesArray = file.Split('.');
            string tifReprojected = $"{filesArray[1]}" +
                    $"_{ModisProduct.Source.Split('/')[1]}" +
                    $"_{ModisProduct.Product.Replace(".", "")}" +
                    $"_B{(DataSetIndex).ToString().PadLeft(2, '0')}" +
                    $"_{ModisProduct.DataSets[DataSetIndex]}" +
                    $"_{ModisProjection}",
                arguments = $" -s \"( {indexes} )\" -o {tifReprojected} -e {ModisProjection} \"{HdfFile}\"";
            GDALExecute(
                "modis_convert.py",
                DownloadedDir,
                arguments);
            string tifConverted = Directory.GetFiles(DownloadedDir, $"{tifReprojected}*.tif").FirstOrDefault();
            File.Move(Path.Combine(DownloadedDir, Path.ChangeExtension(tifConverted, ".tif")),
                Path.Combine(ConvertDir, Path.ChangeExtension(tifReprojected, ".tif")));
        }

        private static void ModisCrop()
        {
            List<Task> taskList = new List<Task>();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (!modisProduct.Spans)
                {
                    foreach (string file in Directory.EnumerateFiles(ConvertDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                    {
                        if (!Path.GetFileName(file).Contains("_KZ"))
                        {
                            //taskList.Add(Task.Factory.StartNew(() => ModisCropTask(file)));
                            ModisCropTask(file);
                        }
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void ModisCropTask(string TifFile)
        {
            string arguments = $" {Path.GetFileName(TifFile)}" +
                $" -projwin 43.990499 56.041399 88.340325 39.642653" +
                $" {Path.ChangeExtension(Path.GetFileNameWithoutExtension(TifFile) + "_KZ", ".tif")}";
            GDALExecute(
                "gdal_translate",
                ConvertDir,
                arguments);
            File.Delete(TifFile);
        }

        private static void ModisNorm()
        {
            List<Task> taskList = new List<Task>();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.Norm)
                {
                    foreach (string file in Directory.EnumerateFiles(ConvertDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                    {
                        taskList.Add(Task.Factory.StartNew(() => ModisNormTask(file)));
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void ModisNormTask(string TifFile)
        {
            string arguments = $" -A {Path.GetFileName(TifFile)}" +
                $" --outfile={Path.GetFileNameWithoutExtension(TifFile)}_Norm.tif" +
                $" --calc=\"A*(A<=100)\"";
            GDALExecute(
                "gdal_calc.py",
                ConvertDir,
                arguments);
            File.Delete(TifFile);
            File.Move(Path.Combine(ConvertDir, Path.GetFileNameWithoutExtension(TifFile) + "_Norm.tif"),
                TifFile);
        }

        private static void ModisPublish()
        {
            List<Task> taskList = new List<Task>();
            foreach (string file in Directory.EnumerateFiles(ConvertDir, "*.tif"))
            {
                File.Move(
                    file,
                    Path.Combine(GeoServerDir, Path.GetFileName(file)));
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                string[] fileNameArray = Path.GetFileNameWithoutExtension(file).Split('_');
                string source_ = fileNameArray[1],
                    product_ = fileNameArray[2];
                ModisProduct modisProduct = modisProducts.FirstOrDefault(m => m.Source.Split('/')[1] == source_ && m.Product.Replace(".", "") == product_);
                if (modisProduct.Publish)
                {
                    taskList.Add(Task.Factory.StartNew(() => ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                    //ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void ModisPublishTask(string TifFile)
        {
            // uncomment
            //string layerName = Path.GetFileNameWithoutExtension(TifFile);
            //// store
            //string publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -POST -H \"Content-type: text/xml\"" +
            //    $" -d \"<coverageStore><name>{layerName}</name><type>GeoTIFF</type><enabled>true</enabled><workspace>{GeoServerWorkspace}</workspace><url>" +
            //    $"/data/{GeoServerWorkspace}/{layerName}.tif</url></coverageStore>\"" +
            //    $" {GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores?configure=all";
            //CurlBatExecute(publishParameters);
            //// layer
            //publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -PUT -H \"Content-type: text/xml\"" +
            //    $" -d \"<coverage><name>{layerName}</name><title>{layerName}</title><defaultInterpolationMethod><name>nearest neighbor</name></defaultInterpolationMethod></coverage>\"" +
            //    $" \"{GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores/{layerName}/coverages?recalculate=nativebbox\"";
            //CurlBatExecute(publishParameters);
            //// style
            //string[] a_layerName = layerName.Split('_');
            //string style = $"{a_layerName[1]}_{a_layerName[2]}_{a_layerName[3]}_{a_layerName[4]}";
            //publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -X PUT -H \"Content-type: text/xml\"" +
            //    $" -d \"<layer><defaultStyle><name>{style}</name></defaultStyle></layer>\"" +
            //    $" {GeoServerURL}rest/layers/{GeoServerWorkspace}:{layerName}.xml";
            //CurlBatExecute(publishParameters);
        }

        private static void Anomaly()
        {
            List<Task> taskList = new List<Task>();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.AnomalyStartYear != null)
                {
                    DateTime dateTime = GetNextDate();
                    if (dateTime.AddDays(-1).Year > modisProduct.AnomalyEndYear)
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileNameWithoutExtension(file).Contains("BASE") && !Path.GetFileNameWithoutExtension(file).Contains("Anomaly"))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnomalyTask(modisProduct, Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                //AnomalyTask(modisProduct, Path.Combine(GeoServerDir, Path.GetFileName(file)));
                            }
                        }
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
        }

        private static void AnomalyTask(ModisProduct ModisProduct, string TifFile)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(TifFile),
                anomalyFile = Path.Combine(GeoServerDir, Path.ChangeExtension(fileNameWithoutExtension + "_Anomaly", "tif")),
                baseFile = Path.Combine(GeoServerDir, Path.ChangeExtension($"ABASE{fileNameWithoutExtension.Substring(5)}", "tif")),
                letters = "ABCDEFGHIJKLMNOPQRSTUVWXY";
            if (File.Exists(anomalyFile))
            {
                return;
            }
            // check if base layer for anomaly calculation already exists, if no then try to create it
            string arguments = "";
            if (!File.Exists(baseFile))
            {
                //// check if base layers for base calculation already exist
                //bool baseExists = true;
                //for (int year = (int)ModisProduct.AnomalyStartYear; year <= (int)ModisProduct.AnomalyEndYear; year++)
                //{
                //    string baseYearFile = Path.Combine(GeoServerDir, Path.ChangeExtension(fileNameWithoutExtension.Remove(1, 4).Insert(1, year.ToString()), "tif"));
                //    if (!File.Exists(baseYearFile))
                //    {
                //        baseExists = false;
                //        break;
                //    }
                //}
                //if (!baseExists)
                //{
                //    return;
                //}

                // create base file to day
                int yearsCount = 0;
                for (int year = (int)ModisProduct.AnomalyStartYear; year <= (int)ModisProduct.AnomalyEndYear; year++)
                {
                    int letterIndex = year - (int)ModisProduct.AnomalyStartYear;
                    string baseYearFile = Path.ChangeExtension(fileNameWithoutExtension.Remove(1, 4).Insert(1, year.ToString()), "tif");
                    if (File.Exists(Path.Combine(GeoServerDir, baseYearFile)))
                    {
                        arguments += $" -{letters[letterIndex]} {Path.GetFileName(baseYearFile)}";
                        yearsCount++;
                    }
                }
                arguments += $" --outfile={Path.GetFileName(baseFile)}";
                arguments += $" --calc=\"((";
                for (int year = (int)ModisProduct.AnomalyStartYear; year <= (int)ModisProduct.AnomalyEndYear; year++)
                {
                    int letterIndex = year - (int)ModisProduct.AnomalyStartYear;
                    arguments += $"{letters[letterIndex]}+";
                }
                arguments = arguments.Remove(arguments.Length - 1);
                arguments += $")/{yearsCount})\"";
                GDALExecute(
                    "gdal_calc.py",
                    GeoServerDir,
                    arguments);
            }
            // calculate
            //arguments = "--co COMPRESS=LZW";
            arguments = $" -{letters[0]} {Path.GetFileName(baseFile)}";
            arguments += $" -{letters[1]} {Path.GetFileName(TifFile)} ";
            arguments += $"--outfile={Path.GetFileName(anomalyFile)} ";
            arguments += $"--calc=\"(B-A)*0.1\"";
            GDALExecute(
                "gdal_calc.py",
                GeoServerDir,
                arguments);

            // uncomment
            //// publish
            //string layerName = Path.GetFileNameWithoutExtension(anomalyFile);
            //// store
            //string publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -POST -H \"Content-type: text/xml\"" +
            //    $" -d \"<coverageStore><name>{layerName}</name><type>GeoTIFF</type><enabled>true</enabled><workspace>{GeoServerWorkspace}</workspace><url>" +
            //    $"/data/{GeoServerWorkspace}/{layerName}.tif</url></coverageStore>\"" +
            //    $" {GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores?configure=all";
            //CurlBatExecute(publishParameters);
            //// layer
            //publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -PUT -H \"Content-type: text/xml\"" +
            //    $" -d \"<coverage><name>{layerName}</name><title>{layerName}</title><defaultInterpolationMethod><name>nearest neighbor</name></defaultInterpolationMethod></coverage>\"" +
            //    $" \"{GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores/{layerName}/coverages?recalculate=nativebbox\"";
            //CurlBatExecute(publishParameters);
            //// style
            //string[] a_layerName = layerName.Split('_');
            //string style = $"{a_layerName[1]}_{a_layerName[2]}_{a_layerName[3]}_{a_layerName[4]}_{a_layerName[7]}";
            //publishParameters = $" -v -u" +
            //    $" {GeoServerUser}:{GeoServerPassword}" +
            //    $" -X PUT -H \"Content-type: text/xml\"" +
            //    $" -d \"<layer><defaultStyle><name>{style}</name></defaultStyle></layer>\"" +
            //    $" {GeoServerURL}rest/layers/{GeoServerWorkspace}:{layerName}.xml";
            //CurlBatExecute(publishParameters);
        }

        private static void Clouds()
        {
            CloudsPairs();
            CloudsMasks();
        }

        private static void CloudsPairs()
        {
            ModisProduct[] modisProductsDaily = modisProducts.Where(m => m.Period == 1).ToArray();
            List<Task> taskList = new List<Task>();
            if (modisProductsDaily.Count() == 2)
            {
                foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProductsDaily[0].Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                {
                    string pairFile = file
                            .Replace(modisProductsDaily[0].Source.Split('/')[1], modisProductsDaily[1].Source.Split('/')[1])
                            .Replace(modisProductsDaily[0].Product.Replace(".", ""), modisProductsDaily[1].Product.Replace(".", "")),
                        cloudFile = file
                            .Replace(modisProductsDaily[0].Source.Split('/')[1], cloudsMaskSourceName);
                    if (File.Exists(pairFile))
                    {
                        taskList.Add(Task.Factory.StartNew(() => CloudsPairsTask(file, pairFile, cloudFile)));
                    }
                    else
                    {
                        File.Move(file, cloudFile);
                    }
                }
                Task.WaitAll(taskList.ToArray());
                taskList = new List<Task>();
                foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProductsDaily[1].Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                {
                    string pairFile = file
                            .Replace(modisProductsDaily[1].Source.Split('/')[1], modisProductsDaily[0].Source.Split('/')[1])
                            .Replace(modisProductsDaily[1].Product.Replace(".", ""), modisProductsDaily[0].Product.Replace(".", "")),
                        cloudFile = file
                            .Replace(modisProductsDaily[1].Source.Split('/')[1], cloudsMaskSourceName);
                    if (File.Exists(pairFile))
                    {
                        taskList.Add(Task.Factory.StartNew(() => CloudsPairsTask(file, pairFile, cloudFile)));
                    }
                    else
                    {
                        File.Move(file, cloudFile);
                    }
                }
                Task.WaitAll(taskList.ToArray());
            }
        }

        private static void CloudsMasks()
        {
            List<Task> taskList = new List<Task>();
            foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceName}*.tif", SearchOption.TopDirectoryOnly))
            {
                string yearS = Path.GetFileName(file).Substring(1, 4),
                    dayS = Path.GetFileName(file).Substring(5, 3);
                int day = Convert.ToInt32(dayS),
                    year = Convert.ToInt32(yearS),
                    dayPrev = day - 1,
                    yearPrev = year,
                    dayNext = day + 1,
                    yearNext = year;
                if (dayPrev == 0)
                {
                    yearPrev--;
                    if (yearPrev % 4 == 0)
                    {
                        dayPrev = 366;
                    }
                    else
                    {
                        dayPrev = 365;
                    }
                }
                if ((day == 366 && yearNext % 4 == 0) || (day == 366 && yearNext % 4 != 0))
                {
                    dayNext = 1;
                    yearNext++;
                }
                string dayPrevS = dayPrev.ToString("D3"),
                    yearPrevS = yearPrev.ToString(),
                    dayNextS = dayNext.ToString("D3"),
                    yearNextS = yearNext.ToString(),
                    filePrev = file.Replace($"A{yearS}{dayS}", $"A{yearPrevS}{dayPrevS}"),
                    fileNext = file.Replace($"A{yearS}{dayS}", $"A{yearNextS}{dayNextS}");
                if (File.Exists(filePrev) && File.Exists(fileNext) && !File.Exists(file.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName)))
                {
                    taskList.Add(Task.Factory.StartNew(() => CloudsMasksTask(filePrev, file, fileNext)));
                    //CloudsMasksTask(filePrev, file, fileNext);
                }
                //else if (!File.Exists(file.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName)))
                //{
                //    File.Copy(file, file.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName));
                //}
                // delete old CLOU files
                DateTime date = new DateTime(year, 1, 1).AddDays(day - 1);
                if (((GetNextDate() - date).Days > 3) && (File.Exists(file.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName))))
                {
                    File.Delete(file);
                }
            }
            Task.WaitAll(taskList.ToArray());
            //foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceName}*.tif", SearchOption.TopDirectoryOnly))
            //{
            //    File.Delete(file);
            //}
            foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*_RGB*.tif", SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
            }
            foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*_SegmentCloud*.tif", SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
            }
        }
        
        private static void CloudsPairsTask(string TifFile1, string TifFile2, string TifFileCloud)
        {
            string arguments = $"-A '{TifFile1}' -B '{TifFile2}' --outfile='{TifFileCloud}' --calc=\"A*(A<250) + B*(A==250)\"";
            try
            {
                GDALExecute(
                    "gdal_calc.py",
                    GeoServerDir,
                    arguments);
                File.Delete(TifFile1);
                File.Delete(TifFile2);
            }
            catch
            { 
            
            }
        }

        private static void CloudsMasksTask(string TifFilePrev, string TifFileToday, string TifFileNext)
        {
            string arguments = $"{CloudMask}" +
                $" \"{Path.GetFileName(TifFilePrev)}\"" +
                $" \"{Path.GetFileName(TifFileToday)}\"" +
                $" \"{Path.GetFileName(TifFileNext)}\"" +
                $" \"{Path.GetFileNameWithoutExtension(TifFileToday.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName))}\"";
            //if (File.Exists(TifFileToday.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName)))
            //{
            //    File.Delete(TifFileToday.Replace(cloudsMaskSourceName, cloudsMaskSourceFinalName));
            //}
            if (Path.GetFileName(TifFileToday) == "A2000056_CLOU_MOD10A1006_B00_NDSISnowCover_4326.tif" ||
                Path.GetFileName(TifFileToday) == "A2000057_CLOU_MOD10A1006_B00_NDSISnowCover_4326.tif" ||
                Path.GetFileName(TifFileToday) == "A2000149_CLOU_MOD10A1006_B00_NDSISnowCover_4326.tif" ||
                Path.GetFileName(TifFileToday) == "A2000150_CLOU_MOD10A1006_B00_NDSISnowCover_4326.tif" ||
                Path.GetFileName(TifFileToday) == "A2000151_CLOU_MOD10A1006_B00_NDSISnowCover_4326.tif")
            {
                return;
            }
            try
            {
                GDALExecute(
                    "python",
                    GeoServerDir,
                    arguments);
            }
            catch
            {

            }
        }

        private static void Analize()
        {
            List<Task> taskList = new List<Task>();
            pointDatas.Clear();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.Analize)
                {
                    if (modisProduct.Period == 1)
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceFinalName}_{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                            }
                        }
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                if (((GetNextDate() - GetTifDate(file)).Days > 3) && (!File.Exists(file.Replace(modisProduct.Product.Split('.')[0], cloudsMaskSourceFinalName))))
                                {
                                    taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                            }
                        }
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
            //using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                StringBuilder text = new StringBuilder();
                List<PointData> pointModis = new List<PointData>();
                foreach (PointData pointData in pointDatas)
                {
                    //string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
                    //    $"{pointData.pointid}, " +
                    //    $"'{pointData.product}', " +
                    //    $"'{pointData.dataset}', " +
                    //    $"'{pointData.date.ToString("yyyy-MM-dd")}', " +
                    //    $"{pointData.value});";
                    //connection.Execute(query);

                    text.Append($"{pointData.pointid}\t" +
                        $"'{pointData.product}'\t" +
                        $"'{pointData.dataset}'\t" +
                        $"'{pointData.date.ToString("yyyy-MM-dd")}'\t" +
                        $"{pointData.value}" + Environment.NewLine);
                }
                File.AppendAllText(Path.Combine(BuferFolder, "modispoints.txt"), text.ToString());
                CopyToDb($"COPY public.modispoints (pointid, product, dataset, date, value) FROM '{Path.Combine(BuferFolder, "modispoints.txt")}' DELIMITER E'\\t';");
                File.Delete(Path.Combine(BuferFolder, "modispoints.txt"));

                List<SnowData> pointSnowInsert = new List<SnowData>(),
                    pointSnowUpdate = new List<SnowData>();
                //foreach (PointData pointModis_date in pointDatas)
                for (long i = 0; i < pointDatas.Count(); i++)
                {
                    PointData pointModis_date = pointDatas[(int)i];
                    bool snow = false;
                    SnowData _snowData = pointSnowInsert.FirstOrDefault(p => p.pointid == pointModis_date.pointid && p.date == pointModis_date.date);
                    if (_snowData != null)
                    {
                        if (_snowData.snow)
                        {
                            continue;
                        }
                    }
                    if (pointModis_date.product == "MOD10A1006" && pointModis_date.dataset == "NDSISnowCover" && pointModis_date.value <= 100)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MYD10A1006" && pointModis_date.dataset == "NDSISnowCover" && pointModis_date.value <= 100)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MOD10A2006" && pointModis_date.dataset == "MaxSnowExtent" && pointModis_date.value == 200)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MOD10A2006" && pointModis_date.dataset == "SnowCover" && pointModis_date.value == 1)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MYD10A2006" && pointModis_date.dataset == "MaxSnowExtent" && pointModis_date.value == 200)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MYD10A2006" && pointModis_date.dataset == "SnowCover" && pointModis_date.value == 1)
                    {
                        snow = true;
                    }
                    else if (pointModis_date.product == "MOD10C2006" && pointModis_date.dataset == "NDSI" && pointModis_date.value <= 100)
                    {
                        snow = true;
                    }

                    if (_snowData == null)
                    {
                        pointSnowInsert.Add(new SnowData()
                        {
                            date = pointModis_date.date,
                            pointid = pointModis_date.pointid,
                            snow = snow
                        });
                    }
                    else if (_snowData.snow == false && snow == true)
                    {
                        pointSnowUpdate.Add(new SnowData()
                        {
                            date = pointModis_date.date,
                            pointid = pointModis_date.pointid,
                            snow = snow
                        });
                    }
                }
                string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
                using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
                {
                    connection.Open();
                    foreach (SnowData snowData in pointSnowUpdate)
                    {
                        string query = $"UPDATE public.modispointssnow" +
                            $" SET pointid={snowData.pointid}, date='{snowData.date.ToString("yyyy-MM-dd")}', snow={snowData.snow.ToString()}" +
                            $" WHERE pointid={snowData.pointid} AND date='{snowData.date.ToString("yyyy-MM-dd")}';";
                        connection.Execute(query);
                    }
                    connection.Close();
                }
                StringBuilder text2 = new StringBuilder();
                foreach (SnowData pointSnow in pointSnowInsert)
                {
                    text2.Append($"{pointSnow.pointid}\t" +
                        $"'{pointSnow.date.ToString("yyyy-MM-dd")}'\t" +
                        $"'{pointSnow.snow}" + Environment.NewLine);
                }

                pointDatas.Clear();
            }
            //List<string> pointDataS = new List<string>();
            //foreach (var pointdata in pointDatas)
            //{
            //    pointDataS.Add($"{pointdata.pointid}\t" +
            //        $"{pointdata.product}\t" +
            //        $"{pointdata.dataset}\t" +
            //        $"{pointdata.date.ToString("yyyy-MM-dd")}\t" +
            //        $"{pointdata.value}");
            //}
            //File.AppendAllLines(@"D:\MODIS\modispoints.txt", pointDataS);
        }

        private static void AnalizeV2()
        {
            List<Task> taskList = new List<Task>();
            pointDatas.Clear();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.Analize)
                {
                    if (modisProduct.Period == 1)
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceFinalName}_{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                            }
                        }
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                if (((GetNextDate() - GetTifDate(file)).Days > 3) && (!File.Exists(file.Replace(modisProduct.Product.Split('.')[0], cloudsMaskSourceFinalName))))
                                {
                                    taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE"))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                            }
                        }
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());

            string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
            while (pointDatas2.TryTake(out _)) { }
            List<int> pointIds = new List<int>();
            using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
            {
                connection.Open();
                // id точек
                pointIds = connection.Query<int>($"SELECT pointid FROM public.watershedsilebasinpnt20201230;").ToList();
                connection.Close();
            }
            List<Task> taskList2 = new List<Task>();
            var pointDatasGrouped = pointDatas
                .GroupBy(p => p.pointid)
                .ToList();
            foreach (IGrouping<int, PointData> gr in pointDatasGrouped)
            {
                taskList2.Add(Task.Factory.StartNew(() => AnalizeTask2(
                    gr.ToList()
                )));
            }
            Task.WaitAll(taskList2.ToArray());

            StringBuilder text = new StringBuilder();
            foreach (PointData pointData_ in pointDatas2)
            {
                text.Append($"{pointData_.pointid}\t" +
                    $"'{pointData_.date.ToString("yyyy-MM-dd")}'\t" +
                    //$"{pointData_.MOD10A1006_NDSISnowCover}\t" +
                    //$"{pointData_.MYD10A1006_NDSISnowCover}\t" +
                    //$"{pointData_.MOD10A2006_MaxSnowExtent}\t" +
                    //$"{pointData_.MOD10A2006_SnowCover}\t" +
                    //$"{pointData_.MYD10A2006_MaxSnowExtent}\t" +
                    //$"{pointData_.MYD10A2006_SnowCover}\t" +
                    //$"{pointData_.MOD10C2006_NDSI}\t" +
                    $"{pointData_.snow}" + Environment.NewLine);
            }
            File.AppendAllText(Path.Combine(BuferFolder, "modispoints.txt"), text.ToString());
            CopyToDb($"COPY public.modispoints (pointid, date, snow) FROM '{Path.Combine(BuferFolder, "modispoints.txt")}' DELIMITER E'\\t';");
            File.Delete(Path.Combine(BuferFolder, "modispoints.txt"));

            pointDatas.Clear();
            while(pointDatas2.TryTake(out _)) { }
        }

        private static void AnalizeTask(string TifFile)
        {
            List<PointData> pointDatasTask = new List<PointData>();

            string[] TifFileArray = Path.GetFileNameWithoutExtension(TifFile).Split('_');
            string product = TifFileArray[2],
                dataset = TifFileArray[4];
            ModisProduct modisProduct = modisProducts.FirstOrDefault(m => m.Product.Replace(".", "") == product);
            int datasetIndex = -1;
            for (int i = 0; i < modisProduct.DataSets.Count(); i++)
            {
                if (modisProduct.DataSets[i] == dataset)
                {
                    datasetIndex = i;
                    break;
                }
            }
            DateTime dateFinish = GetTifDate(TifFile);
            using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                bool opened = false;
                while (!opened)
                {
                    try
                    {
                        connection.Open();
                        opened = true;
                    }
                    catch { }
                }
                string query = $"SELECT COUNT(*) FROM public.modispoints WHERE" +
                    //$" product = '{product}' AND" +
                    //$" dataset = '{dataset}' AND" +
                    $" date = '{dateFinish.ToString("yyyy-MM-dd")}';";
                var count = connection.Query<long>(query, commandTimeout: 60 * 60).FirstOrDefault();
                connection.Close();
                if (count > 0)
                {
                    return;
                }
            }

            string arguments = $"{ExtractRasterValueByPoint}" +
                $" \"{TifFile}\"" +
                $" \"{AnalizeShp}\"" +
                $" pointid",
            output = GDALExecute(
                "python",
                GeoServerDir,
                arguments);
            bool dataStart = false;
            string[] outputLines = output.Split(Environment.NewLine);
            List<string> data = new List<string>();
            for (int i = 0; i < outputLines.Count(); i++)
            {
                if (string.IsNullOrEmpty(outputLines[i]))
                {
                    dataStart = false;
                }
                if (dataStart)
                {
                    data.Add(outputLines[i]);
                }
                if (outputLines[i].Contains("Date") && outputLines[i].Contains("pointid") && outputLines[i].Contains("Values"))
                {
                    dataStart = true;
                }
            }
            foreach (string line in data)
            {
                int pointid = Convert.ToInt32(line.Split(' ')[1].Replace(",", "").Replace(")", ""));
                byte value = Convert.ToByte(line.Split(' ')[2].Replace(",", "").Replace(")", ""));
                if (modisProduct.DayDividedDataSetIndexes.Contains(datasetIndex))
                {
                    BitArray bits = new BitArray(new byte[] { value }); //new BitArray(BitConverter.GetBytes(value).ToArray());
                    for (int d = 0; d < bits.Count; d++)
                    {
                        DateTime date = dateFinish.AddDays(d - bits.Count + 1);
                        int valuei = Convert.ToInt32(bits[d]);
                        pointDatasTask.Add(new PointData()
                        {
                            pointid = pointid,
                            product = product,
                            dataset = dataset,
                            date = date,
                            value = valuei,
                            MOD10A1006_NDSISnowCover = -1,
                            MYD10A1006_NDSISnowCover = -1,
                            MOD10A2006_MaxSnowExtent = -1,
                            MOD10A2006_SnowCover = -1,
                            MYD10A2006_MaxSnowExtent = -1,
                            MYD10A2006_SnowCover = -1,
                            MOD10C2006_NDSI = -1,
                            snow = false
                        });
                    }
                }
                else
                {
                    pointDatasTask.Add(new PointData()
                    {
                        pointid = pointid,
                        product = product,
                        dataset = dataset,
                        date = dateFinish,
                        value = value,
                        MOD10A1006_NDSISnowCover = -1,
                        MYD10A1006_NDSISnowCover = -1,
                        MOD10A2006_MaxSnowExtent = -1,
                        MOD10A2006_SnowCover = -1,
                        MYD10A2006_MaxSnowExtent = -1,
                        MYD10A2006_SnowCover = -1,
                        MOD10C2006_NDSI = -1,
                        snow = false
                    });
                }
            }

            pointDatas.AddRange(pointDatasTask);
        }

        private static void AnalizeTask2(List<PointData> pointDatasPoint)
        {
            // pointDatasPoint(all data)
            // =>
            // pointDatasTask(output)

            using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                connection.Open();

                // pointDatasPoint - добавить данные за последние 8 дней с базы по точке
                DateTime dateTimeLast = pointDatasPoint.Max(p => p.date);
                string query = $"SELECT pointid, date, snow" +
                    $" FROM public.modispoints" +
                    $" WHERE pointid = {pointDatasPoint.FirstOrDefault().pointid}" +
                    $" AND date > '{dateTimeLast.AddDays(-8).ToString("yyyy-MM-dd")}';";
                var pointDatasPointDB = connection.Query<PointData>(query);
                pointDatasPoint.AddRange(pointDatasPointDB.ToList());

                // удалить данные с базы за последние 8 дней по точке
                query = $"DELETE FROM public.modispoints" +
                    $" WHERE pointid = {pointDatasPoint.FirstOrDefault().pointid}" +
                    $" AND date > '{dateTimeLast.AddDays(-8).ToString("yyyy-MM-dd")}';";
                connection.Execute(query);

                connection.Close();
            }

            List<PointData> pointDatasTask = new List<PointData>();
            foreach(PointData pointData in pointDatasPoint)
            {
                PointData pointDataExist = pointDatasTask.FirstOrDefault(p => p.date == pointData.date);
                if (pointDataExist == null)
                {
                    if (pointData.snow)
                    {
                        pointDatasTask.Add(new PointData()
                        {
                            pointid = pointData.pointid,
                            date = pointData.date,
                            snow = pointData.snow
                        });
                    }
                    else
                    {
                        switch ($"{pointData.product}_{pointData.dataset}")
                        {
                            case "MOD10A1006_NDSISnowCover":
                                pointData.MOD10A1006_NDSISnowCover = pointData.value;
                                break;
                            case "MYD10A1006_NDSISnowCover":
                                pointData.MYD10A1006_NDSISnowCover = pointData.value;
                                break;
                            case "MOD10A2006_MaxSnowExtent":
                                pointData.MOD10A2006_MaxSnowExtent = pointData.value;
                                break;
                            case "MOD10A2006_SnowCover":
                                pointData.MOD10A2006_SnowCover = pointData.value;
                                break;
                            case "MYD10A2006_MaxSnowExtent":
                                pointData.MYD10A2006_MaxSnowExtent = pointData.value;
                                break;
                            case "MYD10A2006_SnowCover":
                                pointData.MYD10A2006_SnowCover = pointData.value;
                                break;
                            case "MOD10C2006_NDSI":
                                pointData.MOD10C2006_NDSI = pointData.value;
                                break;
                            default:
                                pointData.MOD10A1006_NDSISnowCover = -1;
                                pointData.MYD10A1006_NDSISnowCover = -1;
                                pointData.MOD10A2006_MaxSnowExtent = -1;
                                pointData.MOD10A2006_SnowCover = -1;
                                pointData.MYD10A2006_MaxSnowExtent = -1;
                                pointData.MYD10A2006_SnowCover = -1;
                                pointData.MOD10C2006_NDSI = -1;
                                break;
                        }
                        pointDatasTask.Add(new PointData()
                        {
                            pointid = pointData.pointid,
                            product = pointData.product,
                            dataset = pointData.dataset,
                            date = pointData.date,
                            value = pointData.value,
                            MOD10A1006_NDSISnowCover = pointData.MOD10A1006_NDSISnowCover,
                            MYD10A1006_NDSISnowCover = pointData.MYD10A1006_NDSISnowCover,
                            MOD10A2006_MaxSnowExtent = pointData.MOD10A2006_MaxSnowExtent,
                            MOD10A2006_SnowCover = pointData.MOD10A2006_SnowCover,
                            MYD10A2006_MaxSnowExtent = pointData.MYD10A2006_MaxSnowExtent,
                            MYD10A2006_SnowCover = pointData.MYD10A2006_SnowCover,
                            MOD10C2006_NDSI = pointData.MOD10C2006_NDSI,
                            snow = false
                        });
                    }
                }
                else
                // if pointDatasPoint[i].date already exists in pointDatasTask (date)
                {
                    switch ($"{pointData.product}_{pointData.dataset}")
                    {
                        case "MOD10A1006_NDSISnowCover":
                            pointDataExist.MOD10A1006_NDSISnowCover = pointData.value;
                            break;
                        case "MYD10A1006_NDSISnowCover":
                            pointDataExist.MYD10A1006_NDSISnowCover = pointData.value;
                            break;
                        case "MOD10A2006_MaxSnowExtent":
                            pointDataExist.MOD10A2006_MaxSnowExtent = pointData.value;
                            break;
                        case "MOD10A2006_SnowCover":
                            pointDataExist.MOD10A2006_SnowCover = pointData.value;
                            break;
                        case "MYD10A2006_MaxSnowExtent":
                            pointDataExist.MYD10A2006_MaxSnowExtent = pointData.value;
                            break;
                        case "MYD10A2006_SnowCover":
                            pointDataExist.MYD10A2006_SnowCover = pointData.value;
                            break;
                        case "MOD10C2006_NDSI":
                            pointDataExist.MOD10C2006_NDSI = pointData.value;
                            break;
                        // pointData from DB, not from raster
                        default:
                            //if (pointDataExist.MOD10A1006_NDSISnowCover == -1)
                            //{
                            //    pointDataExist.MOD10A1006_NDSISnowCover = pointData.MOD10A1006_NDSISnowCover;
                            //}
                            //if (pointDataExist.MYD10A1006_NDSISnowCover == -1)
                            //{
                            //    pointDataExist.MYD10A1006_NDSISnowCover = pointData.MYD10A1006_NDSISnowCover;
                            //}
                            //if (pointDataExist.MOD10A2006_MaxSnowExtent == -1)
                            //{
                            //    pointDataExist.MOD10A2006_MaxSnowExtent = pointData.MOD10A2006_MaxSnowExtent;
                            //}
                            //if (pointDataExist.MOD10A2006_SnowCover == -1)
                            //{
                            //    pointDataExist.MOD10A2006_SnowCover = pointData.MOD10A2006_SnowCover;
                            //}
                            //if (pointDataExist.MYD10A2006_MaxSnowExtent == -1)
                            //{
                            //    pointDataExist.MYD10A2006_MaxSnowExtent = pointData.MYD10A2006_MaxSnowExtent;
                            //}
                            //if (pointDataExist.MYD10A2006_SnowCover == -1)
                            //{
                            //    pointDataExist.MYD10A2006_SnowCover = pointData.MYD10A2006_SnowCover;
                            //}
                            //if (pointDataExist.MOD10C2006_NDSI == -1)
                            //{
                            //    pointDataExist.MOD10C2006_NDSI = pointData.MOD10C2006_NDSI;
                            //}
                            if (pointData.snow)
                            {
                                pointDataExist.snow = pointData.snow;
                            }
                            break;
                    }
                }
            }
            foreach(PointData pointDataTask in pointDatasTask)
            {
                if (pointDataTask.date == new DateTime(2000, 5, 2))
                {
                    int g = 0;
                }
                if (pointDataTask.MOD10A1006_NDSISnowCover > 0 && pointDataTask.MOD10A1006_NDSISnowCover <= 100 && pointDataTask.MOD10A1006_NDSISnowCover != -1)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MYD10A1006_NDSISnowCover > 0 && pointDataTask.MYD10A1006_NDSISnowCover <= 100 && pointDataTask.MYD10A1006_NDSISnowCover != -1)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MOD10A2006_MaxSnowExtent == 200)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MOD10A2006_SnowCover == 1)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MYD10A2006_MaxSnowExtent == 200)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MYD10A2006_SnowCover == 1)
                {
                    pointDataTask.snow = true;
                }
                else if (pointDataTask.MOD10C2006_NDSI > 0 && pointDataTask.MOD10C2006_NDSI <= 100 && pointDataTask.MOD10C2006_NDSI != -1)
                {
                    pointDataTask.snow = true;
                }

                pointDatas2.Add(pointDataTask);
            }
        }

        private static void Snow()
        {
            pointSnows.Clear();
            while (pointSnowsBlocking.TryTake(out _)) { }
            List<Task> taskList = new List<Task>();
            string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
            using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
            {
                connection.Open();
                // id точек
                List<int> pointIds = connection.Query<int>($"SELECT pointid FROM public.watershedsilebasinpnt20201230;").ToList();

                DateTime currentDate = GetNextDate();
                List<PointData> pointModis = connection.Query<PointData>($"SELECT pointid, product, dataset, date, value" +
                    $" FROM public.modispoints" +
                    $" ORDER BY date;").ToList();
                pointModis = pointModis.Where(p => (currentDate - p.date).Days < 30).ToList();
                List<SnowData> pointSnow = connection.Query<SnowData>($"SELECT pointid, date, snow" +
                    $" FROM public.modispointssnow" +
                    $" ORDER BY date;").ToList();

                connection.Close();
                foreach (int pointId in pointIds)
                {
                    //SnowTask(pointId);
                    taskList.Add(Task.Factory.StartNew(() => SnowTask(
                        pointId,
                        currentDate,
                        pointModis.Where(p => p.pointid == pointId).ToList(),
                        pointSnow.Where(p => p.pointid == pointId).ToList())));
                }
            }
            Task.WaitAll(taskList.ToArray());

            StringBuilder text = new StringBuilder();
            pointSnows = pointSnowsBlocking.ToList();
            foreach (SnowData pointSnow in pointSnows)
            {
                text.Append($"{pointSnow.pointid}\t" +
                    $"'{pointSnow.date.ToString("yyyy-MM-dd")}'\t" +
                    $"'{pointSnow.snow}" + Environment.NewLine);
            }
            while (pointSnowsBlocking.TryTake(out _)) { }
            pointSnows.Clear();

            //File.AppendAllText(@"E:\MODIS\modispointssnow.txt", text.ToString());
            File.AppendAllText(Path.Combine(BuferFolder, "modispointssnow.txt"), text.ToString());
            CopyToDb($"COPY public.modispointssnow (pointid, date, snow) FROM '{Path.Combine(BuferFolder, "modispointssnow.txt")}' DELIMITER E'\\t';");
            File.Delete(Path.Combine(BuferFolder, "modispointssnow.txt"));
        }

        //private class ModisData
        //{
        //    public int pointid;
        //    public string product;
        //    public string dataset;
        //    public DateTime date;
        //    public int value;
        //}

        private class SnowData
        {
            public int pointid;
            public DateTime date;
            public bool snow;
        }

        private static void SnowTask(int PointId,
            DateTime CurrentDate,
            List<PointData> PointModis,
            List<SnowData> PointSnow)
        {
            string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
            List<SnowData> pointSnowInsert = new List<SnowData>(),
                pointSnowUpdate = new List<SnowData>();
            foreach (DateTime _date in PointModis.Where(p => (CurrentDate - p.date).Days < 30).Select(p => p.date).Distinct())
            {
                List<PointData> pointModis_date = PointModis.Where(p => p.date == _date).ToList();
                bool snow = false;
                if (pointModis_date.Count(p => p.product == "MOD10A1006" && p.dataset == "NDSISnowCover" && p.value <= 100) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MYD10A1006" && p.dataset == "NDSISnowCover" && p.value <= 100) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MOD10A2006" && p.dataset == "MaxSnowExtent" && p.value == 200) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MOD10A2006" && p.dataset == "SnowCover" && p.value == 1) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MYD10A2006" && p.dataset == "MaxSnowExtent" && p.value == 200) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MYD10A2006" && p.dataset == "SnowCover" && p.value == 1) > 0)
                {
                    snow = true;
                }
                else if (pointModis_date.Count(p => p.product == "MOD10C2006" && p.dataset == "NDSI" && p.value <= 100) > 0)
                {
                    snow = true;
                }
                SnowData _snowData = PointSnow.FirstOrDefault(p => p.pointid == PointId && p.date == _date);
                if (_snowData == null)
                {
                    //pointSnowInsert.Add(new SnowData()
                    //{
                    //    date = _date,
                    //    pointid = PointId,
                    //    snow = snow
                    //});
                    pointSnowsBlocking.Add(new SnowData()
                    {
                        date = _date,
                        pointid = PointId,
                        snow = snow
                    });
                }
                else if (_snowData.snow == false && snow == true)
                {
                    pointSnowUpdate.Add(new SnowData()
                    {
                        date = _date,
                        pointid = PointId,
                        snow = snow
                    });
                }
            }
            //foreach (SnowData snowData in pointSnowInsert)
            //{
            //    string query = $"INSERT INTO public.modispointssnow(pointid, date, snow)" +
            //        $" VALUES ({snowData.pointid}," +
            //        $" '{snowData.date.ToString("yyyy-MM-dd")}'," +
            //        $" {snowData.snow.ToString()});";
            //    connection.Execute(query);
            //}

            //pointSnows.AddRange(pointSnowInsert);

            using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
            {
                connection.Open();
                foreach (SnowData snowData in pointSnowUpdate)
                {
                    string query = $"UPDATE public.modispointssnow" +
                        $" SET pointid={snowData.pointid}, date='{snowData.date.ToString("yyyy-MM-dd")}', snow={snowData.snow.ToString()}" +
                        $" WHERE pointid={snowData.pointid} AND date='{snowData.date.ToString("yyyy-MM-dd")}';";
                    connection.Execute(query);
                }
                connection.Close();
            }
                
        }

        private static void SnowPeriods()
        {
            // don't calc from June to September
            DateTime date = GetNextDate();
            if (date.Month > 5 && date.Month < 10)
            {
                return;
            }

            while (periods.TryTake(out _)) { }
            List<Task> taskList = new List<Task>();
            string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
            using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
            {
                connection.Open();
                // id точек
                List<int> pointIds = connection.Query<int>($"SELECT pointid FROM public.watershedsilebasinpnt20201230;").ToList();
                connection.Close();
                foreach (int pointId in pointIds)
                {
                    //SnowPeriodsTask(pointId);
                    taskList.Add(Task.Factory.StartNew(() => SnowPeriodsTask(pointId)));
                }
            }
            Task.WaitAll(taskList.ToArray());
            StringBuilder text = new StringBuilder();
            foreach (Period period in periods)
            {
                text.Append($"{period.pointid}\t" +
                    $"'{period.start.ToString("yyyy-MM-dd")}'\t" +
                    $"'{period.finish.ToString("yyyy-MM-dd")}'\t" +
                    $"{period.period}" + Environment.NewLine);
            }
            File.AppendAllText(Path.Combine(BuferFolder, "modispointsperiods.txt"), text.ToString());
            CopyToDb($"COPY public.modispointsperiods (pointid, start, finish, period) FROM '{Path.Combine(BuferFolder, "modispointsperiods.txt")}' DELIMITER E'\\t';");
            File.Delete(Path.Combine(BuferFolder, "modispointsperiods.txt"));

            while (periods.TryTake(out _)) { }
        }

        private class Period
        {
            public int pointid;
            public DateTime start;
            public DateTime finish;
            public int period;
        }

        private static void SnowPeriodsTask(int PointId)
        {
            DateTime date = GetNextDate();
            date = new DateTime(date.Year - 1, 7, 15);

            string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
            using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
            {
                List<Period> periodsTask = new List<Period>();
                connection.Open();
                string query = "";
                List<PointData> snows = connection.Query<PointData>($"SELECT pointid, date, snow" +
                    $" FROM public.modispoints" +
                    $" WHERE pointid = {PointId}" +
                    $" AND date > '{date.ToString("yyyy-MM-dd")}'" +
                    $" ORDER BY date;").ToList();
                // заполнить snows (пропущенные даты)
                if (snows.Count() == 0)
                {
                    connection.Close();
                    return;
                }
                for (DateTime d = snows.Min(s => s.date); d < snows.Max(s => s.date); d = d.AddDays(1))
                {
                    if (snows.Count(s => s.date == d) == 0)
                    {
                        snows.Add(new PointData()
                        {
                            pointid = PointId,
                            date = d,
                            snow = false
                        });
                        query = $"INSERT INTO public.modispoints(pointid, date, snow)" +
                            $" VALUES ({PointId}," +
                            $" '{d.ToString("yyyy-MM-dd")}'," +
                            $" {false.ToString()});";
                        connection.Execute(query);
                    }
                }
                // 3 дня без снега
                snows.Add(new PointData()
                {
                    pointid = PointId,
                    date = snows.Min(s => s.date).AddDays(-1),
                    snow = false
                });
                snows.Add(new PointData()
                {
                    pointid = PointId,
                    date = snows.Min(s => s.date).AddDays(-1),
                    snow = false
                });
                snows.Add(new PointData()
                {
                    pointid = PointId,
                    date = snows.Min(s => s.date).AddDays(-1),
                    snow = false
                });
                snows = snows.OrderBy(s => s.date).ToList();
                for (int i = 0; i < snows.Count() - 3; i++)
                {
                    if (!snows[i].snow || !snows[i + 1].snow || !snows[i + 2].snow)
                    {
                        continue;
                    }
                    for (int j = i + 2; j < snows.Count() - 3; j++)
                    {
                        if (!snows[j].snow &&
                            !snows[j + 1].snow
                            && !snows[j + 2].snow
                            && !snows[j + 3].snow)//?
                        {
                            int days = (int)(snows[j - 1].date - snows[i + 2].date).TotalDays + 1;
                            if (days >= 30)
                            {
                                periodsTask.Add(new Period()
                                {
                                    pointid = PointId,
                                    start = snows[i].date,
                                    finish = snows[j - 1].date,
                                    period = days
                                });
                            }
                            i = j + 1;
                            break;
                        }
                    }
                }
                // delete old periods (old data from modispointsperiods)
                query = $"DELETE FROM public.modispointsperiods" +
                    $" WHERE pointid = {PointId}" +
                    $" AND start > '{date.ToString("yyyy-MM-dd")}';";
                connection.Execute(query);
                // add new periods
                foreach (Period period in periodsTask)
                {
                    //query = $"INSERT INTO public.modispointsperiods" +
                    //    $"(pointid, start, finish, period)" +
                    //    $" VALUES ({period.pointid}, '{period.start.ToString("yyyy-MM-dd")}', '{period.finish.ToString("yyyy-MM-dd")}', {period.period});";
                    //connection.Execute(query);
                    periods.Add(period);
                }
                connection.Close();
            }
        }

        private static void AnalizeZonalStatRaster()
        {
            List<Task> taskList = new List<Task>();
            zonalStats.Clear();
            List<string> zonalstattiffsDB = new List<string>(),
                zonalstattiffsNew = new List<string>();
            using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                connection.Open();

                string query = $"SELECT name" +
                    $" FROM public.modiszonalstattiffs;";
                zonalstattiffsDB = connection.Query<string>(query).ToList();

                connection.Close();
            }

            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.ZonalStatRaster)
                {
                    if (modisProduct.Period == 1)
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceFinalName}_{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !zonalstattiffsDB.Contains(Path.GetFileName(file)))
                            {
                                //taskList.Add(Task.Factory.StartNew(() => AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                zonalstattiffsNew.Add(Path.GetFileName(file));
                                AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
                            }
                        }
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !zonalstattiffsDB.Contains(Path.GetFileName(file)))
                            {
                                if (((GetNextDate() - GetTifDate(file)).Days > 3) && (!File.Exists(file.Replace(modisProduct.Product.Split('.')[0], cloudsMaskSourceFinalName))))
                                {
                                    //taskList.Add(Task.Factory.StartNew(() => AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                    zonalstattiffsNew.Add(Path.GetFileName(file));
                                    AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !zonalstattiffsDB.Contains(Path.GetFileName(file)))
                            {
                                //taskList.Add(Task.Factory.StartNew(() => AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                zonalstattiffsNew.Add(Path.GetFileName(file));
                                AnalizeZonalStatRasterTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
                            }
                        }
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
            using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                connection.Open();
                foreach(string zonalstattiff in zonalstattiffsNew)
                {
                    string query = $"INSERT INTO public.modiszonalstattiffs(name) VALUES ('{zonalstattiff}');";
                    connection.Execute(query);
                }

                connection.Close();
            }

            StringBuilder text = new StringBuilder();
            foreach (ZonalStat zonalStat in zonalStats)
            {
                text.Append($"{zonalStat.shp}\t" +
                    $"{zonalStat.gridid}\t" +
                    $"'{zonalStat.date.ToString("yyyy-MM-dd")}'\t" +
                    $"{zonalStat.area}\t" +
                    $"{zonalStat.snow}\t" +
                    $"{zonalStat.nosnow}\t" +
                    $"{zonalStat.count}" + Environment.NewLine);
            }
            File.AppendAllText(Path.Combine(BuferFolder, "modiszonalstats.txt"), text.ToString());
            CopyToDb($"COPY public.modiszonalstats (shp, gridid, date, area, snow, nosnow, count) FROM '{Path.Combine(BuferFolder, "modiszonalstats.txt")}' DELIMITER E'\\t';");
            File.Delete(Path.Combine(BuferFolder, "modiszonalstats.txt"));
            zonalStats.Clear();
        }

        private static void AnalizeZonalStatRasterTask(string TifFile)
        {
            List<ZonalStat> zonalStatsTask = new List<ZonalStat>();
            DateTime date = GetTifDate(TifFile);
            string raster = Path.GetFileNameWithoutExtension(TifFile);
            string[] TifFileArray = raster.Split('_');
            string product = TifFileArray[2],
                dataset = TifFileArray[4];
            ModisProduct modisProduct = modisProducts.FirstOrDefault(m => m.Product.Replace(".", "") == product);
            int datasetIndex = -1;
            for (int i = 0; i < modisProduct.DataSets.Count(); i++)
            {
                if (modisProduct.DataSets[i] == dataset)
                {
                    datasetIndex = i;
                    break;
                }
            }
            foreach (string zonalShp in ZonalShps)
            {
                string arguments = $"{ZonalStatRaster}" +
                        $" \"{zonalShp}\"" +
                        $" \"{TifFile}\"",
                    output = GDALExecute("python",
                        GeoServerDir,
                        arguments),
                        shp = Path.GetFileNameWithoutExtension(zonalShp);
                string[] outputs = output.Split(Environment.NewLine);
                foreach (string s in outputs)
                {
                    if (s.Contains("OrderedDict"))
                    {
                        ZonalStat zonalStat = new ZonalStat();
                        zonalStat.date = date;
                        zonalStat.shp = shp;
                        zonalStat.snow = 0;
                        zonalStat.nosnow = 0;
                        zonalStat.count = 0;
                        string sm = s.Replace("OrderedDict([(", "");
                        sm = sm.Replace(")])", "").Replace("'", "");
                        string[] pairs = sm.Split("), (");
                        List<int> valuesList = new List<int>();
                        List<int> countsList = new List<int>();
                        foreach (string pair in pairs)
                        {
                            string[] values = pair.Split(", ");
                            int v0 = 0;
                            if (values[0] == "Area_sq_km")
                            {
                                zonalStat.area = Convert.ToDecimal(values[1]);
                            }
                            else if (values[0] == "GridID")
                            {
                                zonalStat.gridid = Convert.ToInt32(values[1]);
                            }
                            else if (values[0] == "count")
                            {
                                zonalStat.count = Convert.ToInt32(values[1]);
                            }
                            else if (int.TryParse(values[0], out v0))
                            {
                                valuesList.Add(v0);
                                countsList.Add(Convert.ToInt32(values[1]));
                            }
                        }
                        // load 8-day zonalStats from DB
                        List<ZonalStat> zonalStats1or8 = new List<ZonalStat>();
                        List<ZonalStat> zonalStatsDB = new List<ZonalStat>();
                        using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
                        {
                            connection.Open();

                            // modiszonalstats - добавить данные за последние 8 дней с базы по полигону
                            string query = $"SELECT shp, gridid, date, area, snow, nosnow, count" +
                                $" FROM public.modiszonalstats" +
                                $" WHERE shp = '{Path.GetFileNameWithoutExtension(zonalShp)}'" +
                                $" AND date > '{date.AddDays(-8).ToString("yyyy-MM-dd")}'" +
                                $" AND gridid = {zonalStat.gridid};";
                            var zonalStatDB = connection.Query<ZonalStat>(query);
                            zonalStatsDB.AddRange(zonalStatDB.ToList());

                            // удалить данные с базы за последние 8 дней по полигону
                            query = $"DELETE FROM public.modiszonalstats" +
                                $" WHERE shp = '{Path.GetFileNameWithoutExtension(zonalShp)}'" +
                                $" AND date > '{date.AddDays(-8).ToString("yyyy-MM-dd")}'" +
                                $" AND gridid = {zonalStat.gridid};";
                            connection.Execute(query);

                            connection.Close();
                        }
                        if (modisProduct.DayDividedDataSetIndexes.Contains(datasetIndex))
                        {
                            for (int i = 0; i < valuesList.Count(); i++)
                            {
                                BitArray bits = new BitArray(new byte[] { (byte)valuesList[i] });
                                for (int d = 0; d < bits.Count; d++)
                                {
                                    DateTime date8 = date.AddDays(d - bits.Count + 1);
                                    int valuei = Convert.ToInt32(bits[d]);
                                    bool snow = SnowOrNo(product, dataset, valuei);
                                    if (!zonalStats1or8.Exists(z => z.date == date8))
                                    {
                                        zonalStats1or8.Add(new ZonalStat()
                                        {
                                            area = zonalStat.area,
                                            date = date8,
                                            gridid = zonalStat.gridid,
                                            shp = zonalStat.shp = shp,
                                            snow = snow ? countsList[i] : 0,
                                            nosnow = !snow ? countsList[i] : 0,
                                            count = zonalStat.count
                                        });
                                    }
                                    else
                                    {
                                        ZonalStat zonalStat8 = zonalStats1or8.FirstOrDefault(z => z.date == date8);
                                        if (snow)
                                        {
                                            zonalStat8.snow += countsList[i];
                                        }
                                        else
                                        {
                                            zonalStat8.nosnow += countsList[i];
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < valuesList.Count(); i++)
                            {
                                if (SnowOrNo(product, dataset, valuesList[i]))
                                {
                                    zonalStat.snow += countsList[i];
                                }
                                else
                                {
                                    zonalStat.nosnow += countsList[i];
                                }
                            }
                            zonalStats1or8.Add(zonalStat);
                        }
                        zonalStatsTask.AddRange(zonalStatsDB);
                        zonalStatsTask.AddRange(zonalStats1or8);
                    }
                }
            }
            foreach (ZonalStat zonalStat1or8 in zonalStatsTask)
            {
                if (!zonalStats.Exists(z => z.date == zonalStat1or8.date &&
                    z.shp == zonalStat1or8.shp &&
                    z.gridid == zonalStat1or8.gridid))
                {
                    zonalStats.Add(zonalStat1or8);
                }
                else
                {
                    ZonalStat zonalStatExist = zonalStats.FirstOrDefault(z => z.date == zonalStat1or8.date &&
                        z.shp == zonalStat1or8.shp &&
                        z.gridid == zonalStat1or8.gridid);
                    if (zonalStat1or8.count >= zonalStatExist.count && zonalStat1or8.snowperc > zonalStatExist.snowperc)
                    {
                        zonalStatExist.snow = zonalStat1or8.snow;
                        zonalStatExist.nosnow = zonalStat1or8.nosnow;
                        zonalStatExist.count = zonalStat1or8.count;
                    }
                }
            }
        }

        private static bool SnowOrNo(
            string Product,
            string Dataset,
            int Value)
        {
            bool snow = false;
            switch ($"{Product}_{Dataset}")
            {
                case "MOD10A1006_NDSISnowCover":
                    if (Value > 0 && Value <= 100 && Value != -1)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MYD10A1006_NDSISnowCover":
                    if (Value > 0 && Value <= 100 && Value != -1)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MOD10A2006_MaxSnowExtent":
                    if (Value == 200)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MOD10A2006_SnowCover":
                    if (Value == 1)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MYD10A2006_MaxSnowExtent":
                    if (Value == 200)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MYD10A2006_SnowCover":
                    if (Value == 1)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
                case "MOD10C2006_NDSI":
                    if (Value > 0 && Value <= 100 && Value != -1)
                    {
                        snow = true;
                    }
                    else
                    {
                        snow = false;
                    }
                    break;
            }
            return snow;
        }

        //private static List<DateTime> starts = new List<DateTime>(),
        //            finishes = new List<DateTime>();
        //private static List<int> periods = new List<int>();
        //private static void SnowTask(int PointId)
        //{
        //    string GeoNodeWebModisConnection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres";
        //    using (var connection = new NpgsqlConnection(GeoNodeWebModisConnection))
        //    {
        //        connection.Open();
        //        // данные по точке
        //        List<ModisData> pointSnows = connection.Query<ModisData>($"SELECT date, value" +
        //            $" FROM public.modispoints" +
        //            $" WHERE pointid = {PointId}" +
        //            $" AND product = 'MOD10A2006'" +
        //            $" AND dataset = 'SnowCover'" +
        //            $" ORDER BY date;").ToList();
        //        connection.Close();

        //        for (int i = 0; i < pointSnows.Count() - 30; i++)
        //        {
        //            if(pointSnows[i].date == new DateTime(2010, 2, 11) || pointSnows[i].date == new DateTime(2017, 2, 3))
        //            {

        //            }
        //            if (pointSnows[i].value == 0 || pointSnows[i + 1].value == 0 || pointSnows[i + 2].value == 0)
        //            {
        //                continue;
        //            }
        //            for (int j = i; j < pointSnows.Count() - 3; j++)
        //            {
        //                if (pointSnows[j].value == 0 && pointSnows[j + 1].value == 0 && pointSnows[j + 2].value == 0 && pointSnows[j + 3].value == 0)
        //                {
        //                    int days = (int)(pointSnows[j - 1].date - pointSnows[i].date).TotalDays + 1;
        //                    if (days >= 30)
        //                    {
        //                        starts.Add(pointSnows[i].date);
        //                        finishes.Add(pointSnows[j - 1].date);
        //                        periods.Add(days);
        //                    }
        //                    i = j + 1;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}

        //private static DateTime GetStartDate(ModisProduct ModisProduct)
        //{
        //    DateTime StartDate = ModisProduct.StartDate;
        //    foreach (string file in Directory.EnumerateFiles(DownloadedDir, "*.hdf"))
        //    {
        //        if (GetFileProduct(Path.GetFileName(file)) == ModisProduct.Product)
        //        {
        //            if (GetFileDate(Path.GetFileName(file)) > StartDate)
        //            {
        //                StartDate = GetFileDate(Path.GetFileName(file));
        //            }
        //        }
        //    }
        //    if (StartDate > ModisProduct.StartDate)
        //    {
        //        StartDate = StartDate.AddDays(1);
        //    }
        //    if (StartDate > DateTime.Today)
        //    {
        //        StartDate = DateTime.Today;
        //    }
        //    return StartDate;
        //}

        //private static DateTime GetFinishDate(DateTime StartDate)
        //{
        //    DateTime FinishDate = StartDate.AddDays(10);
        //    if (FinishDate > DateTime.Today)
        //    {
        //        FinishDate = DateTime.Today;
        //    }
        //    return FinishDate;
        //}

        //private static DateTime GetHDFDate(string File)
        //{
        //    File = Path.GetFileName(File);
        //    string date = File.Split('.')[1].Replace("A", ""),
        //        s_year = date.Substring(0, 4),
        //        s_dayofyear = date.Substring(4, 3);
        //    int year = Convert.ToInt32(s_year),
        //        dayofyear = Convert.ToInt32(s_dayofyear);
        //    DateTime Date = new DateTime(year, 1, 1).AddDays(dayofyear - 1);
        //    return Date;
        //}

        private static string GetHDFDate(string File)
        {
            File = Path.GetFileName(File);
            return File.Split('.')[1].Replace("A", "");
        }

        private static string GetHDFProduct(string File)
        {
            File = Path.GetFileName(File);
            return $"{File.Split('.')[0]}.{File.Split('.')[3]}";
        }

        private static DateTime GetTifDate(string File)
        {
            string[] fileArray = Path.GetFileNameWithoutExtension(File).Split('_');
            string year = fileArray[0].Substring(1, 4),
                yearday = fileArray[0].Substring(5, 3);
            return new DateTime(Convert.ToInt32(year), 1, 1).AddDays(Convert.ToInt32(yearday) - 1);
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
