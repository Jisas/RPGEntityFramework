using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

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

        private VisualElement _inspectorContent, _inspectorIcon, _inspectorPlaceHolder, _listColumn, _testBuilderContainer;
        private Button _createButton, _saveButton, _settingsButton;
        private Label _inspectorTitle, _inspectorDefinition, _inspectorID;
        private ScrollView _inspectorColumn;
        private TextField _searchField;
        private ListView _itemList;

        private TestBuilderEditor _testBuilderEditor;
        private System.Type _currentType;
        private RPGEntityDatabase _database;
        private List<RPGDefinition> _currentUnfilteredList;

        // Iconos para el estado Dirty
        private Sprite _saveIconNormal;
        private Sprite _warningIcon;
        private bool _isDirty = false;

        private string _basePath = "Packages/com.jisas.rpgentityframework/Resources/Data/Definitions";
        private const string ROOT_VISUAL_TREE_PATH = "Packages/com.jisas.rpgentityframework/Editor/UXML/RGPEntityWindow.uxml";
        private const string LIST_ELEMENT_TEMPLATE_PATH = "Packages/com.jisas.rpgentityframework/Editor/Templates/ListElementTemplate.uxml";
        private const string PATH_PREF_KEY = "RPG_Framework_BasePath";

        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ROOT_VISUAL_TREE_PATH);
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
            _saveButton = root.Q<Button>("save-button");
            _settingsButton = root.Q<Button>("settings-button");
            _listColumn = root.Q<VisualElement>("list-column");
            _testBuilderContainer = root.Q<VisualElement>("test-builder-container");
            _searchField = root.Q<TextField>("search-field");
            _database = RPGEntityDatabase.Instance;

            // Cargar Iconos
            _saveIconNormal = _saveButton.iconImage.sprite; // Guardamos el original
            _warningIcon = Resources.Load<Sprite>("Icons/warning");

            // Registro de evento para barra de busqueda
            _searchField?.RegisterValueChangedCallback(evt => FilterList(evt.newValue));

            // Setup de elementos
            SetupListView();
            SetupButtons();

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

            // Setup de la herrmaienta de pruebas
            _testBuilderEditor = new();
            _testBuilderEditor.Initialize(root, _testBuilderContainer, _database);
            _testBuilderEditor.PopulateTestBuilderDropdowns();
            _testBuilderEditor.PopulateQuickPresets();
        }

        private void FilterList(string searchTerm)
        {
            if (_currentUnfilteredList == null) return;

            if (string.IsNullOrEmpty(searchTerm))
            {
                // Si no hay texto, restauramos la lista completa
                _itemList.itemsSource = _currentUnfilteredList;
            }
            else
            {
                // Filtramos buscando el término en el nombre (ignorando mayúsculas/minúsculas)
                string termLower = searchTerm.ToLower();
                var filteredList = _currentUnfilteredList
                    .Where(item => item != null && item.name.ToLower().Contains(termLower))
                    .ToList();

                _itemList.itemsSource = filteredList;
            }

            _itemList.Rebuild();
            _itemList.ClearSelection();
            ShowInInspector(null);
        }

        private void SetupButtons()
        {
            _saveButton.clicked += Save;
            _createButton.clicked += CreateNewEntity;
            _settingsButton.clicked += SetSettingsContextMenus;
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

            menu.AddItem(new GUIContent("Change Base Path..."), false, () =>
            {
                // Abrimos el panel en la raíz del proyecto
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string absolutePath = EditorUtility.OpenFolderPanel("Select Data Folder", _basePath, "");

                if (!string.IsNullOrEmpty(absolutePath))
                {
                    // Convertimos la ruta absoluta en una relativa (Assets/... o Packages/...)
                    string relativePath = FileUtil.GetProjectRelativePath(absolutePath);

                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        _basePath = relativePath;
                        EditorPrefs.SetString(PATH_PREF_KEY, _basePath);
                        Debug.Log($"Base path updated to: {_basePath}");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", "The folder must be inside the Project (Assets or Packages).", "OK");
                    }
                }
            });
            menu.AddItem(new GUIContent("Migrate All Assets to Base Path"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Migrate Assets",
                    $"This will move ALL definitions to {_basePath}. Are you sure?", "Yes, Migrate", "Cancel"))
                {
                    MigrateAllAssets();
                }
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ping Database Asset"), false, () => EditorGUIUtility.PingObject(_database));
            menu.ShowAsContext();
        }
        private void MigrateAllAssets()
        {
            if (_database == null) return;

            // Limpiamos la ruta de posibles barras finales para evitar rutas como "Folder//Subfolder"
            _basePath = _basePath.TrimEnd('/', '\\');

            // --- PASO 1: Preparar el terreno (FUERA del StartAssetEditing) ---
            // Creamos todas las carpetas necesarias primero para que Unity las registre
            EnsureFolderExists(_basePath);

            // Lista de tipos para crear subcarpetas de categoría
            System.Type[] types = 
            {
                typeof(RaceDefinition), typeof(SubRaceDefinition), typeof(ClassDefinition),
                typeof(SubClassDefinition), typeof(AttributeDefinition), typeof(AbilityDefinition),
                typeof(EntityPresetDefinition)
            };

            foreach (var t in types)
            {
                string folderName = t.Name.Replace("Definition", "Definitions");
                EnsureFolderExists($"{_basePath}/{folderName}");
            }

            // --- PASO 2: Migración Masiva (DENTRO del StartAssetEditing) ---
            AssetDatabase.StartAssetEditing();
            try
            {
                MoveCategory(_database.allRaces);
                MoveCategory(_database.allSubRaces);
                MoveCategory(_database.allClasses);
                MoveCategory(_database.allSubClasses);
                MoveCategory(_database.allAttributes);
                MoveCategory(_database.allAbilities);
                MoveCategory(_database.allPresets);

                Debug.Log("Migration logic executed.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Migration failed: {e.Message}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _itemList.Rebuild();
                EditorUtility.DisplayDialog("Migration Complete", "All assets have been moved successfully.", "OK");
            }
        }
        private void MoveCategory<T>(List<T> list) where T : RPGDefinition
        {
            if (list == null || list.Count == 0) return;

            // Definir ruta
            string folderName = typeof(T).Name.Replace("Definition", "Definitions");
            string targetFolder = $"{_basePath}/{folderName}";

            foreach (var item in list)
            {
                if (item == null) continue;

                string oldPath = AssetDatabase.GetAssetPath(item);
                if (string.IsNullOrEmpty(oldPath)) continue;

                string fileName = Path.GetFileName(oldPath);
                string newPath = $"{targetFolder}/{fileName}";

                // Solo movemos si la ruta es realmente distinta
                if (oldPath != newPath)
                {
                    string error = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"Could not move {item.name}: {error}");
                    }
                }
            }
        }
        private void EnsureFolderExists(string path)
        {
            string folderPath = path.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] folders = folderPath.Split('/');
            string currentPath = folders[0]; // "Assets" o "Packages"

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = $"{currentPath}/{folders[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        private void CreateNewEntity()
        {
            if (_currentType == null) return;

            // 1. Definir ruta
            string folderName = _currentType.Name.Replace("Definition", "Definitions");
            string folderPath = $"{_basePath}/{folderName}";
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

            // 4. Actualizar la "caché" local de la lista actual
            _currentUnfilteredList?.Add(newAsset as RPGDefinition);

            // 5. Forzar el refresco de la UI aplicando el filtro actual (o vacío)
            FilterList(_searchField?.value ?? "");

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
            _itemList.ClearSelection();             // limpia la selección de la lista
            _currentUnfilteredList?.Remove(item);   // Actualizar la "caché" local de la lista actual       
            FilterList(_searchField?.value ?? "");  // Forzar el refresco de la UI aplicando el filtro actual (o vacío)
            ShowInInspector(null);                  // fuerza que se muestre el Placeholder       

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
                var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LIST_ELEMENT_TEMPLATE_PATH);
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
            _currentType = typeof(T);

            // Guardamos la lista original y aplicamos a la vista
            _currentUnfilteredList = source.Cast<RPGDefinition>().ToList();
            _itemList.itemsSource = _currentUnfilteredList;

            // Limpiamos la barra de búsqueda al cambiar de categoría
            if (_searchField != null)
                _searchField.SetValueWithoutNotify("");

            _itemList.Rebuild();
            _itemList.ClearSelection();

            var createLabel = rootVisualElement.Q<Button>("create-button");
            createLabel.text = $" New {title}";

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
        private void ShowTestBuilderView()
        {
            // Ocultamos toda la interfaz de la base de datos
            if (_listColumn != null) _listColumn.style.display = DisplayStyle.None;
            if (_inspectorColumn != null) _inspectorColumn.style.display = DisplayStyle.None;
            if (_inspectorPlaceHolder != null) _inspectorPlaceHolder.style.display = DisplayStyle.None;

            // Mostramos el Test Builder
            if (_testBuilderContainer != null) _testBuilderContainer.style.display = DisplayStyle.Flex;
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
            _testBuilderEditor.PopulateTestBuilderDropdowns();
            _testBuilderEditor.PopulateQuickPresets();
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
    }
}