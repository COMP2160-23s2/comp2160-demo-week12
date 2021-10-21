using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Map : MonoBehaviour
{
    [SerializeField]
    private Tilemap tilemap;

    public List<Vector3Int> Path(Vector3Int src, Vector3Int dest)
    {
        // simple path planner with no obstacle avoidance
        
        if (IsOccupied(dest))
        {
            return null;
        }

        List<Vector3Int> path = new List<Vector3Int>();
        path.Add(src);

        Vector3Int dist = dest - src;
        Vector3Int dir = Vector3Int.zero;
        dir.x = Math.Sign(dist.x);
        dir.y = Math.Sign(dist.y);
        dist.x = Math.Abs(dist.x);
        dist.y = Math.Abs(dist.y);

        Vector3Int p = src;
        while (dist.x > 0 || dist.y > 0)
        {
            if (dist.x > dist.y)
            {
                // take one step in the x direction
                p.x += dir.x;
                dist.x--;
            }
            else if (dist.x < dist.y)
            {
                // take one step in the y direction
                p.y += dir.y;
                dist.y--;
            }
            else {
                // take one step along the diagonal
                p.x += dir.x;
                p.y += dir.y;
                dist.x--;
                dist.y--;
            }

            path.Add(p);
        }
        return path;
    }


    public List<Vector3Int> Path(Vector3 src, Vector3 dest)
    {        
        return Path(Vector3Int.FloorToInt(src),
            Vector3Int.FloorToInt(dest));
    }

    public bool IsOccupied(Vector3Int p)
    {
        return tilemap.GetColliderType(p) != Tile.ColliderType.None;
    }

    public bool IsOccupied(Vector3 p)
    {
        return IsOccupied(Vector3Int.FloorToInt(p));
    }

    public bool CanMove(Vector3Int src, Vector3Int step)
    {
        if (IsOccupied(src + step)) 
        {
            return false;
        }

        // for diagonal movements, check the horizontal and vertical neighbours too
        if (step.x != 0 && step.y != 0)
        {
            if (IsOccupied(src + Vector3Int.right * step.x))
            {
                return false;
            }
            if (IsOccupied(src + Vector3Int.up * step.y))
            {
                return false;
            }
        }
        return true;
    }

    public Vector3Int? Raycast(Ray ray)
    {
        Plane plane = new Plane(transform.forward, transform.position);
        float t;
        if (plane.Raycast(ray, out t))
        {
            // get the collision point in the local coorindate system of the map
            Vector3 p = ray.GetPoint(t);
            p = transform.InverseTransformPoint(p);
            return Vector3Int.FloorToInt(p);
        }
        else 
        {
            return null;
        }
    }
}
