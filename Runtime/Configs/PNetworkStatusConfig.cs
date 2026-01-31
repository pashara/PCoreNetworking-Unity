using UnityEngine;

// ReSharper disable once CheckNamespace
namespace PCore.Networking.Configs
{
    [CreateAssetMenu(menuName = "PCore/Networking/Config", fileName = "PNetworkStatusConfig")]
    public sealed class PNetworkStatusConfig : ScriptableObject
    {
        [Header("Ping endpoint")]
        // ReSharper disable once InconsistentNaming
        [SerializeField] private string _pingUrl = "https://clients3.google.com/generate_204";

        [Header("Success criteria")]
        // ReSharper disable once InconsistentNaming
        [SerializeField] private string _successResponse = "204";

        [Header("Polling")]
        [Min(0.1f)]
        // ReSharper disable once InconsistentNaming
        [SerializeField] private float _secondsBetweenChecks = 5f;

        [Header("Timeouts")]
        [Min(0.1f)]
        // ReSharper disable once InconsistentNaming
        [SerializeField] private int _timeoutSeconds = 4;


        // ReSharper disable once InconsistentNaming
        public bool ResetTimeOnPause = true;
        // ReSharper disable once InconsistentNaming
        public bool ResetTimeOnFocus = true;

        [Tooltip("Допуск совпадения времени устройства с сервером.")]
        // ReSharper disable once InconsistentNaming
        [Min(0f)] public float DeviceTimeToleranceMinutes = 2f;
        
        

        public string PingUrl
        {
            get => _pingUrl;
            set => _pingUrl = value;
        }

        public string SuccessResponse
        {
            get => _successResponse;
            set => _successResponse = value;
        }

        public float SecondsBetweenChecks
        {
            get => _secondsBetweenChecks;
            set => _secondsBetweenChecks = value;
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => _timeoutSeconds = value;
        }
    }
}
