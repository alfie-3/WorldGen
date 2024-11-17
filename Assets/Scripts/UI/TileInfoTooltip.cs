using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TileInfoTooltip : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tooltipText;

    [SerializeField] RectTransform backgroundRectTransform;
    [SerializeField] Canvas canvas;

    [SerializeField] TileData currentTileData;

    private void Start()
    {
        HideTooltip();
    }

    private void LateUpdate()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), mousePos, null, out Vector2 localPoint);
        transform.localPosition = localPoint;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out hit))
        {
            if (WorldUtils.GetTile(new((int)(hit.point.x), 0, (int)hit.point.z), out Tile tile))
            {
                ShowTooltip($"{tile.tileData.TileId} \n  {tile.tileLocation} \n {WorldUtils.GetChunkLocation(tile.tileLocation)}");
            }
            else HideTooltip();
        }
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out hit))
            {
                if (WorldUtils.GetTile(new((int)(hit.point.x), 0, (int)hit.point.z), out Tile tile))
                {
                    WorldManagement.UpdateAdjacentTiles(tile.tileLocation);
                    tile.RefreshTile();
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (currentTileData == null) return;

            Vector3 mousePos = Mouse.current.position.ReadValue();
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out hit))
            {
                WorldManagement.SetTile(new((int)(hit.point.x), 0, (int)hit.point.z), currentTileData);
            }
        }
    }

    public void ShowTooltip(string tooltipString)
    {
        canvas.enabled = true;

        tooltipText.text = tooltipString;
        tooltipText.ForceMeshUpdate();

        Vector2 textSize = tooltipText.GetRenderedValues(false);
        Vector2 paddingSize = new Vector2(8, 20);

        backgroundRectTransform.sizeDelta = textSize + paddingSize;
        tooltipText.rectTransform.sizeDelta = textSize + paddingSize;
    }

    public void HideTooltip()
    {
        canvas.enabled = false;
    }
}
