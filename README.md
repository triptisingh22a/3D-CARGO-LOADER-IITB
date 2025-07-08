# 🚛 3D Cargo Loader - Smart Visualization & Interactivity (Unity3D)

This Unity3D project presents a **3D interactive visualization** system for smart cargo loading, originally developed as part of a research internship under **IIT Bombay's Industrial Engineering and Operations Research (IEOR)** department. It focuses on **constraint-aware packing**, **real-time interactivity**, and **modular UI components**, laying the groundwork for future integration with **LLM-based optimization and user-guided loading tools**.

---

## 🔍 Overview

Manual cargo planning often lacks visual feedback, optimization flexibility, and adaptability to complex constraints like:
- Fragility
- Weight balancing
- Volume utilization
- Human decision support

This 3D module solves that by enabling:
- **Interactive placement** of cargo boxes inside a truck/container
- **UI-driven scenario simulation**
- **Constraint-aware packing visualizations**

---

## ✨ Features

- 🧱 **Realistic 3D Loader Environment**
  - Accurate box placement inside container space
  - Ground plane and cargo collision handling

- 🧩 **Custom UI Panels**
  - Box creation interface (size, weight, type)
  - Real-time cargo list display
  - Constraint toggles (e.g., stacking limits, spacing)

- ⚙️ **Interactive Manipulation**
  - Add/move/delete cargo in 3D view
  - Mouse-based drag + keyboard controls
  - Snap-to-grid for clean packing alignment

- 📦 **Predefined and Dynamic Packing Modes**
  - Demonstrates sample packing logic
  - Ready to extend for automated LLM-guided decisions

---

## 🧪 Demo Preview

| UI Panel Input | Real-Time Cargo Layout |
|----------------|------------------------|
| ![Input Panel](![WhatsApp Image 2025-05-26 at 15 59 19_4cc44523](https://github.com/user-attachments/assets/41a0fe55-0427-430a-8919-4be5d70e5d51)
) 

>  interactive demo preview:
https://drive.google.com/file/d/12vzPqpxCG3VAHr-lfWeAjGJ9R36QdpoZ/view?usp=drive_link

---

## 🛠️ Tech Stack

- **Engine:** Unity 2022.3.30f1 (or latest LTS)
- **UI:** TextMeshPro, Unity UI Toolkit
- **Scripts:** C# MonoBehaviour-based structure
- **Platform:** Windows

---

## 🧰 How to Run Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/triptisingh22a/3D-CARGO-LOADER-IITB-unity.git
2. Open in Unity Hub (target version: Unity 2022.3.x)

3.Open the MainLoaderScene from the Scenes/ folder.

4. Hit Play and interact using UI + mouse controls.
