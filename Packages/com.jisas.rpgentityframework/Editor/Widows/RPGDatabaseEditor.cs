using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace RPGFramework.Editor
{
    public class RPGDatabaseEditor : EditorWindow
    {
        public enum LogType
        {
            Success,
            Warning,
            Error
        }

        [MenuItem("RPG Entity Framework/Entity Manager")]
        public static void OpenWindow()
        {
            RPGDatabaseEditor wnd = GetWindow<RPGDatabaseEditor>();
            wnd.titleContent = new GUIContent("Entity Manager");
            wnd.minSize = new Vector2(800, 500);
        }

        private VisualElement _inspectorContent, _inspectorIcon, _inspectorPlaceHolder, _listColumn;
        private VisualElement _testBuilderContainer, _statsContainer, _logContainer;
        private Label _inspectorTitle, _inspectorDefinition, _inspectorID, _issuesFoundLabel;
        private DropdownField _raceDropdown, _subRaceDropdown, _classDropdown, _subClassDropdown;
        private Button _createButton, _saveButton, _settingsButton, _btnRecalculate, _btnRunScan, _clearConsoloBtn;
        private ScrollView _inspectorColumn;
        private ListView _itemList;

        private VisualElement _navbar;
        private System.Type _currentType; // Para saber qué estamos creando
        private readonly string _basePath = "Packages/com.jisas.rpgentityframework/Resources/Data/Definitions";
        private RPGEntityDatabase _database;

        // Iconos para el estado Dirty
        private Sprite _saveIconNormal;
        private Sprite _warningIcon;
        private bool _isDirty = false;

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.jisas.rpgentityframework/Editor/UXML/RGPEntityWindow.uxml");
            VisualElement root = visualTree.Instantiate();
            rootVisualElement.Add(root);

            _inspectorContent = root.Q<VisualElement>("inspector-content");
            _inspectorColumn = root.Q<ScrollView>("inspector-column");
            _inspectorPlaceHolder = root.Q<VisualElement>("inspector-place-holder");
            _inspectorIcon = root.Q<VisualElement>("inspector-icon");
            _inspectorTitle = root.Q<Label>("inspector-title");
            _inspectorID = root.Q<Label>("inspector-id");
            _inspectorDefinition = root.Q<Label>("inspector-definition");
            _itemList = root.Q<ListView>("item-list");
            _createButton = root.Q<Button>("create-button");
            _navbar = root.Q<VisualElement>("sidebar");
            _saveButton = root.Q<Button>("save-button");
            _settingsButton = root.Q<Button>("settings-button");
            _listColumn = root.Q<VisualElement>("list-column");
            _testBuilderContainer = root.Q<VisualElement>("test-builder-container");
            _statsContainer = root.Q<VisualElement>("stats-container");
            _logContainer = root.Q<VisualElement>("integrity-log-container");
            _issuesFoundLabel = root.Q<Label>("issues-foud-label");
            _raceDropdown = root.Q<DropdownField>("dropdown-race");
            _subRaceDropdown = root.Q<DropdownField>("dropdown-subrace");
            _classDropdown = root.Q<DropdownField>("dropdown-class");
            _subClassDropdown = root.Q<DropdownField>("dropdown-subclass");
            _btnRecalculate = root.Q<Button>("btn-recalculate");
            _clearConsoloBtn = root.Q<Button>("clear-console-btn");
            _btnRunScan = root.Q<Button>("btn-run-scan");
            _database = RPGEntityDatabase.Instance;

            // Cargar Iconos
            _saveIconNormal = _saveButton.iconImage.sprite; // Guardamos el original
            _warningIcon = Resources.Load<Sprite>("Icons/warning");

            SetupListView();
            SetupButtons();
            PopulateTestBuilderDropdowns();

            // Detectar cualquier cambio en el Inspector
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                MarkAsDirty();

                // Obtenemos el objeto seleccionado actualmente en la lista
                if (_itemList.selectedItem is ScriptableObject selectedObj)
                {
                    RefreshInspectorHeader(selectedObj);
                }
            });

            // Eventos de Navegación: Pasamos el elemento actual para gestionar el estilo .active
            RegisterNavButton("nav-races", "Race", _database.allRaces);
            RegisterNavButton("nav-subraces", "Sub-Race", _database.allSubRaces); // Corregido: antes apuntaba a allSubClasses
            RegisterNavButton("nav-classes", "Class", _database.allClasses);
            RegisterNavButton("nav-subclasses", "Sub-Class", _database.allSubClasses);
            RegisterNavButton("nav-attributes", "Attribute", _database.allAttributes);
            RegisterNavButton("nav-abilities", "Ability", _database.allAbilities);
            RegisterNavButton("nav-presets", "Preset", _database.allPresets);

            VisualElement testBuilderNav = root.Q<VisualElement>("nav-test");
            testBuilderNav?.RegisterCallback<ClickEvent>(evt =>
            {
                SetActiveNavElement(testBuilderNav); // Ilumina el botón en el navbar
                ShowTestBuilderView();              // Cambia el layout
            });

            // Carga inicial (marcando el primero como activo)
            var initialBtn = root.Q<VisualElement>("nav-races");
            SelectCategory("Race", _database.allRaces, initialBtn);
            ShowInInspector(null);
        }
        private void OnDestroy()
        {
            _saveButton.clicked -= Save;
            _createButton.clicked -= CreateNewEntity;
            _settingsButton.clicked -= SetSettingsContextMenus;
            _btnRunScan.clicked -= RunFullDatabaseScan;
            _btnRecalculate.clicked -= RecalculateStats;
            _clearConsoloBtn.clicked -= ClearTestConsole;
        }

        private void SetupButtons()
        {
            _saveButton.clicked += Save;
            _createButton.clicked += CreateNewEntity;
            _settingsButton.clicked += SetSettingsContextMenus;
            _btnRunScan.clicked += RunFullDatabaseScan;
            _btnRecalculate.clicked += RecalculateStats;
            _clearConsoloBtn.clicked += ClearTestConsole;
        }
        private void MarkAsDirty()
        {
            if (_isDirty) return;

            _isDirty = true;
            _saveButton.iconImage = Background.FromSprite(_warningIcon);
            _saveButton.style.backgroundColor = new StyleColor(new Color(0.7f, 0.2f, 0.2f)); // Un tono rojizo opcional
        }
        private void ClearDirty()
        {
            _isDirty = false;
            _saveButton.iconImage = Background.FromSprite(_saveIconNormal);
            _saveButton.style.backgroundColor = new StyleColor(StyleKeyword.Null); // Vuelve al color del USS
        }

        private void SetSettingsContextMenus()
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Validar Base de Datos"), false, ValidateDatabase);
            menu.AddItem(new GUIContent("Ping Database Asset"), false, () => EditorGUIUtility.PingObject(_database));
            menu.ShowAsContext();
        }
        private void CreateNewEntity()
        {
            if (_currentType == null) return;

            // 1. Definir ruta (Base + Tipo)
            string folderPath = $"{_basePath}/{_currentType.Name}s";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // 2. Crear instancia
            ScriptableObject newAsset = ScriptableObject.CreateInstance(_currentType);
            string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/New {_currentType.Name}.asset");

            AssetDatabase.CreateAsset(newAsset, fullPath);

            // 3. Registrar en la base de datos principal
            RegisterInDatabase(newAsset);

            AssetDatabase.SaveAssets();
            _itemList.Rebuild();

            // Seleccionar el nuevo item automáticamente
            _itemList.SetSelection(_itemList.itemsSource.Count - 1);
        }
        private void DeleteEntity(RPGDefinition item)
        {
            if (item == null) return;

            // 1. Cuadro de confirmación
            string mensaje = $"Are you sure you want to permanently delete ‘{item.name}’?\n\nThis action will delete the file and remove it from the database.";
            if (!EditorUtility.DisplayDialog("Delete Definition", mensaje, "Delete", "Cancel"))
            {
                return;
            }

            // 2. Eliminar de la lista de la Base de Datos
            RemoveFromDatabase(item);

            // 3. Eliminar el archivo físico
            string path = AssetDatabase.GetAssetPath(item);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            // 4. Guardar cambios en la base de datos y refrescar
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. Refrescar la UI
            _itemList.Rebuild();
            _itemList.ClearSelection(); // limpia la selección de la lista
            ShowInInspector(null);      // fuerza que se muestre el Placeholder

            Debug.Log($"Entidad eliminada.");
        }
        private void ValidateDatabase()
        {
            int issues = 0;
            foreach (var race in _database.allRaces)
            {
                if (race.Icon == null) { Debug.LogWarning($"Validación: La raza {race.name} no tiene icono."); issues++; }
            }

            if (issues == 0) Debug.Log("Validación: ¡Todo parece estar en orden!");
            else Debug.Log($"Validación completada: {issues} advertencias encontradas.");
        }
        private void RemoveFromDatabase(RPGDefinition item)
        {
            // Dependiendo del tipo, lo quitamos de su lista correspondiente
            if (item is RaceDefinition r) _database.allRaces.Remove(r);
            else if (item is ClassDefinition c) _database.allClasses.Remove(c);
            else if (item is AbilityDefinition a) _database.allAbilities.Remove(a);
            else if (item is SubRaceDefinition sr) _database.allSubRaces.Remove(sr);
            else if (item is SubClassDefinition sc) _database.allSubClasses.Remove(sc);
            else if (item is AttributeDefinition attr) _database.allAttributes.Remove(attr);
        }

        private void RegisterInDatabase(ScriptableObject asset)
        {
            if (asset is RaceDefinition r) _database.allRaces.Add(r);
            else if (asset is SubRaceDefinition sr) _database.allSubRaces.Add(sr);
            else if (asset is ClassDefinition c) _database.allClasses.Add(c);
            else if (asset is SubClassDefinition sc) _database.allSubClasses.Add(sc);
            else if (asset is AttributeDefinition att) _database.allAttributes.Add(att);
            else if (asset is AbilityDefinition ab) _database.allAbilities.Add(ab);
            else if (asset is EntityPresetDefinition ep) _database.allPresets.Add(ep);

            EditorUtility.SetDirty(_database);
        }
        private void RegisterNavButton<T>(string elementName, string title, List<T> source) where T : ScriptableObject
        {
            var btn = rootVisualElement.Q<VisualElement>(elementName);
            btn?.RegisterCallback<ClickEvent>(evt => 
            {
                ShowDatabaseView();
                SelectCategory(title, source, btn);
            });
        }

        private void SetupListView()
        {
            _itemList.fixedItemHeight = 30;
            _itemList.makeItem = () => 
            {
                var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.jisas.rpgentityframework/Editor/Templates/ListElementTemplate.uxml");
                return template.Instantiate();
            };

            _itemList.bindItem = (element, i) =>
            {
                var item = _itemList.itemsSource[i] as RPGDefinition;
                if (item == null) return;

                element.Q<Label>("name").text = item.name;
                element.Q<VisualElement>("icon").style.backgroundImage = Background.FromSprite(item.Icon);

                // --- LÓGICA DEL BOTÓN DE BORRADO ---
                var deleteBtn = element.Q<Button>("delete-btn");

                // Limpiamos acciones previas para evitar ejecuciones múltiples por el reciclaje del ListView
                deleteBtn.clickable = new Clickable(() => 
                {
                    DeleteEntity(item);
                });
            };

            _itemList.selectionChanged += objects =>
            {
                ShowInInspector(objects.ToList());
            };
        }
        private void SelectCategory<T>(string title, List<T> source, VisualElement targetElement) where T : ScriptableObject
        {
            // 1. Actualizar Datos
            _currentType = typeof(T); // Guardamos el tipo actual
            _itemList.itemsSource = source;
            _itemList.Rebuild();
            _itemList.ClearSelection(); // Limpiar inspector al cambiar categoría

            var createLabel = rootVisualElement.Q<Button>("create-button");
            createLabel.text = $" New {title}";

            // 2. Gestionar Estilo .active
            if (targetElement != null) SetActiveNavElement(targetElement);
        }

        private void SetActiveNavElement(VisualElement targetElement)
        {
            targetElement.parent.Query(className: "active").ForEach(el => el.RemoveFromClassList("active"));
            targetElement.AddToClassList("active");
        }
        private void ShowInInspector(List<object> selectedItems)
        {
            // 1.Limpieza obligatoria: eliminamos lo que hubiera antes en el inspector
            _inspectorContent.Clear();

            // 2. Determinar si hay algo válido seleccionado
            bool hasSelection = selectedItems != null && selectedItems.Count > 0 && selectedItems[0] != null;

            // 3. Gestionar visibilidad de las columnas
            _inspectorPlaceHolder.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            _inspectorColumn.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection) return;

            var target = selectedItems[0] as ScriptableObject;

            // 4. Actualizar el encabezado (Icono, ID, Título)
            if (target is RPGDefinition rpgData)
            {
                _inspectorIcon.style.backgroundImage = Background.FromSprite(rpgData.Icon);
                _inspectorID.text = $"ID: {rpgData.Id}";
            }
            else
            {
                _inspectorIcon.style.backgroundImage = Background.FromSprite(null);
                _inspectorID.text = "ID: N/A (No es RPGDefinition)";
            }

            _inspectorTitle.text = target.name;
            _inspectorDefinition.text = $"{target.GetType().Name}(ScriptableObject)";

            // 5. Dibujar los campos de propiedades
            SerializedObject so = new(target);
            SerializedProperty prop = so.GetIterator();
            prop.NextVisible(true); // Saltar m_Script

            while (prop.NextVisible(false))
            {
                PropertyField field = new(prop);
                field.Bind(so);
                _inspectorContent.Add(field);
            }
        }
        private void RefreshInspectorHeader(ScriptableObject target)
        {
            if (target == null) return;

            // Actualizar Título
            _inspectorTitle.text = target.name;

            // Actualizar Icono e ID si es RPGDefinition
            if (target is RPGDefinition rpgData)
            {
                _inspectorIcon.style.backgroundImage = Background.FromSprite(rpgData.Icon);
                _inspectorID.text = $"ID: {rpgData.Id}";
            }
        }
        private void ShowDatabaseView()
        {
            // Ocultamos el Test Builder
            if (_testBuilderContainer != null) _testBuilderContainer.style.display = DisplayStyle.None;

            // Mostramos la columna de la lista
            if (_listColumn != null) _listColumn.style.display = DisplayStyle.Flex;

            // Decidimos si mostrar el Inspector o el Placeholder basado en si hay algo seleccionado
            if (_itemList != null && _itemList.selectedIndex >= 0)
            {
                if (_inspectorColumn != null) _inspectorColumn.style.display = DisplayStyle.Flex;
                if (_inspectorPlaceHolder != null) _inspectorPlaceHolder.style.display = DisplayStyle.None;
            }
            else
            {
                if (_inspectorColumn != null) _inspectorColumn.style.display = DisplayStyle.None;
                if (_inspectorPlaceHolder != null) _inspectorPlaceHolder.style.display = DisplayStyle.Flex;
            }
        }

        private void Save()
        {
            if (!_isDirty) return;

            // 1. Sincronizar nombres de archivos antes de guardar
            foreach (var item in _itemList.itemsSource)
            {
                if (item is ScriptableObject so) SyncFileName(so);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _itemList.Rebuild();
            PopulateTestBuilderDropdowns();
            ClearDirty();

            Debug.Log("Base de datos actualizada y archivos renombrados.");
        }
        private void SyncFileName(ScriptableObject asset)
        {
            string currentPath = AssetDatabase.GetAssetPath(asset);
            string newName = "";

            // Buscamos la variable de nombre según el tipo (esto evita que el SO se llame "New Race")
            if (asset is RaceDefinition r) newName = r.raceName;
            else if (asset is SubRaceDefinition sr) newName = sr.subRaceName;
            else if (asset is ClassDefinition c) newName = c.className;
            else if (asset is SubClassDefinition sc) newName = sc.subClassName;
            else if (asset is AttributeDefinition attr) newName = attr.attributeName;
            else if (asset is AbilityDefinition abty) newName = abty.abilityName;
            else if (asset is EntityPresetDefinition ep) newName = ep.characterName;

            if (string.IsNullOrEmpty(newName) || asset.name == newName) return;

            // Renombrar físicamente el asset
            string error = AssetDatabase.RenameAsset(currentPath, newName);
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"Error al renombrar: {error}");
        }

        private void ShowTestBuilderView()
        {
            // Ocultamos toda la interfaz de la base de datos
            if (_listColumn != null) _listColumn.style.display = DisplayStyle.None;
            if (_inspectorColumn != null) _inspectorColumn.style.display = DisplayStyle.None;
            if (_inspectorPlaceHolder != null) _inspectorPlaceHolder.style.display = DisplayStyle.None;

            // Mostramos el Test Builder
            if (_testBuilderContainer != null) _testBuilderContainer.style.display = DisplayStyle.Flex;
        }
        private void PopulateTestBuilderDropdowns()
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
        private void AddStat(string statName, string baseVal, string modVal, float fillPercent)
        {
            // Cargar el template
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.jisas.rpgentityframework/Editor/Templates/StatItemTemplate.uxml");
            var statInst = template.Instantiate();

            // 1. Agregar la clase a la instancia para que ajuste su tamaño correctamente
            statInst.AddToClassList("stat-item");

            // 2. Buscar las etiquetas (como no les pusiste 'name' en el UXML, las buscamos por tipo)
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
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.jisas.rpgentityframework/Editor/Templates/LogElementTemplate.uxml");
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