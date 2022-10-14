using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolPriorityManager : MonoBehaviour
{

    public float[] EffectorPriorityValues = null;

    private SwitchList _switchList;
    private bool _priorityMayHaveChanged;
    private GameObject[] _effectors = null;


    // Start is called before the first frame update
    void Start()
    {
        _switchList = GetComponent<SwitchList>();
        _effectors = _switchList.Effectors;
    }

    // Update is called once per frame
    void Update()
    {
        _effectors = _switchList.Effectors;
        foreach (GameObject e in _effectors)
        {
            bool used = e.GetComponent<Effector>().CheckToolUseChanged(true);
            if (used)
            {
                e.GetComponent<Effector>().ToolPriority++;
                _priorityMayHaveChanged = true;
            }
        }
    }

    public bool InitPriorityValues()
    {
        _effectors = _switchList.Effectors;

        if (_effectors == null)
            return false;

        EffectorPriorityValues = new float[_effectors.Length];

        return true;
    }


}
