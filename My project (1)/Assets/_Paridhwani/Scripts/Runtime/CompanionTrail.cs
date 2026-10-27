using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani
{
    // Transient path history belongs to one loaded visit and one continuous walking segment.
    public sealed class CompanionTrail
    {
        public const int MaximumPoints=128;
        readonly Queue<Vector3> points=new Queue<Vector3>();
        Vector3 last;
        public int Count => points.Count;
        public Vector3 Next => points.Peek();
        public void Advance() { if(points.Count>0) points.Dequeue(); }
        public void Reset(Vector3 origin) { points.Clear();last=origin; }
        public void Sample(Vector3 position)
        {
            float distance=(position-last).sqrMagnitude;
            if(!float.IsFinite(distance) || distance>144) {Reset(position);return;}
            if(distance<=.65f*.65f) return;
            last=position;points.Enqueue(position);
            while(points.Count>MaximumPoints) points.Dequeue();
        }
    }
}
