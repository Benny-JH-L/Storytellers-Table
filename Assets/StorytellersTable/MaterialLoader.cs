
using UnityEngine;
using System.Collections.Generic;
using StorytellersTable.Utility.Log;
using System.Linq;

/// <summary>
/// Class to load and store all materials in the "/Resources/Material" directory.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]   // ensure this is exectued before most classes, so other classes can access this
public class MaterialLoader : MonoBehaviour
{
    public static MaterialLoader instance;
    private Dictionary<string, Material> materialMap;
    private List<string> originalMatNames;
    private string materialPath = "Material";  // relative path for "/Resources" directory
    
    public readonly string defaultMaterialName = "grass";

    private void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        materialMap = new Dictionary<string, Material>();
        originalMatNames = new List<string>();

        Material[] matList = Resources.LoadAll<Material>(materialPath);

        string s = $"Num materials loaded from `/Resouces/{materialPath}`[{matList.Length}] -> View More\n";
        foreach(Material mat in matList)
        {
            materialMap[mat.name.ToLower()] = mat;
            originalMatNames.Add(mat.name);
            s += $"{mat.name}\n";
        }
        DebugOut.Log(this, s);
    }

    /// <summary>
    /// Returns a list of all loaded materials names.
    /// </summary>
    /// <returns></returns>
    public List<string> GetMaterialNames()
    {
        return new List<string>(materialMap.Keys.ToList());
    }

    /// <summary>
    /// Returns a Material with <paramref name="materialName"/>.
    /// </summary>
    /// <param name="materialName"></param>
    /// <returns></returns>
    public Material GetMaterial(string materialName)
    {
        string matName = materialName.ToLower();

        if (!materialMap.ContainsKey(matName))
        {
            #if UNITY_EDITOR
            DebugOut.Log(this, $"{materialName} could not be found, returning default material");
            #endif

            return GetDefaultMaterial();
        }

        return materialMap[matName];
    }

    public Material GetDefaultMaterial()
    {
        return materialMap[defaultMaterialName];
    }

}

