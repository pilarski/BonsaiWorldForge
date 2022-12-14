# Bonsai World Forge
### Pre-Alpha Demo

>"If you build it, ~~they will come~~ it might break and I'm sorry and please use at your own discretion." (_Field of Dreams_, 1989, probably)

### Summary
**_A flexible world-building game for human-agent machine intelligence research._**

>"A fresh new game for one player where you harness the power of an ancient artifact to smith worlds in miniature out of base materials... only to see your handiwork play out over entire continents as they spring to life. In short play sessions with new, procedurally generated terrains, you will use an increasingly diverse range of tools to shape the world into whatever you want it to be: arid desert, lush forests, or rolling plains. Your artifact learns with you. As you play, and overcome the challenges of the worlds you create, you will also gain valuable resources needed to unlock the full potential of your intelligent world-sculpting tool."

**Key features:**

* More than 15 different world smithing and movement tools;
* A range of visual assets you can use to personalize the worlds you create;
* Increasing challenge levels (and unlockable side projects) as you populate your worlds (not yet implemented);
* Temporarily unlock the machine learned power of the tool to streamline your actions (not yet implemented);
* Default implementations of continual reinforcement learning and machine learning methods, such as <a href="https://sites.ualberta.ca/~pilarski/docs/theses/Edwards_Ann_L_201605_MSc.pdf">adaptive and autonomous switching</a> (not yet implemented).

<img width="1128" alt="BWF-Screencapture-NewAssets" src="https://user-images.githubusercontent.com/1139429/187958905-a5a8217a-607a-4043-842b-777a9587a471.png">

### Setup and Use

**Steps:**
1. Install <a href="https://unity.com/">Unity 2020 LTS and Unity Hub</a>;
2. Clone the BonsaiWorldForge repository main branch to your local disk;
3. Within the project's "Assets/" directory, create a new directory named "3rdParty"
4. In the project's "Assets/3rdParty/" directory, clone Unity's NavMeshComponents (https://github.com/Unity-Technologies/NavMeshComponents); from that repo's assets folder, only include NavMeshComponents and Gizmos, and inside NavMeshComponents delete the folder "Editor". For Unity versions later than 2020.3 LTS, this step might not be required as scripts may be included in future releases of Unity's "AI Navigation" package.
5. Import project into Unity Hub via "Open -> Add project from disk" and select location of cloned repository; note, this project is currently being developed in Unity 2020.3.26f1, using the Universal Render Pipeline (URP)
6. It is likely the NavMeshSurface component script will be have been removed from the Terrain gameobject during the import. Please add it back in, with the parameters listed in the following image:

<img width="1723" alt="navmeshsurface-details" src="https://user-images.githubusercontent.com/1139429/193423227-ff8f7099-6835-4643-817e-eca8cfda3efa.png">


**_Please note, for VR deployment_**: this project requires SteamVR and selected XR/VR package sets for Unity, and is currently being built for the Vive Focus 3, though it shouuld remain largely agnositc to VR platform; some work may be required to re-build the VR rigging approach when deploying the project on a new machine or with new hardware. Suggested steps include:
1. Switch to "vr" branch;
2. In package manager, install XR Plugin Management and dependencies; 
3. In package manager, install OpenXR Plugin;
4. Platform specific: Install VIVE Input Utility (and its scoped registry if needed, and then possibly Wave Essence scripts for the VIVE Focus 3); 
5. In "Project settings -> Player" switch Active Input Handling field to list "Input System Package";
6. In "Project settings -> XR" make sure to select the platforms you wish to support.
7. Platform specific: Modify the buttons in "VRInteraction.cs" to match those desired for your controller.

### Acknowledgements

This work has been supported in part by the <a href="http://amii.ca">Alberta Machine Intelligence Institute (Amii)</a> and the Canada CIFAR AI Chairs Program, part of the Pan-Canadian AI Strategy. Thanks is also due to the excellent ongoing comments, feedback, and ideas from colleagues, and especially all the members of <a href="http://blincdev.ca">BLINCdev</a> and the <a href="http://blinclab.ca">Bionic Limbs for Improved Natural Control (BLINC) Lab</a>.
