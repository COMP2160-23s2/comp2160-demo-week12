using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Priority_Queue;

public class Map : MonoBehaviour
{
    private Vector3 offset = new Vector3(0.5f, 0.5f, 0);

    [SerializeField]
    private Tilemap tilemap;

    private IEnumerator coroutine;

    private List<Vector3Int> path;

    private SimplePriorityQueue<List<Vector3Int>> incomplete;

    private HashSet<Vector3Int> explored;

    private Vector3Int? destination = null;

    public void RequestPath(Move requester, Vector3Int src, Vector3Int dest)
    {
        if (coroutine != null)
        {
            // kill the last request
            StopCoroutine(coroutine);
        }

        coroutine = Path(requester, src, dest);
        StartCoroutine(coroutine);
    }

    private IEnumerator Path(Move requester, Vector3Int src, Vector3Int dest)
    {       
        destination = dest;

        if (IsOccupied(dest))
        {
            requester.SetPath(null);
            coroutine = null;
            yield break;
        }

        path = new List<Vector3Int>();
        path.Add(src);

        // the set of explored locations (do not revisit)
        explored = new HashSet<Vector3Int>();

        // the collection of incomplete path
        incomplete = new SimplePriorityQueue<List<Vector3Int>>();
        incomplete.Enqueue(path, Cost(path) + Distance(src, dest));

        while (incomplete.Count > 0)
        {
            yield return null;

            path = incomplete.Dequeue();
            Vector3Int last = path[path.Count -1];
            if (last == dest)
            {                
                // reached the goal, return the path
                requester.SetPath(path);
                coroutine = null;
                yield break;
            }

            if (!explored.Contains(last))
            {
                explored.Add(last);

                // generate all possible extensions
                List<List<Vector3Int>> extensions = Extend(path);
                foreach (List<Vector3Int> extended in extensions) {
                    // add new extensions to the queue.
                    last = extended[extended.Count -1];
                    float cost = Cost(extended);
                    float distance = Distance(last, dest);
                    incomplete.Enqueue(extended, cost + distance);
                }
            }
        }

        // no path could be found
        requester.SetPath(null);
        coroutine = null;
        yield break;
    }

    private List<List<Vector3Int>> Extend(List<Vector3Int> path)
    {
        List<List<Vector3Int>> extensions = new List<List<Vector3Int>>();

        // extend the path to all the neighbouring cells if possible
        Vector3Int last = path[path.Count -1];
        Vector3Int step = Vector3Int.zero;
        for (step.x = -1; step.x <= 1; step.x++)
        {
            for (step.y = -1; step.y <= 1; step.y++)
            {
                if (step != Vector3Int.zero && CanMove(last, step))
                {
                    // copy the path and add the extra step
                    List<Vector3Int> extended = new List<Vector3Int>(path);
                    extended.Add(last + step);
                    extensions.Add(extended);
                }
            }
        }

        return extensions;
    }

    private float Cost(List<Vector3Int> path)
    {
        float length = 0;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3Int step = path[i] - path[i-1];
            length += step.magnitude;
        }

        return length;
    }

    private float Distance(Vector3Int src, Vector3Int dest)
    {
        // return Vector3Int.Distance(src, dest);
        Vector3Int d = dest - src;
        d.x = Math.Abs(d.x);
        d.y = Math.Abs(d.y);
        float max = Math.Max(d.x, d.y);
        float min = Math.Min(d.x, d.y);
        return min * Mathf.Sqrt(2) + (max - min);
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


    void OnDrawGizmos()
    {
        if (destination.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(destination.Value + offset, 0.1f);
        }

        if (explored != null)
        {
            Gizmos.color = Color.black;
            foreach (Vector3Int p in explored)
            {
                Gizmos.DrawSphere(p + offset, 0.1f);
            }            
        }

        if (incomplete != null)
        {
            Gizmos.color = Color.black;
            foreach (List<Vector3Int> incompletePath in incomplete)
            {
                DrawPathGizmo(incompletePath);            
            }
        }

        Gizmos.color = Color.red;
        DrawPathGizmo(path);
    }

    public void DrawPathGizmo(List<Vector3Int> path)
    {
        if (path != null && path.Count > 0) 
        {
            Vector3 p0 = path[0] + offset;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 p1 = path[i] + offset;
                Gizmos.DrawLine(p0, p1);
                p0 = p1;
            }
        }
    }
}
