using System;
using System.Collections.Generic;

// Priority queue implemented using a min-heap.
public class PriorityQueue<T>
{
    private readonly List<T> data;
    private readonly IComparer<T> comparator;

    public PriorityQueue(IComparer<T> comparer = null)
    {
        data = new List<T>();
        comparator = comparer ?? Comparer<T>.Default;
    }

    public int Count => data.Count;

    public void Enqueue(T item)
    {
        data.Add(item);
        HeapifyUp(data.Count - 1);
    }

    public void Clear()
    {
        data.Clear();
    }

    public T Dequeue()
    {
        if (data.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");
        T root = data[0];
        int lastIndex = data.Count - 1;
        data[0] = data[lastIndex];
        data.RemoveAt(lastIndex);
        HeapifyDown(0);
        return root;
    }

    public T Peek()
    {
        if (data.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");
        return data[0];
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (comparator.Compare(data[index], data[parentIndex]) >= 0)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int smallest = index;
            if (leftChild < data.Count && comparator.Compare(data[leftChild], data[smallest]) < 0)
            {
                smallest = leftChild;
            }
            if (rightChild < data.Count && comparator.Compare(data[rightChild], data[smallest]) < 0)
            {
                smallest = rightChild;
            }
            if (smallest == index)
            {
                break;
            }

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int index1, int index2)
    {
        (data[index2], data[index1]) = (data[index1], data[index2]);
    }
}