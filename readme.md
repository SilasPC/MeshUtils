
# Mesh Utilities

### Note
 - PartialPolySeperate only copies UVs

### Problems so far
 - Sorting siblings (?)
 - Time complexity in ring generation
 - Degenerate case non-planar cutting (parallel DirectionalProject)
 - Rounding errors non-planar cutting
 - Non-planar cutting holes in single triangle wont work (missing hiearchical analysis (?))

### Things fixed/added from old version
 - Boundary checks
 - UV's
 - Cutting gap
 - Builder patterns

### Todo
 - Check box collider fallback
 - Builder pattern for param
 - NonPlanar strip rings need hiearchical analysis
 - Finish/use IvSaver
 - Add normals to other algorithms
