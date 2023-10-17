using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WinWebViewGameObject : MonoBehaviour
{
    public Action OnDestroyed;
    private bool _wasRunInBackground;
    void OnStart()
    {
    }

    void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
}

