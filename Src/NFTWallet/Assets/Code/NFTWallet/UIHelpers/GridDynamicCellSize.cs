using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridDynamicCellSize : MonoBehaviour
{
    public Vector2 BaseSize = new Vector2(800, 600); // Base size of the screen
    public Vector2 BaseCellSize; // In editor Cell Size for GridLayoutComponent
    public Vector2 BaseCellSpacing; // In editor Cell Spacing for GridLayoutComponent
    public GridLayoutGroup LayoutGroup; //Component

    void Start()
    {
        LayoutGroup = GetComponent<GridLayoutGroup>();
        BaseCellSize = LayoutGroup.cellSize;
        BaseCellSpacing = LayoutGroup.spacing;
    }

    void Update()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height); // Current screen size
        LayoutGroup.cellSize = (screenSize / BaseSize) * BaseCellSize;
        LayoutGroup.spacing = (screenSize / BaseSize) * BaseCellSpacing;
    }
}
