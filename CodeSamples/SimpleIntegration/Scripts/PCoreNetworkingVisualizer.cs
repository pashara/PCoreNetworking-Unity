using System;
using TMPro;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace PCore.Networking.Samples.SimpleIntegration
{
    public class PCoreNetworkingVisualizer : MonoBehaviour
    {
        private const string OkColor = "#00A209";
        private const string ErrorColor = "#FF0000";
    
        // ReSharper disable once InconsistentNaming
        [SerializeField] private TMP_Text _statusOnStartText;
        // ReSharper disable once InconsistentNaming
        [SerializeField] private TMP_Text _statusText;
        // ReSharper disable once InconsistentNaming
        [SerializeField] private TMP_Text _dateText;
    
        private void OnEnable()
        {
            OnNetChanged(PNetworkState.InternetAvailable);
            PNetworkState.OnStatusChanged += OnNetChanged;

            PNetworkStateOnOnProtectedChanged(PNetworkState.TimeProtected);
            PNetworkState.OnTimeProtectedChanged += PNetworkStateOnOnProtectedChanged;

            InvokeRepeating(nameof(UpdateDateTime), 0f, 0.5f);
        }

        private void OnDisable()
        {
            PNetworkState.OnStatusChanged -= OnNetChanged;
            PNetworkState.OnTimeProtectedChanged -= PNetworkStateOnOnProtectedChanged;
            CancelInvoke(nameof(UpdateDateTime));
        }

        private void Start()
        {
            Debug.Log($"HasEverChecked: {PNetworkState.HasEverChecked}");
            Debug.Log($"InternetAvailable: {PNetworkState.InternetAvailable}");
        
            _statusOnStartText.SetText($"HasEverChecked: {PNetworkState.HasEverChecked}\n" +
                                       $"InternetAvailable: {PNetworkState.InternetAvailable}");
        }

        private void UpdateDateTime()
        {
            PNetworkStateOnOnProtectedChanged(PNetworkState.TimeProtected);
        }

        private void OnNetChanged(bool ok)
        {
            _statusText.SetText(
                $"Internet status: {(ok ? $"<color={OkColor}>HasInternet</color>" : $"<color={ErrorColor}>NoInternet</color>")}\n" +
                $"Has ever checked: {(PNetworkState.HasEverChecked ? $"<color={OkColor}>Yes</color>" : $"<color={ErrorColor}>No</color>")}");
        }


        private void PNetworkStateOnOnProtectedChanged(bool isProtected)
        {
            var delta = (DateTime.Now - PNetworkState.Now);
            _dateText.SetText("Time:\n" +
                              $"Is Protected: {isProtected}\n" +
                              $"D: {DateTime.Now}\n" +
                              $"N: <color={(isProtected ? OkColor : ErrorColor)}>{PNetworkState.Now}</color> {delta}");
        }
    }
}