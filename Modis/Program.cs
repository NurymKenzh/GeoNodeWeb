using Dapper;
using Microsoft.VisualBasic.CompilerServices;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
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
            public int? AnomalyStartYear;
            public int? AnomalyEndYear;
            public bool Analize = false;
            public int[] DayDividedDataSetIndexes;
        }

        class PointData
        {
            public int pointid;
            public string product;
            public string dataset;
            public DateTime date;
            public int value;
        }

        const string ModisUser = "sandugash_2004",
            ModisPassword = "Arina2009",
            ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
            DownloadingDir = @"C:\MODIS\Downloading",
            DownloadedDir = @"C:\MODIS\Downloaded",
            CMDPath = @"C:\Windows\system32\cmd.exe",
            LastDateFile = "!last_date.txt",
            MosaicDir = @"C:\MODIS\Mosaic",
            ConvertDir = @"C:\MODIS\Convert",
            ModisProjection = "4326",
            GeoServerDir = @"C:\Program Files (x86)\GeoServer 2.13.4\data_dir\data\MODIS",
            GeoServerWorkspace = "MODIS",
            GeoServerUser = "admin",
            GeoServerPassword = "geoserver",
            GeoServerURL = "http://localhost:8080/geoserver/",
            AnalizeShp = @"C:\MODIS\shp\TestSnowExtrPnt.shp",
            ExtractRasterValueByPoint = @"C:\MODIS\Python\ExtractRasterValueByPoint.py";
        //const string ModisUser = "hvreren",
        //    ModisPassword = "Querty123",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
        //    DownloadingDir = @"D:\MODIS\Downloading",
        //    DownloadedDir = @"D:\MODIS\Downloaded",
        //    CMDPath = @"C:\Windows\system32\cmd.exe",
        //    LastDateFile = "!last_date.txt",
        //    MosaicDir = @"D:\MODIS\Mosaic",
        //    ConvertDir = @"D:\MODIS\Convert",
        //    ModisProjection = "4326",
        //    GeoServerDir = @"D:\GeoServer\data_dir\data\MODIS",
        //    GeoServerWorkspace = "MODIS",
        //    GeoServerUser = "admin",
        //    GeoServerPassword = "geoserver",
        //    GeoServerURL = "http://localhost:8080/geoserver/",
        //    AnalizeShp = @"D:\MODIS\shp\TestSnowExtrPnt.shp",
        //    ExtractRasterValueByPoint = @"D:\MODIS\Python\ExtractRasterValueByPoint.py";

        static List<PointData> pointDatas = new List<PointData>();

        static ModisProduct[] modisProducts = new ModisProduct[3];

        static void Main(string[] args)
        {
            //modisProducts[0] = new ModisProduct()
            //{
            //    Source = "SAN/MOST",
            //    Product = "MOD10A1.006",
            //    StartDate = new DateTime(2000, 2, 24),
            //    DataSets = new string[7]
            //    {
            //        "NDSISnowCover",
            //        "NDSISnowCoverBasic",
            //        "NDSISnowCoverAlgorithm",
            //        "NDSI",
            //        "SnowAlbedo",
            //        "orbitpnt",
            //        "granulepnt"
            //    },
            //    ExtractDataSetIndexes = new int[0] { },
            //    Spans = true,
            //    Mosaic = true,
            //    ConvertHdf = false
            //};
            modisProducts[0] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A2.006",
                StartDate = new DateTime(2000, 2, 24),
                DataSets = new string[2]
                {
                    "MaxSnowExtent",
                    "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Analize = true,
                DayDividedDataSetIndexes = new int[1] { 1 }
            };
            //modisProducts[2] = new ModisProduct()
            //{
            //    Source = "SAN/MOSA",
            //    Product = "MYD10A1.006",
            //    StartDate = new DateTime(2002, 7, 4),
            //    DataSets = new string[7]
            //    {
            //        "NDSISnowCover",
            //        "NDSISnowCoverBasic",
            //        "NDSISnowCoverAlgorithm",
            //        "NDSI",
            //        "SnowAlbedo",
            //        "orbitpnt",
            //        "granulepnt"
            //    },
            //    ExtractDataSetIndexes = new int[0] { },
            //    Spans = true,
            //    Mosaic = true,
            //    ConvertHdf = false
            //};
            modisProducts[1] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A2.006",
                StartDate = new DateTime(2002, 7, 4),
                DataSets = new string[2]
                {
                    "MaxSnowExtent",
                    "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 },
                Spans = true,
                Mosaic = true,
                ConvertHdf = false,
                Analize = true,
                DayDividedDataSetIndexes = new int[1] { 1 }
            };
            modisProducts[2] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10C2.006",
                StartDate = new DateTime(2000, 2, 24),
                DataSets = new string[1]
                {
                    "NDSI"
                },
                ExtractDataSetIndexes = new int[1] { 0 },
                Spans = false,
                Mosaic = false,
                ConvertHdf = true,
                Norm = true,
                AnomalyStartYear = 2001,
                AnomalyEndYear = 2019
            };

            while (true)
            {
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
                Analize();

                if (dateNext == DateTime.Today)
                {
                    Log("Sleep 4 hour");
                    Thread.Sleep(1000 * 60 * 60 * 4);
                }
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
                //throw new Exception(exception.ToString(), exception?.InnerException);
                Log($"{exception.ToString()}: {exception?.InnerException}");
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

        private static void ModisDownload(
            ModisProduct ModisProduct,
            DateTime date)
        {
            if (ModisProduct.StartDate > date)
            {
                return;
            }
            EmptyDownloadingDir();
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
                MoveDownloadedFiles();
            }
            catch (Exception exception)
            {
                Log(exception.Message + ": " + exception.InnerException?.Message);
            }
            EmptyDownloadingDir();
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
                    //taskList.Add(Task.Factory.StartNew(() => ModisMosaicTask(listFile)));
                    ModisMosaicTask(listFile);
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
            //List<Task> taskList = new List<Task>();
            //List<List<Task>> taskListList = new List<List<Task>>();
            //taskListList.Add(new List<Task>());
            foreach (string file in Directory.EnumerateFiles(MosaicDir, "*.tif"))
            {
                //taskList.Add(Task.Factory.StartNew(() => ModisConvertTask(file)));
                //if (taskListList.Last().Count() >= 10)
                //{
                //    taskListList.Add(new List<Task>());
                //}
                //taskListList.Last().Add(Task.Factory.StartNew(() => ModisConvertTask(file)));
                ModisConvertTask(file);
            }
            //Task.WaitAll(taskList.ToArray());
            //foreach(var taskList in taskListList)
            //{
            //    Task.WaitAll(taskList.ToArray());
            //}
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
                        //taskList.Add(Task.Factory.StartNew(() => ModisCropTask(file)));
                        ModisCropTask(file);
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
                taskList.Add(Task.Factory.StartNew(() => ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                //ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
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

        private static void Analize()
        {
            List<Task> taskList = new List<Task>();
            pointDatas.Clear();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                if (modisProduct.Analize)
                {
                    foreach (string file in Directory.EnumerateFiles(GeoServerDir, $"*{modisProduct.Product.Split('.')[0]}*.tif", SearchOption.TopDirectoryOnly))
                    {
                        taskList.Add(Task.Factory.StartNew(() => AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                        //AnalizeTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
                    }
                }
            }
            Task.WaitAll(taskList.ToArray());
            using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            {
                foreach(PointData pointData in pointDatas)
                {
                    string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
                        $"{pointData.pointid}, " +
                        $"'{pointData.product}', " +
                        $"'{pointData.dataset}', " +
                        $"'{pointData.date.ToString("yyyy-MM-dd")}', " +
                        $"{pointData.value});";
                    connection.Query(query);
                }
            }
            pointDatas.Clear();
        }

        private static void AnalizeTask(string TifFile)
        {
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
                connection.Open();
                string query = $"SELECT COUNT(*) FROM public.modispoints WHERE" +
                    $" product = '{product}' AND" +
                    $" dataset = '{dataset}' AND" +
                    $" date = '{dateFinish.ToString("yyyy-MM-dd")}';";
                var count = connection.Query<long>(query).FirstOrDefault();
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
            //using (var connection = new NpgsqlConnection("Host=localhost;Database=GeoNodeWebModis;Username=postgres;Password=postgres;Port=5432"))
            //{
            //    connection.Open();
            //    foreach (string line in data)
            //    {
            //        int pointid = Convert.ToInt32(line.Split(' ')[1].Replace(",","").Replace(")", ""));
            //        byte value = Convert.ToByte(line.Split(' ')[2].Replace(",","").Replace(")", ""));
            //        if (modisProduct.DayDividedDataSetIndexes.Contains(datasetIndex))
            //        {
            //            BitArray bits = new BitArray(new byte[] { value }); //new BitArray(BitConverter.GetBytes(value).ToArray());
            //            for (int d = 0; d < bits.Count; d++)
            //            {
            //                DateTime date = dateFinish.AddDays(d - bits.Count + 1);
            //                int valuei = Convert.ToInt32(bits[d]);
            //                string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
            //                    $"{pointid}, " +
            //                    $"'{product}', " +
            //                    $"'{dataset}', " +
            //                    $"'{date.ToString("yyyy-MM-dd")}', " +
            //                    $"{valuei});";
            //                connection.Query(query);
            //            }
            //        }
            //        else
            //        {
            //            string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
            //                $"{pointid}, " +
            //                $"'{product}', " +
            //                $"'{dataset}', " +
            //                $"'{dateFinish.ToString("yyyy-MM-dd")}', " +
            //                $"{value});";
            //            connection.Query(query);
            //        }
            //    }
            //    connection.Close();
            //}
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
                        string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
                            $"{pointid}, " +
                            $"'{product}', " +
                            $"'{dataset}', " +
                            $"'{date.ToString("yyyy-MM-dd")}', " +
                            $"{valuei});";
                        pointDatas.Add(new PointData()
                        {
                            pointid = pointid,
                            product = product,
                            dataset = dataset,
                            date = date,
                            value = valuei
                        });
                    }
                }
                else
                {
                    string query = $"INSERT INTO public.modispoints(pointid, product, dataset, date, value) VALUES (" +
                        $"{pointid}, " +
                        $"'{product}', " +
                        $"'{dataset}', " +
                        $"'{dateFinish.ToString("yyyy-MM-dd")}', " +
                        $"{value});";
                    pointDatas.Add(new PointData()
                    {
                        pointid = pointid,
                        product = product,
                        dataset = dataset,
                        date = dateFinish,
                        value = value
                    });
                }
            }
        }

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
