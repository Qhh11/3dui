# 3DUI · Mobile AR Puzzle Game

A mobile augmented reality puzzle game built with Unity and C#. The project explores spatial interaction and puzzle solving through a handheld AR interface.

## Project highlights

- Mobile AR using AR Foundation and ARCore.
- Touch controls and interactive 3D objects.
- Quest objectives, wayfinding, and in-game UI.
- Universal Render Pipeline (URP) rendering.

## Open the project

1. Install **Unity 6000.4.1f1**, matching `ProjectSettings/ProjectVersion.txt`, with Android Build Support.
2. Add the repository root to Unity Hub and open it.
3. Let Unity restore the dependencies listed in `Packages/manifest.json`. Some packages require Git and network access.
4. Open `Assets/Scenes/Main.unity`.
5. Select Android in Build Profiles, check the enabled scenes, and build to an ARCore-compatible Android device.

The repository also includes experimental scenes. Device-based AR behavior has not been verified as part of this GitHub migration.

## Repository layout

| Directory | Contents |
| --- | --- |
| `Assets/Scripts/` | Gameplay, interactions, quests, and UI |
| `Assets/Scenes/` | Main and experimental scenes |
| `Packages/` | Unity dependencies and embedded packages |
| `ProjectSettings/` | Editor, rendering, and build configuration |

## Credits

Originally developed in the [LRZ GitLab repository](https://gitlab.lrz.de/00000000014B3F3B/3dui). Original contributor attribution is retained in Git history. Third-party packages and assets retain their respective licenses.
