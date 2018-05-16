using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class TableVisual : MonoBehaviour 
{
    public AreaPosition owner;
    public SameDistanceChildren slots;
    private List<GameObject> CreaturesOnTable = new List<GameObject>();
    private bool cursorOverThisTable = false;

    private BoxCollider col;

    public static bool CursorOverSomeTable
    {
        get
        {
            TableVisual[] bothTables = GameObject.FindObjectsOfType<TableVisual>();
            return (bothTables[0].CursorOverThisTable || bothTables[1].CursorOverThisTable);
        }
    }

    public bool CursorOverThisTable
    {
        get{ return cursorOverThisTable; }
    }

    void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    void Update()
    {
        RaycastHit[] hits;
        hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 30f);

        bool passedThroughTableCollider = false;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == col)
                passedThroughTableCollider = true;
        }
        cursorOverThisTable = passedThroughTableCollider;
    }
   
    public void AddCreatureAtIndex(CardAsset ca, int UniqueID ,int index)
    {
        GameObject creature = GameObject.Instantiate(GlobalSettings.Instance.CreaturePrefab, slots.Children[index].transform.position, Quaternion.identity) as GameObject;

        OneCreatureManager manager = creature.GetComponent<OneCreatureManager>();
        manager.cardAsset = ca;
        manager.ReadCreatureFromAsset();

        // add tag according to owner
        foreach (Transform t in creature.GetComponentsInChildren<Transform>())
            t.tag = owner.ToString() + "Creature";

        creature.transform.SetParent(slots.transform);
        CreaturesOnTable.Insert(index, creature);

        WhereIsTheCardOrCreature w = creature.GetComponent<WhereIsTheCardOrCreature>();
        w.Slot = index;
        if (owner == AreaPosition.Low)
            w.VisualState = VisualStates.LowTable;
        else
            w.VisualState = VisualStates.TopTable;

        IDHolder id = creature.AddComponent<IDHolder>();
        id.UniqueID = UniqueID;

        ShiftSlotsGameObjectAccordingToNumberOfCreatures();
        PlaceCreaturesOnNewSlots();

        Command.CommandExecutionComplete();
    }


    // returns an index for a new creature based on mousePosition
    // included for placing a new creature to any positon on the table
    public int TablePosForNewCreature(float MouseX)
    {
        // if there are no creatures or if we are pointing to the right of all creatures with a mouse.
        // right - because the table slots are flipped and 0 is on the right side.
        if (CreaturesOnTable.Count == 0 || MouseX > slots.Children[0].transform.position.x)
            return 0;
        else if (MouseX < slots.Children[CreaturesOnTable.Count - 1].transform.position.x) // cursor on the left relative to all creatures on the table
            return CreaturesOnTable.Count;
        for (int i = 0; i < CreaturesOnTable.Count; i++)
        {
            if (MouseX < slots.Children[i].transform.position.x && MouseX > slots.Children[i + 1].transform.position.x)
                return i + 1;
        }
        Debug.Log("Suspicious behavior. Reached end of TablePosForNewCreature method. Returning 0");
        return 0;
    }

    // Destroy a creature
    public void RemoveCreatureWithID(int IDToRemove)
    {
        // TODO: This has to last for some time
        // Adding delay here did not work because it shows one creature die, then another creature die. 
        // 
        //Sequence s = DOTween.Sequence();
        //s.AppendInterval(1f);
        //s.OnComplete(() =>
        //   {
                
        //    });
        GameObject creatureToRemove = IDHolder.GetGameObjectWithID(IDToRemove);
        CreaturesOnTable.Remove(creatureToRemove);
        Destroy(creatureToRemove);

        ShiftSlotsGameObjectAccordingToNumberOfCreatures();
        PlaceCreaturesOnNewSlots();
        Command.CommandExecutionComplete();
    }

    /// <summary>
    /// Shifts the slots game object according to number of creatures.
    /// </summary>
    void ShiftSlotsGameObjectAccordingToNumberOfCreatures()
    {
        float posX;
        if (CreaturesOnTable.Count > 0)
            posX = (slots.Children[0].transform.localPosition.x - slots.Children[CreaturesOnTable.Count - 1].transform.localPosition.x) / 2f;
        else
            posX = 0f;

        slots.gameObject.transform.DOLocalMoveX(posX, 0.3f);  
    }

    /// <summary>
    /// After a new creature is added or an old creature dies, this method
    /// shifts all the creatures and places the creatures on new slots.
    /// </summary>
    void PlaceCreaturesOnNewSlots()
    {
        foreach (GameObject g in CreaturesOnTable)
        {
            g.transform.DOLocalMoveX(slots.Children[CreaturesOnTable.IndexOf(g)].transform.localPosition.x, 0.3f);
            // apply correct sorting order and HandSlot value for later 
            // TODO: figure out if I need to do something here:
            // g.GetComponent<WhereIsTheCardOrCreature>().SetTableSortingOrder() = CreaturesOnTable.IndexOf(g);
        }
    }

}
