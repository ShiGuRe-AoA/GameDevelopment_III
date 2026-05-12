using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITickUpdatable
{
    void OnTickUpdate(float deltaTime);
}
public interface IMinuteUpdatable
{
    void OnMinuteUpdate();
}
public interface IDateUpdatable
{
    void OnDateUpdate(ComplexTime curTime);
}
