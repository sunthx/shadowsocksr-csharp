using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LeanCloud;
using Shadowsocks.Controller;
using Shadowsocks.Model;

namespace Shadowsocks.Lean
{
    public class SyncController
    {
        private FileSystemWatcher _fileSystemWatcher;
        private readonly WatcherTimer _watcherTimer;
        private string _globolConfFile = "gui-config.json";

        private string _appId;
        private string _appKey;

        private ShadowsocksController _shadowsocksController;

        public SyncController(
            ShadowsocksController controller,
            string appId = "G80hgDhEiwXllsbkS0cpJHoa-gzGzoHsz",
            string appKey = "n53lptg38slufxSYmT28uIFF")
        {
            _appId = appId;
            _appKey = appKey;
            _shadowsocksController = controller;

            AVClient.Initialize(_appId, _appKey);

            _watcherTimer = new WatcherTimer((o,a) =>
            {
                SyncAllServer(GetServersFromConf());
            }, 5000);

            _fileSystemWatcher =
                new FileSystemWatcher(Path.Combine(AppDomain.CurrentDomain.BaseDirectory))
                {
                    NotifyFilter = NotifyFilters.FileName |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.CreationTime,
                    Filter = _globolConfFile,
                    EnableRaisingEvents = true
                };

            _fileSystemWatcher.Created += _watcherTimer.OnFileChanged;
            _fileSystemWatcher.Changed += _watcherTimer.OnFileChanged;
            _fileSystemWatcher.Renamed += _watcherTimer.OnFileChanged;
            _fileSystemWatcher.Deleted += _watcherTimer.OnFileChanged;

            ThreadPool.QueueUserWorkItem(GetRemoveServerConf, null);
        }


        List<Server> GetServersFromConf()
        {
            return Configuration.Load()?.configs;
        }

        void GetRemoveServerConf(object obj)
        {
            try
            {
                var avObjs = new AVQuery<AVObject>("Server").FindAsync().Result;
                List<Server> servers = new List<Server>();
                foreach (var avObject in avObjs)
                    servers.Add(avObject.ToT<Server>());

                if (servers.Any())
                {
                    var configuration = new Configuration();
                    configuration.configs.AddRange(servers);
                    _shadowsocksController.SaveServersConfig(configuration);
                }
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        void SyncAllServer(List<Server> servers)
        {
            servers?.ForEach(item =>
            {
                ThreadPool.QueueUserWorkItem(SyncSingleServer, item);
            });
        }

        void SyncSingleServer(object obj)
        {
            var server = obj as Server;
            if (server == null)
                return;

            try
            {
                var objectId = QueryServerId(server);

                var avObj = server.ToAv();
                avObj.ObjectId = objectId;
                avObj.SaveAsync().Wait();
            }
            catch
            {
                //Ignore
            }
        }

        string QueryServerId(Server server)
        {
            try
            {
                var query = new AVQuery<AVObject>(server.GetType().Name)
                    .WhereEqualTo("server_port", server.server_port)
                    .WhereEqualTo("server",server.server)
                    .WhereEqualTo("group", server.group)
                    .WhereEqualTo("remarks", server.remarks)
                    .FirstOrDefaultAsync()
                    .Result;

                return query.ObjectId;
            }
            catch
            {
                //Ignore
                return null;
            }
        }
    }

    public static class Extension
    {
        public static AVObject ToAv(this object obj)
        {
            if (obj == null)
                throw new NullReferenceException();

            var avObject = AVObject.Create(obj.GetType().Name);

            foreach (var field in obj.GetType().GetFields())
            {
                if(!field.IsPublic)
                    continue;

                var value = field.GetValue(obj);
                if (value == null)
                    continue;

                avObject[field.Name] = value;
            }

            foreach (var property in obj.GetType().GetProperties())
            {
                var value = property.GetValue(obj);
                if (value == null)
                    continue;

                avObject[property.Name] = value;
            }

            return avObject;
        }

        public static T ToT<T>(this AVObject avObject)
        {
            if (avObject == null)
                throw new NullReferenceException();

            var currentCrmsObject = Activator.CreateInstance(typeof(T));

            foreach (var propertyInfo in currentCrmsObject.GetType().GetProperties())
            {
                if (!propertyInfo.CanWrite)
                    continue;


                if (!avObject.Keys.Contains(propertyInfo.Name))
                    continue;

                propertyInfo.SetValue(currentCrmsObject, avObject[propertyInfo.Name]);
            }

            foreach (var field in currentCrmsObject.GetType().GetFields())
            {
                if (!field.IsPublic)
                    continue;


                if (!avObject.Keys.Contains(field.Name))
                    continue;

                field.SetValue(currentCrmsObject, avObject[field.Name]);
            }

            return (T) currentCrmsObject;
        }
    }

    //http://blog.csdn.net/dragon_ton/article/details/50863862
    public class WatcherTimer
    {
        private int TimeoutMillis = 2000;

        System.Threading.Timer m_timer = null;
        List<String> files = new List<string>();
        FileSystemEventHandler fswHandler = null;

        public WatcherTimer(FileSystemEventHandler watchHandler)
        {
            m_timer = new System.Threading.Timer(new TimerCallback(OnTimer),
                         null, Timeout.Infinite, Timeout.Infinite);
            fswHandler = watchHandler;
        }


        public WatcherTimer(FileSystemEventHandler watchHandler, int timerInterval)
        {
            m_timer = new System.Threading.Timer(new TimerCallback(OnTimer),
                        null, Timeout.Infinite, Timeout.Infinite);
            TimeoutMillis = timerInterval;
            fswHandler = watchHandler;
        }

        public void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Mutex mutex = new Mutex(false, "FSW");
            mutex.WaitOne();
            if (!files.Contains(e.Name))
            {
                files.Add(e.Name);
            }
            mutex.ReleaseMutex();

            m_timer.Change(TimeoutMillis, Timeout.Infinite);
        }

        private void OnTimer(object state)
        {
            List<String> backup = new List<string>();

            Mutex mutex = new Mutex(false, "FSW");
            mutex.WaitOne();
            backup.AddRange(files);
            files.Clear();
            mutex.ReleaseMutex();


            foreach (string file in backup)
            {
                fswHandler(this, new FileSystemEventArgs(
                       WatcherChangeTypes.Changed, string.Empty, file));
            }

        }



    }
}