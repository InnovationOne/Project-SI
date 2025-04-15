using System;

/// <summary>
/// A generic heap data structure.
/// </summary>
/// <typeparam name="T">Type of items stored in the heap, which must implement IHeapItem.</typeparam>
public class Heap<T> where T : IHeapItem<T> {
    private T[] _items;
    private int _currentItemCount;
    public int Count => _currentItemCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Heap{T}"/> class with a specified maximum heap size.
    /// </summary>
    /// <param name="maxHeapSize">The maximum number of items the heap can hold.</param>
    public Heap(int maxHeapSize) {
        _items = new T[maxHeapSize];
    }

    /// <summary>
    /// Adds an item to the heap.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item) {
        item.HeapIndex = _currentItemCount;
        _items[_currentItemCount] = item;
        SortUp(item);
        _currentItemCount++;
    }

    /// <summary>
    /// Sorts an item up the heap until it is in the correct position.
    /// </summary>
    /// <param name="item">The item to sort.</param>
    private void SortUp(T item) {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (true) {
            T parentItem = _items[parentIndex];
            if (item.CompareTo(parentItem) > 0) {
                Swap(item, parentItem);
            } else {
                break;
            }

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    /// <summary>
    /// Removes and returns the first item from the heap.
    /// </summary>
    /// <returns>The first item in the heap.</returns>
    public T RemoveFirst() {
        T firstItem = _items[0];
        _currentItemCount--;
        _items[0] = _items[_currentItemCount];
        _items[0].HeapIndex = 0;
        SortDown(_items[0]);
        return firstItem;
    }

    /// <summary>
    /// Sorts an item down the heap until it is in the correct position.
    /// </summary>
    /// <param name="item">The item to sort.</param>
    private void SortDown(T item) {
        while (true) {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;

            if (childIndexLeft < _currentItemCount) {
                int swapIndex = childIndexLeft;

                if (childIndexRight < _currentItemCount) {
                    if (_items[childIndexLeft].CompareTo(_items[childIndexRight]) < 0) {
                        swapIndex = childIndexRight;
                    }
                }

                if (item.CompareTo(_items[swapIndex]) < 0) {
                    Swap(item, _items[swapIndex]);
                } else {
                    return;
                }
            } else {
                return;
            }
        }
    }

    /// <summary>
    /// Swaps two items in the heap.
    /// </summary>
    /// <param name="itemA">The first item.</param>
    /// <param name="itemB">The second item.</param>
    private void Swap(T itemA, T itemB) {
        // Swap the items in the array
        _items[itemA.HeapIndex] = itemB;
        _items[itemB.HeapIndex] = itemA;

        // Swap the heap indexes
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }

    /// <summary>
    /// Determines whether the heap contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the heap.</param>
    /// <returns>true if the item is found in the heap; otherwise, false.</returns>
    public bool Contains(T item) => Equals(_items[item.HeapIndex], item);

    /// <summary>
    /// Updates the position of an item in the heap.
    /// </summary>
    /// <param name="item">The item to update.</param>
    public void UpdateItem(T item) => SortUp(item);
    
}

/// <summary>
/// Defines an interface for heap items, requiring a comparable implementation and a heap index property.
/// </summary>
/// <typeparam name="T">The type of the items to be compared.</typeparam>
public interface IHeapItem<T> : IComparable<T> {
    int HeapIndex { get; set; }
}
