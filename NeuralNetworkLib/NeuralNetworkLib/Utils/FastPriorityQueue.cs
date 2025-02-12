public class FastPriorityQueue<TNodeType>
{
    private struct NodeEntry
    {
        public TNodeType Node;
        public int Priority;
    }

    private readonly List<NodeEntry> _heap = new List<NodeEntry>();
    private readonly Dictionary<TNodeType, int> _indexMap = new Dictionary<TNodeType, int>();

    public int Count => _heap.Count;

    // New Contains method
    public bool Contains(TNodeType node) => _indexMap.ContainsKey(node);

    public void Enqueue(TNodeType node, int priority)
    {
        if (Contains(node))
        {
            UpdatePriority(node, priority);
            return;
        }
        
        _heap.Add(new NodeEntry { Node = node, Priority = priority });
        _indexMap[node] = _heap.Count - 1;
        BubbleUp(_heap.Count - 1);
    }

    public TNodeType Dequeue()
    {
        var result = _heap[0].Node;
        Swap(0, _heap.Count - 1);
        _indexMap.Remove(result);
        _heap.RemoveAt(_heap.Count - 1);
        
        if (_heap.Count > 0)
            BubbleDown(0);
        
        return result;
    }

    public void UpdatePriority(TNodeType node, int newPriority)
    {
        if (!_indexMap.TryGetValue(node, out var index)) return;
        
        var oldPriority = _heap[index].Priority;
        _heap[index] = new NodeEntry { Node = node, Priority = newPriority };
        
        if (newPriority < oldPriority)
            BubbleUp(index);
        else
            BubbleDown(index);
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (_heap[parentIndex].Priority <= _heap[index].Priority)
                break;
            
            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        while (true)
        {
            var leftChild = 2 * index + 1;
            if (leftChild >= _heap.Count) return;
            
            var rightChild = leftChild + 1;
            var minChild = (rightChild < _heap.Count && _heap[rightChild].Priority < _heap[leftChild].Priority)
                ? rightChild
                : leftChild;

            if (_heap[index].Priority <= _heap[minChild].Priority)
                return;
            
            Swap(index, minChild);
            index = minChild;
        }
    }

    private void Swap(int a, int b)
    {
        (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
        _indexMap[_heap[a].Node] = a;
        _indexMap[_heap[b].Node] = b;
    }
}