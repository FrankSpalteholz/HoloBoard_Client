using System.Collections.Generic;
using UnityEngine;

public class SimpleDataSmoother
{
    // Simple moving average implementation for positions
    private Queue<Vector2> gazePositionQueue = new Queue<Vector2>();
    private Queue<Vector3> headPositionQueue = new Queue<Vector3>();
    private Queue<Quaternion> headRotationQueue = new Queue<Quaternion>();
    
    private int maxSamples = 3; // Smaller count for faster response on iPhone
    
    // Constructor with optional sample count
    public SimpleDataSmoother(int sampleCount = 3)
    {
        maxSamples = Mathf.Clamp(sampleCount, 1, 10); // Limit sample size
    }
    
    // Add new gaze position and return smoothed value
    public Vector2 SmoothGazePosition(Vector2 newPosition)
    {
        // Add new position
        gazePositionQueue.Enqueue(newPosition);
        
        // Remove oldest position if queue is full
        if (gazePositionQueue.Count > maxSamples)
            gazePositionQueue.Dequeue();
            
        // Calculate average
        Vector2 smoothedPosition = Vector2.zero;
        foreach (Vector2 pos in gazePositionQueue)
        {
            smoothedPosition += pos;
        }
        
        return smoothedPosition / gazePositionQueue.Count;
    }
    
    // Add new head position and return smoothed value
    public Vector3 SmoothHeadPosition(Vector3 newPosition)
    {
        // Add new position
        headPositionQueue.Enqueue(newPosition);
        
        // Remove oldest position if queue is full
        if (headPositionQueue.Count > maxSamples)
            headPositionQueue.Dequeue();
            
        // Calculate average
        Vector3 smoothedPosition = Vector3.zero;
        foreach (Vector3 pos in headPositionQueue)
        {
            smoothedPosition += pos;
        }
        
        return smoothedPosition / headPositionQueue.Count;
    }
    
    // Add new rotation and return smoothed value
    public Quaternion SmoothHeadRotation(Quaternion newRotation)
    {
        // Add new rotation
        headRotationQueue.Enqueue(newRotation);
        
        // Remove oldest rotation if queue is full
        if (headRotationQueue.Count > maxSamples)
            headRotationQueue.Dequeue();
            
        // Calculate average rotation (Slerp)
        if (headRotationQueue.Count == 1)
            return newRotation;
            
        // Use Quaternion.Slerp for proper quaternion interpolation
        Quaternion result = headRotationQueue.Peek(); // Start with first rotation
        float weight = 1.0f / headRotationQueue.Count;
        float accumWeight = weight;
        
        // Skip first element (already fetched)
        bool isFirst = true;
        foreach (Quaternion rot in headRotationQueue)
        {
            if (isFirst) {
                isFirst = false;
                continue;
            }
            
            // Slerp between current result and next rotation
            result = Quaternion.Slerp(result, rot, accumWeight / (accumWeight + weight));
            accumWeight += weight;
        }
        
        return result;
    }
    
    // Method to reset smoother (e.g., on tracking loss)
    public void Reset()
    {
        gazePositionQueue.Clear();
        headPositionQueue.Clear();
        headRotationQueue.Clear();
    }
}