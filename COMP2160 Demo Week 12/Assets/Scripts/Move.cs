using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private const float tolerance = 0.01f;

    [SerializeField]
    private float speed = 5;

    [SerializeField]
    private List<Vector3Int> path;

    [SerializeField]
    private Map map;

    [SerializeField]
    private SpriteRenderer sprite;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RequestPath();
        }

        if (path != null && path.Count > 0)
        {
            if (MoveTo(path[0]))
            {
                path.RemoveAt(0);
            }
        }  
    }

    private void RequestPath()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3Int? p = map.Raycast(ray);
        if (p.HasValue)
        {
            // cancel the current path and request a new one
            this.path = null;
            Vector3Int src = Vector3Int.FloorToInt(transform.position);
            Vector3Int dest = p.Value;
            map.RequestPath(this, src, dest);
        }
    }

    public void SetPath(List<Vector3Int> path)
    {
        this.path = path;
    }

    private bool MoveTo(Vector3Int pos)
    {
        Vector3Int src = Vector3Int.FloorToInt(transform.position);
        if (!map.CanMove(src, pos - src))
        {
            // blocked
            sprite.color = Color.red;
            return false;
        }
        else {
            sprite.color = Color.green;
        }

        Vector3 dir = pos - transform.position;
        Vector3 move = dir.normalized;
        move = move * speed * Time.deltaTime;

        if (dir.magnitude < tolerance || move.magnitude > dir.magnitude)
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

}
