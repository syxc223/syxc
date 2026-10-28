using Game;

public class RecruitAction : ActionBase
{
    public CharacterDefinitionSO CurrentCharacter { get; private set; }

    private void Awake()
    {
        CurrentCharacter = GetComponent<CharacterIdentity>().CharacterDefinition; // Get the
    }

    public override void TriggerAction(AllyDefinitionSO interactor)
    {
        EventBus.Publish(new PanelRequestEvent(this));
    }

    public override void Execute()
    {
        FadeController.Instance.FadeOut(() =>
        {
            PartyManager.Instance.RecruitMember(CurrentCharacter);
            HidScenceNPC();
            FadeController.Instance.FadeIn();
        });
    }

    private void HidScenceNPC()
    {
        this.gameObject.SetActive(false);
    }
}