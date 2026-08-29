using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Objective
{
    public GameObject targetObject;
    public Color requiredColor;
    public bool isCompleted = false;
}

public class ObjectiveManager : MonoBehaviour
{
    public List<Objective> objectives = new List<Objective>();

    public int CorrectColors { get; private set; } = 0;
    public int IncorrectColors { get; private set; } = 0;

    public event Action OnAllObjectivesCompleted;

    private Dictionary<GameObject, Objective> objectiveMap = new Dictionary<GameObject, Objective>();

    public void InitializeObjectives()
    {
        CorrectColors = 0;
        IncorrectColors = 0;
        objectiveMap.Clear();

        foreach (var obj in objectives)
        {
            if (obj.targetObject != null)
            {
                obj.isCompleted = false;
                objectiveMap[obj.targetObject] = obj;
            }
        }
    }

    void Start()
    {
        InitializeObjectives();
    }

    public void OnObjectColored(GameObject hitObject, Color colorApplied)
    {
        if (objectiveMap.TryGetValue(hitObject, out Objective obj))
        {
            // Simple color equality check (could use a tolerance if needed)
            if (colorApplied == obj.requiredColor)
            {
                if (!obj.isCompleted)
                {
                    obj.isCompleted = true;
                    CorrectColors++;
                    CheckWinCondition();
                }
            }
            else
            {
                // If it was already completed and they colored it wrong, subtract correct
                if (obj.isCompleted)
                {
                    obj.isCompleted = false;
                    CorrectColors--;
                }
                
                IncorrectColors++;
            }
        }
        else 
        {
            // Coloring an object that is not an objective is technically incorrect or neutral.
            // Based on requirements, if it's not required, maybe we just ignore or count as mistake.
            // We will count as a mistake.
            IncorrectColors++;
        }
    }

    private void CheckWinCondition()
    {
        if (CorrectColors >= objectives.Count)
        {
            OnAllObjectivesCompleted?.Invoke();
        }
    }

    public string GetObjectivesText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>OBJECTIVES:</b>");
        foreach (var kvp in objectiveMap)
        {
            Objective obj = kvp.Value;
            string objName = kvp.Key != null ? kvp.Key.name : "Missing Object";
            
            // Clean up cloned names (e.g. "Cube(Clone)" -> "Cube")
            if (objName.Contains("(Clone)"))
                objName = objName.Substring(0, objName.IndexOf("(Clone)"));
            if (objName.Contains("_Clone"))
                objName = objName.Substring(0, objName.IndexOf("_Clone"));

            string colorName = ColorUtility.ToHtmlStringRGB(obj.requiredColor);
            string status = obj.isCompleted ? "<color=green>[x]</color>" : "[ ]";
            
            sb.AppendLine($"{objName} -> <color=#{colorName}>Color</color> {status}");
        }
        return sb.ToString();
    }
}
