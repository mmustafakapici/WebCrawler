using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace WpfApp
{
    
    public partial class MainWindow : Window
    {
        private readonly CrawlerManager[] _crawlerManagers;
        private readonly string _htmlsFolderPath;
        private readonly string _logsUıJsonPath;
        private readonly string _urlsUıJsonPath;
        private readonly string _logsSchemedJsonPath;
        private readonly string _urlsSchemedJsonPath;
        public readonly string ExceptionsSchemedJsonPath;

        private readonly string _databasePath;
        private readonly object _lockObject;
        private MyDbContext _dbContext;

        public MainWindow()
        {
            InitializeComponent();
            _crawlerManagers = new CrawlerManager[3];

            for (int i = 0; i < _crawlerManagers.Length; i++)
            {
                _crawlerManagers[i] = null;
            }

            _htmlsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "htmls");
            _logsUıJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "logsUIcache.json");
            _urlsUıJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "urlsUIcache.json");
            _logsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "logsSCHEMED.json");
            _urlsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "urlsSCHEMED.json");
            ExceptionsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "exceptionsSCHEMED.json");
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebCrawlerDatabase.db");
            _lockObject = new object();

            StartClock();
            LoadUrlsFromUıJson();
            LoadLogsFromUıJson();
            CreateDatabaseAndPaths();
            //LoadUrlsFromDatabase();
        }

        private void StartClock()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LblClock.Content = DateTime.Now.ToString("HH:mm:ss");
                    });
                    Thread.Sleep(1000);
                }
            });
        }

        private void BtnAdder_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                int index = GetAvailableCrawlerManagerIndex();
                if (index != -1)
                {
                    AddLabel(url, index);
                    if (_crawlerManagers[index] == null)
                    {
                        _crawlerManagers[index] = new CrawlerManager(url, index, _htmlsFolderPath, this);
                    }
                    else
                    {
                        _crawlerManagers[index].AddRootUrl(url);
                    }

                    TxtUrl.Clear();
                    if (!_crawlerManagers[index].IsCrawling)
                    {
                        _crawlerManagers[index].StartCrawling();
                    }
                }
                else
                {
                    MessageBox.Show("Maksimum 3 adet web sitesi girilebilir.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Lütfen bir URL giriniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddLabel(string url, int index)
        {
            switch (index)
            {
                case 0:
                    LabelUrl1.Content = "URL: " + url;
                    break;
                case 1:
                    LabelUrl2.Content = "URL: " + url;
                    break;
                case 2:
                    LabelUrl3.Content = "URL: " + url;
                    break;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int buttonNumber = int.Parse(btn.Tag.ToString());

            _crawlerManagers[buttonNumber].StartCrawling();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int buttonNumber = int.Parse(btn.Tag.ToString());

            _crawlerManagers[buttonNumber].PauseCrawling();
        }

        private void BtnResume_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int buttonNumber = int.Parse(btn.Tag.ToString());

            _crawlerManagers[buttonNumber].ResumeCrawling();
        }

        private void BtnClear(object sender, RoutedEventArgs e)
        {
            foreach (var crawlerManager in _crawlerManagers)
            {
                crawlerManager?.StopCrawling();
            }

            LabelUrl1.Content = "URL: ";
            LabelUrl2.Content = "URL:";
            LabelUrl3.Content = "URL:";
            TxtUrl.Text = "https://example.com";
            LstFoundUrls1.Items.Clear();
            LstFoundUrls2.Items.Clear();
            LstFoundUrls3.Items.Clear();
            LstCrawledUrls1.Items.Clear();
            LstCrawledUrls2.Items.Clear();
            LstCrawledUrls3.Items.Clear();
            LstAllLogs.Items.Clear();
            LstAllUrlsCrawled.Items.Clear();

            for (int i = 0; i < _crawlerManagers.Length; i++)
            {
                _crawlerManagers[i] = null;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveLogsToUıJson();
            SaveUrlsToUıJson();
            SaveUrlsToDatabase();
           // SaveUrlsToMongo();
        }

        private int GetAvailableCrawlerManagerIndex()
        {
            for (int i = 0; i < _crawlerManagers.Length; i++)
            {
                if (_crawlerManagers[i] == null)
                    return i;
            }
            return -1;
        }

       
        
      


        private void SaveUrlsToDatabase()
        {
            using (var dbContext = new MyDbContext(_databasePath))
            {
                // Url sınıfından nesneler oluşturarak veritabanına kaydetme
                foreach (var crawlerManager in _crawlerManagers)
                {
                    if (crawlerManager != null)
                    {
                        var urls = crawlerManager.GetCrawledUrls();
                        foreach (var url in urls)
                        {
                            dbContext.TblUrls.Add(url);
                        }
                    }
                }

                dbContext.SaveChanges();
            }
            
            MessageBox.Show("URL'ler başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveLogsToUıJson()
        {
            List<string> logs = new List<string>();
            foreach (var item in LstFoundUrls1.Items)
            {
                logs.Add(item.ToString());
            }
            foreach (var item in LstFoundUrls2.Items)
            {
                logs.Add(item.ToString());
            }
            foreach (var item in LstFoundUrls3.Items)
            {
                logs.Add(item.ToString());
            }
            string json = JsonConvert.SerializeObject(logs, Formatting.Indented);
            File.WriteAllText(_logsUıJsonPath, json);
            MessageBox.Show("Loglar başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveUrlsToUıJson()
        {
            List<string> urls = new List<string>();
            foreach (var item in LstCrawledUrls1.Items)
            {
                urls.Add(item.ToString());
            }
            foreach (var item in LstCrawledUrls2.Items)
            {
                urls.Add(item.ToString());
            }
            foreach (var item in LstCrawledUrls3.Items)
            {
                urls.Add(item.ToString());
            }
            string json = JsonConvert.SerializeObject(urls, Formatting.Indented);
            File.WriteAllText(_urlsUıJsonPath, json);
            MessageBox.Show("URL'ler başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadUrlsFromUıJson()
        {
            if (File.Exists(_urlsUıJsonPath))
            {
                string json = File.ReadAllText(_urlsUıJsonPath);
                List<string> urls = JsonConvert.DeserializeObject<List<string>>(json);
                if (urls != null)
                {
                    foreach (var url in urls)
                    {
                        if (url.StartsWith("Thread: 1"))
                        {
                            LstCrawledUrls1.Items.Add(url);
                        }
                        else if (url.StartsWith("Thread: 2"))
                        {
                            LstCrawledUrls2.Items.Add(url);
                        }
                        else if (url.StartsWith("Thread: 3"))
                        {
                            LstCrawledUrls3.Items.Add(url);
                        }
                    }
                }
            }
        }

        private void LoadLogsFromUıJson()
        {
            if (File.Exists(_logsUıJsonPath))
            {
                string json = File.ReadAllText(_logsUıJsonPath);
                List<string> logs = JsonConvert.DeserializeObject<List<string>>(json);
                if (logs != null)
                {
                    foreach (var log in logs)
                    {
                        if (log.StartsWith("Thread: 1"))
                        {
                            LstFoundUrls1.Items.Add(log);
                        }
                        else if (log.StartsWith("Thread: 2"))
                        {
                            LstFoundUrls2.Items.Add(log);
                        }
                        else if (log.StartsWith("Thread: 3"))
                        {
                            LstFoundUrls3.Items.Add(log);
                        }
                    }
                }
            }
        }

        private void CreateDatabaseAndPaths()
        {
            // Create the "htmls" folder if it doesn't exist
            if (!Directory.Exists(_htmlsFolderPath))
            {
                Directory.CreateDirectory(_htmlsFolderPath);
            }

            // Create the "jsons" folder if it doesn't exist
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons"));
            }

            _dbContext = new MyDbContext(_databasePath);
            _dbContext.Database.EnsureCreated();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dbContext?.Dispose();
        }
    }
    public class CrawlerManager
    {
        private readonly ConcurrentQueue<string> _rootUrls;
        private readonly ConcurrentQueue<string> _urlQueue;
        private readonly object _lockObject;
        private readonly Dictionary<string, int> _urlThreadMap;
        private readonly string _htmlsFolderPath;
       private readonly string _logsSchemedJsonPath;
       private readonly string _urlsSchemedJsonPath;
       private readonly string _exceptionsSchemedJsonPath;


        private readonly MainWindow _mainWindow;
        private bool _isPaused;
        private int _threadId;
        private CancellationTokenSource _tokenSource;
        private List<Task> _tasks;
        private TaskScheduler _taskScheduler;
        private List<Url> _crawledUrls;

        public bool IsCrawling { get; private set; }

        public CrawlerManager(string rootUrl, int threadId, string htmlsFolderPath,   MainWindow mainWindow)
        {
            _rootUrls = new ConcurrentQueue<string>();
            _urlQueue = new ConcurrentQueue<string>();
            _lockObject = new object();
            _tasks = new List<Task>();
            _isPaused = false;
            _urlThreadMap = new Dictionary<string, int>();
            _crawledUrls = new List<Url>();
            
            
            
            this._threadId = threadId;
            this._htmlsFolderPath = htmlsFolderPath;
            _logsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "logsSCHEMED.json");
            _urlsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "urlsSCHEMED.json");
            _exceptionsSchemedJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jsons", "exceptionsSCHEMED.json");

            this._mainWindow = mainWindow;
            
            
            AddRootUrl(rootUrl);
        }

        public void AddRootUrl(string url)
        {
            _rootUrls.Enqueue(url);
        }

        public void StartCrawling()
        {
            if (IsCrawling) return;

            _tokenSource = new CancellationTokenSource();
            _tasks.Clear();

            var threadRootUrls = _rootUrls.ToList();
            foreach (var url in threadRootUrls)
            {
                _urlThreadMap[url] = _threadId;
                _urlQueue.Enqueue(url);
            }

            _taskScheduler = TaskScheduler.Current;

            Task task = Task.Factory.StartNew(() => CrawlUrls(_tokenSource.Token), CancellationToken.None,
                TaskCreationOptions.None, _taskScheduler);
            _tasks.Add(task);

            IsCrawling = true;
        }

        public void PauseCrawling()
        {
            _isPaused = true;
        }

        public void ResumeCrawling()
        {
            _isPaused = false;
            lock (_lockObject)
            {
                Monitor.PulseAll(_lockObject);
            }
        }

        public void StopCrawling()
        {
            _tokenSource?.Cancel();
            _tasks.Clear();
            _rootUrls.Clear();
            _urlQueue.Clear();
            _urlThreadMap.Clear();
            IsCrawling = false;
        }

        private void CrawlUrls(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _urlQueue.TryDequeue(out string url))
            {
                if (_isPaused)
                {
                    lock (_lockObject)
                    {
                        Monitor.Wait(_lockObject);
                    }
                }

                try
                {
                    var document = HtmlDocumentOperations(url, out var downloadTime);
                    var fileName = SaveHtmlDocument(document, url);

                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        lock (_lockObject)
                        {
                            WriteCrawledUrlsToUi(url, downloadTime, fileName);
                            WriteCrawledUrlsToJsons(url, downloadTime, fileName);
                            FindNewLinks(document, url);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        lock (_lockObject)
                        {
                            WriteExceptionToUi(ex, url);
                            WriteExceptionToJsons(ex, url);
                        }
                    });
                }
            }
        }

        private static HtmlDocument HtmlDocumentOperations(string url, out TimeSpan downloadTime)
        {
            DateTime startTime = DateTime.Now;
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load(url);
            downloadTime = DateTime.Now - startTime;
            return document;
        }

        private string SaveHtmlDocument(HtmlDocument document, string url)
        {
            var fileName = GetValidFileName(url);
            var filePath = Path.Combine(_htmlsFolderPath, $"{fileName}.html");

            document.Save(filePath);
            return filePath;
        }

        private string GetValidFileName(string url)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var validFileName = string.Join("_", url.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return validFileName;
        }

        private void FindNewLinks(HtmlDocument document, string crawledUrl)
        {
            var links = document.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                foreach (var link in links)
                {
                    string href = link.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        _urlQueue.Enqueue(href);
                        _urlThreadMap[href] = _threadId;
                        _mainWindow.Dispatcher.Invoke(() =>
                        {
                            lock (_lockObject)
                            {
                                WriteFoundUrlsToUi(crawledUrl, href);
                                WriteFoundUrlsToJsons(crawledUrl, href);
                            }
                        });
                    }
                }
            }
        }

        private void WriteExceptionToUi(Exception ex, string url)
        {
            _mainWindow.LstAllLogs.Items.Insert(0,
                $"Thread: {_threadId + 1}, Error: {ex.Message}, URL: {url}, Time: {DateTime.Now}");
        }

        private void WriteCrawledUrlsToUi(string url, TimeSpan downloadTime, string fileName)
        {
            ListBox listBox;
            if (_threadId == 0)
                listBox = _mainWindow.LstCrawledUrls1;
            else if (_threadId == 1)
                listBox = _mainWindow.LstCrawledUrls2;
            else
                listBox = _mainWindow.LstCrawledUrls3;

            listBox.Items.Insert(0, $"Thread: {_threadId + 1}, URL: {url}, Download Time: {downloadTime.TotalMilliseconds} ms");
            _mainWindow.LstAllUrlsCrawled.Items.Insert(0,
                $"Thread: {_threadId + 1}, URL: {url}, Download Time: {downloadTime.TotalMilliseconds} ms, File: {fileName}, Time: {DateTime.Now}");

            // CrawlerManager üzerinden kaydedilen her bir URL'ü Url sınıfı nesnesine dönüştürerek listeye ekleme
            _crawledUrls.Add(new Url
            {
                UrlAddress = url,
                DownloadTime = downloadTime,
                ThreadId = _threadId + 1,
                CreatedAt = DateTime.Now
            });
        }

        private void WriteFoundUrlsToUi(string crawledUrl, string href)
        {
            ListBox listBox;
            if (_threadId == 0)
                listBox = _mainWindow.LstFoundUrls1;
            else if (_threadId == 1)
                listBox = _mainWindow.LstFoundUrls2;
            else
                listBox = _mainWindow.LstFoundUrls3;

            listBox.Items.Insert(0,
                $"Thread: {_threadId + 1}, Crawled URL: {crawledUrl}, Link: {href}, Depth Level: 0, Time: {DateTime.Now}");
        }

        private void WriteFoundUrlsToJsons(string crawledUrl, string href)
        {
            var jsonData = new
            {
                ThreadId = _threadId + 1,
                CrawledUrl = crawledUrl,
                Link = href,
                DepthLevel = 0,
                Time = DateTime.Now
            };
            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.AppendAllText(_urlsSchemedJsonPath, json + Environment.NewLine);
        }

        private void WriteCrawledUrlsToJsons(string url, TimeSpan downloadTime, string fileName)
        {
            var jsonData = new
            {
                ThreadId = _threadId + 1,
                URL = url,
                DownloadTime = downloadTime,
                File = fileName,
                Time = DateTime.Now
            };
            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.AppendAllText(_logsSchemedJsonPath, json + Environment.NewLine);
        }

        private void WriteExceptionToJsons(Exception ex, string url)
        {
            var jsonData = new
            {
                ThreadId = _threadId + 1,
                Error = ex.Message,
                URL = url,
                Time = DateTime.Now
            };
            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.AppendAllText(_exceptionsSchemedJsonPath, json + Environment.NewLine);
        }

        public List<Url> GetCrawledUrls()
        {
            return _crawledUrls;
        }
    }
    public class MyDbContext : DbContext
    {
        private readonly string _databasePath;

        public DbSet<Url> TblUrls { get; set; }

        public MyDbContext(string databasePath)
        {
            this._databasePath = databasePath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }
    }

    public class Url
    {
        public int Id { get; set; }
        public string UrlAddress { get; set; }
        public TimeSpan DownloadTime { get; set; }
        public int ThreadId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
}
