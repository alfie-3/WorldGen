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

    Vector3Int hitLoc;

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
            Debug.DrawRay(ray.origin, ray.direction * 500, Color.green);

            hitLoc = Vector3Int.RoundToInt(new(hit.point.x, 0, hit.point.z));

            if (WorldUtils.TryGetTile(WorldUtils.GetTopTileLocation(hitLoc), out Tile tile))
            {
                ShowTooltip($"{tile.tileData.TileId} \n  {tile.TileLocationVect3} \n {WorldUtils.GetChunkLocation(hitLoc)} \n Matrix \n {tile.rotation} \n {tile.tileLocation}");
            }
            else HideTooltip();
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 500, Color.red);
            hitLoc = Vector3Int.zero;
        }
    }

    public void Update()
    {
        //Removing tiles using left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            if (currentTileData == null) return;

            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3Int roundedHit = Vector3Int.RoundToInt(new(hit.point.x, 0, hit.point.z));
                WorldManagement.RemoveTile(WorldUtils.GetTopTileLocation(roundedHit));
            }
        }

        //Placing tiles with right mouse click
        if (Input.GetMouseButtonDown(1))
        {
            if (currentTileData == null) return;

            Vector3 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3Int roundedHit = Vector3Int.RoundToInt(new(hit.point.x, 0, hit.point.z));
                WorldManagement.SetTile(WorldUtils.GetTopTileLocation(roundedHit), currentTileData, true);
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

    private void OnDrawGizmos()
    {
        if (hitLoc != Vector3Int.zero)
        {
            Vector3 drawPos = WorldUtils.GetTopTileLocation(hitLoc);
            drawPos.y += 0.5f;
            Gizmos.DrawWireCube(drawPos, Vector3Int.one);
        }
    }
}
