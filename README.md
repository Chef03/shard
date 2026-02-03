# Shard - Game Engine Architecture

## Some Notes
The repo has a basic .gitignore, everyone has different dev environments setup on different machines. Although a proper Visual Studio .gitignore might be worthwhile if everyone uses visual studio, not everyone will. Hence the basic and simplistic .gitignore.

## ChangeLog

=-=-=-=-=-=-=-=

If you want to see the specifics of how code files have been changed, I recommend 
using this: https://www.diffchecker.com/diff

You can grab specific files and compare them against older files.  Or you can do it 
on a more comprehensive basis through a whole pile of diff tools.

We don't use GitHub for this because of the 'bug bounty' that's part of the assessment.  
I want people to have a chance to fix bugs within 'release windows' so there's less
time pressure.  

You don't have to use any specific version of Shard for your assessment (or any version of 
Shard at all really) so pick the release that causes you the least problems.  I can't 
exhaustively test any of these releases other than trying stuff out and saying 'Yep, 
seems fine'.  

The rule in maintenance is that for every two bugs that get fixed, a new 
one gets introduced.   Upgrading to a new version is not necessarily the best idea 
mid-development unless it solves a problem you're otherwise having.  

********************************
### 01/19/2026 - 1.4.0 Dandelion
Game Engine Architecture 2026

Ch..ch...ch...ch...changes
--------------------------
* 

### 01/08/2025 - 1.3.0  Horizons

Game Engine Architecture 2025

Ch..ch...ch...ch...changes
--------------------------
* Adjusted path to one less parent for the envar.cfg file in Bootstrap.cs
* Changed line 199 in DisplayText.cs from TTF_RenderText_Blended to TTF_RenderText_Solid to resolve engine crash on game over font loaded. (Bug fix by Kyle Agius)
* Moved input.getInput() from line 288 in Bootstrap.cs to line 285. Moving the handling of input outside the gameloop while statement. (Bug fix by Erik Tran Simonsson)
* Added full sprite path using Bootstrap.getAssetManager().getAssetPath() to line 37 in Invader.cs. Fixes image loading error. (Bug fix by Biting Lin) 
* Added a fail safe in OnColissionEnter to line 67 in Bullet.cs. Stops bullets from initiating destroy events for multiple objects in the scene. (Bug fix by Lucas Lonegro Gurfinkel)
* Changed Transform.X to Transform.Y on line 95 in Bullet.cs to fix the toString bug of printing the X value twice. (Bug fix by Osama Ateeq)
* Added two variables for remembering the offset of the hitbox. This fixes the overwritten offsets in the original ColliderRect.cs class. Lines changed are 22, 38, 39, 80, 81. (Bug fix by Ida Altenstedt)
* Added control statements to only shoot if invaders are in the array and end the game once all invaders are gone on lines 32, 50 - 57 and 112 - 116 in GameSpaceInvaders.cs. Fixes game crash on trying to shoot with no invaders available. (Bug fix by Aristotelis Anthopoulos)
* Changed renderCircle in DisplaySDL.cs to render with an array using DrawPoints call instead of using multiple DrawPoint calls, lines 146 - 177. Gives a performance boost to the engine. (Bug fix by Aristotelis Anthopoulos)
* Added missing call AddtoDraw for the background in GameTest.cs line 21. Fixes bug of forgetting to draw the background to the screen. (Bug fix by Lisa te Braak)

### 01/02/2024 - 1.2.0 - Shake it Off

Game Engine Architecture 2024

Ch..ch...ch...ch...changes
--------------------------

* Retargeted to .NET SDK 8.0 from .NET SDK 5.0 which is now fully deprecated
** There are some side effects to this - it now uses an SDL nuget package rather than DLLs in the 	directory, it's now a 64 bit application, 
some of the paths have changed a bit, and there are a few tweaks here and there in the code to make it all work properly.  
Those not running a 64 bit platform should use the 1.1 version and install Net 5.0 for minimal disruption.
* Added a CultureInfo.InvariantCulture to line 122 of the PhysicsManager to resolve the problem of Swedish number referencing (1,2) versus other contexts (1.2)  (Joel Hilmersson / Timothy Nilsson / Axel Söderberg)
*  Lines 312 and 313 in PhysicsManager seem to be doing an impossible cast, so this has been changed to reflect the proper intention.  A corresponding property has been added to PhysicsBody for Colh.  (Faton Hoti)
* The first call to loadTexture(string) for a particular path would return the wrong IntPtr, and now returns the right one.  
	Probably.  (Faton Hoti) 


### 11/01/2023 - 1.1.0 - Just Like Starting Over

The starting point for Game Engine Architecture 2023.  

Ch..ch...ch...ch...changes
--------------------------
* Some tidying up of code, and enforcement of code simplicity.  (Michael James Heron)
* A new rudimentary asset management system has been added.  Assets can be directly referenced as before, but now you can also
	ask the asset manager to recursively index a particular directory (defined in envar.cfg) and access resources by their 
	filename, rather than path.   However, this currently works on the assumption of a Windows environment and specifically 
	the directory structure of Visual Studio.  Those using 'off script' environments may need to fiddle with this a bit. 
	(Michael James Heron)
* Config files are now broken into two core types - envar.cfg and config.cfg  Envar.cfg is processed first, and can be used to 
	set variables to be used for other files in the system.  Config.cfg is loaded second and sets which components are used 
	for the current session.  These now live in the base project directory rather than the /bin/ directory for the debug 
	compilation. (Michael James Heron)
* onCollisionExit is now properly called when an object is set to be destroyed. (Michael James Heron)
* A bug with gravity that forces objects to fall through other objects has been fixed. (Michael James Heron)
* A rudimentary sound implementation has been added in SoundBeep, and the base structural class Sound now has
	an abstract method that must be implemented.  This integrates with the aset management system, 
	so all a file must do is pass in the filename without the path, such as:
	Bootstrap.getSound().playSound ("fire.wav"); (Michael James Heron)
* Gravity direction and its strength can now be set in the Envar.cfg file.  (Michael James Heron)
* Drawing colliders is now handled via the Envar.cfg file.  (Michael James Heron)
* You can now get the frame rate for the last second by using getSecondFPS().

### 24/2/2022 - 1.0.2 - Central Station

A couple of fixes related to the centre of objects in this version.  

Ch..ch...ch...ch...changes
--------------------------

* The centre of transforms didn't take into account scaling.  It now does, within recalculateCentre 
	in Transform.cs (Alexander Larsson Vahlberg)
* Colliders by default rotate around their offsets, but sometimes you don't want that to happen - for example, 
	if you just want to fine-tune the position of a collider around a spride.  A new property has been added
	in the colliders, RotateAtOffset that can be set to false if you'd rather they treated their
	offsets as their centre.  See lines 40-43 in Asteroid.cs for an example of it in use, but the 
	basic system is that you get the reference to the collider when you add it via addRectCollider or 
	addCircleCollider and then you use that reference to adjust the RotateAtOffset property to what 
	you want.  It's easier to see what I mean than it is to explain, so if you're interested in what 
	this does play around with lines 42 and 43 in Asteroid.cs to see what happens when the property
	is toggled.  (Michael James Heron)
	
********************************
### 4/2/2022 - 1.0.1 - Mårten's Opus

Since this release contains a high proportion of fixes from Mårten Åsberg, 
it's named in his honour.  This is what immortality feels like - drink deeply 
from the keg of glory.

Changes
-------

* Manic Miner - code for handling falling fixed so that it properly works when a collision is 
	from below rather than from above (Michael James Heron)
* Missile Command - Time between missile firings made a consistent 0.5f so that 
	difficulty scales with FPS (Michael James Heron)
* getTargetFrameRate in Game.cs changed to be virtual so it can be overridden. (MJH)
* GameTest - Listeners were not being properly de-registered when an object was destroyed.  
	That's fixed - killMe in GameObject made virtual and asteroids de-register themselves in an 
	overridden method.  (Alexander Larsson Vahlberg)
* The Vector2 issue that plagued me in development has been addressed, and the standard System.Numerics 
	Vector2 class is once again in play.  That has implications across the whole system as the 
	conventions are different. (Mårten Åsberg)
* onCollisionStay was called twice per physics tick, and now it isn't (Mårten Åsberg)
* Various inconsistencies with angular drag and drag have been resolved.  (Mårten Åsberg)
* Collision exiting and entering while within a collider was applied inconsistently, and
	that's been fixed.  (Mårten Åsberg)
* There was a slow memory leak somewhere in 1.0.0 - the engine would take up 42MB or so and 
	then very gently increase.   That seems to have been fixed, and it wasn't through anything 
	I did and it's not related to listeners (as per Alexander's fix).  So I'm going to say Mårten 
	fixed this one too. (Mårten Åsberg)

***************************************************
### 21/1/2022 - 1.0.0 - Something Wicked This Way Comes

This is the starting point of Shard 2022, the teaching game engine used for Game Engine 
Architecture.

