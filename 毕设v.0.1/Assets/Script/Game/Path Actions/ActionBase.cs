using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game;

public class ActionBase : MonoBehaviour
{
    public PlayerJob MatchJob = PlayerJob.Any;
    public ActionCommandInfo CommandInfo;
    public virtual bool CanShow(Game.AllyDefinitionSO interactor)
    {
        return IsJobMatch(interactor);
    }

    public virtual bool CanExecute(Game.AllyDefinitionSO interactor)
    {
        return IsJobMatch(interactor);
    }
    // 0个引用
public virtual void TriggerAction(Game.AllyDefinitionSO interactor)
{
    //需要打开面板再操作的就重写
    //默认执行命令
    Execute();
}

// 1个引用
public virtual void Execute() { }

// 1个引用
protected virtual bool IsJobMatch(Game.AllyDefinitionSO interactor)
{
    if (MatchJob == PlayerJob.Any)
        return true;

    return interactor.Job == MatchJob;
}
}

[System.Serializable]
public struct ActionCommandInfo
{
    public string DisplayName;
    public Sprite Icon;
    public int Order;
}