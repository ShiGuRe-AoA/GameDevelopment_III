using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Press Mouse.Button(1)
public interface IInteractable
{
    public void OnInteract();
    public InteractPhase OnInteractDetected();
}

// Press 'F'
public interface IEntityInteractable
{
    public void OnEntityInteract();
    public InteractPhase OnEntityInteractDetected();
}

