# Mockery

## Setup

### Requirements

- **Unity 6000.3.11f1** — install via [Unity Hub](https://unity.com/download). Use the exact version to avoid compatibility issues. You can find it under *Installs > Add > Archive* in Unity Hub.

### Steps

1. Clone the repository:
   ```sh
   git clone <repo-url>
   cd DCJam
   ```

2. Open **Unity Hub**, click **Open**, and select the cloned `DCJam` folder.

3. Unity will automatically restore all packages listed in `Packages/manifest.json` on first open — no manual installation needed.

4. Once the editor finishes importing, open the main scene from the **Project** window and press **Play**.

4. Configure the Unity smart merge driver so Git can resolve scene/prefab conflicts automatically:
   ```sh
   git config merge.unityyamlmerge.name "Unity SmartMerge"
   git config merge.unityyamlmerge.driver "'/home/<your-username>/Unity/Hub/Editor/6000.3.11f1/Editor/Data/Tools/UnityYAMLMerge' merge -p %O %B %A %D"
   git config merge.unityyamlmerge.recursive binary
   ```
   Replace `<your-username>` with your system username. On Windows the path will look like:
   `'C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data/Tools/UnityYAMLMerge.exe'`

### Notes

- The `Library/`, `Temp/`, and `Logs/` folders are not tracked in git and will be regenerated automatically by Unity.
- The project uses the **Universal Render Pipeline (URP)** — make sure you do not switch the render pipeline in Project Settings.
