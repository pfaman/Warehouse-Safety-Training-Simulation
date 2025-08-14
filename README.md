Project Name
Short description of your Unity project.
Example: "An interactive Unity-based training simulation with SCORM/xAPI integration."

📦 Build Instructions
Install Unity Hub and Unity version 2022.3.38 LTS (or the version listed in ProjectSettings/ProjectVersion.txt).
Clone this repository or extract the .zip source files.
Open the project in Unity Hub.
Go to File → Build Settings.
For Desktop Build:
Select PC, Mac & Linux Standalone.
Target platform: Windows x86_64.
Click Build and choose an output folder.
For WebGL Build:
Select WebGL in Build Settings.
Click Build and upload to hosting service (Itch.io, GitHub Pages, Netlify).
🎯 SCORM/xAPI Implementation Summary
Implemented xAPI (Tin Can API) in Unity for LMS compatibility using a Unity xAPI API wrapper. Configured Learning Record Store (LRS) endpoint: URL: https://cloud.scorm.com/lrs/B0Z9LAVY2B/sandbox/ Authentication: LRS credentials securely stored in Unity scripts (not exposed in build). The system sends xAPI statements to record learner interactions and progress. Tracked verbs: experienced – when the learner launches or views the module. completed – when the learner finishes the activity or training module. answered – when the learner responds to a question or assessment.

Each xAPI statement contains:

Actor – learner’s unique ID and display name. Verb – the action taken by the learner. Object – the activity, lesson, or module being tracked. Result – includes score, success/failure status, and completion state.

The implementation ensures:

Real-time tracking of learner activity. Data can be viewed and analysed in the SCORM Cloud LRS dashboard. Works in both WebGL and desktop builds when internet access is available.

🖥 Desktop Build
Download: Windows Build (Game Exe File.zip) (Extract and run Warehouse Safety Training Simulation Unity.exe)
🔄 Git Workflow & Collaboration
Repository Host: GitHub
Branching Strategy:
master – stable release-ready code
Dev_Feature – integration branch for new features
Workflow:
Create feature branch
Commit and push changes
Open Pull Request to Dev_Feature
Code review before merging to master
Tools:
Git LFS enabled for large assets (audio/video/textures)
⚠ Known Issues / Limitations
WebGL version may have reduced texture quality due to compression.
Audio latency observed on some browsers in WebGL.
Desktop build tested only on Windows 10/11.
SCORM/xAPI reporting requires active internet connection and LMS credentials.
📄 License
Specify your license (e.g., MIT, Apache 2.0, Proprietary).
