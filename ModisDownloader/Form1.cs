using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModisDownloader
{
    public partial class FormMain : Form
    {
        public class ModisProduct
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
            public string GetProductWithoutVersion()
            {
                return Product.Split('.')[0];
            }
            public string GetProductVersion()
            {
                string version = Product.Split('.')[1];
                while (version[0] == '0')
                {
                    version = version.Substring(1);
                }
                return version;
            }
        }

        //static ModisProduct[] modisProducts = new ModisProduct[5];
        List<ModisProduct> modisProducts = new List<ModisProduct>();

        public FormMain()
        {
            InitializeComponent();
            modisProducts.Add(new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A1.061",
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
            });
            modisProducts.Add(new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A1.061",
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
            });
            modisProducts.Add(new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10A2.061",
                StartDate = new DateTime(2000, 2, 24),
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
            });
            modisProducts.Add(new ModisProduct()
            {
                Source = "SAN/MOSA",
                Product = "MYD10A2.061",
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
            });
            modisProducts.Add(new ModisProduct()
            {
                Source = "SAN/MOST",
                Product = "MOD10C2.061",
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
            });
        }

        //bool download = false;

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Log("Download started!");
            //download = true;
            backgroundWorkerDownloader.RunWorkerAsync();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            //download = false;
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            backgroundWorkerDownloader.CancelAsync();
        }

        private void backgroundWorkerDownloader_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorkerDownloader.CancellationPending)
            {
                StartDownloadAll();
            }
        }

        public void StartDownloadAll()
        {
            DateTime dateMin = new DateTime(2100, 12, 31);
            ModisProduct modisProductToDownload = new ModisProduct();
            foreach (ModisProduct modisProduct in modisProducts)
            {
                DateTime dateCurrent = GetProductLastDate(modisProduct);
                if (dateCurrent < dateMin)
                {
                    dateMin = dateCurrent;
                    modisProductToDownload = modisProduct;
                }
            }
            StartDownloadProduct(modisProductToDownload);
        }

        public DateTime GetProductLastDate(ModisProduct ModisProduct)
        {
            string dateFile = Path.Combine(textBoxArchiveDirectory.Text, ModisProduct.GetProductWithoutVersion());
            dateFile = Path.ChangeExtension(dateFile, "txt");
            if (!File.Exists(dateFile))
            {
                return ModisProduct.StartDate.AddDays(-1);
            }
            string s_lastDate = File.ReadAllText(dateFile),
                s_year = s_lastDate.Split('-')[0],
                s_month = s_lastDate.Split('-')[1],
                s_day = s_lastDate.Split('-')[2];
            int year = Convert.ToInt32(s_year),
                month = Convert.ToInt32(s_month),
                day = Convert.ToInt32(s_day);
            return new DateTime(year, month, day);
        }

        public void StartDownloadProduct(ModisProduct ModisProduct)
        {
            DateTime dateLast = GetProductLastDate(ModisProduct),
                dateDownload = dateLast.AddDays(ModisProduct.Period);
            DownloadProduct(ModisProduct, dateDownload);
        }

        public void DownloadProduct(ModisProduct ModisProduct, DateTime Date)
        {
            Log($"{ModisProduct.GetProductWithoutVersion()} {Date.ToString("yyyy-MM-dd")} download started!");
            ClearBufer();
            bool ok = false;
            while (!ok)
            {
                try
                {
                    string resource = $"https://n5eil02u.ecs.nsidc.org/egi/request?" +
                            $"short_name={ModisProduct.GetProductWithoutVersion()}&" +
                            $"version={ModisProduct.GetProductVersion()}&" +
                            $"time={Date.ToString("yyyy-MM-dd")},{Date.ToString("yyyy-MM-dd")}&" +
                            $"bounding_box={textBoxBoundingBox.Text}&" +
                            $"agent=NO&" +
                            $"page_size=2000",
                        urs = "https://urs.earthdata.nasa.gov",
                        username = "sandugash_2004",
                        password = "Arina2009";
                    CookieContainer myContainer = new CookieContainer();
                    CredentialCache cache = new CredentialCache();
                    cache.Add(new Uri(urs), "Basic", new NetworkCredential(username, password));
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resource);
                    request.Method = "GET";
                    request.Credentials = cache;
                    request.CookieContainer = myContainer;
                    request.PreAuthenticate = false;
                    request.AllowAutoRedirect = true;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    long length = response.ContentLength;
                    string type = response.ContentType;
                    Stream stream = response.GetResponseStream();
                    string fileName = response.Headers["Content-Disposition"].Replace("attachment; filename=", String.Empty).Replace("\"", String.Empty);
                    using (Stream s = File.Create(Path.Combine(textBoxArchiveDirectory.Text, "Bufer", fileName)))
                    {
                        stream.CopyTo(s);
                    }
                    stream.Close();
                    MoveFilesToArchive();
                    SaveProductLastDate(ModisProduct, Date);
                    ok = true;
                }
                catch (WebException webException)
                {
                    if ((HttpWebResponse)webException.Response != null)
                    {
                        if (((HttpWebResponse)webException.Response).StatusCode == HttpStatusCode.NotImplemented)
                        {
                            Log($"{ModisProduct.GetProductWithoutVersion()} {Date.ToString("yyyy-MM-dd")} download error: " +
                                $"{((HttpWebResponse)webException.Response).StatusCode} ({((HttpWebResponse)webException.Response).StatusDescription})");
                            SaveProductLastDate(ModisProduct, Date);
                            ok = true;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log($"{ModisProduct.GetProductWithoutVersion()} {Date.ToString("yyyy-MM-dd")} download error: {exception.Message}");
                }
            }
            Log($"{ModisProduct.GetProductWithoutVersion()} {Date.ToString("yyyy-MM-dd")} download finished!");
        }

        public void MoveFilesToArchive()
        {
            Unzip();
            foreach (string file in Directory.EnumerateFiles(Path.Combine(textBoxArchiveDirectory.Text, "Bufer"), "*.hdf*", SearchOption.AllDirectories))
            {
                string product = Path.GetFileNameWithoutExtension(file).Split('.')[0],
                    year = Path.GetFileNameWithoutExtension(file).Split('.')[1].Substring(1, 4),
                    archiveDirecory = Path.Combine(textBoxArchiveDirectory.Text, $"{year}.{product}");
                if (!Directory.Exists(archiveDirecory))
                {
                    Directory.CreateDirectory(archiveDirecory);
                }
                File.Move(file, Path.Combine(archiveDirecory, Path.GetFileName(file)));
            }
            ClearBufer();
        }

        public void Unzip()
        {
            foreach (string zip in Directory.GetFiles(Path.Combine(textBoxArchiveDirectory.Text, "Bufer"), "*.zip"))
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zip, Path.Combine(textBoxArchiveDirectory.Text, "Bufer"));
            }
        }

        public void ClearBufer()
        {
            DirectoryInfo di = new DirectoryInfo(Path.Combine(textBoxArchiveDirectory.Text, "Bufer"));
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public void SaveProductLastDate(ModisProduct ModisProduct, DateTime Date)
        {
            string dateFile = Path.Combine(textBoxArchiveDirectory.Text, ModisProduct.GetProductWithoutVersion());
            dateFile = Path.ChangeExtension(dateFile, "txt");
            File.WriteAllText(dateFile, Date.ToString("yyyy-MM-dd"));
        }

        public void Log(string Message)
        {
            //textBoxLog.AppendText($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} >> {Message}{Environment.NewLine}");
            Action action = () => textBoxLog.AppendText($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} >> {Message}{Environment.NewLine}");
            textBoxLog.Invoke(action);
        }

        private void backgroundWorkerDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Log("Download stopped!");
        }
    }
}
