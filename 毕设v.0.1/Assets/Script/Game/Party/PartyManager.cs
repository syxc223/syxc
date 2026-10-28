using System.Collections.Generic;
using UnityEngine;
using Game;
[RequireComponent(typeof(PartyFieldController))]
public class PartyManager : Singleton<PartyManager>
{
    private PartyFieldController fieldController;
    [Header("Initial Party")]
    [SerializeField] private CharacterDefinitionSO playerDefinition;

    [SerializeField] private List<CharacterRuntimeData> partyMembers = new();
    public List<CharacterRuntimeData> PartyMembers => partyMembers;

    protected override void Awake()
    {
        base.Awake();
        InitParty();
        fieldController = GetComponent<PartyFieldController>();
    }

     void Start()
    {
        ApplyInitialEquipment();
    }

    private void InitParty()
    {
        if (partyMembers.Count == 0)
        {
            if (playerDefinition != null)
                partyMembers.Add(new CharacterRuntimeData(playerDefinition));
        }
    }

    public void AddMember(CharacterDefinitionSO characterDefinition)
    {
        //队伍人员唯一
        partyMembers.Add(new CharacterRuntimeData(characterDefinition));
    }

    private void AddMemberInternal(CharacterDefinitionSO characterDefinition)
    {
        var member = new CharacterRuntimeData(characterDefinition);
        partyMembers.Add(member);
        member.ApplyInitialEquipment();
        RefreshFieldFollowers();
    }

    public void RecruitMember(CharacterDefinitionSO newCharacter)
    {
        AddMemberInternal(newCharacter);
        GameModeManager.Instance.RequestChangeMode(GameMode.Explore);
    }

    private void RefreshFieldFollowers()
    {
        List<CharacterDefinitionSO> defs = new(partyMembers.Count);
        foreach (var member in partyMembers)
        {
            defs.Add(member.Definition);
        }
        fieldController.UpdateFollowers(defs);
    }

    public void ApplyInitialEquipment()
    {
        foreach (var member in partyMembers)
        {
            member.ApplyInitialEquipment();
        }
    }
}