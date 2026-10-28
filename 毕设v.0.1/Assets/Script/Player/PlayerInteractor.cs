using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MFramework.Event;
using Game;

public class PlayerInteractor : MonoBehaviour
{
    private CharacterIdentity _characterIdentity;
    private InteractionBase _target;
    private void Awake()
    {
        _characterIdentity = GetComponent<CharacterIdentity>();
    }
    void Update()
    {
        if (_target != null && (!_target.isActiveAndEnabled || !_target.gameObject.activeInHierarchy))
            _target = null;

        if(_target == null || _target.CachedCommandInfo.Count == 0) return;
        var input = InputSystemController.Instance;

        if(input == null) return;
        if(input.GetPlayerConfirmPressed()&& _target != null)
        {
             _target.Interact(_characterIdentity.CharacterDefinition as AllyDefinitionSO);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out InteractionBase interactionBase))
        {
            _target = interactionBase;
            interactionBase.OnFocus(_characterIdentity.CharacterDefinition as AllyDefinitionSO);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out InteractionBase interactionBase))
        {
            interactionBase.OnLoseFocus(_characterIdentity.CharacterDefinition as AllyDefinitionSO);
            
        }
        _target = null;
    }
}