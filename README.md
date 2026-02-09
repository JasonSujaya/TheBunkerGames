
## 🎮 How to Play

### Prerequisites
- Unity 2022.3 or later (check ProjectSettings for exact version).
- An API key for **Mistral** (get one from [Mistral AI](https://console.mistral.ai/)).
  - *Note: OpenRouter is NOT needed.*

### Setup Instructions

1.  **Clone the Repository**
    ```bash
    git clone https://github.com/JasonSujaya/TheBunkerGames.git
    ```

2.  **Open the Project**
    - Launch Unity Hub.
    - Click "Add" and select the cloned folder.
    - Open the project.

3.  **Configure AI Settings**
    - In the Project window, navigate to the root **Assets** folder.
    - Locate the `AIConfigSO` asset (file: `Assets/AIConfigSO.asset`).
    - Select it and in the Inspector, find the **API Key** field.
    - Paste your **Mistral API Key**.

4.  **Run the Game**
    - Open the **Game** scene located at:
      `Assets/GAME.unity`
    - Press the **Play** button in the Unity Editor.

---

## 📜 License

MIT License

Copyright (c) 2024 Jason Sujaya

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
