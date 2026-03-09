using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct ExitSelectContext
{

}
public interface IExitSelect
{
    public void ExitSelect(ExitSelectContext context, ref GameObject prefabInstance, ref List<GameObject> cellInstance);
}
