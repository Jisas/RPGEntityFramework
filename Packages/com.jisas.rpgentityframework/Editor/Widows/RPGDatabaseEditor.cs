using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace RPGFramework.Editor
{
    public class RPGDatabaseEditor : EditorWindow
    {
        [MenuItem("RPG Framework/Database Editor")]
        public static void OpenWindow()
        {
            RPGDatabaseEditor wnd = GetWindow<RPGDatabaseEditor>();
            wnd.titleContent = new GUIContent("RPG Entity Framework");
            wnd.minSize = new Vector2(800, 500);
        }

        private VisualElement _inspectorContent, _inspectorIcon;
        private Label _inspectorTitle, _inspectorDefinition, _inspectorID;
        private Button _saveButton, _settingsButton;
        private ListView _itemList;

        private RPGEntityDatabase _database;
        private VisualElement _navbar;

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
            _inspectorIcon = root.Q<VisualElement>("inspector-icon");
            _inspectorTitle = root.Q<Label>("inspector-title");
            _inspectorID = root.Q<Label>("inspector-id");
            _inspectorDefinition = root.Q<Label>("inspector-definition");
            _itemList = root.Q<ListView>("item-list");
            _navbar = root.Q<VisualElement>("sidebar");
            _saveButton = root.Q<Button>("save-button");
            _settingsButton = root.Q<Button>("settings-button");
            _database = RPGEntityDatabase.Instance;

            // Cargar Iconos
            _saveIconNormal = _saveButton.iconImage.sprite; // Guardamos el original
            _warningIcon = Resources.Load<Sprite>("Icons/warning");

            SetupListView();
            SetupButtons();

            // EVENTO CLAVE: Detectar cualquier cambio en el Inspector
            // SerializedPropertyChangeEvent se dispara cuando un PropertyField cambia su valor
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

            // Carga inicial (marcando el primero como activo)
            var initialBtn = root.Q<VisualElement>("nav-races");
            SelectCategory("Race", _database.allRaces, initialBtn);
        }

        private void SetupButtons()
        {
            // Lógica de Guardado
            _saveButton.clicked += Save;

            // Lógica de Configuración (Sugerencia: Menú contextual)
            _settingsButton.clicked += () =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Validar Base de Datos"), false, ValidateDatabase);
                menu.AddItem(new GUIContent("Ping Database Asset"), false, () => EditorGUIUtility.PingObject(_database));
                menu.ShowAsContext();
            };
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
        private void Save()
        {
            if (!_isDirty) return;

            // 1. Forzar el guardado de los assets modificados en disco
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Refrescar la lista para mostrar nombres actualizados si cambiaron
            _itemList.Rebuild();

            // 3. Limpiar estado
            ClearDirty();

            Debug.Log("<color=green>RPG Database: Cambios guardados correctamente.</color>");
        }
        private void ValidateDatabase()
        {
            // Ejemplo de funcionalidad para el botón Settings
            int issues = 0;
            foreach (var race in _database.allRaces)
            {
                if (race.Icon == null) { Debug.LogWarning($"Validación: La raza {race.name} no tiene icono."); issues++; }
            }

            if (issues == 0) Debug.Log("Validación: ¡Todo parece estar en orden!");
            else Debug.Log($"Validación completada: {issues} advertencias encontradas.");
        }

        private void RegisterNavButton<T>(string elementName, string title, List<T> source) where T : ScriptableObject
        {
            var btn = rootVisualElement.Q<VisualElement>(elementName);
            btn?.RegisterCallback<ClickEvent>(evt => SelectCategory(title, source, btn));
        }
        private void SetupListView()
        {
            // Cambiamos la carga del template para asegurar que use la ruta correcta
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("Templates/ListElementTemplate");

            _itemList.fixedItemHeight = 35;
            _itemList.makeItem = () => template.CloneTree();
            _itemList.bindItem = (element, i) =>
            {
                var item = _itemList.itemsSource[i] as RPGDefinition;
                element.Q<Label>("name").text = item != null ? item.name : "Null Item";
                element.Q<VisualElement>("icon").style.backgroundImage = Background.FromSprite(item != null ? item.Icon : null);
            };

            // FIX: Convertimos a List para evitar el NullReferenceException por cast fallido
            _itemList.selectionChanged += objects =>
            {
                ShowInInspector(objects.ToList());
            };
        }
        private void SelectCategory<T>(string title, List<T> source, VisualElement targetElement) where T : ScriptableObject
        {
            // 1. Actualizar Datos
            _itemList.itemsSource = source;
            _itemList.Rebuild();
            _itemList.ClearSelection(); // Limpiar inspector al cambiar categoría

            var createLabel = rootVisualElement.Q<Label>("create-btn-text");
            if (createLabel != null) createLabel.text = $"+ New {title}";

            // 2. Gestionar Estilo .active
            if (targetElement != null)
            {
                // Removemos la clase de todos los hermanos para que solo uno brille
                targetElement.parent.Query(className: "active").ForEach(el => el.RemoveFromClassList("active"));
                targetElement.AddToClassList("active");
            }
        }
        private void ShowInInspector(List<object> selectedItems)
        {
            if (_inspectorContent == null) return;

            _inspectorContent.Clear();
            if (selectedItems == null || selectedItems.Count == 0 || selectedItems[0] == null)
            {
                _inspectorTitle.text = "No selection";
                _inspectorDefinition.text = "No selection";
                _inspectorID.text = "ID: No selection";
                return;
            }

            var target = selectedItems[0] as ScriptableObject;

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
    }
}