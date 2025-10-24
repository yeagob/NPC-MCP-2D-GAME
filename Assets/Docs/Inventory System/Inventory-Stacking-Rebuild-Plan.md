# Inventory Stacking System - Rebuild Plan

## Executive Summary

**Objetivo**: Implementar un sistema de stacking para el inventario que permita acumular múltiples unidades de cada tipo de ítem en lugar del sistema actual binario (tiene/no tiene).

**Complejidad**: Media
**Impacto**: Alto (afecta componentes, tools, UI, y contexto LLM)
**Tiempo estimado**: 5 horas

---

## 1. Análisis del Sistema Actual

### 1.1. Arquitectura Existente

**InventoryComponent** (Assets/Scripts/Components/InventoryComponent.cs)
- 3 slots fijos: `_keySlot`, `_moneySlot`, `_appleSlot`
- Cada slot almacena un `InventoryItem` (struct)
- Lógica binaria: `HasItem()` retorna true/false
- `AddItem()` falla si ya existe un ítem de ese tipo (línea 46-49)
- `RemoveItem()` elimina completamente el slot

**InventoryItem** (Assets/Scripts/Models/Inventory/InventoryItem.cs)
- POCO struct con 3 campos:
  - `itemId`: string (identificador único del objeto en el mapa)
  - `itemElement`: ItemElement (referencia al prefab/elemento)
  - `itemType`: ItemType (Key/Money/Apple)
- Sin campo de cantidad

**InventoryToolSet** (Assets/Scripts/Services/Tools/InventoryToolSet.cs)
- 3 tools: `pickup_item`, `drop_item`, `give_item`
- `pickup_item`: Solo funciona si no tiene el ítem (línea 117-120)
- `drop_item`: Elimina completamente el ítem del inventario
- `give_item`: Solo funciona si el receptor no tiene el ítem

**UI System**
- Referencias a GameObjects: `_appleUI`, `_moneyUI`, `_keyUI`
- Solo muestra/oculta el ícono (activo/inactivo)
- No hay display de cantidades

### 1.2. Limitaciones Actuales

❌ No se pueden recoger múltiples ítems del mismo tipo
❌ No hay concepto de cantidad/stack
❌ Desperdiciar espacio de inventario (solo 1 manzana aunque haya 10 en el mapa)
❌ Imposibilidad de dar cantidades específicas (siempre todo o nada)
❌ UI no refleja cuántos ítems se tienen

---

## 2. Diseño Propuesto

### 2.1. Cambios en Data Models

#### InventoryItem (POCO)
```csharp
// Assets/Scripts/Models/Inventory/InventoryItem.cs
[Serializable]
public struct InventoryItem
{
    public ItemType itemType;
    public int quantity;              // NUEVO: cantidad stackeada
    public List<string> itemIds;      // NUEVO: ids de ítems stackeados (para drop individual)

    public InventoryItem(ItemType itemType, int quantity, List<string> itemIds)
    {
        this.itemType = itemType;
        this.quantity = quantity;
        this.itemIds = itemIds ?? new List<string>();
    }

    public static InventoryItem Empty()
    {
        return new InventoryItem(ItemType.Key, 0, new List<string>());
    }

    public bool IsValid()
    {
        return quantity > 0;
    }

    public bool CanAddMore(int maxStackSize)
    {
        return quantity < maxStackSize;
    }
}
```

**Justificación de Diseño**:
- **quantity**: Campo esencial para stacking
- **itemIds**: Lista de IDs permite trackear objetos individuales para lógica de drop/debug
- **Eliminación de itemElement**: No necesitamos referencia al elemento cuando es un stack (se pierde la referencia individual)

#### InventoryConfiguration (ScriptableObject)
```csharp
// Assets/Scripts/Configuration/InventoryConfiguration.cs
[CreateAssetMenu(fileName = "InventoryConfiguration", menuName = "Game/Configuration/Inventory")]
public class InventoryConfiguration : ScriptableObject
{
    [Header("Stack Limits")]
    [SerializeField] private int keyMaxStack = 5;
    [SerializeField] private int moneyMaxStack = 999;
    [SerializeField] private int appleMaxStack = 20;

    [Header("UI Settings")]
    [SerializeField] private bool showQuantityAlways = true;
    [SerializeField] private string quantityFormat = "x{0}";

    public int GetMaxStackSize(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Key => keyMaxStack,
            ItemType.Money => moneyMaxStack,
            ItemType.Apple => appleMaxStack,
            _ => 1
        };
    }
}
```

**Rationale**: Separar configuración de lógica (SOLID principles)

### 2.2. Cambios en Components

#### InventoryComponent - Métodos Modificados

**AddItem** (nuevo signature):
```csharp
public AddItemResult AddItem(string itemId, ItemType itemType, int quantity = 1)
{
    InventoryItem slot = GetSlot(itemType);
    int maxStack = _configuration.GetMaxStackSize(itemType);

    if (!slot.IsValid())
    {
        // Slot vacío - crear nuevo stack
        List<string> ids = new List<string> { itemId };
        SetSlot(itemType, new InventoryItem(itemType, quantity, ids));
        UpdateUIState(itemType);
        return new AddItemResult(true, quantity, 0);
    }

    if (!slot.CanAddMore(maxStack))
    {
        return new AddItemResult(false, 0, slot.quantity, "Stack full");
    }

    int spaceAvailable = maxStack - slot.quantity;
    int actualAdded = Mathf.Min(quantity, spaceAvailable);
    int overflow = quantity - actualAdded;

    slot.quantity += actualAdded;
    slot.itemIds.Add(itemId);
    SetSlot(itemType, slot);
    UpdateUIState(itemType);

    return new AddItemResult(true, actualAdded, overflow);
}
```

**RemoveItem** (nuevo signature):
```csharp
public RemoveItemResult RemoveItem(ItemType itemType, int quantity = 1)
{
    InventoryItem slot = GetSlot(itemType);

    if (!slot.IsValid())
    {
        return new RemoveItemResult(false, 0, null);
    }

    int actualRemoved = Mathf.Min(quantity, slot.quantity);
    List<string> removedIds = new List<string>();

    for (int i = 0; i < actualRemoved; i++)
    {
        if (slot.itemIds.Count > 0)
        {
            removedIds.Add(slot.itemIds[0]);
            slot.itemIds.RemoveAt(0);
        }
    }

    slot.quantity -= actualRemoved;

    if (slot.quantity <= 0)
    {
        SetSlot(itemType, InventoryItem.Empty());
    }
    else
    {
        SetSlot(itemType, slot);
    }

    UpdateUIState(itemType);
    return new RemoveItemResult(true, actualRemoved, removedIds);
}
```

**GetInventoryDescription** (modificado):
```csharp
public string GetInventoryDescription()
{
    List<string> items = new List<string>();

    if (_keySlot.IsValid())
        items.Add($"Key x{_keySlot.quantity}");
    if (_moneySlot.IsValid())
        items.Add($"Money x{_moneySlot.quantity}");
    if (_appleSlot.IsValid())
        items.Add($"Apple x{_appleSlot.quantity}");

    return items.Count == 0 ? "Empty inventory" : string.Join(", ", items);
}
```

#### Nuevos Result POCOs

```csharp
// Assets/Scripts/Models/Inventory/AddItemResult.cs
public struct AddItemResult
{
    public bool success;
    public int quantityAdded;
    public int overflow;
    public string message;

    public AddItemResult(bool success, int quantityAdded, int overflow, string message = "")
    {
        this.success = success;
        this.quantityAdded = quantityAdded;
        this.overflow = overflow;
        this.message = message;
    }
}

// Assets/Scripts/Models/Inventory/RemoveItemResult.cs
public struct RemoveItemResult
{
    public bool success;
    public int quantityRemoved;
    public List<string> removedItemIds;

    public RemoveItemResult(bool success, int quantityRemoved, List<string> removedItemIds)
    {
        this.success = success;
        this.quantityRemoved = quantityRemoved;
        this.removedItemIds = removedItemIds;
    }
}
```

### 2.3. Cambios en Tools (MCP)

#### InventoryToolSet - Tool Signatures

**pickup_item** (modificado):
```json
{
  "name": "pickup_item",
  "description": "Pick up an item from the ground and add it to inventory",
  "parameters": {
    "type": "object",
    "properties": {
      "itemId": {
        "type": "string",
        "description": "The unique ID of the item to pick up"
      },
      "quantity": {
        "type": "integer",
        "description": "Number of items to pick up (default: 1)",
        "default": 1
      }
    },
    "required": ["itemId"]
  }
}
```

**drop_item** (modificado):
```json
{
  "name": "drop_item",
  "description": "Drop items from inventory onto the ground",
  "parameters": {
    "type": "object",
    "properties": {
      "itemType": {
        "type": "string",
        "enum": ["Key", "Money", "Apple"],
        "description": "Type of item to drop"
      },
      "quantity": {
        "type": "integer",
        "description": "Number of items to drop (default: 1)",
        "default": 1
      }
    },
    "required": ["itemType"]
  }
}
```

**give_item** (modificado):
```json
{
  "name": "give_item",
  "description": "Transfer items from your inventory to another character",
  "parameters": {
    "type": "object",
    "properties": {
      "targetCharacterId": {
        "type": "string",
        "description": "ID of the character to give items to"
      },
      "itemType": {
        "type": "string",
        "enum": ["Key", "Money", "Apple"]
      },
      "quantity": {
        "type": "integer",
        "description": "Number of items to transfer (default: 1)",
        "default": 1
      }
    },
    "required": ["targetCharacterId", "itemType"]
  }
}
```

#### InventoryToolSet - Implementation Changes

**ExecutePickupItemAsync** (key changes):
```csharp
private async Task<ToolResponse> ExecutePickupItemAsync(ToolCall toolCall)
{
    // ... validaciones de elemento y distancia ...

    int quantity = 1;
    if (args.ContainsKey("quantity"))
    {
        quantity = Convert.ToInt32(args["quantity"]);
    }

    AddItemResult result = _inventoryComponent.AddItem(itemId, itemType, quantity);

    if (result.success)
    {
        _mapSystem.UnregisterElement(targetElement);
        targetElement.gameObject.SetActive(false);

        string message = result.overflow > 0
            ? $"Picked up {result.quantityAdded} {itemType} (id: {itemId}). {result.overflow} didn't fit."
            : $"Picked up {result.quantityAdded} {itemType} (id: {itemId})";

        UniversalLogUI.Instance.Log($"{_characterAgent.name} {message}");
        return CreateSuccessResponse(toolCall.id, message);
    }

    return CreateErrorResponse(toolCall.id, result.message);
}
```

**ExecuteGiveItemAsync** (key changes):
```csharp
private async Task<ToolResponse> ExecuteGiveItemAsync(ToolCall toolCall)
{
    // ... validaciones ...

    int quantity = 1;
    if (args.ContainsKey("quantity"))
    {
        quantity = Convert.ToInt32(args["quantity"]);
    }

    RemoveItemResult removeResult = _inventoryComponent.RemoveItem(itemType, quantity);

    if (!removeResult.success)
    {
        return CreateErrorResponse(toolCall.id, $"No {itemType} in inventory");
    }

    AddItemResult addResult = targetInventory.AddItem(removeResult.removedItemIds[0], itemType, removeResult.quantityRemoved);

    if (!addResult.success)
    {
        // Rollback
        _inventoryComponent.AddItem(removeResult.removedItemIds[0], itemType, removeResult.quantityRemoved);
        return CreateErrorResponse(toolCall.id, $"Target inventory full. {addResult.message}");
    }

    string message = $"Gave {removeResult.quantityRemoved} {itemType} to {targetCharacter.name}";
    if (addResult.overflow > 0)
    {
        message += $". {addResult.overflow} didn't fit.";
    }

    return CreateSuccessResponse(toolCall.id, message);
}
```

### 2.4. Cambios en UI

**Requirements**:
- Añadir TextMeshProUGUI component a cada slot UI
- Mostrar "x[quantity]" cuando quantity > 1
- Ocultar texto cuando quantity == 0 (slot vacío)
- Posicionar texto en esquina inferior derecha del ícono

**InventoryComponent - UI Updates**:
```csharp
[Header("UI References")]
[SerializeField] private GameObject _appleUI;
[SerializeField] private GameObject _moneyUI;
[SerializeField] private GameObject _keyUI;

[SerializeField] private TMPro.TextMeshProUGUI _appleQuantityText;
[SerializeField] private TMPro.TextMeshProUGUI _moneyQuantityText;
[SerializeField] private TMPro.TextMeshProUGUI _keyQuantityText;

private void UpdateUIState(ItemType itemType)
{
    InventoryItem slot = GetSlot(itemType);
    GameObject uiObject = GetUIObject(itemType);
    TMPro.TextMeshProUGUI quantityText = GetQuantityText(itemType);

    bool hasItem = slot.IsValid();

    if (uiObject != null)
    {
        uiObject.SetActive(hasItem);
    }

    if (quantityText != null)
    {
        if (hasItem)
        {
            quantityText.text = _configuration.showQuantityAlways || slot.quantity > 1
                ? string.Format(_configuration.quantityFormat, slot.quantity)
                : "";
        }
        else
        {
            quantityText.text = "";
        }
    }
}
```

### 2.5. Cambios en LLM Context

**CharacterAgent - Inventory Context Prompt**:
```csharp
private PromptConfig BuildInventoryPrompt()
{
    InventoryComponent inventory = GetComponent<InventoryComponent>();
    string inventoryDescription = inventory.GetInventoryDescription();

    string content = $@"CURRENT INVENTORY:
{inventoryDescription}

STACKING RULES:
- Keys: Max {_inventoryConfiguration.GetMaxStackSize(ItemType.Key)} per stack
- Money: Max {_inventoryConfiguration.GetMaxStackSize(ItemType.Money)} per stack
- Apples: Max {_inventoryConfiguration.GetMaxStackSize(ItemType.Apple)} per stack

Use pickup_item, drop_item, give_item with 'quantity' parameter to manage stacks.";

    return new PromptConfig("inventory-status", content, PromptType.Context);
}
```

---

## 3. Plan de Implementación

### Phase 1: Data Models & Configuration
**Archivos a crear:**
1. `Assets/Scripts/Configuration/InventoryConfiguration.cs` (nuevo)
2. `Assets/Scripts/Models/Inventory/AddItemResult.cs` (nuevo)
3. `Assets/Scripts/Models/Inventory/RemoveItemResult.cs` (nuevo)

**Archivos a modificar:**
1. `Assets/Scripts/Models/Inventory/InventoryItem.cs` (refactor struct)

**Tareas:**
- [ ] Crear InventoryConfiguration ScriptableObject
- [ ] Crear POCOs de resultado (AddItemResult, RemoveItemResult)
- [ ] Modificar InventoryItem struct (añadir quantity y itemIds)
- [ ] Crear asset de configuración en `Assets/Configuration/InventoryConfiguration.asset`
- [ ] Configurar stack limits: Key=5, Money=999, Apple=20

**Tiempo estimado:** 45 minutos

### Phase 2: Component Logic
**Archivos a modificar:**
1. `Assets/Scripts/Components/InventoryComponent.cs`

**Tareas:**
- [ ] Añadir referencia a InventoryConfiguration
- [ ] Refactorizar AddItem() con stacking logic
- [ ] Refactorizar RemoveItem() con quantity parameter
- [ ] Modificar GetInventoryDescription() para mostrar cantidades
- [ ] Actualizar HasItem() (check quantity > 0)
- [ ] Añadir método GetItemQuantity(ItemType)

**Tiempo estimado:** 90 minutos

### Phase 3: Tool Integration
**Archivos a modificar:**
1. `Assets/Scripts/Services/Tools/InventoryToolSet.cs`
2. `Assets/Agents/ToolSets/-2DGameTools-/Inventory/PickupToolConfig.asset`
3. `Assets/Agents/ToolSets/-2DGameTools-/Inventory/DropToolConfig.asset`
4. `Assets/Agents/ToolSets/-2DGameTools-/Inventory/GiveToolConfig.asset`

**Tareas:**
- [ ] Modificar ExecutePickupItemAsync (añadir quantity handling)
- [ ] Modificar ExecuteDropItemAsync (añadir quantity parameter)
- [ ] Modificar ExecuteGiveItemAsync (añadir quantity + overflow logic)
- [ ] Actualizar FunctionDefinitions en ToolConfigs (añadir quantity param)

**Tiempo estimado:** 90 minutos

### Phase 4: UI Updates
**Archivos a modificar:**
1. `Assets/Scripts/Components/InventoryComponent.cs` (UI methods)
2. `Assets/Scenes/NpcMcp2DGame.unity` (scene setup)

**Tareas:**
- [ ] Añadir TextMeshProUGUI components en Canvas
- [ ] Conectar referencias de textos en InventoryComponent
- [ ] Añadir métodos GetQuantityText()
- [ ] Modificar UpdateUIState() para actualizar cantidades
- [ ] Styling de textos (font size, color, posición)

**Tiempo estimado:** 60 minutos

### Phase 5: LLM Context
**Archivos a modificar:**
1. `Assets/Scripts/Services/Agents/CharacterAgent.cs`
2. `Assets/Agents/Prompts/-2DGamePrompts-/InventoryContextPromptConfig.asset` (si existe)

**Tareas:**
- [ ] Actualizar BuildInventoryPrompt() con stack info
- [ ] Añadir stack limits al contexto LLM

**Tiempo estimado:** 30 minutos

---

## 4. Impacto y Riesgos

### 4.1. Breaking Changes

⚠️ **InventoryItem struct**: Cambio de firma incompatible con saves existentes
- **Mitigación**: Este juego no tiene sistema de guardado, no aplica

⚠️ **InventoryComponent API**: AddItem/RemoveItem cambian signatures
- **Mitigación**: Solo usado por InventoryToolSet, fácil de actualizar

⚠️ **Tool Definitions**: Añadir parámetro "quantity" a tools
- **Mitigación**: Parámetro opcional (default=1), backward compatible

### 4.2. Riesgos Técnicos

**🟡 Medium Risk: LLM Adoption**
- LLM podría no usar quantity parameter inicialmente
- **Mitigación**: Añadir ejemplos en system prompts

**🟢 Low Risk: UI Performance**
- TextMeshPro updates por frame
- **Mitigación**: Solo actualizar cuando inventory cambia (event-driven)

**🟢 Low Risk: Serialization**
- List<string> en struct puede causar problemas en Unity
- **Mitigación**: Usar class en lugar de struct si es necesario

### 4.3. Oportunidades

✅ Sistema de economía más robusto (dinero stackeable)
✅ NPCs pueden acumular recursos (estrategia)
✅ Foundation para trading system futuro
✅ Mejor UX (UI muestra cantidades claras)

---

## 5. Future Enhancements (Out of Scope)

1. **Variable Stack Sizes**: Items en el mapa con cantidades variables
2. **Weight System**: Stack limits basados en peso total
3. **Item Durability**: Stackear solo ítems con misma durabilidad
4. **Inventory Expansion**: Más slots con ScriptableObject config
5. **Stack Splitting**: Dividir stacks en cantidades específicas

---

## 6. Configuration Reference

### InventoryConfiguration.asset
```
keyMaxStack: 5
moneyMaxStack: 999
appleMaxStack: 20
showQuantityAlways: false (solo muestra cuando >1)
quantityFormat: "x{0}"
```

### Tool Configs
- `PickupToolConfig.asset`: Añadir "quantity" parameter (integer, default 1)
- `DropToolConfig.asset`: Añadir "quantity" parameter (integer, default 1)
- `GiveToolConfig.asset`: Añadir "quantity" parameter (integer, default 1)

---

## 7. Success Criteria

✅ Player puede recoger múltiples ítems del mismo tipo
✅ UI muestra cantidades correctamente
✅ NPCs usan quantity parameter en tools
✅ Stack limits se respetan (overflow detectado)
✅ LLM context incluye información de cantidades
✅ No regression en funcionalidad existente

---

## Conclusión

Este rebuild transforma el inventario de un sistema binario (tiene/no tiene) a un sistema de stacking robusto. El diseño mantiene los principios SOLID, usa configuración data-driven, y es extensible para futuras features como trading o crafting.

**Esfuerzo Total Estimado**: 5 horas
**Complejidad**: Media
**Prioridad**: Alta (foundational feature)

---

**Documento creado**: 2025-10-20
**Autor**: Claude Code
**Versión**: 1.0
