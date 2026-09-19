using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    Broom,
    Mop,
    GarbageBag
}

public class PlayerEquipmentInventory : MonoBehaviour
{
    public static PlayerEquipmentInventory Instance { get; private set; }

    [SerializeField] private Transform equipmentHandPoint;

    private readonly Dictionary<EquipmentType, GameObject> ownedItems = new();
    private readonly Dictionary<EquipmentType, Transform> itemHandPoints = new();
    private readonly Dictionary<EquipmentType, Transform> originalParents = new();
    private readonly Dictionary<EquipmentType, Vector3> originalPositions = new();
    private readonly Dictionary<EquipmentType, Quaternion> originalRotations = new();
    private readonly Dictionary<EquipmentType, Collider2D[]> itemColliders = new();
    private readonly Dictionary<EquipmentType, bool[]> originalColliderStates = new();
    private readonly Dictionary<EquipmentType, bool> originalInteractableStates = new();

    public EquipmentType? SelectedEquipment { get; private set; }
    public event Action InventoryChanged;

    private void Awake()
    {
        Instance = this;
        ResolveEquipmentHandPoint();
    }

    public bool Owns(EquipmentType equipment)
    {
        return ownedItems.TryGetValue(equipment, out GameObject item)
            && item != null;
    }

    public IReadOnlyCollection<EquipmentType> OwnedEquipment =>
        ownedItems.Keys;

    public void Add(
        EquipmentType equipment,
        GameObject visual,
        Transform itemHandPoint = null)
    {
        if (visual == null)
            return;

        ResolveEquipmentHandPoint();
        ownedItems[equipment] = visual;
        originalParents[equipment] = visual.transform.parent;
        originalPositions[equipment] = visual.transform.position;
        originalRotations[equipment] = visual.transform.rotation;
        itemHandPoints[equipment] = itemHandPoint != null
            ? itemHandPoint
            : equipmentHandPoint;
        Collider2D[] colliders = visual.GetComponentsInChildren<Collider2D>(true);
        itemColliders[equipment] = colliders;
        bool[] colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;
        originalColliderStates[equipment] = colliderStates;

        Interactable interactable = visual.GetComponent<Interactable>();
        if (interactable != null)
            originalInteractableStates[equipment] = interactable.enabled;

        PrepareVisual(visual);
        Select(equipment);
    }

    public void Select(EquipmentType equipment)
    {
        if (!Owns(equipment))
            return;

        ResolveEquipmentHandPoint();
        SelectedEquipment = equipment;

        foreach (KeyValuePair<EquipmentType, GameObject> item in ownedItems)
        {
            if (item.Value == null)
                continue;

            bool isSelected = item.Key == equipment;
            item.Value.SetActive(isSelected);

            if (isSelected)
            {
                Transform handPoint = itemHandPoints[item.Key];
                item.Value.transform.SetParent(handPoint, false);
                item.Value.transform.localPosition = Vector3.zero;
                item.Value.transform.localRotation = Quaternion.identity;
            }
        }

        InventoryChanged?.Invoke();
    }

    public void ResetForDay()
    {
        foreach (KeyValuePair<EquipmentType, GameObject> item in ownedItems)
        {
            if (item.Value == null)
                continue;

            item.Value.transform.SetParent(originalParents[item.Key], true);
            item.Value.transform.position = originalPositions[item.Key];
            item.Value.transform.rotation = originalRotations[item.Key];
            Collider2D[] colliders = itemColliders[item.Key];
            bool[] colliderStates = originalColliderStates[item.Key];
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = colliderStates[i];

            Interactable interactable = item.Value.GetComponent<Interactable>();
            if (interactable != null &&
                originalInteractableStates.TryGetValue(item.Key, out bool interactableState))
                interactable.enabled = interactableState;

            item.Value.SetActive(false);
        }

        ownedItems.Clear();
        itemHandPoints.Clear();
        originalParents.Clear();
        originalPositions.Clear();
        originalRotations.Clear();
        itemColliders.Clear();
        originalColliderStates.Clear();
        originalInteractableStates.Clear();
        SelectedEquipment = null;
        InventoryChanged?.Invoke();
    }

    private void ResolveEquipmentHandPoint()
    {
        if (equipmentHandPoint != null)
            return;

        Transform existingPoint = transform.Find("EquipmentPoint");
        if (existingPoint != null)
        {
            equipmentHandPoint = existingPoint;
            return;
        }

        GameObject point = new GameObject("EquipmentPoint");
        equipmentHandPoint = point.transform;
        equipmentHandPoint.SetParent(transform, false);
        equipmentHandPoint.localPosition = new Vector3(-0.37f, -0.234f, 0f);
    }

    private void PrepareVisual(GameObject visual)
    {
        Collider2D[] colliders = visual.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
            collider.enabled = false;

        Interactable interactable = visual.GetComponent<Interactable>();
        if (interactable != null)
            interactable.enabled = false;
    }
}
