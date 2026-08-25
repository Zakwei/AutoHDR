using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AutoHDR
{
    public class GameMonitor : IDisposable
    {
        private readonly HashSet<string> _gameNames;
        private readonly HdrController _hdr;
        private readonly Action<string> _log;
        private readonly Timer _timer;
        private bool _gameRunning;

        public bool GameRunning => _gameRunning;

        public GameMonitor(IEnumerable<string> gameNames, HdrController hdr, Action<string> log)
        {
            _gameNames = new HashSet<string>(gameNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.ToLowerInvariant()));
            _hdr = hdr;
            _log = log;
            _timer = new Timer(Check, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        private void Check(object state)
        {
            try
            {
                bool any = false;
                string foundGame = null;
                Process[] processes = null;

                try
                {
                    processes = Process.GetProcesses();
                }
                catch (Exception ex)
                {
                    _log?.Invoke(Locale.Get("ProcessReadError") + ": " + ex.Message);
                    return;
                }

                foreach (var p in processes)
                {
                    try
                    {
                        string name = p.ProcessName;
                        if (string.IsNullOrEmpty(name)) continue;

                        if (_gameNames.Contains(name.ToLowerInvariant()))
                        {
                            any = true;
                            foundGame = name;
                            break;
                        }
                    }
                    catch
                    {
                        // Process mogl zostac zamkniety.
                    }
                    finally
                    {
                        try { p.Dispose(); } catch { }
                    }
                }

                if (any && !_gameRunning)
                {
                    _gameRunning = true;
                    _log?.Invoke(Locale.Format("GameDetected", foundGame));
                    try
                    {
                        int enabled = _hdr.BeginGameMode();
                        if (enabled > 0)
                            _log?.Invoke(Locale.Format("GameEnabledCount", enabled));
                        else
                            _log?.Invoke(Locale.Get("NoHDRToEnable"));
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke(Locale.Get("HDREnableError") + ": " + ex.Message);
                    }
                }
                else if (!any && _gameRunning)
                {
                    _gameRunning = false;
                    _log?.Invoke(Locale.Get("LastGameClosed"));
                    try
                    {
                        int restored = _hdr.EndGameMode();
                        if (restored > 0)
                            _log?.Invoke(Locale.Format("HDRRestoredCount", restored));
                        else
                            _log?.Invoke(Locale.Get("HDRStaysOn"));
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke(Locale.Get("HDRDisableError") + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke(Locale.Get("MonitorError") + ": " + ex.Message);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
