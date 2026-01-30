using System;
using System.Globalization;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace PCore.Networking
{
    internal sealed class TimeProtectionService
    {
        private readonly object _gate = new object();

        private bool _isProtected;
        private bool _hasSync;

        private DateTime _serverUtcAtSync;
        private double _realtimeAtSync;

        private bool _deviceTimeLooksValid;

        internal event Action<bool> ProtectedChanged;
        internal bool IsProtected { get { lock (_gate) return _isProtected; } }
        internal bool HasSync { get { lock (_gate) return _hasSync; } }

        internal DateTime UtcNow
        {
            get
            {
                lock (_gate)
                {
                    if (!_hasSync) return default;

                    var elapsed = Time.realtimeSinceStartupAsDouble - _realtimeAtSync;
                    return _serverUtcAtSync.AddSeconds(elapsed);
                }
            }
        }

        internal DateTime Now => HasSync ? UtcNow.ToLocalTime() : default;

        internal void ResetProtection()
        {
            bool raise = false;

            lock (_gate)
            {
                if (_isProtected)
                {
                    _isProtected = false;
                    raise = true;
                }
                _hasSync = false;
            }

            if (raise)
                ProtectedChanged?.Invoke(false);
        }


        internal bool TryApplyHttpDate(string httpDate, float toleranceMinutes)
        {
            if (string.IsNullOrEmpty(httpDate))
                return false;

            // RFC1123: "Tue, 30 Jan 2026 20:15:01 GMT"
            if (!DateTime.TryParseExact(httpDate, "r", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var serverUtc))
                return false;

            var deviceUtc = DateTime.UtcNow;
            var diff = (deviceUtc - serverUtc).Duration();
            var looksValid = diff <= TimeSpan.FromMinutes(Mathf.Max(0f, toleranceMinutes));

            bool raise;
            lock (_gate)
            {
                _serverUtcAtSync = serverUtc;
                _realtimeAtSync = Time.realtimeSinceStartupAsDouble;
                _deviceTimeLooksValid = looksValid;

                _hasSync = true;

                raise = !_isProtected;
                _isProtected = true;
            }

            if (raise)
                ProtectedChanged?.Invoke(true);

            return true;
        }
    }
}
