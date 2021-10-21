using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField]
    private float speed = 5;

    [SerializeField]
    private List<Vector3Int> path;

    [SerializeField]
    private Map map;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetPath();
        }

        if (path != null && path.Count > 0)
        {
            if (MoveTo(path[0]))
            {
                path.RemoveAt(0);
            }
        }  
    }

    private void SetPath()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3Int? p = map.Raycast(ray);
        if (p.HasValue)
        {
            Vector3Int src = Vector3Int.FloorToInt(transform.position);
            Vector3Int dest = p.Value;
            path = map.Path(src, dest);
        }
    }

    private bool MoveTo(Vector3Int pos)
    {
        Vector3 dir = pos - transform.position;
        Vector3 move = dir.normalized * speed * Time.deltaTime;

        if (dir.magnitude == 0 || move.magnitude > dir.magnitude)
        {
            // reached the destination
            transform.position = pos;
            return true;
        }
        else
        {
            transform.Translate(move);
            return false;
        }
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Vector3 offset = new Vector3(0.5f, 0.5f, 0);
        Vector3 p0 = path[0] + offset;
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 p1 = path[i] + offset;
            Gizmos.DrawLine(p0, p1);
            p0 = p1;
        }
    }
}
