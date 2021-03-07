using log4net;
using System;
using System.ServiceProcess;
using System.Timers;

namespace SFTPSyncService
{
    public partial class SFTPSyncService : ServiceBase
    {

        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Settings _settings; 
        private int eventId = 1;
        private bool executing = false;
        private Timer timer;

        public SFTPSyncService()
        {
            InitializeComponent();
            _settings = Settings.Load();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Service Started");
            SetTimer();
            ExecuteSync();
        }

        protected override void OnStop()
        {
            this.timer.Stop();
            this.timer = null;

            _logger.Info("Service Stopped");
        }
        protected override void OnContinue()
        {
            _logger.Info("Service Continued");
        }

        private void SetTimer()
        {
            _logger.Info("Configuring Timer");
            timer = new Timer();
            timer.Interval = _settings.FrequencyInSec * 1000;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            _logger.Info($"Service monitoring. Count: {eventId++}");
            
            if(!executing) 
                ExecuteSync();
        }
        public void ExecuteSync()
        {
            try
            {
                _logger.Info(new string('-', 50));
                _logger.Info("Sync initialized.");

                executing = true;
                

                _logger.Info(_settings);
                
                foreach (var syncItem in _settings.FoldersSettings)
                {
                    var sftpService = new SFTPService(_settings, _logger);

                    sftpService.SyncDirectories(syncItem);
                }

                _logger.Info("Sync finished");
                _logger.Info(new string('-', 50));
            }
            catch (Exception er)
            {
                _logger.Error("Error: ", er);
            }
            finally
            {
                executing = false;
            }
        }
    }
}
