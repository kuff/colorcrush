# Colorcrush

Colorcrush is a Unity-based game where players interact with emojis to create a colorful mural.

## Project Structure

The project is organized into several key components:

- `Assets/Scripts/Colorcrush/Game/`: Contains the main game logic scripts.
- `Assets/Resources/Colorcrush/`: Stores game resources like emojis and mural images.
- `Assets/Scenes/`: Includes the main game scenes.

## Key Features

1. Emoji Grid: Players interact with a 3x3 grid of emojis.
2. Color Filtering: Emojis are filtered based on color selection.
3. Mural Creation: Filtered emojis contribute to building a colorful mural.

## Main Components

### ButtonController

The `ButtonController` script manages the emoji grid interaction. It handles:

- Emoji loading and shuffling
- Button click events
- Color updates
- Progress tracking

For more details, see: csharp:Assets/Scripts/Colorcrush/Game/ButtonController.cs

### EmojiMaterialLoader

This script is responsible for loading and managing emoji materials: csharp:Assets/Scripts/Colorcrush/Game/EmojiMaterialLoader.cs


## Setup and Configuration

1. Ensure all required assets are in the `Assets/Resources/Colorcrush/` directory.
2. Configure the `ProjectConfig` scriptable object with desired game settings.
3. Set up the main game scene with the necessary UI elements and scripts.

## Building and Running

1. Open the project in Unity (version X.X.X or later).
2. Ensure all scenes are added to the build settings.
3. Build for your target platform (iOS, Android, etc.).

## Attributions

### Art

- Mural: <a href="https://www.vecteezy.com/free-vector/colorful-parrot">Colorful Parrot Vectors by Vecteezy</a>
- Emojis: <a href="https://www.reshot.com/free-svg-icons/emoji/">Reshot.com</a>

### Third-Party Assets

- TextMesh Pro: Used for text rendering. Included in Unity.

## License

This project is MIT licensed.
