using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
     public string id;
     public List<string> parentRoomNodeIDList = new List<string>();
     public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    public RoomNodeTypeSO roomNodeType;

    #region Editor Code

    //the following code should only be run in the Unity Editor
#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;
    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;

        //load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;

    }
    public void Draw(GUIStyle nodeStyle)
    {
        //Draw node box using begin area
        //put other things between begin and end area
        GUILayout.BeginArea(rect, nodeStyle);

        //Start region to detect popup selection changes
        EditorGUI.BeginChangeCheck();

        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            //display a lable that cant be changed
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            //Display a popup using the RoomNodeType name values that can be selected from (default to the currently set roomNodeType)

            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];
            //If the room type selection is not changed making child connections potentially invalid
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor||
                !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor||
                !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                //if a room node type has been changed and it already has children then delete the parent child links since we need to revalidate any
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        //Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);
                        //if child room node is not null
                        if (childRoomNode != null )
                        {
                            //Remove Child ID from parent room node 
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                            //Remove parent ID from child room node 
                            childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                        }

                    }
                }

            }

        }


        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(this);
        GUILayout.EndArea();

    }


    //Populate a string array with the room node types to display that can be selected

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }
        return roomArray;
    }
    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            default:
                break;
        }
    }
    private void ProcessMouseDownEvent(Event currentEvent)
    {   //left click down
        if (currentEvent.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        //right click down
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }
    //Process Left Click Down
    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;
        //it makes editor to select the same thing at the project tab

        //toggle node selection
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }
    //Process Right Click Down
    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {


            ProcessLeftClickUpEvent();
        }
    }
    private void ProcessLeftClickUpEvent()
    {

        //toggle node selection
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }

    }
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }
    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {

        isLeftClickDragging = true;

        DragNode(currentEvent.delta);

        GUI.changed = true;
    }
    //Drag Node
    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
        //tells unity that something happend on this asset.So,save it.
    }

    //Add childID to the node(returns true if the node has been added, false otherwise)
    public bool AddChildRoomNodeIDToRoomNode(string childID)
    {
        //Check child node can be added validly to parent
        if (IsChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }

        return false;
       
    }
    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        //Check if there is already a connected boss room in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count>0)
            {
                isConnectedBossNodeAlready = true;
            }
        }

        //if the child node has a type of boss room and there is already a connected boss room node then return false
        //if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
        //{
        //    return false;
        //}
        //if the child node has a type of none then return false
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
        {
            return false;
        }
        //if the node already has a child with this child ID return false
        if (childRoomNodeIDList.Contains(childID))
        {
            return false;
        }
        //if this node ID and the child ID are the same return false
        if (id==childID)
        {
            return false;
        }
        //if this childID is already in the parentID list return false
        if (parentRoomNodeIDList.Contains(childID))
        {
            return false;
        }
        // if the child node already has a parent return false
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count>0)
        {
            return false;
        }
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
        {
            return false;
        }
        // if child is not a corridor and this node is not a corridor return false
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
        {
            return false;
        }
        //if adding a corridor check that this node has < the max permitted child corridors
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count>=Settings.maxChildCorridors)
        {
            return false;
        }
        //if the child room is an entrance return false- the entrance must always be the top level parent node
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
        {
            return false;
        }
        //if adding a room to a corridor check that this corridor node doesn't already have a room added
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count>0)
        {
            return false;
        }
        return true;
    }

    //Add childID to the node(returns true if the node has been added, false otherwise)
    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }
    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }
        return false;
    }

    public bool RemoveParentRoomNodeIDFromRoomNode(string parentID)
    {
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }
        return false;
    }



#endif
    #endregion Editor Code

}

