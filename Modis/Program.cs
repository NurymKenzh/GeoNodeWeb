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
                    return count > 0 ? (decimal)snow / count : 0;
                }
            }
            public decimal nosnowperc
            {
                get
                {
                    return count > 0 ? (decimal)nosnow / count : 0;
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

        const string ModisUser = "sandugash_2004",
            ModisPassword = "Arina2009",
            ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
            DownloadingDir = @"E:\MODIS\Downloading",
            DownloadedDir = @"E:\MODIS\Downloaded",
            Exclusions = @"E:\MODIS\exclusions.txt",
            CMDPath = @"C:\Windows\system32\cmd.exe",
            LastDateFile = "!last_date.txt",
            MosaicDir = @"E:\MODIS\Mosaic",
            ConvertDir = @"E:\MODIS\Convert",
            ArchiveDir = @"C:\MODIS\Archive",
            ModisProjection = "4326",
            GeoServerDir = @"E:\GeoServer\data_dir\data\MODIS",
            GeoServerWorkspace = "MODIS",
            GeoServerUser = "admin",
            GeoServerPassword = "geoserver",
            GeoServerURL = "http://localhost:8080/geoserver/",
            AnalizeShp = @"E:\MODIS\shp\WatershedsIleBasinPnt20201230.shp",
            ExtractRasterValueByPoint = @"E:\MODIS\Python\ExtractRasterValueByPoint.py",
            CloudMask = @"E:\MODIS\Python\CloudMask_v03.py",
            Connection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432;CommandTimeout=0;Keepalive=0;";

        //const string ModisUser = "hvreren",
        //    ModisPassword = "Querty123",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
        //    DownloadingDir = @"D:\MODIS\Downloading",
        //    DownloadedDir = @"D:\MODIS\Downloaded",
        //    Exclusions = @"D:\MODIS\exclusions.txt",
        //    CMDPath = @"C:\Windows\system32\cmd.exe",
        //    LastDateFile = "!last_date.txt",
        //    MosaicDir = @"D:\MODIS\Mosaic",
        //    ConvertDir = @"D:\MODIS\Convert",
        //    ArchiveDir = @"D:\MODIS\Archive",
        //    ModisProjection = "4326",
        //    GeoServerDir = @"D:\GeoServer\data_dir\data\MODIS",
        //    GeoServerWorkspace = "MODIS",
        //    GeoServerUser = "admin",
        //    GeoServerPassword = "geoserver",
        //    GeoServerURL = "http://localhost:8080/geoserver/",
        //    AnalizeShp = @"D:\MODIS\shp\WatershedsIleBasinPnt20201230.shp",
        //    ExtractRasterValueByPoint = @"D:\MODIS\Python\ExtractRasterValueByPoint.py",
        //    CloudMask = @"D:\MODIS\Python\CloudMask_v03.py",
        //    Connection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432;CommandTimeout=0;Keepalive=0;";

        //const string ModisUser = "hvreren",
        //    ModisPassword = "Querty123",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
        //    DownloadingDir = @"D:\MODISGNW\Downloading",
        //    DownloadedDir = @"D:\MODISGNW\Downloaded",
        //    Exclusions = @"D:\MODISGNW\exclusions.txt",
        //    CMDPath = @"C:\Windows\system32\cmd.exe",
        //    LastDateFile = "!last_date.txt",
        //    MosaicDir = @"D:\MODISGNW\Mosaic",
        //    ConvertDir = @"D:\MODISGNW\Convert",
        //    ArchiveDir = @"D:\MODISGNW\Archive",
        //    ModisProjection = "4326",
        //    GeoServerDir = @"D:\GeoServer\data_dir\data\MODISGNW",
        //    GeoServerWorkspace = "MODISGNW",
        //    GeoServerUser = "admin",
        //    GeoServerPassword = "geoserver",
        //    GeoServerURL = "http://localhost:8080/geoserver/",
        //    AnalizeShp = @"D:\MODISGNW\shp\WatershedsIleBasinPnt20201230.shp",
        //    ExtractRasterValueByPoint = @"D:\MODISGNW\Python\ExtractRasterValueByPoint.py",
        //    CloudMask = @"D:\MODISGNW\Python\CloudMask_v03.py",
        //    Connection = "Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432;CommandTimeout=0;Keepalive=0;";

        const string cloudsMaskSourceName = "CLOU",
            cloudsMaskSourceFinalName = "CLMA"; // CLOUD MASK

        static List<PointData> pointDatas = new List<PointData>();
        static BlockingCollection<PointData> pointDatas2 = new BlockingCollection<PointData>();
        static BlockingCollection<Period> periods = new BlockingCollection<Period>();

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
                //AnalizeZonalStatRaster();
                //Console.WriteLine("Press ESC to stop!");

                if (dateNext == DateTime.Today)
                {
                    Log("Sleep 4 hour");
                    Thread.Sleep(1000 * 60 * 60 * 4);
                }
                EmptyDownloadedDir();

                DateTime finish = DateTime.Now;
                TimeSpan duration = finish - start;
                File.AppendAllText(@"E:\MODIS\time.txt", $"{start}\t{finish}\t{dateNext.ToString("yyyy-MM-dd")}\t{duration}{Environment.NewLine}");
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
                Log($"{exception}: {exception?.InnerException}");
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
            if (dateLast == DateTime.Today)
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
            string layerName = Path.GetFileNameWithoutExtension(TifFile);
            // store
            string publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -POST -H \"Content-type: text/xml\"" +
                $" -d \"<coverageStore><name>{layerName}</name><type>GeoTIFF</type><enabled>true</enabled><workspace>{GeoServerWorkspace}</workspace><url>" +
                $"/data/{GeoServerWorkspace}/{layerName}.tif</url></coverageStore>\"" +
                $" {GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores?configure=all";
            CurlBatExecute(publishParameters);
            // layer
            publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -PUT -H \"Content-type: text/xml\"" +
                $" -d \"<coverage><name>{layerName}</name><title>{layerName}</title><defaultInterpolationMethod><name>nearest neighbor</name></defaultInterpolationMethod></coverage>\"" +
                $" \"{GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores/{layerName}/coverages?recalculate=nativebbox\"";
            CurlBatExecute(publishParameters);
            // style
            string[] a_layerName = layerName.Split('_');
            string style = $"{a_layerName[1]}_{a_layerName[2]}_{a_layerName[3]}_{a_layerName[4]}";
            publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -X PUT -H \"Content-type: text/xml\"" +
                $" -d \"<layer><defaultStyle><name>{style}</name></defaultStyle></layer>\"" +
                $" {GeoServerURL}rest/layers/{GeoServerWorkspace}:{layerName}.xml";
            CurlBatExecute(publishParameters);
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

            // publish
            string layerName = Path.GetFileNameWithoutExtension(anomalyFile);
            // store
            string publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -POST -H \"Content-type: text/xml\"" +
                $" -d \"<coverageStore><name>{layerName}</name><type>GeoTIFF</type><enabled>true</enabled><workspace>{GeoServerWorkspace}</workspace><url>" +
                $"/data/{GeoServerWorkspace}/{layerName}.tif</url></coverageStore>\"" +
                $" {GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores?configure=all";
            CurlBatExecute(publishParameters);
            // layer
            publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -PUT -H \"Content-type: text/xml\"" +
                $" -d \"<coverage><name>{layerName}</name><title>{layerName}</title><defaultInterpolationMethod><name>nearest neighbor</name></defaultInterpolationMethod></coverage>\"" +
                $" \"{GeoServerURL}rest/workspaces/{GeoServerWorkspace}/coveragestores/{layerName}/coverages?recalculate=nativebbox\"";
            CurlBatExecute(publishParameters);
            // style
            string[] a_layerName = layerName.Split('_');
            string style = $"{a_layerName[1]}_{a_layerName[2]}_{a_layerName[3]}_{a_layerName[4]}_{a_layerName[7]}";
            publishParameters = $" -v -u" +
                $" {GeoServerUser}:{GeoServerPassword}" +
                $" -X PUT -H \"Content-type: text/xml\"" +
                $" -d \"<layer><defaultStyle><name>{style}</name></defaultStyle></layer>\"" +
                $" {GeoServerURL}rest/layers/{GeoServerWorkspace}:{layerName}.xml";
            CurlBatExecute(publishParameters);
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

        private static void AnalizeV2()
        {
            List<Task> taskList = new List<Task>();
            pointDatas.Clear();
            List<string> modispointsrastersDB = new List<string>(),
                modispointsrastersNew = new List<string>();
            using (var connection = new NpgsqlConnection(Connection))
            {
                connection.Open();

                string query = $"SELECT name" +
                    $" FROM public.modispointsrasters;";
                modispointsrastersDB = connection.Query<string>(query).ToList();

                connection.Close();
            }
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.Analize)
                {
                    if (modisProduct.Period == 1)
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{cloudsMaskSourceFinalName}_{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !modispointsrastersDB.Contains(Path.GetFileName(file)))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                modispointsrastersNew.Add(Path.GetFileName(file));
                            }
                        }
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !modispointsrastersDB.Contains(Path.GetFileName(file)))
                            {
                                if (((GetNextDate() - GetTifDate(file)).Days > 3) && (!File.Exists(file.Replace(modisProduct.Product.Split('.')[0], cloudsMaskSourceFinalName))))
                                {
                                    taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                    modispointsrastersNew.Add(Path.GetFileName(file));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                        {
                            if (!Path.GetFileName(file).Contains("BASE") && !modispointsrastersDB.Contains(Path.GetFileName(file)))
                            {
                                taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                                modispointsrastersNew.Add(Path.GetFileName(file));
                            }
                        }
                    }
                }
            }
            using (var connection = new NpgsqlConnection(Connection))
            {
                connection.Open();
                foreach (string modispointsraster in modispointsrastersNew)
                {
                    string query = $"INSERT INTO public.modispointsrasters(name) VALUES ('{modispointsraster}');";
                    connection.Execute(query);
                }

                connection.Close();
            }
            Task.WaitAll(taskList.ToArray());

            while (pointDatas2.TryTake(out _)) { }
            List<int> pointIds = new List<int>();
            using (var connection = new NpgsqlConnection(Connection))
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

            //StringBuilder text = new StringBuilder();
            //foreach (PointData pointData_ in pointDatas2)
            //{
            //    text.Append($"{pointData_.pointid}\t" +
            //        $"'{pointData_.date.ToString("yyyy-MM-dd")}'\t" +
            //        //$"{pointData_.MOD10A1006_NDSISnowCover}\t" +
            //        //$"{pointData_.MYD10A1006_NDSISnowCover}\t" +
            //        //$"{pointData_.MOD10A2006_MaxSnowExtent}\t" +
            //        //$"{pointData_.MOD10A2006_SnowCover}\t" +
            //        //$"{pointData_.MYD10A2006_MaxSnowExtent}\t" +
            //        //$"{pointData_.MYD10A2006_SnowCover}\t" +
            //        //$"{pointData_.MOD10C2006_NDSI}\t" +
            //        $"{pointData_.snow}" + Environment.NewLine);
            //}
            //File.AppendAllText(Path.Combine(BuferFolder, "modispoints.txt"), text.ToString());
            //CopyToDb($"COPY public.modispoints (pointid, date, snow) FROM '{Path.Combine(BuferFolder, "modispoints.txt")}' DELIMITER E'\\t';");
            //File.Delete(Path.Combine(BuferFolder, "modispoints.txt"));
            if (pointDatas2.Count() > 0)
            {
                StringBuilder text = new StringBuilder();
                text.Append($"INSERT INTO public.modispoints(pointid, date, snow) VALUES ");
                foreach (PointData pointData_ in pointDatas2)
                {
                    text.Append($"({pointData_.pointid}, '{pointData_.date.ToString("yyyy-MM-dd")}', {pointData_.snow})," + Environment.NewLine);
                }
                text.Length--;
                text.Length--;
                text.Length--;
                text.Append(";");
                using (var connection = new NpgsqlConnection(Connection))
                {
                    connection.Open();
                    connection.Execute(text.ToString());
                    connection.Close();
                }
            }

            pointDatas.Clear();
            while (pointDatas2.TryTake(out _)) { }
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
            using (var connection = new NpgsqlConnection(Connection))
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

            using (var connection = new NpgsqlConnection(Connection))
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
            foreach (PointData pointData in pointDatasPoint)
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
            foreach (PointData pointDataTask in pointDatasTask)
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

        private class SnowData
        {
            public int pointid;
            public DateTime date;
            public bool snow;
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
            using (var connection = new NpgsqlConnection(Connection))
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
            //foreach (Period period in periods)
            //{
            //    text.Append($"{period.pointid}\t" +
            //        $"'{period.start.ToString("yyyy-MM-dd")}'\t" +
            //        $"'{period.finish.ToString("yyyy-MM-dd")}'\t" +
            //        $"{period.period}" + Environment.NewLine);
            //}
            //File.AppendAllText(Path.Combine(BuferFolder, "modispointsperiods.txt"), text.ToString());
            //CopyToDb($"COPY public.modispointsperiods (pointid, start, finish, period) FROM '{Path.Combine(BuferFolder, "modispointsperiods.txt")}' DELIMITER E'\\t';");
            //File.Delete(Path.Combine(BuferFolder, "modispointsperiods.txt"));
            text.Append($"INSERT INTO public.modispointsperiods(pointid, start, finish, period) VALUES ");
            foreach (Period period in periods)
            {
                text.Append($"({period.pointid}, " +
                    $"'{period.start.ToString("yyyy-MM-dd")}', " +
                    $"'{period.finish.ToString("yyyy-MM-dd")}', " +
                    $"{period.period})," + 
                    Environment.NewLine);
            }
            text.Length--;
            text.Length--;
            text.Length--;
            text.Append(";");
            using (var connection = new NpgsqlConnection(Connection))
            {
                connection.Open();
                connection.Execute(text.ToString());
                connection.Close();
            }

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

            using (var connection = new NpgsqlConnection(Connection))
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