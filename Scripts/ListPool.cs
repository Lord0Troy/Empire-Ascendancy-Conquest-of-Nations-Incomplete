using System.Collections.Generic;

public static class ListPool<T> {

    // Stack to hold the pooled lists
    static Stack<List<T>> stack = new Stack<List<T>>();

    // Method to get a list from the pool
    public static List<T> Get () {
        // If there are lists in the pool, return one
        if (stack.Count > 0) {
            return stack.Pop();
        }
        // Otherwise, return a new list
        return new List<T>();
    }

    // Method to add a list to the pool
    public static void Add (List<T> list) {
        // Clear the list before adding it to the pool
        list.Clear();
        // Add the list to the pool
        stack.Push(list);
    }
}