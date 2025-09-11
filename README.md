# Resettables

Unity editor utility for resetting `ScriptableObject` state after changes made during editor play mode, mimicking scene object behavior.

## Why is this useful?

Unlike scene `MonoBehaviour` objects, `ScriptableObject` instances are assets which persist changes made during editor play mode. This makes them less useful as stateful runtime data containers such as entities, singleton managers, or shared variables.

By aligning their behavior with scene objects, they can serve as stateful singletons shared across scenes and referenced through the inspector, simplifying cross-object communication.

## Requirements

- Unity 2019.2+

## Install

### Remote (2019.3+)

1. In the Unity Editor, click **Window** → **Package Manager**.
2. Click **+** (top left) → **Install package from git URL…**.
3. Enter this repository URL with the `.git` suffix:
	```
	https://github.com/darksailstudio/resettables.git
	```

### Local (2019.2+)

1. Download the [latest release](https://github.com/darksailstudio/resettables/releases/latest), or clone this repository.
2. In the Unity Editor, click **Window** → **Package Manager**.
3. Click **+** (top left) → **Install package from disk…**.
4. Select the `package.json` file.

## Example

Create a new `ScriptableObject` derived class and mark it with `ResetOnExitPlayMode` attribute:

```cs
using DarkSail.Resettables;
using UnityEngine;

[ResetOnExitPlayMode]
[CreateAssetMenu(menuName = "My Project/My Entity")]
class MyEntity : ScriptableObject
{
	public int Value;
}
```

All derived classes will inherit the resetting behavior unless explicitly disabled:

```cs
using DarkSail.Resettables;
using UnityEngine;

[ResetOnExitPlayMode(Inherit = false)]
class Resettable : ScriptableObject
{
	public int Value;
}
```

```cs
class NotResettable : Resettable { }
```

## License

[MIT](LICENSE.md)
