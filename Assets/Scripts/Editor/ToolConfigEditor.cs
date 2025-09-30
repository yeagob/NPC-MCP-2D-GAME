using UnityEngine;
using UnityEditor;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Models.Tools.MCP;
using ChatSystem.Enums;
using System.Collections.Generic;

namespace ChatSystem.Editor
{
    [CustomEditor(typeof(ToolConfig))]
    public class ToolConfigEditor : UnityEditor.Editor
    {
        private bool showFunctionDetails = true;
        private bool showParameterProperties = true;
        private bool showAnnotations = false;
        private bool showExecutionSettings = true;
        
        public override void OnInspectorGUI()
        {
            ToolConfig config = (ToolConfig)target;
            
            EditorGUILayout.LabelField("Tool Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawIdentificationSection(config);
            DrawFunctionSection(config);
            DrawAnnotationsSection(config);
            DrawExecutionSection(config);
            DrawActionsSection(config);
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
            }
        }
        
        private void DrawIdentificationSection(ToolConfig config)
        {
            EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
            config.toolId = EditorGUILayout.TextField("Tool ID", config.toolId);
            config.toolName = EditorGUILayout.TextField("Tool Name", config.toolName);
            config.toolType = (ToolType)EditorGUILayout.EnumPopup("Tool Type", config.toolType);
            EditorGUILayout.Space();
        }
        
        private void DrawFunctionSection(ToolConfig config)
        {
            showFunctionDetails = EditorGUILayout.Foldout(showFunctionDetails, "MCP Function Definition", true);
            
            if (showFunctionDetails)
            {
                EditorGUI.indentLevel++;
                
                InitializeFunctionIfNull(config);
                
                config.function.name = EditorGUILayout.TextField("Name", config.function.name);
                config.function.description = EditorGUILayout.TextField("Description", config.function.description);
                
                EditorGUILayout.Space();
                showParameterProperties = EditorGUILayout.Foldout(showParameterProperties, "Parameters", true);
                
                if (showParameterProperties)
                {
                    EditorGUI.indentLevel++;
                    DrawPropertiesEditor(config.function.parameters);
                    DrawRequiredList(config.function.parameters);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        private void DrawAnnotationsSection(ToolConfig config)
        {
            showAnnotations = EditorGUILayout.Foldout(showAnnotations, "Annotations (MCP)", true);
            
            if (showAnnotations)
            {
                EditorGUI.indentLevel++;
                
                if (config.annotations == null)
                {
                    config.annotations = new ToolAnnotations();
                }
                
                config.annotations.title = EditorGUILayout.TextField("Title", config.annotations.title);
                config.annotations.readOnlyHint = EditorGUILayout.Toggle("Read Only Hint", config.annotations.readOnlyHint);
                config.annotations.destructiveHint = EditorGUILayout.Toggle("Destructive Hint", config.annotations.destructiveHint);
                config.annotations.idempotentHint = EditorGUILayout.Toggle("Idempotent Hint", config.annotations.idempotentHint);
                config.annotations.openWorldHint = EditorGUILayout.Toggle("Open World Hint", config.annotations.openWorldHint);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        private void DrawExecutionSection(ToolConfig config)
        {
            showExecutionSettings = EditorGUILayout.Foldout(showExecutionSettings, "Execution Settings", true);
            
            if (showExecutionSettings)
            {
                EditorGUI.indentLevel++;
                
                config.enabled = EditorGUILayout.Toggle("Enabled", config.enabled);
                config.requiresAuthentication = EditorGUILayout.Toggle("Requires Authentication", config.requiresAuthentication);
                config.timeoutMs = EditorGUILayout.IntSlider("Timeout (ms)", config.timeoutMs, 100, 30000);
                config.maxRetries = EditorGUILayout.IntSlider("Max Retries", config.maxRetries, 0, 10);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rate Limiting", EditorStyles.miniBoldLabel);
                config.hasRateLimit = EditorGUILayout.Toggle("Has Rate Limit", config.hasRateLimit);
                
                if (config.hasRateLimit)
                {
                    config.requestsPerMinute = EditorGUILayout.IntSlider("Requests Per Minute", config.requestsPerMinute, 1, 1000);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        private void DrawActionsSection(ToolConfig config)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Preview MCP Format"))
            {
                Debug.Log("=== MCP Format Preview ===");
                Debug.Log(config.GetMCPFormat());
            }
            
            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfiguration(config);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void InitializeFunctionIfNull(ToolConfig config)
        {
            if (config.function == null)
            {
                config.function = new FunctionDefinition();
            }
            
            if (config.function.parameters == null)
            {
                config.function.parameters = new ParameterDefinition();
            }
            
            if (config.function.parameters.properties == null)
            {
                config.function.parameters.properties = new List<SerializableProperty>();
            }
        }
        
        private void DrawPropertiesEditor(ParameterDefinition parameters)
        {
            if (parameters.properties == null)
            {
                parameters.properties = new List<SerializableProperty>();
            }
            
            EditorGUILayout.LabelField("Properties:", EditorStyles.miniBoldLabel);
            
            List<int> indicesToRemove = new List<int>();
            
            for (int i = 0; i < parameters.properties.Count; i++)
            {
                SerializableProperty prop = parameters.properties[i];
                
                if (prop.value == null)
                {
                    prop.value = new PropertyDefinition();
                }
                
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                prop.key = EditorGUILayout.TextField("Key", prop.key);
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    indicesToRemove.Add(i);
                }
                
                EditorGUILayout.EndHorizontal();
                
                prop.value.type = EditorGUILayout.TextField("Type", prop.value.type);
                prop.value.description = EditorGUILayout.TextField("Description", prop.value.description);
                
                DrawEnumValues(prop.value);
                
                parameters.properties[i] = prop;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            {
                parameters.properties.RemoveAt(indicesToRemove[i]);
            }
            
            if (GUILayout.Button("Add Property"))
            {
                parameters.properties.Add(new SerializableProperty
                {
                    key = "newProperty",
                    value = new PropertyDefinition { type = "string" }
                });
            }
        }
        
        private void DrawEnumValues(PropertyDefinition property)
        {
            if (property.enumValues == null)
            {
                property.enumValues = new List<string>();
            }
            
            if (property.type == "string" && property.enumValues.Count > 0)
            {
                EditorGUILayout.LabelField("Enum Values:", EditorStyles.miniLabel);
                
                List<int> enumIndicesToRemove = new List<int>();
                
                for (int j = 0; j < property.enumValues.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    property.enumValues[j] = EditorGUILayout.TextField(property.enumValues[j]);
                    
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        enumIndicesToRemove.Add(j);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                for (int j = enumIndicesToRemove.Count - 1; j >= 0; j--)
                {
                    property.enumValues.RemoveAt(enumIndicesToRemove[j]);
                }
                
                if (GUILayout.Button("Add Enum Value", GUILayout.Height(18)))
                {
                    property.enumValues.Add("");
                }
            }
            else if (property.type == "string")
            {
                if (GUILayout.Button("Add Enum Values", GUILayout.Height(18)))
                {
                    property.enumValues.Add("");
                }
            }
        }
        
        private void DrawRequiredList(ParameterDefinition parameters)
        {
            if (parameters.required == null)
            {
                parameters.required = new List<string>();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Required Parameters:", EditorStyles.miniBoldLabel);
            
            List<int> requiredIndicesToRemove = new List<int>();
            
            for (int i = 0; i < parameters.required.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                parameters.required[i] = EditorGUILayout.TextField(parameters.required[i]);
                
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    requiredIndicesToRemove.Add(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            for (int i = requiredIndicesToRemove.Count - 1; i >= 0; i--)
            {
                parameters.required.RemoveAt(requiredIndicesToRemove[i]);
            }
            
            if (GUILayout.Button("Add Required Parameter"))
            {
                parameters.required.Add("");
            }
        }
        
        private void ValidateConfiguration(ToolConfig config)
        {
            List<string> errors = new List<string>();
            
            if (string.IsNullOrEmpty(config.toolId))
                errors.Add("Tool ID is required");
            
            if (string.IsNullOrEmpty(config.toolName))
                errors.Add("Tool Name is required");
            
            if (config.function == null)
                errors.Add("Function definition is required");
            else
            {
                if (string.IsNullOrEmpty(config.function.name))
                    errors.Add("Function name is required");
                
                if (string.IsNullOrEmpty(config.function.description))
                    errors.Add("Function description is required");
            }
            
            if (errors.Count == 0)
            {
                Debug.Log("✅ Configuration is valid!");
            }
            else
            {
                Debug.LogWarning("❌ Configuration has errors:\n" + string.Join("\n", errors));
            }
        }
    }
}