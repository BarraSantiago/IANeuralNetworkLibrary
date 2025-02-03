namespace NeuralNetworkLib.Utils;

/// <summary>
/// A min-heap-based priority queue for .NET Standard 2.1.
/// Stores elements with an associated priority and allows 
/// O(log N) enqueue and dequeue-min operations.
/// </summary>
/// <typeparam name="TElement">The item type.</typeparam>
/// <typeparam name="TPriority">The priority type (must be IComparable).</typeparam>
public sealed class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TElement Element, TPriority Priority)> _heap;

    public PriorityQueue()
    {
        _heap = new List<(TElement, TPriority)>();
    }

    public int Count => _heap.Count;

    /// <summary>
    /// Add an element with a given priority into the min-heap.
    /// </summary>
    public void Enqueue(TElement element, TPriority priority)
    {
        _heap.Add((element, priority));
        SiftUp(_heap.Count - 1);
    }

    /// <summary>
    /// Removes and returns the element with the smallest priority.
    /// Throws InvalidOperationException if empty.
    /// </summary>
    public TElement Dequeue()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        // Swap root with last element
        TElement? root = _heap[0].Element;
        _heap[0] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);

        // Re-heapify down
        if (_heap.Count > 0)
        {
            SiftDown(0);
        }

        return root;
    }

    /// <summary>
    /// Peek the element with the smallest priority, without removing it.
    /// </summary>
    public TElement Peek()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _heap[0].Element;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) >> 1;
            if (_heap[index].Priority.CompareTo(_heap[parent].Priority) < 0)
            {
                // swap
                (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);

                index = parent;
            }
            else
            {
                break;
            }
        }
    }

    private void SiftDown(int index)
    {
        int count = _heap.Count;
        while (true)
        {
            int left = (index << 1) + 1;
            int right = left + 1;
            int smallest = index;

            if (left < count &&
                _heap[left].Priority.CompareTo(_heap[smallest].Priority) < 0)
            {
                smallest = left;
            }

            if (right < count &&
                _heap[right].Priority.CompareTo(_heap[smallest].Priority) < 0)
            {
                smallest = right;
            }

            if (smallest == index)
                break;

            // swap
            (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);

            index = smallest;
        }
    }
}