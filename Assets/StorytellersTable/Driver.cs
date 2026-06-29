
using StorytellersTable.Map;
using UnityEngine;

[DisallowMultipleComponent]
public class Driver : MonoBehaviour
{
    public void OnEnable()
    {
        MapDataManager.Instance.SwitchActiveMap("simulated world map load from disk");
    }
}
