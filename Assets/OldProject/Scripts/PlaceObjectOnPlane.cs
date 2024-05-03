using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[System.Serializable]
public class PrefabArray
{
    public GameObject[] prefabs;
}

public class PlaceObjectOnPlane : MonoBehaviour
{
    [SerializeField] private PrefabArray[] prefabGroups;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI countText; 

    private List<GameObject> placedObjects = new List<GameObject>();
    // model cycling timers for each object
    private List<float> timers = new List<float>();
    // current model index for each object
    private List<int> currentModelIndices = new List<int>();
    // group index for each object
    private List<int> currentGroupIndices = new List<int>(); 

    private ARRaycastManager raycaster;
    private float placementCooldown = 0.5f;
    private float timeSinceLastPlacement = 0;

    private float gameTimer = 20.0f;
    private bool gameActive = false; 
    private int objectsPlaced = 0;

    void Update()
    {
        // check if the game has started
        if (gameActive)
        {
            gameTimer -= Time.deltaTime;
            timerText.text = "Time: " + gameTimer.ToString("F2") + "s";

            if (gameTimer <= 0)
            {
                // set the game active to false
                gameActive = false;
                // reset the game timer
                gameTimer = 20f;
                timerText.text = "Game Over!";
                countText.text = "Objects Placed: " + objectsPlaced.ToString();
                // reset the number of objects placed
                objectsPlaced = 0;
            }
        }

        timeSinceLastPlacement += Time.deltaTime; // update the placement cooldown timer

        // update model cycling timers for each placed object
        for (int i = 0; i < placedObjects.Count; i++)
        {
            timers[i] += Time.deltaTime;
            if (timers[i] >= 1.0f)
            {
                timers[i] = 0;
                CycleModel(i);
            }
        }
    }

    public void StartGame()
    {
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }

        placedObjects.Clear();
        timers.Clear();
        currentModelIndices.Clear(); 
        currentGroupIndices.Clear();

        gameActive = true;
        gameTimer = 20.0f;
        objectsPlaced = 0;
        timerText.text = "Time: 20.00s"; 
        countText.text = "";

        // reset placement cooldown timer to avoid immediate placement after starting
        timeSinceLastPlacement = placementCooldown;
    }

    public void OnPlaceObject(InputValue value)
    {
        if (!gameActive || timeSinceLastPlacement < placementCooldown) return;

        Vector2 touchPosition = value.Get<Vector2>();
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycaster.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            int randomGroupIndex = Random.Range(0, prefabGroups.Length);
            GameObject[] group = prefabGroups[randomGroupIndex].prefabs;
            int modelIndex = Random.Range(0, group.Length);
            GameObject newObject = Instantiate(group[modelIndex], hitPose.position, hitPose.rotation);
            newObject.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);

            placedObjects.Add(newObject);
            timers.Add(0);
            currentModelIndices.Add(modelIndex);
            currentGroupIndices.Add(randomGroupIndex);
            objectsPlaced++;
            timeSinceLastPlacement = 0;
        }
    }

    void Start()
    {
        raycaster = GetComponent<ARRaycastManager>();
    }

    void CycleModel(int index)
    {
        // get current object and its model/group index
        GameObject currentObject = placedObjects[index];
        int groupIndex = currentGroupIndices[index];
        int modelIndex = currentModelIndices[index];

        // determine next model index
        int nextModelIndex = (modelIndex + 1) % prefabGroups[groupIndex].prefabs.Length;
        currentModelIndices[index] = nextModelIndex;

        // replace the current object with the next model

        Vector3 position = currentObject.transform.position;
        Quaternion rotation = currentObject.transform.rotation;
        Destroy(currentObject);
        GameObject newModel = Instantiate(prefabGroups[groupIndex].prefabs[nextModelIndex], position, rotation);
        newModel.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
        placedObjects[index] = newModel;
    }
}