using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FocusTrack.Business.Interfaces;
using FocusTrack.Data.Entities;
using FocusTrack.Data.Repositories;

namespace FocusTrack.Business.Services
{
    /// <summary>
    /// Polls the Windows foreground window via P/Invoke on a background Task and persists
    /// completed sessions through the Data layer. Never touches WinForms controls directly —
    /// the UI subscribes to <see cref="SessionRecorded"/> and marshals updates itself.
    /// </summary>
    public class WindowTrackerService : IWindowTrackerService
    {
        private const int PollIntervalMs = 1000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private readonly ISessionRepository _sessionRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly int _profileId;

        private IntPtr _lastHandle = IntPtr.Zero;
        private string _lastExecutable = string.Empty;
        private string _lastTitle = string.Empty;
        private DateTime _sessionStart;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public bool IsRunning { get; private set; }
        public event EventHandler<SessionRecordedEventArgs>? SessionRecorded;

        public WindowTrackerService(ISessionRepository sessionRepository, IApplicationRepository applicationRepository, int profileId = 1)
        {
            _sessionRepository = sessionRepository;
            _applicationRepository = applicationRepository;
            _profileId = profileId;
        }

        public Task StartTrackingAsync(CancellationToken cancellationToken)
        {
            if (IsRunning) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsRunning = true;
            _sessionStart = DateTime.Now;

            // Runs on a dedicated background Task — the UI thread is never blocked.
            _loopTask = Task.Run(() => PollLoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task StopTrackingAsync()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            if (_loopTask != null) await _loopTask;
            await CloseCurrentSessionAsync(DateTime.Now);
            IsRunning = false;
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var (exeName, title, handle) = GetForegroundWindowInfo();

                    if (handle != IntPtr.Zero && handle != _lastHandle)
                    {
                        var previousStart = _sessionStart;
                        var previousExe = _lastExecutable;
                        var previousTitle = _lastTitle;
                        var now = DateTime.Now;

                        if (_lastHandle != IntPtr.Zero && !string.IsNullOrEmpty(previousExe))
                        {
                            await PersistSessionAsync(previousExe, previousTitle, previousStart, now);
                        }

                        _lastHandle = handle;
                        _lastExecutable = exeName;
                        _lastTitle = title;
                        _sessionStart = now;
                    }
                }
                catch
                {
                    // Swallow transient P/Invoke failures (e.g. a window closing mid-read) —
                    // tracking must never crash the background loop or the app.
                }

                await Task.Delay(PollIntervalMs, token).ContinueWith(_ => { });
                if (token.IsCancellationRequested) break;
            }
        }

        private async Task CloseCurrentSessionAsync(DateTime endTime)
        {
            if (_lastHandle != IntPtr.Zero && !string.IsNullOrEmpty(_lastExecutable))
            {
                await PersistSessionAsync(_lastExecutable, _lastTitle, _sessionStart, endTime);
            }
        }

        private async Task PersistSessionAsync(string executableName, string title, DateTime start, DateTime end)
        {
            if ((end - start).TotalSeconds < 1) return; // ignore sub-second flickers

            var app = await _applicationRepository.GetOrCreateAsync(executableName, executableName);
            if (app.IsIgnored) return;

            var session = new Session
            {
                ApplicationId = app.Id,
                ProfileId = _profileId,
                WindowTitle = title,
                StartTime = start,
                EndTime = end
            };

            await _sessionRepository.AddSessionAsync(session);

            SessionRecorded?.Invoke(this, new SessionRecordedEventArgs
            {
                ApplicationName = app.DisplayName,
                DurationSeconds = session.DurationSeconds
            });
        }

        private (string exeName, string title, IntPtr handle) GetForegroundWindowInfo()
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) return (string.Empty, string.Empty, IntPtr.Zero);

            var sb = new StringBuilder(256);
            GetWindowText(handle, sb, sb.Capacity);
            string title = sb.ToString();

            string exeName = string.Empty;
            try
            {
                GetWindowThreadProcessId(handle, out uint pid);
                using var process = Process.GetProcessById((int)pid);
                exeName = process.ProcessName + ".exe";
            }
            catch
            {
                exeName = "unknown.exe";
            }

            return (exeName, title, handle);
        }
    }
}
