using System;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using PCore.Networking.Configs;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace PCore.Networking.Internal
{
    internal sealed class InternetCheckService : IDisposable
    {
        private readonly object _gate = new object();

        private readonly PNetworkStatusConfig _config;

        private bool _isPaused;
        private bool _hasFocus = true;

        private bool _hasEverChecked;
        private bool _internetAvailable;

        private bool _running;
        private UniTask _loopTask;
        private CancellationTokenSource _cts;

        internal event Action<bool> StatusChanged;
        internal TimeProtectionService TimeProtection { get; }

        internal bool InternetAvailable { get { lock (_gate) return _internetAvailable; } }
        internal bool HasEverChecked { get { lock (_gate) return _hasEverChecked; } }

        public InternetCheckService(PNetworkStatusConfig config)
        {
            _config = config != null ? config : CreateFallbackConfig();
            TimeProtection = new TimeProtectionService();
        }

        public void Start()
        {
            lock (_gate)
            {
                if (_running) return;
                _running = true;
                _cts = new CancellationTokenSource();
            }

            _loopTask = LoopAsync(_cts.Token);
            _loopTask.Forget();
        }

        public void Stop()
        {
            CancellationTokenSource cts;

            lock (_gate)
            {
                if (!_running) return;
                _running = false;
                cts = _cts;
                _cts = null;
            }

            try
            {
                cts?.Cancel();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                cts?.Dispose();
            }
        }

        public void Dispose() => Stop();

        public void OnApplicationPause(bool pause)
        {
            lock (_gate) _isPaused = pause;

            if (pause)
            {
                if (_config.ResetTimeOnPause)
                {
                    TimeProtection.ResetProtection();
                }
            }
            else
            {
                ForceCheck().Forget();
            }
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            lock (_gate) _hasFocus = hasFocus;

            if (!hasFocus)
            {
                if (_config.ResetTimeOnFocus)
                {
                    TimeProtection.ResetProtection();
                }
            }
        }

        public UniTask ForceCheck(CancellationToken ctx = default)
        {
            CancellationTokenSource cts;
            lock (_gate)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(_cts?.Token ?? CancellationToken.None, ctx);
            }
            return CheckOnceAsync(cts.Token);
        }

        private async UniTask LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                bool shouldPause;
                float interval;

                lock (_gate)
                {
                    shouldPause = _isPaused || !_hasFocus;
                    interval = Mathf.Max(0.1f, _config.SecondsBetweenChecks);
                }

                Logger.Log("NetworkChecker", $"On check cycle. Should pause: {shouldPause} = {_isPaused} || {!_hasFocus}");
                if (shouldPause)
                {
                    await UniTask.Delay(TimeSpan.FromMilliseconds(250), ignoreTimeScale: true, cancellationToken: ct);
                    continue;
                }

                await CheckOnceAsync(ct);

                await UniTask.Delay(TimeSpan.FromSeconds(interval), ignoreTimeScale: true, cancellationToken: ct);
            }
        }
        

        private async UniTask CheckOnceAsync(CancellationToken ct)
        {
            Logger.Log("NetworkChecker", "CheckOnceAsync");
            string url;
            string successResponse;
            int timeout;

            lock (_gate)
            {
                url = _config.PingUrl;
                successResponse = _config.SuccessResponse;
                timeout = Mathf.Max(1, _config.TimeoutSeconds);
            }

            var ok = false;

            try
            {
                await UniTask.SwitchToMainThread(ct);

                using var req = UnityWebRequest.Get(url);
                req.timeout = timeout;

                await req.SendWebRequest().ToUniTask(cancellationToken: ct);
                ok = req.result == UnityWebRequest.Result.Success &&
                     IsSuccess(req.responseCode, req.downloadHandler?.text, successResponse);
                if (ok)
                {
                    var dateHeader = req.GetResponseHeader("Date");
                    TimeProtection.TryApplyHttpDate(dateHeader, _config.DeviceTimeToleranceMinutes);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (UnityWebRequestException)
            {
            }
            catch (Exception e)
            {
                ok = false;
                Debug.LogException(e);
            }
            finally
            {
                ApplyStatus(ok);
            
                Logger.Log("NetworkChecker", $"CheckOnceAsync status: {ok}");
            }

        }

        private void ApplyStatus(bool newStatus)
        {
            bool raise;

            lock (_gate)
            {
                var oldStatus = _internetAvailable;
                var wasChecked = _hasEverChecked;

                _hasEverChecked = true;
                _internetAvailable = newStatus;

                // Status valid after first request
                raise = (!wasChecked) || (oldStatus != newStatus);
            }

            if (raise)
                StatusChanged?.Invoke(newStatus);
        }

        private static bool IsSuccess(long responseCode, string body, string successResponse)
        {
            successResponse = successResponse?.Trim() ?? "";

            if (long.TryParse(successResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expectedCode))
                return responseCode == expectedCode;

            body ??= "";
            return string.Equals(body.Trim(), successResponse, StringComparison.Ordinal);
        }

        private static PNetworkStatusConfig CreateFallbackConfig()
        {
            var cfg = ScriptableObject.CreateInstance<PNetworkStatusConfig>();
            cfg.PingUrl = "https://clients3.google.com/generate_204";
            cfg.SuccessResponse = "204";
            cfg.SecondsBetweenChecks = 5f;
            cfg.TimeoutSeconds = 4;
            return cfg;
        }
    }
}
