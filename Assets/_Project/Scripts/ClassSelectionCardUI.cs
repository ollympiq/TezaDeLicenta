using System.Collections;
using TMPro;
using UnityEngine;

public class ClassSelectionCardUI : MonoBehaviour
{
    [Header("Class")]
    [SerializeField] private CharacterClass classType;
    [SerializeField] private string classTitle = "Melee";

    [TextArea(2, 5)]
    [SerializeField] private string classDescription;

    [Header("Base Attributes")]
    [SerializeField] private int strength;
    [SerializeField] private int constitution;
    [SerializeField] private int dexterity;
    [SerializeField] private int intelligence;

    [Header("Bonuses Rich Text")]
    [TextArea(4, 10)]
    [SerializeField] private string bonusDescription;

    [Header("Starting Loadout")]
    [SerializeField] private WeaponDefinition startingWeapon;
    [SerializeField] private SkillDefinition startingSkill1;
    [SerializeField] private SkillDefinition startingSkill2;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI attributesText;
    [SerializeField] private TextMeshProUGUI bonusesText;

    [Header("Slots")]
    [SerializeField] private ClassSelectionSlotUI weaponSlot;
    [SerializeField] private ClassSelectionSlotUI skillSlot1;
    [SerializeField] private ClassSelectionSlotUI skillSlot2;

    private Coroutine refreshRoutine;

    private void OnEnable()
    {
        ScheduleRefresh();
    }

    private void Start()
    {
        ScheduleRefresh();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            Refresh();
    }

    private void ScheduleRefresh()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshAfterFrame());
    }

    private IEnumerator RefreshAfterFrame()
    {
        yield return null;
        Refresh();
        refreshRoutine = null;
    }

    public void Refresh()
    {
        if (titleText != null)
            titleText.text = BuildTitle();

        if (descriptionText != null)
            descriptionText.text = classDescription;

        if (attributesText != null)
            attributesText.text = BuildAttributesText();

        if (bonusesText != null)
            bonusesText.text = bonusDescription;

        if (weaponSlot != null)
            weaponSlot.BindWeapon(startingWeapon);

        if (skillSlot1 != null)
            skillSlot1.BindSkill(startingSkill1);

        if (skillSlot2 != null)
            skillSlot2.BindSkill(startingSkill2);
    }

    private string BuildTitle()
    {
        string color = GetClassColor();
        return $"<color={color}><b>{classTitle}</b></color>";
    }

    private string BuildAttributesText()
    {
        return
            $"<color=#FF9F1C><b>Strength:</b></color> {strength}\n" +
            $"<color=#FF6B6B><b>Constitution:</b></color> {constitution}\n" +
            $"<color=#2ECC71><b>Dexterity:</b></color> {dexterity}\n" +
            $"<color=#C77DFF><b>Intelligence:</b></color> {intelligence}";
    }

    private string GetClassColor()
    {
        switch (classType)
        {
            case CharacterClass.Melee:
                return "#FF9F1C";

            case CharacterClass.Ranger:
                return "#2ECC71";

            case CharacterClass.Mage:
                return "#C77DFF";

            default:
                return "#FFFFFF";
        }
    }
}