using OdinInspector.RequiredFields;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: RegisterValidator(typeof(AllRequiredValidator), Priority = 500)]

namespace OdinInspector.RequiredFields
{
	public class AllRequiredValidator : Validator
	{
		public override RevalidationCriteria RevalidationCriteria => RevalidationCriteria.OnValueChange;
		

		public override void RunValidation(ref ValidationResult result)
		{
			if (Property.ValueEntry == null) return;
			if (Property.Name.StartsWith("$")) return;

			if (result == null)
			{
				result = new ValidationResult
				{
					Setup = new ValidationSetup
					{
						Root      = Property.SerializationRoot.ValueEntry.WeakValues[0] as Object,
						ParentInstance = Property.Parent,
						//Member    = Property.Info.GetMemberInfo(),
						Validator = this
					},
					ResultType = ValidationResultType.Valid
				};
			}
		}

		public override bool CanValidateProperty(InspectorProperty property)
		{
			bool is_public_field      = property.Info.IsEditable && property.Info.HasBackingMembers;
			bool serialized_attr      = property.GetAttribute<SerializeField>() != null;
			bool serialized_attr_odin = property.GetAttribute<OdinSerializeAttribute>() != null;

			return is_public_field || serialized_attr || serialized_attr_odin;
		}
	}
}