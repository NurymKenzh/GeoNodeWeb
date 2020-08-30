using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            GeoServerURL = "http://localhost:8080/geoserver/";
        //const string ModisUser = "caesarmod",
        //    ModisPassword = "caesar023Earthdata",
        //    ModisSpans = "h21v03,h21v04,h22v03,h22v04,h23v03,h23v04,h24v03,h24v04",
        //    DownloadingDir = @"R:\MODIS\Downloading",
        //    DownloadedDir = @"D:\MODIS",
        //    CMDPath = @"C:\Windows\system32\cmd.exe",
        //    LastDateFile = "!last_date.txt",
        //    MosaicDir = @"R:\MODIS\Mosaic",
        //    ConvertDir = @"R:\MODIS\Convert",
        //    ModisProjection = "4326",
        //    GeoServerDir = @"D:\GeoServer\data_dir\data\MODIS",
        //    GeoServerWorkspace = "MODIS",
        //    GeoServerUser = "admin",
        //    GeoServerPassword = "geoserver",
        //    GeoServerURL = "http://localhost:8080/geoserver/";

        static ModisProduct[] modisProducts = new ModisProduct[4];

        static void Main(string[] args)
        {
            modisProducts[0] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A1.006",
                StartDate = new DateTime(2000, 2, 24),
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
                ExtractDataSetIndexes = new int[0] { }
            };
            modisProducts[1] = new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A2.006",
                StartDate = new DateTime(2000, 2, 24),
                DataSets = new string[2] 
                { 
                    "MaxSnowExtent",
                    "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 }
            };
            modisProducts[2] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A1.006",
                StartDate = new DateTime(2002, 7, 4),
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
                ExtractDataSetIndexes = new int[0] { }
            };
            modisProducts[3] = new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A2.006",
                StartDate = new DateTime(2002, 7, 4),
                DataSets = new string[2]
                {
                    "MaxSnowExtent",
                    "SnowCover"
                },
                ExtractDataSetIndexes = new int[2] { 0, 1 }
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
                ModisConvert();
                ModisPublish();

                if (dateNext == DateTime.Today)
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
                    //throw new Exception(error);
                }
            }
            catch (Exception exception)
            {
                //throw new Exception(exception.ToString(), exception?.InnerException);
                Log($"{exception.ToString()}: {exception?.InnerException}");
            }
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
                    $" -t {ModisSpans}" +
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
                if (GeoServerFiles.Count(g => g.Contains(fileDate) && g.Contains(fileProduct.Replace(".",""))) > 0)
                {
                    continue;
                }
                string product = Path.GetFileName(file).Split('.')[0],
                    date = Path.GetFileName(file).Split('.')[1],
                    listFile = Path.Combine(DownloadedDir, $"{product}.{date}.txt");
                if (!File.Exists(listFile))
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

        private static void ModisConvert()
        {
            List<Task> taskList = new List<Task>();
            foreach (string file in Directory.EnumerateFiles(MosaicDir, "*.tif"))
            {
                taskList.Add(Task.Factory.StartNew(() => ModisConvertTask(file)));
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
            //File.Move(
            //    Path.Combine(ConvertDir, Path.ChangeExtension(tifReprojected, ".tif")),
            //    Path.Combine(GeoServerDir, Path.ChangeExtension(tifReprojected, ".tif")));
        }

        private static void ModisPublish()
        {
            //List<Task> taskList = new List<Task>();
            foreach (string file in Directory.EnumerateFiles(ConvertDir, "*.tif"))
            {
                File.Move(
                    file,
                    Path.Combine(GeoServerDir, Path.GetFileName(file)));
                //taskList.Add(Task.Factory.StartNew(() => ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)))));
                ModisPublishTask(Path.Combine(GeoServerDir, Path.GetFileName(file)));
            }
            //Task.WaitAll(taskList.ToArray());
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

        private static void Log(string log)
        {
            foreach (string line in log.Split("\r\n"))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} >> {line}");
            }
        }
    }
}
