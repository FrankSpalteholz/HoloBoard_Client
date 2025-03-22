using System.Collections.Generic;
using UnityEngine;

// Diese Klasse als separates Skript anlegen
public class SimpleDataSmoother
{
    // Einfache gleitende Mittelwert-Implementierung für Positionen
    private Queue<Vector2> gazePositionQueue = new Queue<Vector2>();
    private Queue<Vector3> headPositionQueue = new Queue<Vector3>();
    private Queue<Quaternion> headRotationQueue = new Queue<Quaternion>();
    
    private int maxSamples = 3; // Kleinere Anzahl für schnellere Reaktion auf dem iPhone
    
    // Konstruktor mit optionaler Sample-Anzahl
    public SimpleDataSmoother(int sampleCount = 3)
    {
        maxSamples = Mathf.Clamp(sampleCount, 1, 10); // Begrenze die Sample-Größe
    }
    
    // Fügt neue Gaze-Position hinzu und gibt geglätteten Wert zurück
    public Vector2 SmoothGazePosition(Vector2 newPosition)
    {
        // Füge neue Position hinzu
        gazePositionQueue.Enqueue(newPosition);
        
        // Entferne älteste Position, wenn die Queue voll ist
        if (gazePositionQueue.Count > maxSamples)
            gazePositionQueue.Dequeue();
            
        // Berechne Durchschnitt
        Vector2 smoothedPosition = Vector2.zero;
        foreach (Vector2 pos in gazePositionQueue)
        {
            smoothedPosition += pos;
        }
        
        return smoothedPosition / gazePositionQueue.Count;
    }
    
    // Fügt neue Kopfposition hinzu und gibt geglätteten Wert zurück
    public Vector3 SmoothHeadPosition(Vector3 newPosition)
    {
        // Füge neue Position hinzu
        headPositionQueue.Enqueue(newPosition);
        
        // Entferne älteste Position, wenn die Queue voll ist
        if (headPositionQueue.Count > maxSamples)
            headPositionQueue.Dequeue();
            
        // Berechne Durchschnitt
        Vector3 smoothedPosition = Vector3.zero;
        foreach (Vector3 pos in headPositionQueue)
        {
            smoothedPosition += pos;
        }
        
        return smoothedPosition / headPositionQueue.Count;
    }
    
    // Fügt neue Rotation hinzu und gibt geglätteten Wert zurück
    public Quaternion SmoothHeadRotation(Quaternion newRotation)
    {
        // Füge neue Rotation hinzu
        headRotationQueue.Enqueue(newRotation);
        
        // Entferne älteste Rotation, wenn die Queue voll ist
        if (headRotationQueue.Count > maxSamples)
            headRotationQueue.Dequeue();
            
        // Berechne durchschnittliche Rotation (Slerp)
        if (headRotationQueue.Count == 1)
            return newRotation;
            
        // Wir verwenden Quaternion.Slerp für korrekte Quaternion-Interpolation
        Quaternion result = headRotationQueue.Peek(); // Beginne mit der ersten Rotation
        float weight = 1.0f / headRotationQueue.Count;
        float accumWeight = weight;
        
        // Überspringe erstes Element (wurde schon geholt)
        bool isFirst = true;
        foreach (Quaternion rot in headRotationQueue)
        {
            if (isFirst) {
                isFirst = false;
                continue;
            }
            
            // Slerp zwischen aktuellem Ergebnis und nächster Rotation
            result = Quaternion.Slerp(result, rot, accumWeight / (accumWeight + weight));
            accumWeight += weight;
        }
        
        return result;
    }
    
    // Methode zum Zurücksetzen der Smoother (z.B. bei Tracking-Verlusten)
    public void Reset()
    {
        gazePositionQueue.Clear();
        headPositionQueue.Clear();
        headRotationQueue.Clear();
    }
}

// ÄNDERUNGEN AM TrackingDataSender.cs:

/*
// Am Anfang der TrackingDataSender-Klasse folgende Variable hinzufügen:
private SimpleDataSmoother dataSmoother;

// In der Start-Methode nach der Überprüfung der Komponenten:
dataSmoother = new SimpleDataSmoother(3); // 3 Samples für das iPhone

// In der SendTrackingData-Methode ändern:
// Vor dem Senden die Werte glätten:
lastGazePosition = dataSmoother.SmoothGazePosition(gazeTracker.GetHeadPositionProjection());
lastZDistance = gazeTracker.GetZDistance(); // Z-Distanz muss nicht unbedingt geglättet werden
lastHeadPosition = dataSmoother.SmoothHeadPosition(headTrackingTransform.transform.position * 100);
lastHeadRotation = dataSmoother.SmoothHeadRotation(headTrackingTransform.transform.rotation);
*/