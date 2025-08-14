# Project Name
Short description of your Unity project.  
Example: "An interactive Unity-based training simulation with SCORM/xAPI integration."

---

## 📦 Build Instructions
1. Install [Unity Hub](https://unity.com/download) and Unity version **2022.3.38 LTS** (or the version listed in `ProjectSettings/ProjectVersion.txt`).
2. Clone this repository or extract the `.zip` source files.
3. Open the project in Unity Hub.
4. Go to **File → Build Settings**.
5. For **Desktop Build**:
   - Select **PC, Mac & Linux Standalone**.
   - Target platform: **Windows x86_64**.
   - Click **Build** and choose an output folder.
6. For **WebGL Build**:
   - Select **WebGL** in Build Settings.
   - Click **Build** and upload to hosting service (Itch.io, GitHub Pages, Netlify).

---

## 🎯 SCORM/xAPI Implementation Summary
- Implemented **XAPI** for LMS compatibility using Unity API wrapper.
- Configured LMS endpoint:
  - URL: `https://cloud.scorm.com/lrs/B0Z9LAVY2B/sandbox/`
  - Credentials stored securely in script.
- Sends SCORM `cmi.core.lesson_status` (`completed` / `incomplete`) and `cmi.core.score.raw`.
- Added **xAPI** tracking for:
  - Verb: `experienced`
  - Verb: `completed`
  - Verb: `answered`
- xAPI statements include:
  - `actor` (user ID, name)
  - `verb` (action taken)
  - `object` (activity/module ID)
  - `result` (score, success status)

---

## 🖥 Desktop Build
- Download: Windows Build (Game Exe File) 
  *(Extract and run `Warehouse Safety Training Simulation Unity.exe`)*

---

## 🔄 Git Workflow & Collaboration
- **Repository Host**: GitHub
- **Branching Strategy**:
  - `master` – stable release-ready code
  - `Dev_Feature` – integration branch for new features
- **Workflow**:
  - Create feature branch
  - Commit and push changes
  - Open Pull Request to `Dev_Feature`
  - Code review before merging to `master`
- **Tools**:
  - Git LFS enabled for large assets (audio/video/textures)

---

## ⚠ Known Issues / Limitations
- WebGL version may have reduced texture quality due to compression.
- Audio latency observed on some browsers in WebGL.
- Desktop build tested only on **Windows 10/11**.
- SCORM/xAPI reporting requires active internet connection and LMS credentials.

---

## 📄 License
Specify your license (e.g., MIT, Apache 2.0, Proprietary).

---
