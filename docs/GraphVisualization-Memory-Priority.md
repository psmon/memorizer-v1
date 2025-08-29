# Graph Visualization Memory Priority Update

## Date: 2025-08-27

## Changes Summary

### 1. Memory Node Prioritization ✅
- Changed query strategy from mixed (Memory OR Word) to Memory-first approach
- Guarantees Memory nodes are always shown when available
- Word nodes only included if connected to displayed Memory nodes

### 2. Sort by Recent Option ✅
- Added checkbox in UI to toggle between sorting options
- Default: Sort by recent (checked)
- Alternative: Sort by title (alphabetical)

### 3. Word Node Size Improvement ✅
- Increased Word node size range from 5-20px to 8-16px
- Better visibility while maintaining visual hierarchy
- Memory nodes remain larger (10-20px)

### 4. Default Limit Reduction ✅
- Changed default limit from 100 to 20 nodes
- Improves performance and initial visualization clarity

## Implementation Details

### Frontend Changes

#### Views/Graph/Index.cshtml
1. **Added Sort Control**:
```html
<div class="form-check form-check-inline">
    <input class="form-check-input" type="checkbox" id="sortByRecent" value="true" checked>
    <label class="form-check-label" for="sortByRecent">
        Sort by Recent
    </label>
</div>
```

2. **Updated loadGraphData Function**:
```javascript
function loadGraphData() {
    const limit = $('#nodeLimit').val();
    const sortByRecent = $('#sortByRecent').is(':checked');
    
    $.ajax({
        url: '/api/graph/visualization',
        data: { limit: limit, sortByRecent: sortByRecent },
        // ...
    });
}
```

3. **Added Event Handler**:
```javascript
$('#sortByRecent').change(function() {
    loadGraphData();
});
```

### Backend Changes

#### GraphSyncService.cs
1. **Updated Interface**:
```csharp
Task<GraphVisualizationData> GetGraphVisualizationAsync(int limit = 20, bool sortByRecent = true);
```

2. **Modified Query Logic**:
```csharp
// Dynamic ORDER BY clause based on sortByRecent parameter
var orderBy = sortByRecent ? "ORDER BY m.createdAt DESC" : "ORDER BY m.title ASC";
var query = $@"
    MATCH (m:Memory)
    RETURN m
    {orderBy}
    LIMIT $limit";
```

3. **Two-Step Query Process**:
   - Step 1: Get Memory nodes (with sorting option)
   - Step 2: Get Word nodes connected to selected Memory nodes

#### GraphController.cs
1. **Updated API Endpoint**:
```csharp
[HttpGet("visualization")]
public async Task<IActionResult> GetVisualization(
    [FromQuery] int limit = 20, 
    [FromQuery] bool sortByRecent = true)
{
    var data = await _graphSyncService.GetGraphVisualizationAsync(limit, sortByRecent);
    return Ok(data);
}
```

## Query Comparison

### Before (Mixed Query)
```cypher
MATCH (n)
WHERE n:Memory OR n:Word
RETURN n
ORDER BY n.createdAt DESC
LIMIT 100
```
**Issues**: Could return mostly Words, no guaranteed Memory nodes

### After (Memory-First Query)
```cypher
-- Step 1: Get Memory nodes
MATCH (m:Memory)
RETURN m
ORDER BY m.createdAt DESC  -- or ORDER BY m.title ASC
LIMIT 20

-- Step 2: Get connected Words
MATCH (m:Memory)-[r:HAS_KEYWORD]->(w:Word)
WHERE m.id IN $memoryIds
RETURN DISTINCT w
```
**Benefits**: Always shows Memory nodes with their relationships

## UI Features

| Control | Default | Purpose |
|---------|---------|---------|
| Show Word Nodes | Unchecked | Toggle Word node visibility |
| Node Limit | 20 | Control number of Memory nodes |
| Sort by Recent | Checked | Toggle between recent/alphabetical |

## API Parameters

**GET /api/graph/visualization**
- `limit`: Number of Memory nodes to retrieve (default: 20)
- `sortByRecent`: Sort by creation date (true) or title (false) (default: true)

## Benefits

1. **Guaranteed Memory Nodes**: Always shows Memory content when available
2. **Flexible Sorting**: Users can choose between recent or alphabetical ordering
3. **Better Performance**: Reduced default limit improves rendering speed
4. **Improved Visibility**: Word nodes are more visible with increased size
5. **Meaningful Connections**: Only shows Words connected to displayed Memories

## Testing Checklist

- [ ] Verify Memory nodes always appear first
- [ ] Test sort by recent (default)
- [ ] Test sort by title (alphabetical)
- [ ] Confirm Word node size is appropriate (8-16px)
- [ ] Check limit parameter works correctly
- [ ] Verify Word nodes are only shown when connected to displayed Memories
- [ ] Test performance with various limit values

## Files Modified

1. `/src/Memorizer/Views/Graph/Index.cshtml` - UI controls and JavaScript
2. `/src/Memorizer/Services/GraphSyncService.cs` - Query logic and sorting
3. `/src/Memorizer/Controllers/GraphController.cs` - API endpoint parameters

## Migration Notes

- No breaking changes to existing API
- Default behavior improved (Memory-first, recent sort)
- All existing parameters remain backward compatible