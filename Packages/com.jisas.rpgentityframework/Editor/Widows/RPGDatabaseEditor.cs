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
        [MenuItem("RPG Entity Framework/Entity Manager")]
        public static void OpenWindow()
        {
            RPGDatabaseEditor wnd = GetWindow<RPGDatabaseEditor>();
            wnd.titleContent = new GUIContent("Entity Manager");
            wnd.minSize = new Vector2(800, 500);
        }

        private VisualElement _inspectorContent, _inspectorIcon, _inspectorPlaceHolder;
        private Label _inspectorTitle, _inspectorDefinition, _inspectorID;
        private Button _createButton, _saveButton, _settingsButton;
        private ScrollView _inspectorColumn;
        private ListView _itemList;

        private VisualElement _navbar;
        private System.Type _currentType; // Para saber qué estamos creando
        private string _basePath = "Assets/Data/Definitions";
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
            ShowInInspector(null);
        }

        private void SetupButtons()
        {
            // Lógica de Guardado
            _saveButton.clicked += Save;
            _createButton.clicked += CreateNewEntity;

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

            // 1. Sincronizar nombres de archivos antes de guardar
            foreach (var item in _itemList.itemsSource)
            {
                if (item is ScriptableObject so) SyncFileName(so);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _itemList.Rebuild();
            ClearDirty();

            Debug.Log("Base de datos actualizada y archivos renombrados.");
        }
        private void SyncFileName(ScriptableObject so)
        {
            string currentPath = AssetDatabase.GetAssetPath(so);
            string newName = "";

            // Buscamos la variable de nombre según el tipo (esto evita que el SO se llame "New Race")
            if (so is RaceDefinition r) newName = r.raceName;
            else if (so is ClassDefinition c) newName = c.className;
            else if (so is AbilityDefinition a) newName = a.abilityName;

            if (string.IsNullOrEmpty(newName) || so.name == newName) return;

            // Renombrar físicamente el asset
            string error = AssetDatabase.RenameAsset(currentPath, newName);
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"Error al renombrar: {error}");
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

        private void RegisterInDatabase(ScriptableObject asset)
        {
            if (asset is RaceDefinition r) _database.allRaces.Add(r);
            else if (asset is SubRaceDefinition sr) _database.allSubRaces.Add(sr);
            else if (asset is ClassDefinition c) _database.allClasses.Add(c);
            else if (asset is SubClassDefinition sc) _database.allSubClasses.Add(sc);
            else if (asset is AttributeDefinition att) _database.allAttributes.Add(att);
            else if (asset is AbilityDefinition ab) _database.allAbilities.Add(ab);

            EditorUtility.SetDirty(_database);
        }
        private void RegisterNavButton<T>(string elementName, string title, List<T> source) where T : ScriptableObject
        {
            var btn = rootVisualElement.Q<VisualElement>(elementName);
            btn?.RegisterCallback<ClickEvent>(evt => SelectCategory(title, source, btn));
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
            if (targetElement != null)
            {
                // Removemos la clase de todos los hermanos para que solo uno brille
                targetElement.parent.Query(className: "active").ForEach(el => el.RemoveFromClassList("active"));
                targetElement.AddToClassList("active");
            }
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
    }
}