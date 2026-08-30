using System;
using System.Collections.Generic;
using LoogaSoft.UI.Design;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LoogaSoft.UI.Extensions.Editor.DesignSystem
{
    /// <summary>Renames the sub-assets stored in one design-system asset.</summary>
    internal sealed class LoogaDesignSubAssetManagerWindow : EditorWindow
    {
        private readonly List<UnityEngine.Object> _allAssets = new List<UnityEngine.Object>();
        private readonly List<UnityEngine.Object> _visibleAssets = new List<UnityEngine.Object>();

        [SerializeField] private LoogaDesignSystem _designSystem;
        [SerializeField] private string _search = string.Empty;

        private ListView _assetList;
        private Label _summary;

        internal static void Open(LoogaDesignSystem designSystem)
        {
            LoogaDesignSubAssetManagerWindow window = GetWindow<LoogaDesignSubAssetManagerWindow>();
            window.titleContent = new GUIContent("Design Sub-assets");
            window.minSize = new Vector2(520f, 320f);
            window._designSystem = designSystem;
            window.RefreshAssets();
            window.Show();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            Toolbar toolbar = new Toolbar();
            ObjectField designSystemField = new ObjectField
            {
                objectType = typeof(LoogaDesignSystem),
                allowSceneObjects = false,
                value = _designSystem
            };
            designSystemField.style.flexGrow = 1f;
            designSystemField.RegisterValueChangedCallback(evt =>
            {
                _designSystem = evt.newValue as LoogaDesignSystem;
                RefreshAssets();
            });
            toolbar.Add(designSystemField);

            ToolbarSearchField searchField = new ToolbarSearchField { value = _search };
            searchField.style.width = 220f;
            searchField.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue;
                RefreshFilter();
            });
            toolbar.Add(searchField);
            toolbar.Add(new ToolbarButton(RefreshAssets) { text = "Refresh" });
            root.Add(toolbar);

            _summary = new Label();
            _summary.style.marginLeft = 6f;
            _summary.style.marginTop = 4f;
            _summary.style.marginBottom = 4f;
            root.Add(_summary);

            _assetList = new ListView(_visibleAssets, 24f, MakeRow, BindRow)
            {
                selectionType = SelectionType.None,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                reorderable = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly
            };
            _assetList.style.flexGrow = 1f;
            root.Add(_assetList);

            RefreshAssets();
        }

        private VisualElement MakeRow()
        {
            return new SubAssetRow(RenameAsset);
        }

        private void BindRow(VisualElement element, int index)
        {
            ((SubAssetRow)element).Bind(_visibleAssets[index]);
        }

        private void RefreshAssets()
        {
            _allAssets.Clear();
            if (_designSystem != null)
            {
                string path = AssetDatabase.GetAssetPath(_designSystem);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] != null &&
                        AssetDatabase.IsSubAsset(assets[i]) &&
                        LoogaDesignSystemConsolidator.IsSupportedToken(assets[i]))
                    {
                        _allAssets.Add(assets[i]);
                    }
                }

                _allAssets.Sort(CompareAssets);
            }

            RefreshFilter();
        }

        private void RefreshFilter()
        {
            _visibleAssets.Clear();
            string filter = _search?.Trim();
            for (int i = 0; i < _allAssets.Count; i++)
            {
                UnityEngine.Object asset = _allAssets[i];
                if (string.IsNullOrEmpty(filter) ||
                    asset.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.GetType().Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _visibleAssets.Add(asset);
                }
            }

            if (_summary != null)
            {
                _summary.text =
                    $"{_visibleAssets.Count} of {_allAssets.Count} token sub-assets. " +
                    "Rename a row and press Enter to apply.";
            }
            _assetList?.Rebuild();
        }

        private void RenameAsset(UnityEngine.Object asset, string requestedName)
        {
            string trimmedName = requestedName?.Trim();
            if (asset == null || string.IsNullOrEmpty(trimmedName) || asset.name == trimmedName)
            {
                _assetList?.Rebuild();
                return;
            }

            string uniqueName = MakeUniqueName(asset, trimmedName);
            Undo.RecordObject(asset, "Rename Design Sub-asset");
            asset.name = uniqueName;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintProjectWindow();
            RefreshAssets();
        }

        private string MakeUniqueName(UnityEngine.Object renamedAsset, string requestedName)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _allAssets.Count; i++)
            {
                if (_allAssets[i] != renamedAsset)
                {
                    names.Add(_allAssets[i].name);
                }
            }

            if (names.Add(requestedName))
            {
                return requestedName;
            }

            for (int suffix = 2; suffix < int.MaxValue; suffix++)
            {
                string candidate = $"{requestedName} {suffix}";
                if (names.Add(candidate))
                {
                    return candidate;
                }
            }

            return requestedName;
        }

        private static int CompareAssets(UnityEngine.Object left, UnityEngine.Object right)
        {
            int typeComparison = string.CompareOrdinal(left.GetType().Name, right.GetType().Name);
            return typeComparison != 0 ? typeComparison : string.CompareOrdinal(left.name, right.name);
        }

        private sealed class SubAssetRow : VisualElement
        {
            private readonly Action<UnityEngine.Object, string> _rename;
            private readonly Image _icon;
            private readonly Label _type;
            private readonly TextField _name;
            private readonly Button _ping;

            private UnityEngine.Object _asset;

            internal SubAssetRow(Action<UnityEngine.Object, string> rename)
            {
                _rename = rename;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.paddingLeft = 4f;
                style.paddingRight = 4f;

                _icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit
                };
                _icon.style.width = 18f;
                _icon.style.height = 18f;
                _icon.style.marginRight = 4f;
                Add(_icon);

                _type = new Label();
                _type.style.width = 150f;
                _type.style.overflow = Overflow.Hidden;
                Add(_type);

                _name = new TextField { isDelayed = true };
                _name.style.flexGrow = 1f;
                _name.RegisterValueChangedCallback(evt => _rename(_asset, evt.newValue));
                Add(_name);

                _ping = new Button(Ping) { text = "Select" };
                _ping.style.width = 54f;
                Add(_ping);
            }

            internal void Bind(UnityEngine.Object asset)
            {
                _asset = asset;
                _icon.image = AssetPreview.GetMiniThumbnail(asset);
                _type.text = ObjectNames.NicifyVariableName(asset.GetType().Name);
                _type.tooltip = asset.GetType().FullName;
                _name.SetValueWithoutNotify(asset.name);
            }

            private void Ping()
            {
                Selection.activeObject = _asset;
                EditorGUIUtility.PingObject(_asset);
            }
        }
    }
}
