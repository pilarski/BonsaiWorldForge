# Bonsai World Forge
### Pre-Alpha Demo
"If you build it, [it might break and I'm sorry but please use at your own discretion]." (Field of Dreams, 1989, probably)

### Summary
A flexible world-building game for human-agent machine intelligence research.

"A fresh new game for one player where you harness the power of an ancient artifact to smith worlds in miniature out of base materials... only to see your handiwork play out over entire continents as they spring to life. In short play sessions with new, procedurally generated terrains, you will use an increasingly diverse range of tools to shape the world into whatever you want it to be: arid desert, lush forests, or rolling plains. Your artifact learns with you. As you play, and overcome the challenges of the worlds you create, you will also gain valuable resources needed to unlock the full potential of your intelligent world-sculpting tool."

Key features:

* More than 15 different world smithing and movement tools;
* A range of visual assets you can use to personalize the worlds you create;
* Increasing challenge levels (and unlockable side projects) as you populate your worlds (not yet implemented);
* Temporarily unlock the machine learned power of the tool to streamline your actions (not yet implemented);

<img width="1128" alt="BWF-Screencapture-NewAssets" src="https://user-images.githubusercontent.com/1139429/187958905-a5a8217a-607a-4043-842b-777a9587a471.png">

### Setup and Use

Steps:
* Install <a href="https://unity.com/">Unity3D</a> 2020 LTS and Unity Hub;
* Clone this repository to your local disk;
* Import project into Unity Hub via "Open -> Add project from disk" and select location of cloned repository;
  * n.b., this project is currently being developed in Unity 2020.3.26f1, using the Universal Render Pipeline (URP)
* Within the "Assets" directory, create a new directory named "3rdParty"
  * (Note about NavMesh github clone goes here)

Please note, for VR deployment: this project requires SteamVR or another VR package set for Unity; specific SteamVR settings and files have been excluded from this repository, so some work may be required to re-insert the VR rigging approach when deploying the project on a new machine or with new hardware.
