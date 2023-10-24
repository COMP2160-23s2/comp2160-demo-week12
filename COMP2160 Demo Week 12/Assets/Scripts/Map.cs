using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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

    /**
     * Place a request for a path from src to dest.
     * This will run as a coroutine and called requestor.SetPath() 
     * when done.
     *
     * Note: for simplicity's sake, we only allow one request at
     * a time. If a new request is made, the old one is cancelled
     */

    public void RequestPath(Move requester, Vector3Int src, Vector3Int dest)
    {
        if (coroutine != null)
        {
            // if a another request is already running, kill it
            StopCoroutine(coroutine);
        }

        // start a new coroutine to generate a path
        coroutine = Path(requester, src, dest);
        StartCoroutine(coroutine);
        Debug.Break();
    }

    private IEnumerator Path(Move requester, Vector3Int src, Vector3Int dest)
    {       
        destination = dest;

        // start with the path: [src]
        path = new List<Vector3Int>();
        path.Add(src);

        // the set of explored locations (do not revisit)
        explored = new HashSet<Vector3Int>();

        // the collection of incomplete paths
        // sorting the set as a FIFO queue means search will
        // be done in breadth-first order
        incomplete = new SimplePriorityQueue<List<Vector3Int>>();
        float priority = 0;
        incomplete.Enqueue(path, priority);

        // Keep going until we run out of new paths to try
        while (incomplete.Count > 0)
        {
            // yield to wait a frame
            yield return null;

            // take the first path from the queue and see if it is complete
            // if so, return it
            path = incomplete.Dequeue();
            Vector3Int last = path[path.Count -1];
            if (last == dest)
            {                
                // reached the goal, return the path
                requester.SetPath(path);
                coroutine = null;
                yield break;
            }

            // if the last location in the path has already been explored
            // then we don't need to expand it.
            if (!explored.Contains(last))
            {
                // mark this location as explored
                explored.Add(last);

                // generate all possible extensions
                List<List<Vector3Int>> extensions = Extend(path);
                foreach (List<Vector3Int> extended in extensions) {
                    // add new extensions to the queue.
                    last = extended[extended.Count - 1];
                    // h = f + g
                    // h = heuristic expected final path length
                    // f = path length so far
                    // g = heuristic predicted distance to goal (optimistic)
                    priority = Length(extended) + Distance(last, dest);
                    incomplete.Enqueue(extended, priority);
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
        // generate all possible extensions of the given path
        List<List<Vector3Int>> extensions = new List<List<Vector3Int>>();

        // extend the path to all the eight neighbouring cells if possible
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

    // Set:
    // Length = 0, Distance = 0 -> Breadth first search
    // Length = L, Distance = 0 -> Shortest path first
    // Length = 0, Distance = D -> Greedy path to goal
    // Length = L, Distance = D -> Shortest path to goal (full A*)

    private float Distance(Vector3Int src, Vector3Int dest)
    {
        float distance = 0;
        // distance = Vector3Int.Distance(src, dest);

        Vector3Int d = dest - src;
        d.x = Math.Abs(d.x);
        d.y = Math.Abs(d.y);

        int max = Math.Max(d.x, d.y);
        int min = Math.Min(d.x, d.y);
        distance = (max - min) + Mathf.Sqrt(2) * min;

        return distance;
    }

    private float Length(List<Vector3Int> path)
    {
        float length = 0;

        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3Int.Distance(path[i-1], path[i]);
        }

        return length;
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

    /**
     * Do a raycast to see which cell location the player has clicked on.
     */
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
        // highlight the destination
        if (destination.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(destination.Value + offset, 0.1f);
        }

        // add a dot to every location that has been explored
        if (explored != null)
        {
            Gizmos.color = Color.black;
            foreach (Vector3Int p in explored)
            {
                Gizmos.DrawSphere(p + offset, 0.1f);
            }            
        }

        // draw all the incomplete paths
        if (incomplete != null)
        {
            foreach (List<Vector3Int> incompletePath in incomplete)
            {
                DrawPathGizmo(incompletePath, Color.black, 3);            
            }
        }

        // draw the final path
        DrawPathGizmo(path, Color.red, 5);
    }

    public void DrawPathGizmo(List<Vector3Int> path, Color color, int thickness)
    {
        if (path != null && path.Count > 0) 
        {
            Vector3 p0 = path[0] + offset;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 p1 = path[i] + offset;
//                Gizmos.DrawLine(p0, p1);
                // draw thicker lines
                Handles.DrawBezier(p0, p1, p0, p1, color, null, thickness);
                p0 = p1;
            }
        }
    }
}
