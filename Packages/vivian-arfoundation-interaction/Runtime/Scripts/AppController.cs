using System;
using System.Collections.Generic;
using de.ugoe.cs.vivian.core;
using UnityEngine;
using UnityEngine.UI;

public class AppController : MonoBehaviour
{
    public Button returnButtonGameObject;
    public GameObject menuCanvas;
    public GameObject appCanvas;
    public GameObject vivianFrameworkPrefab;
    public ObjectPositioner objectPositioner;
    private VirtualPrototype virtualPrototype;

    [Serializable]
    public struct PrototypeUrls
    {
        public string bundleURL;
        public string prototypePrefabName;
        public Button buttonGameObject;
    };
    public List<PrototypeUrls> prototypeUrls;

    void Start()
    {
        // Deactivate OP to avoid raycasts
        objectPositioner.enabled = false;
        // Add listeners to the buttons
        foreach (PrototypeUrls pUrl in prototypeUrls)
        {
            pUrl.buttonGameObject.onClick.AddListener(() => LoadPrototype(pUrl.bundleURL, pUrl.prototypePrefabName));
        }
        returnButtonGameObject.onClick.AddListener(() => ToggleMenu(true));
    }

    private void ToggleMenu(bool bToggle)
    {
        this.appCanvas.SetActive(!bToggle);
        this.menuCanvas.SetActive(bToggle);
        if (bToggle)
            objectPositioner.enabled = false;
    }

    private void LoadPrototype(string url, string prefabName)
    {
        // Hide menu
        ToggleMenu(false);

        if (virtualPrototype != null && virtualPrototype.BundleURL != url)
        {
            Destroy(virtualPrototype.gameObject);
            InstantiateVirtualPrototype(url, prefabName);
        }
        else if(virtualPrototype == null)
            InstantiateVirtualPrototype(url, prefabName);
    }

    private void InstantiateVirtualPrototype(string url, string prefabName)
    {
        virtualPrototype = Instantiate(vivianFrameworkPrefab).GetComponent<VirtualPrototype>();
        virtualPrototype.BundleURL = url;
        virtualPrototype.PrototypePrefabName = prefabName;
        objectPositioner.enabled = true;
        objectPositioner.ResetVirtualPrototype(virtualPrototype);
    }
}
