# Deliver

## Overview
This is a Unity-based 3D pizza delivery game where the player must deliver all pizzas within a time limit while avoiding hazards. The game features dynamic UI elements, including a countdown timer, pizza delivery checklist, death counter, and success/fail screens.

---

## Features
- **Delivery Gameplay:** Deliver pizzas to multiple targets in the scene by pressing **E** when close.  
- **Dynamic UI:**  
  - Countdown timer  
  - Pizza delivery count and checklist with live updates  
  - Death counter  
  - Success and fail panels with retry/main menu options  
- **Navigation Arrow:** Points in real-time to the closest undelivered pizza delivery target, relative to player camera.  
- **Automatic Updates:** Delivered pizzas disappear, arrow updates to next target, and win/fail conditions trigger automatically.  
- **Cursor Handling:** Cursor is locked during gameplay and unlocked on success/fail screens.  

---

## Controls
- **Movement:** WASD (default Unity FPS controls)  
- **Deliver Pizza:** Press **E** when near a delivery target  
- **Retry/Restart:** Click UI buttons on success/fail screens  

---

## How It Works
1. Player starts with all pizzas in inventory.  
2. The nearest undelivered pizza target is highlighted by an arrow on the UI.  
3. When the player delivers a pizza, the target disappears and UI updates.  
4. Game ends when all pizzas are delivered (win) or the timer runs out (fail).  
5. Success/Fail screens allow retrying or returning to the main menu.

This project is © 2025 Thom Kelly — play and submit improvements by issue or PR. Redistribution or claiming authorship is prohibited. See LICENSE.md.
