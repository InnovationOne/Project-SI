using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public class ConditionalHideAttribute : PropertyAttribute
{
    public string ConditionalSourceField = ""; // The name of the controlling field
    public bool HideInInspector = false; // TRUE = Hide in inspector, FALSE = Disable in inspector
    public int EnumValue = -1; // Optional: The specific enum value to check for (use when the controlling field is an enum)
    
    // Constructor for boolean fields
    public ConditionalHideAttribute(string conditionalSourceField)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = false;
    }

    // Constructor for boolean fields with a hide-in-inspector option
    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector)
    {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
    }

    // Constructor for enum fields where a specific enum value is the trigger
    public ConditionalHideAttribute(string conditionalSourceField, int enumValue) {
        ConditionalSourceField = conditionalSourceField;
        EnumValue = enumValue;
        HideInInspector = false;
    }

    // Full constructor for enum fields with all options
    public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector, int enumValue) {
        ConditionalSourceField = conditionalSourceField;
        HideInInspector = hideInInspector;
        EnumValue = enumValue;
    }
}