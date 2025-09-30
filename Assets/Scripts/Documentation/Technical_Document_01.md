# Documento Técnico #1 - Sistema de Chat con Agentes LLM
## Configuraciones ScriptableObject - Estado Inicial

**Versión:** 1.0  
**Fecha:** 2025-08-02  
**Estado:** Configuraciones Base Implementadas  

---

## 1. RESUMEN EJECUTIVO

Se ha completado la implementación de las configuraciones base del sistema de chat con agentes LLM utilizando Unity ScriptableObjects. La arquitectura sigue los principios de Model Context Protocol (MCP) y mantiene separación de responsabilidades mediante patrones SOLID.

### Estado Actual
- ✅ **Configuraciones ScriptableObject**: Completadas
- ⏳ **Modelos de Datos**: Pendiente
- ⏳ **Interfaces de Servicio**: Pendiente
- ⏳ **Implementación MVC**: Pendiente
- ⏳ **Servicios de Orquestación**: Pendiente

---

## 2. ARQUITECTURA IMPLEMENTADA

### 2.1 Estructura de Directorios
```
Assets/Scripts/
├── Configuration/
│   ├── ScriptableObjects/
│   │   ├── AgentConfig.cs
│   │   ├── ToolConfig.cs
│   │   ├── PromptConfig.cs
│   │   └── LoggingConfig.cs
│   └── ApplicationConfig.cs
├── Enums/
│   ├── ToolType.cs
│   └── LogLevel.cs
└── Models/
    └── Tools/
        ├── ToolSchema.cs
        ├── ParameterSchema.cs
        └── ToolAnnotations.cs
```

### 2.2 Principios Aplicados
- **SOLID**: Separación de responsabilidades por archivo
- **YAGNI**: Solo campos imprescindibles implementados
- **KISS**: Estructuras simples y autoexplicativas
- **MCP Compliance**: Nomenclatura y estructura siguiendo Model Context Protocol

---

## 3. COMPONENTES IMPLEMENTADOS

### 3.1 Enumeraciones Core

#### ToolType
```csharp
public enum ToolType
{
    UserManagement,    // Gestión de usuarios
    TravelSearch,      // Búsqueda de viajes
    TravelDetails,     // Detalles de viajes
    Custom            // Herramientas personalizadas
}
```

#### LogLevel
```csharp
public enum LogLevel
{
    Debug,     // Información de desarrollo
    Info,      // Información general
    Warning,   // Advertencias
    Error,     // Errores
    Critical   // Errores críticos
}
```

### 3.2 Estructuras MCP (POCO)

#### ToolSchema
Estructura que define el esquema de entrada para herramientas siguiendo MCP:
- **type**: Tipo de datos (object, string, etc.)
- **properties**: Diccionario de parámetros con sus esquemas
- **required**: Lista de parámetros obligatorios

#### ParameterSchema
Define cada parámetro individual de una herramienta:
- **type**: Tipo del parámetro
- **description**: Descripción del parámetro
- **enum**: Valores permitidos (opcional)
- **default**: Valor por defecto (opcional)

#### ToolAnnotations
Metadatos de herramientas siguiendo MCP:
- **title**: Título descriptivo
- **readOnlyHint**: Indica si es solo lectura
- **destructiveHint**: Indica si puede ser destructiva
- **idempotentHint**: Indica si es idempotente
- **openWorldHint**: Indica si acepta parámetros adicionales

### 3.3 ScriptableObjects de Configuración

#### ToolConfig
**Responsabilidad**: Configuración de herramientas individuales
**CreateAssetMenu**: "LLM/Tool Configuration"

**Campos Principales**:
- `toolId`: Identificador único
- `toolName`: Nombre de la herramienta
- `description`: Descripción funcional
- `toolType`: Tipo de herramienta (enum)
- `inputSchema`: Esquema de entrada MCP
- `annotations`: Metadatos MCP

#### AgentConfig
**Responsabilidad**: Configuración de agentes LLM
**CreateAssetMenu**: "LLM/Agent Configuration"

**Campos Principales**:
- `agentId`: Identificador único del agente
- `agentName`: Nombre descriptivo
- `token`: Token de autenticación
- `model`: Modelo LLM específico
- `serviceProvider`: Proveedor del servicio (OpenAI, etc.)
- `serviceUrl`: URL del endpoint
- `inputTokenCost`: Costo por token de entrada
- `outputTokenCost`: Costo por token de salida
- `toolConfigs`: Lista de herramientas disponibles

#### PromptConfig
**Responsabilidad**: Configuración de prompts y parámetros LLM
**CreateAssetMenu**: "LLM/Prompt Configuration"

**Campos Principales**:
- `promptId`: Identificador único
- `promptText`: Texto del prompt (TextArea 5-15 líneas)
- `temperature`: Creatividad del modelo (0-2)
- `maxTokens`: Máximo de tokens de respuesta
- `topP`: Nucleus sampling (0-1)
- `topK`: Top-K sampling (0-1)

#### LoggingConfig
**Responsabilidad**: Configuración del sistema de logging
**CreateAssetMenu**: "LLM/Logging Configuration"

**Campos Principales**:
- `minimumLogLevel`: Nivel mínimo de logging
- `enableAgentExecution`: Log de ejecución de agentes
- `enableToolCalls`: Log de llamadas a herramientas
- `enableMessageReception`: Log de recepción de mensajes
- `enablePromptConstruction`: Log de construcción de prompts
- `enableContextUpdates`: Log de actualizaciones de contexto
- `enablePerformanceMetrics`: Log de métricas de rendimiento

#### ApplicationConfig
**Responsabilidad**: Configuración centralizada de la aplicación
**CreateAssetMenu**: "LLM/Application Configuration"

**Campos Principales**:
- `agentConfigurations`: Lista de configuraciones de agentes
- `loggingConfiguration`: Referencia a configuración de logging
- `maxConversationHistory`: Máximo histórico de conversación
- `requestTimeoutSeconds`: Timeout para requests HTTP
- `maxRetryAttempts`: Máximo intentos de reintento

---

## 4. CARACTERÍSTICAS TÉCNICAS

### 4.1 Diseño de Acceso
- **Propiedades ReadOnly**: Todos los campos son accesibles via propiedades públicas de solo lectura
- **Serialización Unity**: Campos privados serializados con `[SerializeField]`
- **Encapsulación**: Acceso controlado a través de propiedades

### 4.2 Validaciones Unity Editor
- **Range Sliders**: Temperature (0-2), TopP/TopK (0-1)
- **TextArea**: Prompts con área expandida (5-15 líneas)
- **CreateAssetMenu**: Menús organizados bajo "LLM/"

### 4.3 Referencias Cruzadas
- **AgentConfig** → Referencias a `List<ToolConfig>`
- **ApplicationConfig** → Referencias a `List<AgentConfig>` y `LoggingConfig`
- **Desacoplamiento**: Configuraciones independientes conectables

---

## 5. CUMPLIMIENTO DE ESTÁNDARES

### 5.1 Model Context Protocol (MCP)
- ✅ **ToolSchema**: Estructura exacta MCP
- ✅ **ParameterSchema**: Parámetros según especificación
- ✅ **ToolAnnotations**: Metadatos MCP completos
- ✅ **Nomenclatura**: Nombres siguiendo convención MCP

### 5.2 Principios SOLID
- ✅ **Single Responsibility**: Cada ScriptableObject una responsabilidad
- ✅ **Open/Closed**: Extensible via nuevas configuraciones
- ✅ **Interface Segregation**: Preparado para interfaces específicas
- ✅ **Dependency Inversion**: Configuraciones inyectables

### 5.3 Clean Code
- ✅ **Sin Comentarios**: Código autoexplicativo
- ✅ **Tipado Estricto**: Todas las variables tipadas
- ✅ **No Hardcoding**: Todo configurable externamente
- ✅ **POCO Structures**: Estructuras de datos simples

---

## 6. DEPENDENCIAS Y REQUISITOS

### 6.1 Unity Dependencies
- **Unity 2021.3 LTS** o superior
- **Newtonsoft.Json** (para serialización MCP)
- **System.Collections.Generic** (listas y diccionarios)

### 6.2 Pendientes de Implementación
- **Modelos de Datos**: Message, ConversationContext, ToolCall
- **Interfaces de Servicio**: IContextManager, IAgentExecutor, IToolSet
- **Servicios de Orquestación**: ChatOrchestrator, LLMOrchestrator
- **Implementación MVC**: Views, Controllers, Models

---

## 7. TESTING Y VALIDACIÓN

### 7.1 Creación de Assets
```csharp
// Crear configuración de herramienta
ToolConfig userTool = CreateInstance<ToolConfig>();
// Configurar campos mediante inspector

// Crear configuración de agente
AgentConfig chatAgent = CreateInstance<AgentConfig>();
// Asignar herramientas y configurar
```

### 7.2 Validaciones Requeridas
- ⏳ **Validación de Schema**: Validar estructura MCP
- ⏳ **Validación de Referencias**: Verificar integridad de referencias
- ⏳ **Testing de Serialización**: Probar serialización/deserialización

---

## 8. PRÓXIMOS PASOS

### 8.1 Prioridad Alta
1. **Modelos de Datos Core**: Message, ConversationContext
2. **Interfaces de Servicio**: Contratos para servicios
3. **Context Manager**: Gestión de contexto de conversación

### 8.2 Prioridad Media
1. **Agent Executor**: Implementación de ejecución de agentes
2. **Tool Sets**: UserToolSet y TravelToolSet
3. **MVC Controllers**: ChatController implementación

### 8.3 Prioridad Baja
1. **Logging Service**: Servicio de logging implementación
2. **Persistence Service**: Persistencia de conversaciones
3. **UI Views**: Implementación de vistas Unity

---

## 9. CONSIDERACIONES DE ARQUITECTURA

### 9.1 Escalabilidad
- **Configuraciones Modulares**: Fácil adición de nuevos agentes/herramientas
- **Referencias Desacopladas**: Configuraciones independientes
- **Extensibilidad**: Estructura preparada para crecimiento

### 9.2 Mantenibilidad
- **Separación Clara**: Cada responsabilidad en archivo independiente
- **Configuración Externa**: Sin hardcoding en código
- **Documentación**: Estructura autoexplicativa

### 9.3 Performance
- **ScriptableObjects**: Carga bajo demanda
- **Referencias**: Evita duplicación de datos
- **Serialización Eficiente**: Estructuras optimizadas

---

**Fin del Documento Técnico #1**

*Documento generado automáticamente por el sistema de desarrollo*
*Próxima actualización: Implementación de Modelos de Datos Core*
