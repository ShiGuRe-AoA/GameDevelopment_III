using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    public void OnInteract();
    public InteractPhase OnInteractDetected();
}

public interface IEntityInteractable
{
    public void OnEntityInteract();
    public InteractPhase OnEntityInteractDetected();
}