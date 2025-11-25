using System.Collections.Generic;
using UnityEngine;

public class Script
{
    private List<ScriptAction> _actions;
    private int _currentIndex = -1;
    private Dictionary<string, int> _sceneMap = new Dictionary<string, int>();

    public Script(List<ScriptAction> actions, Dictionary<string, int> sceneMap)
    {
        _actions = actions;
        _sceneMap = sceneMap;
        _currentIndex = -1;
    }

    public bool HasNextAction()
    {
        return _currentIndex < _actions.Count - 1;
    }

    public ScriptAction Continue()
    {
        if (!HasNextAction())
            return null;

        _currentIndex++;
        ScriptAction currentAction = _actions[_currentIndex];

        return currentAction;
    }

    public ScriptAction GetCurrent()
    {
        if (_currentIndex >= 0 && _currentIndex < _actions.Count)
            return _actions[_currentIndex];
        return null;
    }

    public void JumpTo(string sceneName)
    {
        _currentIndex = _sceneMap[sceneName] - 1; // Continue() 호출 시 해당 인덱스가 되도록 -1
        Debug.Log($"Script :: Jump to scene: {sceneName} (Index: {_currentIndex + 1})");
    }
}
