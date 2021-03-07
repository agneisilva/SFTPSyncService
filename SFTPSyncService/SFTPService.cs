using log4net;
using System;
using System.IO;
using WinSCP;
using static SFTPSyncService.Settings;

namespace SFTPSyncService
{
    public class SFTPService : IDisposable
    {
        private Settings _settings;
        private Session _currentSession;
        private readonly ILog _logger;

        public SFTPService(Settings settings, ILog logger)
        {
            _logger = logger;
            _settings = settings;
            Open();
        }

        private void Open()
        {
            try
            {
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = _settings.HostName,
                    PortNumber = _settings.PortNumber,
                    UserName = _settings.UserName,
                    SshHostKeyFingerprint = _settings.SshHostKeyFingerprint,
                    SshPrivateKeyPath = _settings.SshPrivateKeyPath,
                };

                sessionOptions.AddRawSettings("Shell", "sudo%20su%20-");

                _currentSession = new Session();

                _currentSession.FileTransferred += FileTransferred;
                _currentSession.OutputDataReceived += OutputDataReceived;

                _currentSession.Open(sessionOptions);
            }
            catch (Exception er)
            {
                _logger.Error($"Error during open connection, {er}");
                _currentSession = null;
            }
        }

        public void SyncDirectories(SyncFoldersSettings foldersSettings)
        {
            try
            {
                if (!IsOpen())
                {
                    _logger.Error("Connection not opened!");
                    return;
                }

                _currentSession.SynchronizeDirectories(foldersSettings.Direction,
                                                       foldersSettings.LocalPath,
                                                       foldersSettings.RemotePath,
                                                       removeFiles: false,
                                                       mirror: false,
                                                       criteria: SynchronizationCriteria.Size,
                                                       options: new TransferOptions()
                                                       {
                                                           FilePermissions = null,
                                                           PreserveTimestamp = false
                                                       });

                if(foldersSettings.Direction == SynchronizationMode.Remote)
                    MoveFilesSent(foldersSettings.LocalPath, _settings.FolderToMoveFilesSentRemote);
            }
            catch (Exception er)
            {
                _logger.Error(er);
            }
        }

        private void MoveFilesSent(string from, string to)
        {
            if (!Directory.Exists(from)) return;

            if (!Directory.Exists(to)) Directory.CreateDirectory(to);

            var files = Directory.GetFiles(from);

            foreach (var file in files)
            {
                if (File.Exists(Path.Combine(to, Path.GetFileName(file)))) File.Delete(Path.Combine(to, Path.GetFileName(file)));

                _logger.Error($"Moving file {file} to {to}");

                File.Move(file, Path.Combine(to, Path.GetFileName(file)));
            }
        }

        private void OutputDataReceived(object sender, OutputDataReceivedEventArgs e)
        {
            _logger.Info(e);
        }

        private void FileTransferred(object sender, TransferEventArgs e)
        {
            if (e.Error == null)
            {
                _logger.Info($"Upload of {e.FileName} succeeded");
            }
            else
            {
                _logger.Error($"Upload of {e.FileName} failed: {e.Error}");
            }

            if (e.Chmod != null)
            {
                if (e.Chmod.Error == null)
                {
                    _logger.Info(
                        $"Permissions of {e.Chmod.FileName} set to {e.Chmod.FileName}");
                }
                else
                {
                    _logger.Error($"Setting permissions of {e.Chmod.FileName} failed: {e.Chmod.Error}");
                }
            }
            else
            {
                _logger.Info($"Permissions of {e.Destination} kept with their defaults");
            }

            if (e.Touch != null)
            {
                if (e.Touch.Error == null)
                {
                    _logger.Info($"Timestamp of {e.Touch.FileName} set to {e.Touch.LastWriteTime}");
                }
                else
                {
                    _logger.Error($"Setting timestamp of {e.Touch.FileName} failed: {e.Touch.Error}");
                }
            }
            else
            {
                // This should never happen during "local to remote" synchronization
                _logger.Info(
                    $"Timestamp of {e.Destination} kept with its default (current time)");
            }
        }

        public bool IsOpen() => _currentSession != null && _currentSession.Opened;

        public void Dispose() { if (_currentSession != null && _currentSession.Opened) _currentSession.Close(); }
    }
}
