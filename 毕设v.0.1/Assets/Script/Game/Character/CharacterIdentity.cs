using UnityEngine;

namespace Game
{
    public class CharacterIdentity : MonoBehaviour
{
    [SerializeField] private CharacterDefinitionSO _characterDefinition;

    public CharacterDefinitionSO CharacterDefinition => _characterDefinition;

    public void SetDefinition(CharacterDefinitionSO characterDefinition)
    {
        _characterDefinition = characterDefinition;
    }
}
}