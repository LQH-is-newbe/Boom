using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T> where T: class {
    private readonly float allowedError;

    public PriorityQueue(float allowedError = 0) {
        this.allowedError = allowedError;
    }

    public class PQNode {
        public T element;
        public float priority;
        public int index;
    }

    private readonly List<PQNode> nodes = new();
    private readonly Dictionary<T, PQNode> nodesDic = new();

    public void Add(T element, float priority) {
        PQNode node = new();
        node.element = element;
        node.priority = priority;
        node.index = nodes.Count;
        nodes.Add(node);
        nodesDic[element] = node;
        PushUp(nodes.Count - 1);
    }

    public T Pop() {
        T top = nodes[0].element;
        Remove(0);
        return top;
    }

    public void Remove(T element) {
        if (nodesDic.TryGetValue(element, out PQNode node)) {
            Remove(node.index);
        }
    }

    public T Get(T element) {
        if (nodesDic.TryGetValue(element, out PQNode node)) {
            return node.element;
        } else {
            return null;
        }
    }

    public bool Empty() {
        return nodes.Count == 0;
    }

    public void Clear() {
        nodes.Clear();
        nodesDic.Clear();
    }

    private bool Smaller(float a, float b) {
        return a < b - allowedError || a < b + allowedError && Random.RandomFloat() < 0.5f;
    }

    private void PushUp(int i) {
        int p = (i - 1) / 2;
        while (i > 0 && Smaller(nodes[i].priority, nodes[p].priority)) {
            Swap(i, p);
            i = p;
            p = (i - 1) / 2;
        }
    }

    private void PushDown(int i) {
        while (true) {
            int l = i * 2 + 1;
            int r = i * 2 + 2;
            if (l >= nodes.Count && r >= nodes.Count) {
                break;
            } else {
                int s; // smaller one
                if (r >= nodes.Count) {
                    s = l;
                } else {
                    s = Smaller(nodes[l].priority, nodes[r].priority) ? l : r;
                }
                if (Smaller(nodes[i].priority, nodes[s].priority)) {
                    break;
                } else {
                    Swap(i, s);
                    i = s;
                }
            }
        }
    }

    private void Swap(int i, int j) {
        nodes[i].index = j;
        nodes[j].index = i;
        (nodes[i], nodes[j]) = (nodes[j], nodes[i]);
    }

    private void Remove(int i) {
        int j = nodes.Count - 1;
        Swap(i, j);
        nodesDic.Remove(nodes[j].element);
        nodes.RemoveAt(j);
        if (i < nodes.Count) Reposition(i);
    }

    private void Reposition(int i) {
        if (i == 0 || nodes[i].priority.CompareTo(nodes[(i - 1) / 2].priority) > 0) {
            PushDown(i);
        } else {
            PushUp(i);
        }
    }
}
