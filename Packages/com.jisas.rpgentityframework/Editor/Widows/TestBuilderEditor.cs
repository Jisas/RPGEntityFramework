using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Editor
{
    [System.Serializable]
    public class TestBuilderEditor
    {
        public enum LogType
        {
            Success,
            Warning,
            Error
        }

        private VisualElement _root, _testBuilderContainer, _statsContainer, _logContainer;
        private DropdownField _raceDropdown, _subRaceDropdown, _classDropdown, _subClassDropdown;
        private Button _btnRecalculate, _btnRunScan, _clearConsoloBtn;
        private Label _issuesFoundLabel;
        private RPGEntityDatabase _database;

        private const string QUICK_PRESET_TEMPLATE_PATH = "Packages/com.jisas.rpgentityframework/Editor/Templates/QuickPresetTemplate.uxml";
        private const string STAT_TEMPLATE_PATH = "Packages/com.jisas.rpgentityframework/Editor/Templates/StatItemTemplate.uxml";
        private const string LOG_TEMPLATE_PATH = "Packages/com.jisas.rpgentityframework/Editor/Templates/LogElementTemplate.uxml";

        public void Initialize(VisualElement root, VisualElement testBuilderContainer, RPGEntityDatabase DB) 
        {
            _root = root;
            _database = DB;
            _testBuilderContainer = testBuilderContainer;

            SetupReferences();
            SetupButtons();
        }
        public void PopulateTestBuilderDropdowns()
        {
            // Función auxiliar para obtener los nombres de las listas
            static List<string> GetNames<T>(List<T> list, bool includeNone = false) where T : RPGDefinition
            {
                var names = list.Where(x => x != null).Select(x => x.name).ToList();
                if (includeNone || names.Count == 0) names.Insert(0, "None");
                return names;
            }

            // Llenar Razas
            _raceDropdown.choices = GetNames(_database.allRaces);
            _raceDropdown.value = _raceDropdown.choices.Count > 0 ? _raceDropdown.choices[0] : "None";

            // Llenar Sub-Razas (Permitimos "None" porque es opcional)
            _subRaceDropdown.choices = GetNames(_database.allSubRaces, true);
            _subRaceDropdown.value = _subRaceDropdown.choices[0];

            // Llenar Clases
            _classDropdown.choices = GetNames(_database.allClasses);
            _classDropdown.value = _classDropdown.choices.Count > 0 ? _classDropdown.choices[0] : "None";

            // Llenar Sub-Clases (Permitimos "None" porque es opcional)
            _subClassDropdown.choices = GetNames(_database.allSubClasses, true);
            _subClassDropdown.value = _subClassDropdown.choices[0];
        }
        public void PopulateQuickPresets()
        {
            // Buscamos el contenedor dentro del Test Builder (ajusta el nombre si es distinto en tu UXML)
            VisualElement container = _testBuilderContainer.Q<VisualElement>("presets-list-container");
            if (container == null) return;

            container.Clear();

            if (_database.allPresets == null || _database.allPresets.Count == 0)
            {
                Label log = new("No presets found in database.");
                log.AddToClassList("text-muted");
                log.AddToClassList("text-xs");
                container.Add(log);
                return;
            }

            // Cargamos el asset del template
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(QUICK_PRESET_TEMPLATE_PATH);

            foreach (var preset in _database.allPresets)
            {
                if (preset == null) continue;

                // Instanciar el template
                VisualElement entry = tree.Instantiate();

                // Configurar los datos internos (basado en los nombres de tu UXML)
                Label nameLabel = entry.Q<Label>("preset-name"); // Nombre del Label en tu snippet

                if (nameLabel != null)
                    nameLabel.text = string.IsNullOrEmpty(preset.characterName) ? preset.name : preset.characterName;

                Button uploadBtn = entry.Q<Button>("upload-btn");
                uploadBtn.clicked += () => LoadPresetIntoBuilder(preset);

                // Añadir al contenedor
                container.Add(entry);
            }
        }

        private void SetupReferences()
        {
            _statsContainer = _root.Q<VisualElement>("stats-container");
            _logContainer = _root.Q<VisualElement>("integrity-log-container");

            _btnRecalculate = _root.Q<Button>("btn-recalculate");
            _clearConsoloBtn = _root.Q<Button>("clear-console-btn");
            _btnRunScan = _root.Q<Button>("btn-run-scan");

            _issuesFoundLabel = _root.Q<Label>("issues-foud-label");

            _raceDropdown = _root.Q<DropdownField>("dropdown-race");
            _subRaceDropdown = _root.Q<DropdownField>("dropdown-subrace");
            _classDropdown = _root.Q<DropdownField>("dropdown-class");
            _subClassDropdown = _root.Q<DropdownField>("dropdown-subclass");
        }
        private void SetupButtons()
        {
            _btnRunScan.clicked += RunFullDatabaseScan;
            _btnRecalculate.clicked += RecalculateStats;
            _clearConsoloBtn.clicked += ClearTestConsole;
        }
        private void LoadPresetIntoBuilder(EntityPresetDefinition preset)
        {
            if (preset == null) return;

            // Actualizamos los valores de los dropdowns
            // Nota: Esto disparará los eventos 'RegisterValueChangedCallback' que ya tengas configurados
            if (preset.race != null) _raceDropdown.value = preset.race.name;
            if (preset.subRace != null) _subRaceDropdown.value = preset.subRace.name;
            if (preset.classDef != null) _classDropdown.value = preset.classDef.name;
            if (preset.subClass != null) _subClassDropdown.value = preset.subClass.name;

            // Forzamos el recálculo para mostrar los stats del preset inmediatamente
            RecalculateStats();
            AddLog("Preset Loaded", $"Applied configuration: {preset.characterName}", LogType.Success);
        }
        private void AddStat(string statName, string baseVal, string modVal, float fillPercent)
        {
            // Cargar el template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(STAT_TEMPLATE_PATH);
            var statInst = template.Instantiate();

            // 1. Agregar la clase a la instancia para que ajuste su tamaño correctamente
            statInst.AddToClassList("stat-item");

            // 2. Buscar las etiquetas
            var labels = statInst.Query<Label>().ToList();
            if (labels.Count >= 2)
            {
                labels[0].text = statName;
                labels[1].text = $"{baseVal} ({modVal})";
            }

            // 3. Ajustar el porcentaje de la barra
            var fill = statInst.Q<VisualElement>(className: "stat-fill");
            if (fill != null)
            {
                fill.style.width = Length.Percent(fillPercent);
            }

            // Añadir al contenedor
            _statsContainer.Add(statInst);
        }
        private void AddLog(string title, string description, LogType type)
        {
            // Cargar el template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LOG_TEMPLATE_PATH);
            var logInst = template.Instantiate();

            // Obtener referencias internas
            var rootEl = logInst.Q<VisualElement>("log-element");
            var iconEl = logInst.Q<VisualElement>("log-icon");
            var titleLbl = logInst.Q<Label>("log-tittle"); // Usando el nombre exacto de tu UXML
            var descLbl = logInst.Q<Label>("log-description");

            titleLbl.text = title;
            descLbl.text = description;

            // Configuración por defecto
            Color targetColor = Color.white;
            string iconName = "warning";

            // Asignar Color e Icono según el Enum
            switch (type)
            {
                case LogType.Success:
                    targetColor = new Color(0.145098f, 0.3882353f, 0.9215686f); // Azul
                    iconName = "check"; // Asegúrate de tener este icono
                    break;
                case LogType.Warning:
                    targetColor = new Color(0.98f, 0.8f, 0.08f); // Amarillo
                    iconName = "danger";
                    break;
                case LogType.Error:
                    targetColor = new Color(0.97f, 0.44f, 0.44f); // Rojo
                    iconName = "error"; // Asegúrate de tener este icono
                    break;
            }

            // 1. Aplicar el color al borde izquierdo y al tinte del icono
            rootEl.style.borderLeftColor = targetColor;
            iconEl.style.unityBackgroundImageTintColor = targetColor;

            // 2. Cargar el sprite correspondiente
            var sprite = Resources.Load<Sprite>($"Icons/{iconName}");
            if (sprite != null)
            {
                iconEl.style.backgroundImage = Background.FromSprite(sprite);
            }

            // Añadir al contenedor
            _logContainer.Add(logInst);
        }
        private void RecalculateStats()
        {
            // 1. Limpiar la UI
            _statsContainer.Clear();
            _logContainer.Clear();
            int issueCount = 0;

            // 2. Obtener selecciones (Usamos asset.name que es el que cargamos en PopulateTestBuilderDropdowns)
            string selectedRaceName = _raceDropdown.value;
            string selectedSubRaceName = _subRaceDropdown.value;
            string selectedClassName = _classDropdown.value;
            string selectedSubClassName = _subClassDropdown.value;

            // 3. Buscar las referencias reales en la DB
            RaceDefinition race = _database.allRaces.FirstOrDefault(r => r.name == selectedRaceName);
            SubRaceDefinition subRace = selectedSubRaceName != "None" ? _database.allSubRaces.FirstOrDefault(sr => sr.name == selectedSubRaceName) : null;
            ClassDefinition cls = selectedClassName != "None" ? _database.allClasses.FirstOrDefault(c => c.name == selectedClassName) : null;
            SubClassDefinition subClass = selectedSubClassName != "None" ? _database.allSubClasses.FirstOrDefault(sc => sc.name == selectedSubClassName) : null;

            if (race == null)
            {
                AddLog("Missing Core Data", "You must select at least a valid Race to calculate stats.", LogType.Error);
                _issuesFoundLabel.text = "1 Issues found";
                _issuesFoundLabel.style.display = DisplayStyle.Flex;
                return;
            }

            // --- 4. VALIDACIÓN DE INTEGRIDAD (Reglas de tu Framework) ---

            if (subRace != null && !race.availableSubRaces.Contains(subRace))
            {
                AddLog("Incompatible Sub-Race", $"'{subRace.subRaceName}' is not available for '{race.raceName}'.", LogType.Warning);
                issueCount++;
            }

            if (cls != null && !race.availableClasses.Contains(cls))
            {
                AddLog("Incompatible Class", $"The race '{race.raceName}' cannot choose the class '{cls.className}'.", LogType.Error);
                issueCount++;
            }

            if (cls != null && subClass != null && !cls.availableSubClasses.Contains(subClass))
            {
                AddLog("Incompatible Sub-Class", $"'{subClass.subClassName}' is not a valid path for '{cls.className}'.", LogType.Warning);
                issueCount++;
            }

            if (issueCount == 0)
            {
                AddLog("Configuration Valid", "All selected entities are perfectly compatible.", LogType.Success);
            }

            // --- 5. RECOLECCIÓN Y CÁLCULO DE ATRIBUTOS ---

            // Usamos un Diccionario para rastrear el Atributo y agrupar (Valor Base, Valor Bonus)
            Dictionary<AttributeDefinition, (float baseVal, float bonusVal)> statMap = new();

            // Función local para procesar las listas de tus ScriptableObjects
            void ProcessAttributes(List<AttributeValue> attributes, bool isBase)
            {
                if (attributes == null) return;

                foreach (var attrVal in attributes)
                {
                    if (attrVal.attribute == null) continue; // Por si hay campos vacíos en el inspector

                    // Si el atributo no existe en el diccionario, lo inicializamos
                    if (!statMap.ContainsKey(attrVal.attribute))
                    {
                        statMap[attrVal.attribute] = (0f, 0f);
                    }

                    // Sumamos donde corresponda
                    var (baseVal, bonusVal) = statMap[attrVal.attribute];

                    if (isBase) statMap[attrVal.attribute] = (baseVal + attrVal.value, bonusVal);
                    else statMap[attrVal.attribute] = (baseVal, bonusVal + attrVal.value);
                }
            }

            // Pasamos todas las capas del personaje al procesador
            ProcessAttributes(race.baseAttributes, true); // La raza da los stats base

            if (subRace != null) ProcessAttributes(subRace.bonusAttributes, false); // SubRaza da bonos
            if (cls != null) ProcessAttributes(cls.bonusAttributes, false);         // Clase da bonos
            if (subClass != null) ProcessAttributes(subClass.bonusAttributes, false); // SubClase da bonos

            // --- 6. INSTANCIAR LOS ELEMENTOS VISUALES ---

            // Asumimos un máximo temporal de 100 para la barra de progreso. 
            // Puedes ajustar este '100f' si en tu juego el máximo es 50, 255, etc.
            float CalculatePercent(float total) => Mathf.Clamp01(total / 100f) * 100f;

            foreach (var kvp in statMap)
            {
                AttributeDefinition attrDef = kvp.Key;
                float baseVal = kvp.Value.baseVal;
                float bonusVal = kvp.Value.bonusVal;
                float totalVal = baseVal + bonusVal;

                // Formatear el string del modificador (ej. "+5" o "-2" o "+0")
                string modifierStr = bonusVal != 0 ? $"{(bonusVal > 0 ? "+" : "")}{bonusVal}" : "+0";

                // Instanciamos el template usando los métodos que ya creaste
                AddStat(
                    statName: attrDef.attributeName,
                    baseVal: baseVal.ToString("0.##"), // "0.##" remueve decimales si son .0
                    modVal: modifierStr,
                    fillPercent: CalculatePercent(totalVal)
                );
            }

            // Actualizar UI del contador de problemas
            _issuesFoundLabel.text = $"{issueCount} Issues found";
            _issuesFoundLabel.style.display = issueCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private int ValidateUniqueIDs()
        {
            int collisions = 0;
            // Diccionario para rastrear: <ID, Nombre del primer objeto encontrado con ese ID>
            Dictionary<string, string> idTracker = new Dictionary<string, string>();

            // Creamos una lista maestra con todas las entidades para iterar una sola vez
            var allEntities = new List<RPGDefinition>();
            if (_database.allRaces != null) allEntities.AddRange(_database.allRaces);
            if (_database.allSubRaces != null) allEntities.AddRange(_database.allSubRaces);
            if (_database.allClasses != null) allEntities.AddRange(_database.allClasses);
            if (_database.allSubClasses != null) allEntities.AddRange(_database.allSubClasses);
            if (_database.allAttributes != null) allEntities.AddRange(_database.allAttributes);
            if (_database.allAbilities != null) allEntities.AddRange(_database.allAbilities);
            if (_database.allPresets != null) allEntities.AddRange(_database.allPresets);

            foreach (var entity in allEntities)
            {
                if (entity == null) continue;

                string id = entity.Id;

                if (string.IsNullOrEmpty(id))
                {
                    AddLog("Empty ID Found", $"Entity '{entity.name}' has no ID assigned.", LogType.Error);
                    collisions++;
                    continue;
                }

                if (idTracker.ContainsKey(id))
                {
                    // ¡Colisión detectada!
                    AddLog("Duplicate ID Collision",
                        $"ID '{id}' is shared by '{entity.name}' and '{idTracker[id]}'. Generate a new one!",
                        LogType.Error);
                    collisions++;
                }
                else
                {
                    idTracker.Add(id, entity.name);
                }
            }

            return collisions;
        }
        private void RunFullDatabaseScan()
        {
            if (_database == null) return;

            _logContainer.Clear();
            int issueCount = 0;

            // 1. Validar IDs Duplicados (Crítico)
            issueCount += ValidateUniqueIDs();

            // 2. Validar Razas
            foreach (var race in _database.allRaces)
            {
                if (race == null) continue;

                if (race.Icon == null)
                {
                    AddLog("Missing Icon", $"Race '{race.name}' has no icon assigned.", LogType.Warning);
                    issueCount++;
                }
                if (race.availableClasses == null || race.availableClasses.Count == 0)
                {
                    AddLog("Integrity Warning", $"Race '{race.name}' has no available classes.", LogType.Warning);
                    issueCount++;
                }
            }

            // 3. Validar Clases y Subclases
            foreach (var @class in _database.allClasses)
            {
                if (@class == null) continue;

                if (@class.availableSubClasses != null)
                {
                    foreach (var sub in @class.availableSubClasses)
                    {
                        if (sub == null)
                        {
                            AddLog("Null Reference", $"Class '{@class.name}' contains a null Sub-Class reference.", LogType.Error);
                            issueCount++;
                        }
                    }
                }
            }

            // 4. Validar Presets (Configuraciones críticas)
            foreach (var preset in _database.allPresets)
            {
                if (preset == null) continue;

                if (preset.race == null || preset.classDef == null)
                {
                    AddLog("Invalid Preset", $"Preset '{preset.name}' is missing a mandatory Race or Class.", LogType.Error);
                    issueCount++;
                }
            }

            // 5. Validar Habilidades
            foreach (var ability in _database.allAbilities)
            {
                if (ability == null) continue;
                if (ability.manaCost > 0 && ability.staminaCost > 0)
                {
                    AddLog("Balance Check", $"Ability '{ability.name}' consumes both Mana and Stamina. Is this intended?", LogType.Warning);
                    issueCount++;
                }
            }

            // 6. Actualizar UI del contador
            _issuesFoundLabel.text = $"{issueCount} Issues found";
            _issuesFoundLabel.style.display = issueCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (issueCount == 0)
            {
                AddLog("Database Healthy", "No issues found in the current database configuration.", LogType.Success);
            }
        }
        private void ClearTestConsole()
        {
            _logContainer.Clear();
            _statsContainer.Clear();
            _issuesFoundLabel.style.display = DisplayStyle.None;
        }
    }
}
