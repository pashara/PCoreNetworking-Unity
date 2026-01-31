using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PCore.Networking.Configs;
using PCore.Networking.Internal;

#if PCORE_NETWORK_UNIRX
using UniRx;
#endif

#if PCORE_NETWORK_R3
using R3;
#endif

// ReSharper disable once CheckNamespace
namespace PCore.Networking
{
    public static class PNetworkState
    {
        private const string NetworkConfigName = "PNetworkStatusConfig";
        
        private static bool _bootstrapped;
        private static InternetCheckService _service;


#region Time Protection
        
        public static bool TimeProtected => Ensure().TimeProtection.IsProtected;

        public static DateTime UtcNow => Ensure().TimeProtection.HasSync
            ? Ensure().TimeProtection.UtcNow
            : DateTime.UtcNow;

        public static DateTime Now => Ensure().TimeProtection.HasSync
            ? Ensure().TimeProtection.Now
            : DateTime.Now;

        public static event Action<bool> OnTimeProtectedChanged;

#if PCORE_NETWORK_UNIRX || PCORE_NETWORK_R3
        public static IReactiveProperty<bool> TimeProtectedRx => _isTimeProtectedRx ??= new ReactiveProperty<bool>(false);
        private static ReactiveProperty<bool> _isTimeProtectedRx;
#endif
        
        public static void ResetTimeProtection()
        {
            Ensure().TimeProtection.ResetProtection();
        }

#endregion



#region Network Status

        public static bool InternetAvailable => Ensure().InternetAvailable;

        public static bool HasEverChecked => Ensure().HasEverChecked;

        public static event Action<bool> OnStatusChanged;

#if PCORE_NETWORK_UNIRX || PCORE_NETWORK_R3
        public static IReactiveProperty<bool> InternetAvailableRx => _internetAvailableRx ??= new ReactiveProperty<bool>(false);
        public static IReactiveProperty<bool> HasEverCheckedRx => _hasEverCheckedRx ??= new ReactiveProperty<bool>(false);
        private static ReactiveProperty<bool> _internetAvailableRx;
        private static ReactiveProperty<bool> _hasEverCheckedRx;
#endif
        

        public static UniTask ForceCheckNetworkStatusAsync(CancellationToken ct = default)
        {
            return Ensure().ForceCheck(ct);
        }
        
#endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_bootstrapped) return;
            _bootstrapped = true;

            var go = new GameObject("[PNetworkHandler:Runtime]");
            go.AddComponent<PNetworkStateBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(go);

            var config = Resources.Load<PNetworkStatusConfig>(NetworkConfigName);

            _service = new InternetCheckService(config);
            _service.StatusChanged += HandleStatusChanged;
            _service.TimeProtection.ProtectedChanged += p =>
            {
                SyncReactiveState();
                OnTimeProtectedChanged?.Invoke(p);
            };
            
            _service.Start();

            SyncReactiveState();
        }

        private static InternetCheckService Ensure()
        {
            if (!_bootstrapped) Bootstrap();
            return _service;
        }

        private static void HandleStatusChanged(bool status)
        {
            SyncReactiveState();
            OnStatusChanged?.Invoke(status);
        }

        private static void SyncReactiveState()
        {
#if PCORE_NETWORK_UNIRX || PCORE_NETWORK_R3
            if (_internetAvailableRx != null) _internetAvailableRx.Value = _service != null && _service.InternetAvailable;
            if (_hasEverCheckedRx != null) _hasEverCheckedRx.Value = _service != null && _service.HasEverChecked;
#endif
        
            
#if PCORE_NETWORK_UNIRX || PCORE_NETWORK_R3
            if (_isTimeProtectedRx != null) _isTimeProtectedRx.Value = _service != null && _service.TimeProtection.IsProtected;
#endif
            
        }

        internal static void InternalOnPause(bool pause)
        {
            Ensure().OnApplicationPause(pause);
            SyncReactiveState();
        }

        internal static void InternalOnFocus(bool hasFocus)
        {
            Ensure().OnApplicationFocus(hasFocus);
            SyncReactiveState();
        }

    }
}
