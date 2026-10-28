
using MFramework.Event;
using Game;

using System.Collections.Generic;
using UnityEngine;
public class InteractionBase : MonoBehaviour
{
    [Header("Sign Trans")]
    public Transform HeadAnchor;
    private AllyDefinitionSO _currentInteractor;
    private ActionBase[] _actionsCache;
    private readonly List<ActionCommandInfo> _cachedCommandInfo = new(8);
    private readonly List<VisibleActionEntry> _visibleEntries = new(8);

// 0个引用
  public IReadOnlyList<ActionCommandInfo> CachedCommandInfo => _cachedCommandInfo;

  private struct VisibleActionEntry
    {
        public ActionBase Action;
        public ActionCommandInfo CommandInfo;
    }

// Start is called before the first frame update
// 0个引用 | ↕↘ | 解释 | 添加注释 | × | Unity 消息
private void Awake()
{
    CacheActions();
    // Inspector 已手动指定 HeadAnchor 时不要覆盖；仅在未指定时才回退到第一个子物体
    if (HeadAnchor == null && transform.childCount > 0)
        HeadAnchor = transform.GetChild(0);
}
 
// 0个引用 | ↕↘ | 解释 | 添加注释 | ×
public void Interact(AllyDefinitionSO interactor)
 { 
    EventBus.Publish(new InteractionMenuRequestEvent(this));
}

// 1个引用 | ↕↘ | 解释 | 添加注释 | ×
public void OnFocus(AllyDefinitionSO interactor)
{
    CacheActions();
    _currentInteractor = interactor;
    Debug.Log(interactor.name + " " + interactor.Job);
    RebuildCommands();
    PublishEvent(true);
}

// 1个引用 | ↕↘ | 解释 | 添加注释 | ×
public void OnLoseFocus(AllyDefinitionSO interactor)
{
    _currentInteractor = null;
    _cachedCommandInfo.Clear(); // TODO: Clear cached command info
    
    PublishEvent(false);
    HeadAnchor.gameObject.SetActive(true);
}

// 2个引用 | ↕↘ | 解释 | 添加注释 | ×
private void CacheActions() => _actionsCache = GetComponents<ActionBase>();

private void RebuildCommands()
    {
        _cachedCommandInfo.Clear();
        _visibleEntries.Clear();
        for(int i = 0; i < _actionsCache.Length; i++)
        {
            var action = _actionsCache[i];

            //ToDo:之后这里改成Part Member检测
            //if (!action.CanShow(_currentInteractor))
            if (!CanAnyPartyMemberExecute(action))
                continue;

            _visibleEntries.Add(new VisibleActionEntry
            {
                Action = action,
                CommandInfo = action.CommandInfo
            });
        }

        if (_visibleEntries.Count > 1)
         _visibleEntries.Sort((a, b) => a.CommandInfo.Order.CompareTo(b.CommandInfo.Order));

        for (int i = 0; i < _visibleEntries.Count; i++)
{
        _cachedCommandInfo.Add(_visibleEntries[i].CommandInfo);
        Debug.Log(_visibleEntries[i].CommandInfo.DisplayName);
}
     if(_visibleEntries.Count > 0)
         HeadAnchor.gameObject.SetActive(false);
    }
      private void PublishEvent(bool inRange)
    {
        EventBus.Publish(new InteractionChangedEvent(this, inRange));
    }

      private bool CanAnyPartyMemberExecute(ActionBase actionBase)
{
    var partyMembers = PartyManager.Instance.PartyMembers;
    if (partyMembers.Count > 0)
    {
        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            if (member.Definition == null)
                continue;
            if (actionBase.CanShow(member.Definition as AllyDefinitionSO))
                return true;
        }
        return false;
    }
    return false;
}


    public bool ExecuteCommandFromUI(int commandIndex)
{
    if (commandIndex >= _visibleEntries.Count || commandIndex < 0)
        return false;

     var action = _visibleEntries[commandIndex].Action;
     if(!action.CanExecute(_currentInteractor))
         return false;
        action.TriggerAction(_currentInteractor);
    return true;
}

}