using UnityEngine;

// ReSharper disable once CheckNamespace
namespace PCore.Networking.Internal
{
    internal sealed class PNetworkStateBehaviour : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool pause)
        {
            PNetworkState.InternalOnPause(pause);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            PNetworkState.InternalOnFocus(hasFocus);
        }
    }
}