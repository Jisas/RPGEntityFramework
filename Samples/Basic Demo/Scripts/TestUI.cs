using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    [Header("Lógica de creación")]
    public CharacterCreatorLogic creatorLogic;

    [Header("Referencias UI")]
    public TMP_Dropdown raceDropdown;
    public TMP_Dropdown subRaceDropdown;
    public TMP_Dropdown classDropdown;
    public TMP_Dropdown subClassDropdown;
    public Button confimButton;

    void Start()
    {
        // Llenar dropdowns iniciales
        FillRaceDropdown();
        FillClassDropdown();

        // Listeners
        raceDropdown.onValueChanged.AddListener(OnRaceChanged);
        subRaceDropdown.onValueChanged.AddListener(OnSubRaceChanged);
        classDropdown.onValueChanged.AddListener(OnClassChanged);
        subClassDropdown.onValueChanged.AddListener(OnSubClassChanged);
        confimButton.onClick.AddListener(creatorLogic.OnConfirmCharacterCreation);

        // Inicializa el personaje
        creatorLogic.NewCharacter();

        // Refresca la UI con el personaje inicial
        UpdateDropdownSelections();
    }

    void OnEnable()
    {
        creatorLogic.OnCharacterChanged += UpdateDropdownSelections;
    }

    void OnDisable()
    {
        creatorLogic.OnCharacterChanged -= UpdateDropdownSelections;
        confimButton.onClick.RemoveListener(creatorLogic.OnConfirmCharacterCreation);
    }

    void FillRaceDropdown()
    {
        raceDropdown.ClearOptions();
        var raceNames = RPGDatabase.Instance.allRaces.Select(r => r.raceName).ToList();
        raceDropdown.AddOptions(raceNames);
    }

    void FillSubRaceDropdown(int raceIndex)
    {
        subRaceDropdown.ClearOptions();
        var race = RPGDatabase.Instance.allRaces[raceIndex];

        if (race.availableSubRaces != null && race.availableSubRaces.Count > 0)
        {
            subRaceDropdown.interactable = true;
            var subRaceNames = race.availableSubRaces.Select(sr => sr.subRaceName).ToList();
            subRaceDropdown.AddOptions(subRaceNames);
            subRaceDropdown.value = 0;
        }
        else
        {
            subRaceDropdown.interactable = false;
            subRaceDropdown.AddOptions(new System.Collections.Generic.List<string> { "Ninguna" });
            subRaceDropdown.value = 0;
        }
    }

    void FillClassDropdown()
    {
        classDropdown.ClearOptions();
        var classNames = RPGDatabase.Instance.allClasses.Select(c => c.className).ToList();
        classDropdown.AddOptions(classNames);
    }

    void FillSubClassDropdown(int classIndex)
    {
        subClassDropdown.ClearOptions();
        var classDef = RPGDatabase.Instance.allClasses[classIndex];

        if (classDef.availableSubClasses != null && classDef.availableSubClasses.Count > 0)
        {
            subClassDropdown.interactable = true;
            var subClassNames = classDef.availableSubClasses.Select(sc => sc.subClassName).ToList();
            subClassDropdown.AddOptions(subClassNames);
            subClassDropdown.value = 0;
        }
        else
        {
            subClassDropdown.interactable = false;
            subClassDropdown.AddOptions(new System.Collections.Generic.List<string> { "Ninguna" });
            subClassDropdown.value = 0;
        }
    }

    void OnRaceChanged(int raceIndex)
    {
        // Actualiza el modelo
        creatorLogic.SetRace(raceIndex);

        // Refresca subrazas según la raza seleccionada
        FillSubRaceDropdown(raceIndex);

        // Actualiza la subraza en el modelo si corresponde
        if (subRaceDropdown.interactable)
            OnSubRaceChanged(subRaceDropdown.value);
        else
            creatorLogic.Character.SetSubRace(null);
    }

    void OnSubRaceChanged(int subRaceIndex)
    {
        var raceIndex = raceDropdown.value;
        var race = RPGDatabase.Instance.allRaces[raceIndex];

        if (race.availableSubRaces != null && race.availableSubRaces.Count > subRaceIndex)
        {
            creatorLogic.Character.SetSubRace(race.availableSubRaces[subRaceIndex]);
        }
        else
        {
            creatorLogic.Character.SetSubRace(null);
        }
    }

    void OnClassChanged(int classIndex)
    {
        creatorLogic.SetClass(classIndex);
        FillSubClassDropdown(classIndex);

        if (subClassDropdown.interactable)
        {
            OnSubClassChanged(subClassDropdown.value);
        }
        else creatorLogic.Character.SetSubClass(null);
    }

    void OnSubClassChanged(int subClassIndex)
    {
        var classIndex = classDropdown.value;
        var classDef = RPGDatabase.Instance.allClasses[classIndex];

        if (classDef.availableSubClasses != null && classDef.availableSubClasses.Count > subClassIndex)
            creatorLogic.SetSubClass(subClassIndex);
        else
            creatorLogic.Character.SetSubClass(null);
    }

    void UpdateDropdownSelections()
    {
        var character = creatorLogic.Character;
        if (character == null) return;

        var raceIdx = RPGDatabase.Instance.allRaces.IndexOf(character.Race);
        if (raceIdx >= 0) raceDropdown.value = raceIdx;

        FillSubRaceDropdown(raceIdx);
        var subRaceIdx = -1;

        if (character.Race.availableSubRaces != null && character.SubRace != null)
            subRaceIdx = character.Race.availableSubRaces.IndexOf(character.SubRace);

        if (subRaceIdx >= 0) subRaceDropdown.value = subRaceIdx;

        var classIdx = RPGDatabase.Instance.allClasses.IndexOf(character.ClassDef);
        if (classIdx >= 0) classDropdown.value = classIdx;

        FillSubClassDropdown(classIdx);
        var subClassIdx = -1;

        if (character.ClassDef.availableSubClasses != null && character.SubClass != null)
            subClassIdx = character.ClassDef.availableSubClasses.IndexOf(character.SubClass);

        if (subClassIdx >= 0) subClassDropdown.value = subClassIdx;
    }
}