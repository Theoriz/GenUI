using UnityEngine;

// Holds every prefab UIMaster instantiates, so the references live in one asset instead of on each
// UIMaster instance. UIMaster loads it from Resources at runtime; adding a supported type means adding
// a field here and assigning it in the GenUIPrefabs asset — no per-instance wiring to keep in sync.
[CreateAssetMenu(fileName = "GenUIPrefabs", menuName = "Theoriz/GenUI/Prefabs")]
public class UIPrefabs : ScriptableObject
{
    public GameObject PanelPrefab;
    public GameObject MethodButtonPrefab;
    public GameObject SliderPrefab;
    public GameObject CheckboxPrefab;
    public GameObject InputPrefab;
    public GameObject DropdownPrefab;
    public GameObject HeaderTextPrefab;
    public GameObject TooltipTextPrefab;
    public GameObject ColorPrefab;
    public GameObject Vector2Prefab;
    public GameObject Vector2IntPrefab;
    public GameObject Vector3Prefab;
    public GameObject Vector3IntPrefab;
    public GameObject Vector4Prefab;

    public GameObject RightClickMenuPrefab;
    public GameObject ColorPickerPrefab;
}
