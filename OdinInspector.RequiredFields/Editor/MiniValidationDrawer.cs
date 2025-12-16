using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OdinInspector.RequiredFields
{
    //[DrawerPriority(0, 10000.1)]
    public class MiniValidationDrawer<T> : OdinValueDrawer<T>, IDisposable
    {
        private List<ValidationResult> _validationResults;
        private bool _rerunFullValidation;
        private object _shakeGroupKey;
        private ValidationComponent _validationComponent;

        private static readonly Color BG = new Color(1, 0, 0, 0.15f);
        private static readonly Color Shadow = new Color(0, 0, 0, 0.3f);
        private static readonly Color Band = new Color(1, 0, 0, 0.5f);

        private static readonly Color WarnBG = new Color(1f, 0.92f, 0f, 0.05f);
        private static readonly Color WarnShadow = new Color(0, 0, 0, .3f);
        private static readonly Color WarnBand = new Color(1f, 0.92f, 0f, 0.0f);

        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            ValidationComponent component = property.GetComponent<ValidationComponent>();
            return component != null && component.ValidatorLocator.PotentiallyHasValidatorsFor(property);
        }

        protected override void Initialize()
        {
            _validationComponent = Property.GetComponent<ValidationComponent>();
            _validationComponent.ValidateProperty(ref _validationResults);
            if (_validationResults.Count > 0)
            {
                _shakeGroupKey = UniqueDrawerKey.Create(Property, this);
                Property.Tree.OnUndoRedoPerformed += OnUndoRedoPerformed;
                ValueEntry.OnValueChanged += OnValueChanged;
                ValueEntry.OnChildValueChanged += OnChildValueChanged;
            }
            else
            {
                SkipWhenDrawing = true;
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_validationResults.Count == 0)
            {
                CallNextDrawer(label);
            }
            else
            {
                GUILayout.BeginVertical();
                SirenixEditorGUI.BeginShakeableGroup(_shakeGroupKey);

                ValidationResult error = null;
                ValidationResult warning = null;

                for (var i = 0; i < _validationResults.Count; ++i)
                {
                    ValidationResult result = _validationResults[i];

                    if (Event.current.type == EventType.Layout && _rerunFullValidation)
                    {
                        _rerunFullValidation = false;
                        ValidationResultType resultType = result.ResultType;

                        result.Setup.ParentInstance = Property.ParentValues[0];
                        //result.Setup.Validator = 
                        var prop = Property.SerializationRoot;
                        var validators = _validationComponent.ValidatorLocator.GetValidators(Property);
                        foreach (var validator in validators)
                        {
                            result = new ValidationResult
                            {
                                Setup = new ValidationSetup
                                {
                                    Root = Property.SerializationRoot.ValueEntry.WeakValues[0] as Object,
                                    ParentInstance = Property.Parent,
                                    //Member    = Property.Info.GetMemberInfo(),
                                    Validator = validator
                                },
                                ResultType = ValidationResultType.Valid
                            };
                            validator.Initialize(prop);
                            // validator.InitializeResult(ref result);
                            // validator.RunValidation(ref result);
                        }


                        if (resultType != result.ResultType && result.ResultType == ValidationResultType.Error)
                            SirenixEditorGUI.StartShakingGroup(_shakeGroupKey);
                    }

                    if (!IsValid(_validationComponent.Property.ValueEntry))
                    {
                        if (_validationComponent.Property.GetAttribute<OptionalAttribute>() == null)
                            error = result;
                        else
                            warning = result;
                    }
                }

                if (Event.current.type == EventType.Layout)
                    _rerunFullValidation = false;

                CallNextDrawer(label);

                if (error != null)
                {
                    if (label != null)
                        label.tooltip = $"ERROR: {error.Message}";


                    Rect rect = GUIHelper.GetCurrentLayoutRect();
                    if (Event.current.type == EventType.Repaint)
                    {
                        SirenixEditorGUI.DrawSolidRect(rect, BG);
                        SirenixEditorGUI.DrawBorders(rect, 0, 0, 1, 0, Shadow);
                        SirenixEditorGUI.DrawBorders(rect, 3, 0, 0, 0, BG);
                    }
                }
                else if (warning != null)
                {
                    if (label != null)
                        label.tooltip = $"WARNING: {warning.Message}";


                    Rect rect = GUIHelper.GetCurrentLayoutRect();
                    if (Event.current.type == EventType.Repaint)
                    {
                        SirenixEditorGUI.DrawSolidRect(rect, WarnBG);
                        SirenixEditorGUI.DrawBorders(rect, 0, 0, 1, 0, WarnShadow);
                        SirenixEditorGUI.DrawBorders(rect, 3, 0, 0, 0, WarnBand);
                    }
                }

                SirenixEditorGUI.EndShakeableGroup(_shakeGroupKey);
                GUILayout.EndVertical();
            }
        }

        public void Dispose()
        {
            if (_validationResults.Count > 0)
            {
                Property.Tree.OnUndoRedoPerformed -= OnUndoRedoPerformed;
                ValueEntry.OnValueChanged -= OnValueChanged;
                ValueEntry.OnChildValueChanged -= OnChildValueChanged;
            }

            _validationResults = null;
        }

        private void OnUndoRedoPerformed()
        {
            _rerunFullValidation = true;
        }

        private void OnValueChanged(int index)
        {
            _rerunFullValidation = true;
        }

        private void OnChildValueChanged(int index)
        {
            _rerunFullValidation = true;
        }

        private static bool IsValid(IPropertyValueEntry valueEntry)
        {
            object v = valueEntry?.WeakSmartValue;

            if (valueEntry == null) return false;
            if (valueEntry.ValueState == PropertyValueState.NullReference) return false;
            if (v == null) return false;
            if (v is Object o && !o) return false;
            if (v is string s && string.IsNullOrEmpty(s)) return false;

            return true;
        }
    }
}