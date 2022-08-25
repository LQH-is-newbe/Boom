using System.Collections.Generic;

public abstract class Table<T> where T: class {
    protected int id = -1;
    protected Dictionary<int, T> elements = new();

    public int Add(T element) {
        elements[++id] = element;
        OnAdd(element, id);
        return id;
    }

    public T Get(int id) {
        if (elements.ContainsKey(id)) {
            return elements[id];
        } else {
            return null;
        }
    }

    public void Clear() {
        id = -1;
        elements.Clear();
    }

    protected virtual void OnAdd(T element, int id) { }
}
