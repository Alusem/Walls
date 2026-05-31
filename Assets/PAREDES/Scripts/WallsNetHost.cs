using System.Collections;
using UnityEngine;

/// <summary>
/// Executa corrotinas quando o GameObject original já não existe (ex.: enviar pontuação após game over).
/// </summary>
public sealed class WallsNetHost : MonoBehaviour
{
    static WallsNetHost _instance;

    public static void Run(IEnumerator routine)
    {
        if (routine == null)
            return;
        if (_instance == null)
        {
            var go = new GameObject("WallRushNetHost");
            _instance = go.AddComponent<WallsNetHost>();
            DontDestroyOnLoad(go);
        }

        _instance.StartCoroutine(routine);
    }
}
